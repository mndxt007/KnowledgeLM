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

    public RAGSearchPlugin(VectorStoreService vectorStoreService)
    {
        _vectorStoreService = vectorStoreService;
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
