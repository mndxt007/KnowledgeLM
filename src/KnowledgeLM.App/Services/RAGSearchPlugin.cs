using System.ComponentModel;
using System.Text;
using Microsoft.SemanticKernel;

namespace KnowledgeLM.App.Services;

/// <summary>
/// Semantic Kernel plugin that exposes RAG text search over vectorized notebook content.
/// </summary>
public class RAGSearchPlugin
{
    private readonly VectorStoreService _vectorStoreService;
    private readonly HarvestStateService _harvestState;

    public RAGSearchPlugin(VectorStoreService vectorStoreService, HarvestStateService harvestState)
    {
        _vectorStoreService = vectorStoreService;
        _harvestState = harvestState;
    }

    [KernelFunction("get_current_context")]
    [Description("Gets the current user context — which resource, section, and course is currently scoped/selected. Call this before search_notes to understand what the user is looking at. Returns null values if nothing is scoped.")]
    public Task<string> GetCurrentContextAsync()
    {
        var scope = _harvestState.CurrentScope;
        if (scope is null)
            return Task.FromResult("No resource is currently scoped. The user has not selected any specific notebook, section, or course.");

        var sb = new StringBuilder();
        sb.AppendLine("Currently scoped context:");
        sb.AppendLine($"- Course: {scope.CourseName} (ID: {scope.CourseId})");
        sb.AppendLine($"- Section: {scope.SectionName} (ID: {scope.SectionId})");
        sb.AppendLine($"- Resource: {scope.ResourceName} (ID: {scope.ResourceId})");
        sb.AppendLine();
        sb.AppendLine("Use these IDs when calling search_notes to filter results to the user's current context.");

        return Task.FromResult(sb.ToString());
    }

    [KernelFunction("search_notes")]
    [Description("Search through vectorized notes and resources using semantic similarity. Returns relevant text chunks with source citations. Use this to find information in the user's notebooks.")]
    public async Task<string> SearchNotesAsync(
        [Description("The search query to find relevant content")] string query,
        [Description("Optional: filter by specific resource ID")] int? resourceId = null,
        [Description("Optional: filter by section ID to search within a section")] int? sectionId = null,
        [Description("Optional: filter by course ID to search within a course")] int? courseId = null,
        [Description("Number of results to return (default: 5)")] int top = 5)
    {
        var results = await _vectorStoreService.SearchAsync(query, resourceId, sectionId, courseId, top);

        if (results.Count == 0)
            return "No relevant content found in the notebooks.";

        var sb = new StringBuilder();
        sb.AppendLine("## Search Results\n");

        for (int i = 0; i < results.Count; i++)
        {
            var r = results[i];
            sb.AppendLine($"### [{r.SourceName}]({r.SourceLink})");
            sb.AppendLine($"**Relevance:** {r.Score:P0}\n");
            sb.AppendLine(r.Text);
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
