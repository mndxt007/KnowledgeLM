using System.Text.Json;
using KnowledgeLM.App.Models.AI;
using Markdig;
using Microsoft.AspNetCore.Components;

namespace KnowledgeLM.App.Components.Widgets;

public partial class QuizViewer
{
    [Parameter]
    public List<Quiz>? Quizzes { get; set; }

    private int currentIndex = 0;
    private int? selectedOption = null;
    private bool hasAnswered = false;
    private bool isCompleted = false;
    private int correctCount = 0;

    // Track answers per question index
    private readonly Dictionary<int, int> answers = new();

    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private static string RenderMarkdown(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        return Markdown.ToHtml(text, MarkdownPipeline);
    }

    private List<string> GetOptions()
    {
        if (Quizzes is null || currentIndex >= Quizzes.Count)
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<string>>(Quizzes[currentIndex].OptionsJson) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private string GetOptionState(int optionIndex)
    {
        if (!hasAnswered) return selectedOption == optionIndex ? "selected" : "";

        var correct = Quizzes![currentIndex].CorrectOptionIndex;
        if (optionIndex == correct) return "correct";
        if (optionIndex == selectedOption && selectedOption != correct) return "incorrect";
        return "disabled";
    }

    private void SelectOption(int optionIndex)
    {
        if (hasAnswered) return;

        selectedOption = optionIndex;
        hasAnswered = true;
        answers[currentIndex] = optionIndex;

        if (optionIndex == Quizzes![currentIndex].CorrectOptionIndex)
            correctCount++;
    }

    private void Previous()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            RestoreAnswerState();
        }
    }

    private void Next()
    {
        if (Quizzes is not null && currentIndex < Quizzes.Count - 1)
        {
            currentIndex++;
            RestoreAnswerState();
        }
    }

    private void Finish()
    {
        isCompleted = true;
    }

    private void RestoreAnswerState()
    {
        if (answers.TryGetValue(currentIndex, out var answer))
        {
            selectedOption = answer;
            hasAnswered = true;
        }
        else
        {
            selectedOption = null;
            hasAnswered = false;
        }
    }

    protected override void OnParametersSet()
    {
        currentIndex = 0;
        selectedOption = null;
        hasAnswered = false;
        isCompleted = false;
        correctCount = 0;
        answers.Clear();
    }
}
