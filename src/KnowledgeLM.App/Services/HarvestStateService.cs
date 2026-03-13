using KnowledgeLM.App.Models.Notes.DTOs;

namespace KnowledgeLM.App.Services;

/// <summary>
/// Manages shared state between HarvestChatPane and HarvestNotesPane.
/// Tracks the current resource scope, available flashcards, and podcasts.
/// </summary>
public class HarvestStateService
{
    public ResourceBreadcrumbDto? CurrentScope { get; private set; }

    public event Action? OnScopeChanged;
    public event Action? OnContentAvailabilityChanged;

    public bool HasFlashcards { get; private set; }
    public bool HasPodcast { get; private set; }
    public bool HasQuizzes { get; private set; }
    public int? FlashcardCount { get; private set; }
    public int? QuizCount { get; private set; }

    public void SetScope(ResourceBreadcrumbDto breadcrumb)
    {
        CurrentScope = breadcrumb;
        HasFlashcards = false;
        HasPodcast = false;
        HasQuizzes = false;
        FlashcardCount = null;
        QuizCount = null;
        OnScopeChanged?.Invoke();
    }

    public void ClearScope()
    {
        CurrentScope = null;
        HasFlashcards = false;
        HasPodcast = false;
        HasQuizzes = false;
        FlashcardCount = null;
        QuizCount = null;
        OnScopeChanged?.Invoke();
    }

    public void UpdateContentAvailability(bool hasFlashcards, int? flashcardCount, bool hasPodcast, bool hasQuizzes = false, int? quizCount = null)
    {
        HasFlashcards = hasFlashcards;
        FlashcardCount = flashcardCount;
        HasPodcast = hasPodcast;
        HasQuizzes = hasQuizzes;
        QuizCount = quizCount;
        OnContentAvailabilityChanged?.Invoke();
    }
}
