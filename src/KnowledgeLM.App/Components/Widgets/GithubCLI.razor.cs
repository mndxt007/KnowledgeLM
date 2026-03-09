using GitHub.Copilot.SDK;
using KnowledgeLM.App.Helpers;
using KnowledgeLM.App.Models;
using KnowledgeLM.App.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Microsoft.SemanticKernel;
using Markdig;
using Microsoft.AspNetCore.Components.Web;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using FluentColor = Microsoft.FluentUI.AspNetCore.Components.Color;

namespace KnowledgeLM.App.Components.Widgets
{
    partial class GithubCLI : IAsyncDisposable
    {
        [Inject] 
        IJSRuntime JS { get; set; } = default!;
        [Inject]
        private NotesCRUDService CrudService { get; set; } = default!;
        [Inject]
        private StateService StateService { get; set; } = default!;
        [Inject]
        private Kernel Kernel { get; set; } = default!;
        private bool HasNoCopilotMesssages => cardText.Count == 0;

        private CopilotClient? ghcpClient;
        private CopilotSession? session;
        private BlockingCollection<CoPilotMessages> cardText = new(new ConcurrentQueue<CoPilotMessages>());
        private string queryText = string.Empty;
        private bool SendingMessage = false;
        [Inject]
        private ILogger<CopilotClient> CopilotLogger { get; set; } = default!;
        private FluentColor SessionStatus { get; set; } = FluentColor.Warning;

        private bool AgentWorking { get; set; } = false;

        protected override void OnInitialized()
        {
            ghcpClient = new CopilotClient(new()
            {
                UseLoggedInUser = true,
                //CliPath = "C:\\Users\\manojdixit\\AppData\\Local\\Microsoft\\WinGet\\Links\\copilot.exe",
                Logger = CopilotLogger,
                //LogLevel = "debug",
                //CliArgs = ["--log-dir", "D:\\LearnLoom\\Logs"]

            });

            StateService.OnRefreshRequested += OnRefreshRequested;
        }

        private async void OnRefreshRequested()
        {
            await InvokeAsync(StateHasChanged);
        }

        public async ValueTask DisposeAsync()
        {
            StateService.OnRefreshRequested -= OnRefreshRequested;
            await ValueTask.CompletedTask;
        }

        private IList<AIFunction> GetAIFunctions()
        {
            //convert from Kernel to AIFunction
            List<AIFunction> aiFunctions = new List<AIFunction>();

            foreach (var plugin in Kernel.Plugins)
            {
                foreach (var function in plugin)
                {
                    aiFunctions.Add(function);
                }

            }

            var getNotesFolderTool = AIFunctionFactory.Create(
                () => AgentPrompts.NotesFolder,
                name: "get_notes_folder",
                description: "Returns the absolute folder path on disk where all resources (courses, sections, notes, images) must be stored. Call this before creating any files or folders.");

            aiFunctions.Add(getNotesFolderTool);

            return aiFunctions;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await CreateNewSession();
            }

            await JS.InvokeVoidAsync("scrollToBottom", "AgentMessages");
        }

        private async Task CreateNewSession()
        {
            if(ghcpClient is not null)
            {
                session = await ghcpClient.CreateSessionAsync(new SessionConfig
                {
                    Tools = GetAIFunctions(),
                    OnPermissionRequest = PermissionHandler.ApproveAll,

                    SystemMessage = new()
                    {
                        Content = AgentPrompts.SystemPrompt,
                        Mode = SystemMessageMode.Replace,

                    },
                    McpServers = new Dictionary<string, object>
                    {
                        ["playwright"] = new McpLocalServerConfig
                        {
                            Type = "local",
                            Command = "npx",
                            Args = [
                               "@playwright/mcp@latest",
                                "--browser",
                                "msedge",
                                //"--user-data-dir",
                               //Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "User Data")
                               ],
                            Tools = new List<string> { "*" },
                            //Env = new Dictionary<string, string>
                            //{
                            //    ["NODE_DEBUG"] = "mcp",
                            //    ["DEBUG"] = "*",
                            //    ["MCP_DEBUG"] = "1",

                            //},

                        }
                    },
                });
                // Session already started by the time CreateSessionAsync returns,
                // so SessionStartEvent may have fired before .On() subscription.
                SessionStatus = FluentColor.Success;
                await InvokeAsync(StateHasChanged);

                session.On(async evt =>
                {
                    switch (evt)
                    {
                        case AssistantMessageEvent assistantMessageEvent:
                            await InvokeAsync(() =>
                            {
                                cardText.Add(new()
                                {
                                    EventType = evt.Type,
                                    Message = assistantMessageEvent.Data.Content ?? string.Empty,
                                    ReasoningText = assistantMessageEvent.Data.ReasoningText ?? string.Empty
                                });
                                StateHasChanged();
                            });
                            break;
                        case UserMessageEvent userMessageEvent:
                            await InvokeAsync(() =>
                            {
                                cardText.Add(new()
                                {
                                    EventType = evt.Type,
                                    Message = userMessageEvent.Data.Content
                                });
                                StateHasChanged();
                            });
                            break;
                        case ToolExecutionStartEvent toolExecutionCompleteEvent:
                            await InvokeAsync(() =>
                            {
                                cardText.Add(new()
                                {
                                    EventType = evt.Type,
                                    Message = toolExecutionCompleteEvent.Data.ToolName
                                });
                                StateHasChanged();
                            });
                            break;
                        case SessionStartEvent:
                        case SessionResumeEvent:
                            await InvokeAsync(() =>
                            {
                                SessionStatus = FluentColor.Success;
                                StateHasChanged();
                            });
                            break;
                        case SessionShutdownEvent:
                        case SessionErrorEvent:
                            await InvokeAsync(() =>
                            {
                                SessionStatus = FluentColor.Error;
                                StateHasChanged();
                            });
                            break;
                        case AssistantTurnStartEvent:
                            CopilotLogger.LogInformation("Assistant Started Working");
                            await InvokeAsync(() =>
                            {
                                AgentWorking = true;
                                StateHasChanged();
                            });
                            break;
                        case AssistantTurnEndEvent:
                        case AbortEvent:
                            CopilotLogger.LogInformation("Assistant Turn ended");
                            await InvokeAsync(() =>
                            {
                                AgentWorking = false;
                                StateHasChanged();
                            });
                            break;
                    }
                });
            }
            
        }

        private async Task HandleQuery(string query)
        {
            if(session!=null)
            {
                SendingMessage = true;
                await InvokeAsync(StateHasChanged);
                //await session.SendAndWaitAsync(new MessageOptions { Prompt = query });
                await session.SendAsync(
                    new MessageOptions 
                    { 
                        Prompt = query,
                        Mode = "immediate"
                    }
                    );
                SendingMessage = false;
                await InvokeAsync(StateHasChanged);
                queryText = String.Empty;
            }
            
        }

        private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

        private async Task HandleKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && !e.ShiftKey && !string.IsNullOrWhiteSpace(queryText))
            {
                await HandleQuery(queryText);
            }
        }

        private string RenderMarkdown(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return string.Empty;
            return Markdown.ToHtml(markdown, MarkdownPipeline);
        }



        private void NewSession(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
        {
            cardText = new(new ConcurrentQueue<CoPilotMessages>());
            _ = CreateNewSession();
        }
        private async Task AbortSession(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
        {
            if(session is not null)
            {
                await session.AbortAsync();
            }
        }
    }

    
}
