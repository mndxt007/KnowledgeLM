
using GitHub.Copilot.SDK;
using KnowledgeLM.App.Models;
using FluentColor = Microsoft.FluentUI.AspNetCore.Components.Color;

namespace KnowledgeLM.App.Components.Widgets
{
    partial class GithubCLI
    {
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
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            session = await ghcpClient.CreateSessionAsync(new SessionConfig
            {
                 //To do
                  
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
                await session.SendAndWaitAsync(new MessageOptions { Prompt = query });
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
