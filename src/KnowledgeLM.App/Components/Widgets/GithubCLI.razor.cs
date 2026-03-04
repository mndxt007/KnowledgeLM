using GitHub.Copilot.SDK;
using KnowledgeLM.App.Helpers;
using KnowledgeLM.App.Models;
using KnowledgeLM.App.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.AI;
using Microsoft.JSInterop;
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

        private readonly CopilotClient ghcpClient;
        private CopilotSession? session;
        private List<CoPilotMessages> cardText = [];
        private string queryText = string.Empty;
        private bool agentWorking = false;

        public GithubCLI()
        {
            ghcpClient = new CopilotClient(new()
            {
                 UseLoggedInUser = true,
                 CliPath = "C:\\Users\\manojdixit\\AppData\\Local\\Microsoft\\WinGet\\Links\\copilot.exe"
            });
        }

        protected override void OnInitialized()
        {
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
            return [
                AIFunctionFactory.Create(
                    CrudService.CreateCourseAsync,
                    "create_course",
                    "Create a new course with a name, description, and location"),
                AIFunctionFactory.Create(
                    CrudService.CreateSectionAsync,
                    "create_section",
                    "Create a new section within a course, requires courseId, name, description, and location"),
                AIFunctionFactory.Create(
                    CrudService.CreateResourceAsync,
                    "create_resource",
                    "Create a new resource within a section, requires sectionId, name, location, and resourceType (Image, Video, or Other)"),
            ];
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await JS.InvokeVoidAsync("scrollToBottom", "AgentMessages");
            if (firstRender)
            {
                session = await ghcpClient.CreateSessionAsync(new SessionConfig
                {
                    Tools = GetAIFunctions(),
                    SystemMessage = new()
                    {
                        Content = AgentPrompts.SystemPrompt,
                        Mode = SystemMessageMode.Append
                    }
                });
                session.On(async evt =>
                {
                    switch (evt)
                    {
                        case AssistantMessageEvent assistantMessageEvent:
                            string message = String.Concat(assistantMessageEvent.Data.ReasoningText, assistantMessageEvent.Data.Content);
                            cardText.Add(new()
                            {
                                EventType = evt.Type,
                                Message = message
                            });
                            //StateService.RequestRefresh();
                            await InvokeAsync(StateHasChanged);
                            break;
                        case UserMessageEvent userMessageEvent:
                            cardText.Add(new()
                            {
                                EventType = evt.Type,
                                Message = userMessageEvent.Data.Content
                            });
                            await InvokeAsync(StateHasChanged);
                            break;
                         case ToolExecutionStartEvent toolExecutionCompleteEvent:
                            cardText.Add(new()
                            {
                                EventType = evt.Type,
                                Message = toolExecutionCompleteEvent.Data.ToolName
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
                agentWorking = true;
                await InvokeAsync(StateHasChanged);
                //await session.SendAndWaitAsync(new MessageOptions { Prompt = query });
                await session.SendAsync(new MessageOptions { Prompt = query });
                agentWorking = false;
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

    }

    
}
