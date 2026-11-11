using GitHub.Copilot;
using KnowledgeLM.App.Helpers;
using KnowledgeLM.App.Models;
using KnowledgeLM.App.Models.AI;
using KnowledgeLM.App.Services;
using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using FluentColor = Microsoft.FluentUI.AspNetCore.Components.Color;

namespace KnowledgeLM.App.Components.Widgets;

public partial class HarvestChatPane : IDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private CopilotService CopilotService { get; set; } = default!;
    [Inject] private HarvestStateService HarvestState { get; set; } = default!;
    [Inject] private PodcastService PodcastService { get; set; } = default!;
    [Inject] private FlashcardService FlashcardService { get; set; } = default!;
    [Inject] private QuizService QuizService { get; set; } = default!;
    [Inject] private AppNotificationService Notifications { get; set; } = default!;
    [Inject] private ILogger<HarvestChatPane> Logger { get; set; } = default!;

    private ICopilotSession? _session;
    private string queryText = string.Empty;
    private bool SendingMessage = false;

    private IEnumerable<CoPilotMessages> messages => _session?.Messages ?? Enumerable.Empty<CoPilotMessages>();
    private bool HasNoMessages => _session is null || !_session.HasMessages;
    private bool AgentWorking => _session?.IsWorking ?? false;
    private FluentColor SessionStatus => _session?.Status switch
    {
        CopilotSessionStatus.Active => FluentColor.Success,
        CopilotSessionStatus.Error => FluentColor.Error,
        _ => FluentColor.Warning,
    };

    private Podcast? activePodcast;
    private bool showPodcastPlayer = false;
    private List<Flashcard>? activeFlashcards;
    private bool showFlashcardViewer = false;
    private List<Quiz>? activeQuizzes;
    private bool showQuizViewer = false;
    private bool loadingContent = false;

    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    protected override void OnInitialized()
    {
        HarvestState.OnScopeChanged += OnScopeChanged;
        HarvestState.OnContentAvailabilityChanged += OnContentAvailabilityChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await InitializeSession();
            await RefreshContentAvailability();
        }
        await JS.InvokeVoidAsync("scrollToBottom", "HarvestMessages");
    }

    private async Task InitializeSession()
    {
        _session = await CopilotService.GetOrCreateSessionAsync(
            CopilotPaneKey.Harvest,
            AgentStatics.HarvestPaneSystemPrompt);

        _session.StateChanged += OnSessionStateChanged;
        _session.OnEvent += HandleSessionEvent;

        await InvokeAsync(StateHasChanged);
    }

    private void OnSessionStateChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    private async Task HandleSessionEvent(SessionEvent evt)
    {
        // The handle already maintains messages and status; we only need to react to
        // events that drive component-local behaviour (refreshing content availability
        // after an assistant turn ends).
        if (evt is AssistantTurnEndEvent)
        {
            await InvokeAsync(async () =>
            {
                await RefreshContentAvailability();
                StateHasChanged();
            });
        }
    }

    private async Task HandleQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || _session is null) return;

        SendingMessage = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            await _session.SendAsync(query);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Harvest SendAsync failed; restarting session");
            Notifications.NotifyError($"Copilot error: {ex.Message}. Starting a new session…");

            try
            {
                await _session.RestartAsync();
                Notifications.NotifyInfo("A new Copilot session has been started.");
            }
            catch (Exception restartEx)
            {
                Logger.LogError(restartEx, "Failed to restart Harvest session after send failure");
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

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey && !string.IsNullOrWhiteSpace(queryText))
        {
            await HandleQuery(queryText);
        }
    }

    private string RenderMarkdown(string markdown)
    {
        if (string.IsNullOrEmpty(markdown)) return string.Empty;
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

    private void ClearScope()
    {
        HarvestState.ClearScope();
    }

    private async Task RefreshContentAvailability()
    {
        var scope = HarvestState.CurrentScope;
        if (scope is null) return;

        loadingContent = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            var hasFlashcards = await FlashcardService.HasFlashcardsAsync(scope.ResourceId);
            var flashcardCount = hasFlashcards ? (await FlashcardService.GetFlashcardsByResourceIdAsync(scope.ResourceId)).Count : 0;
            var hasPodcast = await PodcastService.HasPodcastAsync(scope.ResourceId);
            var hasQuizzes = await QuizService.HasQuizzesAsync(scope.ResourceId);
            var quizCount = hasQuizzes ? await QuizService.GetQuizCountAsync(scope.ResourceId) : 0;
            HarvestState.UpdateContentAvailability(hasFlashcards, flashcardCount, hasPodcast, hasQuizzes, quizCount);
        }
        finally
        {
            loadingContent = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task RequestGeneration(string type)
    {
        var prompt = type switch
        {
            "flashcards" => AgentStatics.FlashCardPrompt,
            "podcast" => AgentStatics.QuizPrompt,
            "quiz" => AgentStatics.PodcastPrompt,
            _ => string.Empty
        };

        if (!string.IsNullOrEmpty(prompt))
            await HandleQuery(prompt);
    }

    private async Task PlayPodcast()
    {
        var scope = HarvestState.CurrentScope;
        if (scope is null) return;

        var podcast = await PodcastService.GetPodcastByResourceIdAsync(scope.ResourceId);
        if (podcast is not null)
        {
            activePodcast = podcast;
            showPodcastPlayer = true;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void ClosePodcastPlayer()
    {
        showPodcastPlayer = false;
        activePodcast = null;
    }

    private async Task ViewFlashcards()
    {
        var scope = HarvestState.CurrentScope;
        if (scope is null) return;

        var flashcards = await FlashcardService.GetFlashcardsByResourceIdAsync(scope.ResourceId);
        if (flashcards is not null && flashcards.Count > 0)
        {
            activeFlashcards = flashcards;
            showFlashcardViewer = true;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void CloseFlashcardViewer()
    {
        showFlashcardViewer = false;
        activeFlashcards = null;
    }

    private async Task ViewQuiz()
    {
        var scope = HarvestState.CurrentScope;
        if (scope is null) return;

        var quizzes = await QuizService.GetQuizzesByResourceIdAsync(scope.ResourceId);
        if (quizzes is not null && quizzes.Count > 0)
        {
            activeQuizzes = quizzes;
            showQuizViewer = true;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void CloseQuizViewer()
    {
        showQuizViewer = false;
        activeQuizzes = null;
    }

    private async void OnScopeChanged()
    {
        CloseFlashcardViewer();
        CloseQuizViewer();
        ClosePodcastPlayer();
        await InvokeAsync(StateHasChanged);
    }

    private async void OnContentAvailabilityChanged()
    {
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        if (_session is not null)
        {
            _session.StateChanged -= OnSessionStateChanged;
            _session.OnEvent -= HandleSessionEvent;
        }

        HarvestState.OnScopeChanged -= OnScopeChanged;
        HarvestState.OnContentAvailabilityChanged -= OnContentAvailabilityChanged;
    }
}
