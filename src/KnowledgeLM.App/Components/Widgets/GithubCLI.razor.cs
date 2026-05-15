using GitHub.Copilot.SDK;
using KnowledgeLM.App.Helpers;
using KnowledgeLM.App.Models;
using KnowledgeLM.App.Services;
using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using FluentColor = Microsoft.FluentUI.AspNetCore.Components.Color;

namespace KnowledgeLM.App.Components.Widgets
{
    partial class GithubCLI : IAsyncDisposable
    {
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private NotesCRUDService CrudService { get; set; } = default!;
        [Inject] private StateService StateService { get; set; } = default!;
        [Inject] private CopilotService CopilotService { get; set; } = default!;
        [Inject] private AppNotificationService Notifications { get; set; } = default!;
        [Inject] private ILogger<GithubCLI> Logger { get; set; } = default!;

        private ICopilotSession? _session;
        private string queryText = string.Empty;
        private bool SendingMessage = false;

        private IEnumerable<CoPilotMessages> cardText => _session?.Messages ?? Enumerable.Empty<CoPilotMessages>();
        private bool HasNoMessages => _session is null || !_session.HasMessages;
        private bool AgentWorking => _session?.IsWorking ?? false;
        private FluentColor SessionStatus => _session?.Status switch
        {
            CopilotSessionStatus.Active => FluentColor.Success,
            CopilotSessionStatus.Error => FluentColor.Error,
            _ => FluentColor.Warning,
        };

        protected override void OnInitialized()
        {
            StateService.OnRefreshRequested += OnRefreshRequested;
        }

        private async void OnRefreshRequested()
        {
            await InvokeAsync(StateHasChanged);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await InitializeSession();
            }

            await JS.InvokeVoidAsync("scrollToBottom", "AgentMessages");
        }

        private async Task InitializeSession()
        {
            var mcpServers = new Dictionary<string, object>
            {
                ["playwright"] = new McpLocalServerConfig
                {
                    Type = "local",
                    Command = "npx",
                    Args = [
                        "@playwright/mcp@latest",
                        "--browser",
                        "msedge",
                    ],
                    Tools = new List<string> { "*" },
                }
            };

            _session = await CopilotService.GetOrCreateSessionAsync(
                CopilotPaneKey.Notes,
                AgentStatics.NotesPaneSystemPrompt,
                mcpServers);

            _session.StateChanged += OnSessionStateChanged;
            await InvokeAsync(StateHasChanged);
        }

        private void OnSessionStateChanged()
        {
            _ = InvokeAsync(StateHasChanged);
        }

        private async Task HandleQuery(string query)
        {
            if (_session is null || string.IsNullOrWhiteSpace(query)) return;

            SendingMessage = true;
            await InvokeAsync(StateHasChanged);

            try
            {
                await _session.SendAsync(query);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Notes SendAsync failed; restarting session");
                Notifications.NotifyError($"Copilot error: {ex.Message}. Starting a new session…");

                try
                {
                    await _session.RestartAsync();
                    Notifications.NotifyInfo("A new Copilot session has been started.");
                }
                catch (Exception restartEx)
                {
                    Logger.LogError(restartEx, "Failed to restart Notes session after send failure");
                    Notifications.NotifyError($"Could not restart Copilot session: {restartEx.Message}");
                }
            }
            finally
            {
                SendingMessage = false;
                queryText = string.Empty;
                await InvokeAsync(StateHasChanged);
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

        private async Task NewSession(MouseEventArgs args)
        {
            if (_session is null) return;
            await _session.RestartAsync();
        }

        private async Task AbortSession(MouseEventArgs args)
        {
            if (_session is null) return;
            await _session.AbortAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (_session is not null)
            {
                _session.StateChanged -= OnSessionStateChanged;
            }

            StateService.OnRefreshRequested -= OnRefreshRequested;
            await ValueTask.CompletedTask;
        }
    }
}
