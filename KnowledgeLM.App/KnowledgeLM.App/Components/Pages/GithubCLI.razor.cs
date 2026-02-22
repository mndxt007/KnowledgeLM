
using GitHub.Copilot.SDK;

namespace KnowledgeLM.App.Components.Pages
{
    partial class GithubCLI
    {
        private readonly CopilotClient ghcpClient;
        private CopilotSession? session;
        private List<String> cardText = [];
        private string queryText = string.Empty;

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
                if (evt is AssistantMessageEvent msg)
                {
                    cardText.Add(msg.Data.Content);
                    await InvokeAsync(StateHasChanged);
                }
            });

        }
        private async Task HandleQuery(string query)
        {
            if(session!=null)
            {
                await session.SendAsync(new MessageOptions { Prompt = query });
            }
            
        }

        
    }

    
}
