using GitHub.Copilot.SDK;
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
        }
        await JS.InvokeVoidAsync("scrollToBottom", "HarvestMessages");
    }

    private async Task InitializeSession()
    {
        var systemPrompt = """
            You are a RAG-powered study assistant for the KnowledgeLM notebook app. 
            Your job is to help users understand and learn from their notes.
            
            You have access to these tools:
            - search_notes: Search through the user's vectorized notebook content
            - get_flashcards: Get flashcards for a resource
            - save_flashcards: Save generated flashcards
            - generate_podcast_audio: Generate podcast audio from SSML
            - save_podcast: Save podcast metadata
            
            When answering questions:
            1. Use search_notes to find relevant content from the user's notebooks
            2. Cite your sources with markdown links when possible
            3. If the user asks to generate flashcards, create Q&A pairs and save them
            4. If the user asks to generate a podcast, create a conversational SSML script with two speakers
            
            Format your responses in Markdown.
            """;

        await CopilotService.CreateSessionAsync(systemPrompt, onEvent: HandleSessionEvent);
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
                        Message = assistantMsg.Data.Content ?? string.Empty,
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

        var scope = HarvestState.CurrentScope;
        var fullQuery = scope is not null
            ? $"[Context: searching in resource '{scope.ResourceName}' (ID: {scope.ResourceId}), section '{scope.SectionName}' (ID: {scope.SectionId}), course '{scope.CourseName}' (ID: {scope.CourseId})]\n\n{query}"
            : query;

        await CopilotService.SendAsync(fullQuery);
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

    private async Task RequestGeneration(string type)
    {
        var scope = HarvestState.CurrentScope;
        if (scope is null) return;

        var prompt = type switch
        {
            "flashcards" => $"Generate flashcards for resource ID {scope.ResourceId} ('{scope.ResourceName}'). Search the notes content, create Q&A pairs, and save them using save_flashcards.",
            "podcast" => $"Generate a podcast for resource ID {scope.ResourceId} ('{scope.ResourceName}'). Search the notes content, create a conversational SSML script with two speakers (en-US-AndrewMultilingualNeural and en-US-AvaMultilingualNeural), generate the audio using generate_podcast_audio, and save the metadata using save_podcast.",
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

    private async void OnScopeChanged()
    {
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
