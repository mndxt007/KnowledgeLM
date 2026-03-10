using KnowledgeLM.App.Models.AI;
using Microsoft.AspNetCore.Components;

namespace KnowledgeLM.App.Components.Widgets;

public partial class FlashcardViewer
{
    [Parameter]
    public List<Flashcard>? Flashcards { get; set; }

    private int currentIndex = 0;
    private bool isFlipped = false;

    private void ToggleFlip() => isFlipped = !isFlipped;

    private void Previous()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            isFlipped = false;
        }
    }

    private void Next()
    {
        if (Flashcards is not null && currentIndex < Flashcards.Count - 1)
        {
            currentIndex++;
            isFlipped = false;
        }
    }

    protected override void OnParametersSet()
    {
        currentIndex = 0;
        isFlipped = false;
    }
}
