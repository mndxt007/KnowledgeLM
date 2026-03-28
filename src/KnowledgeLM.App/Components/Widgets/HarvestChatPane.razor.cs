using GitHub.Copilot.SDK;
using KnowledgeLM.App.Helpers;
using KnowledgeLM.App.Models;
using KnowledgeLM.App.Models.AI;
using KnowledgeLM.App.Services;
using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Collections.Concurrent;
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

    private BlockingCollection<CoPilotMessages> messages = new(new ConcurrentQueue<CoPilotMessages>());
    private string queryText = string.Empty;
    private bool SendingMessage = false;
    private FluentColor SessionStatus { get; set; } = FluentColor.Warning;
    private bool AgentWorking { get; set; } = false;
    private bool HasNoMessages => messages.Count == 0;

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
        var systemPrompt = AgentStatics.HarvestPaneSystemPrompt;

        await CopilotService.CreateSessionAsync(systemPrompt, onEvent: HandleSessionEvent);
        // This can be an issue and not real representation of state
        SessionStatus = FluentColor.Success;
        await InvokeAsync(StateHasChanged);
    }

    private async Task HandleSessionEvent(SessionEvent evt)
    {
        switch (evt)
        {
            case AssistantMessageEvent assistantMsg:
                await InvokeAsync(() =>
                {
                    messages.Add(new CoPilotMessages
                    {
                        EventType = evt.Type,
                        Message = String.IsNullOrWhiteSpace(assistantMsg.Data.Content) ? "....." : assistantMsg.Data.Content,
                        ReasoningText = assistantMsg.Data.ReasoningText ?? string.Empty
                    });
                    StateHasChanged();
                });
                break;
            case UserMessageEvent userMsg:
                await InvokeAsync(() =>
                {
                    messages.Add(new CoPilotMessages
                    {
                        EventType = evt.Type,
                        Message = userMsg.Data.Content
                    });
                    StateHasChanged();
                });
                break;
            case ToolExecutionStartEvent toolEvt:
                await InvokeAsync(() =>
                {
                    messages.Add(new CoPilotMessages
                    {
                        EventType = evt.Type,
                        Message = toolEvt.Data.ToolName
                    });
                    StateHasChanged();
                });
                break;
            case SessionStartEvent:
                await InvokeAsync(() => { SessionStatus = FluentColor.Success; StateHasChanged(); });
                break;
            case SessionShutdownEvent:
            case SessionErrorEvent:
                await InvokeAsync(() => { SessionStatus = FluentColor.Error; StateHasChanged(); });
                break;
            case AssistantTurnStartEvent:
                await InvokeAsync(() => { AgentWorking = true; StateHasChanged(); });
                break;
            case AssistantTurnEndEvent:
                await InvokeAsync(async () =>
                {
                    AgentWorking = false;
                    await RefreshContentAvailability();
                    StateHasChanged();
                });
                break;
            case AbortEvent:
                await InvokeAsync(() => { AgentWorking = false; StateHasChanged(); });
                break;
        }
    }

    private async Task HandleQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return;

        SendingMessage = true;
        await InvokeAsync(StateHasChanged);

        await CopilotService.SendAsync(query);
        SendingMessage = false;
        queryText = string.Empty;
        await InvokeAsync(StateHasChanged);
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

    private async void NewSession(MouseEventArgs args)
    {
        messages = new(new ConcurrentQueue<CoPilotMessages>());
        CopilotService.ResetSession();
        await InitializeSession();
    }

    private async Task AbortSession(MouseEventArgs args)
    {
        await CopilotService.AbortAsync();
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
        HarvestState.OnScopeChanged -= OnScopeChanged;
        HarvestState.OnContentAvailabilityChanged -= OnContentAvailabilityChanged;
    }
}
