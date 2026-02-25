using GitHub.Copilot.SDK;
using KnowledgeLM.App.Helpers;
using KnowledgeLM.App.Models;
using KnowledgeLM.App.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.AI;
using FluentColor = Microsoft.FluentUI.AspNetCore.Components.Color;

namespace KnowledgeLM.App.Components.Widgets
{
    partial class GithubCLI
    {
        [Inject]
        private NotesCRUDService CrudService { get; set; } = default!;

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
                switch(evt)
                {
                    case AssistantMessageEvent assistantMessageEvent:
                        cardText.Add(new()
                        {
                            EventType = evt.Type,
                            Message = assistantMessageEvent.Data.Content
                        });
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
                }
            });

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
        
    }

    
}
