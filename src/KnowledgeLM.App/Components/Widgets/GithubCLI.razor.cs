using GitHub.Copilot.SDK;
using KnowledgeLM.App.Helpers;
using KnowledgeLM.App.Models;
using KnowledgeLM.App.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Microsoft.SemanticKernel;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using FluentColor = Microsoft.FluentUI.AspNetCore.Components.Color;

namespace KnowledgeLM.App.Components.Widgets
{
    partial class GithubCLI : IDisposable
    {
        [Inject] 
        IJSRuntime JS { get; set; } = default!;
        [Inject]
        private NotesCRUDService CrudService { get; set; } = default!;
        [Inject]
        private StateService StateService { get; set; } = default!;
        [Inject]
        private Kernel Kernel { get; set; } = default!;
        private bool HasNoCopilotMesssages => cardText is null || cardText.IsEmpty;

        private CopilotClient? ghcpClient;
        private CopilotSession? session;
        private ConcurrentBag<CoPilotMessages> cardText = [];
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

        public void Dispose()
        {
            StateService.OnRefreshRequested -= OnRefreshRequested;
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
            //await JS.InvokeVoidAsync("scrollToBottom", "AgentMessages");
            if (firstRender)
            {
                await CreateNewSession();
            }

            

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
                session.On(async evt =>
                {
                    switch (evt)
                    {
                        case AssistantMessageEvent assistantMessageEvent:
                            string message = String.Concat(assistantMessageEvent.Data.ReasoningText, assistantMessageEvent.Data.Content);
                            await InvokeAsync(() =>
                            {
                                cardText.Add(new()
                                {
                                    EventType = evt.Type,
                                    Message = message
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
                            SessionStatus = FluentColor.Success;
                            await InvokeAsync(StateHasChanged);
                            break;
                        case SessionShutdownEvent:
                        case SessionErrorEvent:
                            SessionStatus = FluentColor.Error;
                            await InvokeAsync(StateHasChanged);
                            break;
                        case AssistantTurnStartEvent:
                            CopilotLogger.LogInformation("Assistant Started Working");
                            AgentWorking = true;
                            await InvokeAsync(StateHasChanged);
                            break;
                        case AssistantTurnEndEvent:
                        case AbortEvent:
                            CopilotLogger.LogInformation("Assistant Turn ended");
                            AgentWorking = false;
                            await InvokeAsync(StateHasChanged);
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

        private FluentColor GetColor(string eventType) =>
        eventType switch
        {
            "user.message" => FluentColor.Accent,
            "assistant.message" => FluentColor.Success,
            _ => FluentColor.Neutral
        };
        private String GetEmoji(string eventType) =>
       eventType switch
       {
           "user.message" => "👤",
           "assistant.message" => "🤖",
           "tool.execution_start" => "🛠️",
           _ => "❓"
       };

        private void NewSession(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
        {
            throw new NotImplementedException();
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
