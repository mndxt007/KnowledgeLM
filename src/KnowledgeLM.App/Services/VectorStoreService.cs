using KnowledgeLM.App.Data;
using KnowledgeLM.App.Models.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.SqliteVec;
using System.Text.RegularExpressions;

namespace KnowledgeLM.App.Services;

/// <summary>
/// Manages vectorization of notebook resources and provides scoped vector search.
/// Uses SQLite vector store (SqliteVec) for embeddings storage.
/// </summary>
public class VectorStoreService
{
    private readonly IDbContextFactory<NotesContext> _contextFactory;
    private readonly NotesCRUDService _notesCrudService;
    private readonly IEmbeddingGenerator<string, Embedding<float>>? _embeddingGenerator;
    private readonly ILogger<VectorStoreService> _logger;
    private readonly AppNotificationService _notifications;
    private readonly string _vectorDbConnectionString;
    private const string CollectionName = "note_chunks";

    public VectorStoreService(
        IDbContextFactory<NotesContext> contextFactory,
        NotesCRUDService notesCrudService,
        ILogger<VectorStoreService> logger,
        AppNotificationService notifications,
        IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator = null)
    {
        _contextFactory = contextFactory;
        _notesCrudService = notesCrudService;
        _embeddingGenerator = embeddingGenerator;
        _logger = logger;
        _notifications = notifications;
        var vectorDbPath = Path.Combine(FileSystem.AppDataDirectory, "knowledgelm_vectors.db");
        _vectorDbConnectionString = $"Data Source={vectorDbPath}";
    }

    /// <summary>
    /// Check if a resource has been vectorized.
    /// </summary>
    public async Task<bool> IsResourceVectorizedAsync(int resourceId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.VectorizedResources
            .AnyAsync(v => v.ResourceId == resourceId && v.Status == "Completed");
    }

    /// <summary>
    /// Get the vectorization status for a resource.
    /// </summary>
    public async Task<VectorizedResource?> GetVectorizationStatusAsync(int resourceId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.VectorizedResources
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.ResourceId == resourceId);
    }

    /// <summary>
    /// Get vectorization status for multiple resource IDs.
    /// </summary>
    public async Task<Dictionary<int, string>> GetVectorizationStatusBatchAsync(IEnumerable<int> resourceIds)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.VectorizedResources
            .AsNoTracking()
            .Where(v => resourceIds.Contains(v.ResourceId))
            .ToDictionaryAsync(v => v.ResourceId, v => v.Status);
    }

    /// <summary>
    /// Vectorize all resources in a section.
    /// </summary>
    public async Task VectorizeSectionAsync(int sectionId, Action<int, string>? onProgress = null)
    {
        var resources = await _notesCrudService.GetResourcesBySectionIdAsync(sectionId);
        foreach (var resource in resources)
        {
            if (resource.ResourceType != Models.Notes.ResourceType.Markdown &&
                resource.ResourceType != Models.Notes.ResourceType.Other)
                continue;

            var isVectorized = await IsResourceVectorizedAsync(resource.Id);
            if (!isVectorized)
            {
                onProgress?.Invoke(resource.Id, "Processing");
                await VectorizeResourceAsync(resource.Id);
                onProgress?.Invoke(resource.Id, "Completed");
            }
        }
    }

    /// <summary>
    /// Vectorize all resources across all sections in a course.
    /// </summary>
    public async Task VectorizeCourseAsync(int courseId, Action<int, string>? onProgress = null)
    {
        var course = await _notesCrudService.GetCourseByIdAsync(courseId);
        if (course is null) return;

        foreach (var section in course.Sections)
        {
            await VectorizeSectionAsync(section.Id, onProgress);
        }
    }

    /// <summary>
    /// Vectorize a resource: chunk its markdown content, generate embeddings, and store in vector DB.
    /// Returns (success, errorMessage). On success errorMessage is null.
    /// </summary>
    public async Task<(bool Success, string? Message)> VectorizeResourceAsync(int resourceId)
    {
        if (_embeddingGenerator is null)
        {
            var msg = "No embedding model configured. Check Azure OpenAI settings.";
            _logger.LogWarning("No embedding generator configured. Skipping vectorization for resource {ResourceId}.", resourceId);
            _notifications.NotifyError(msg);
            return (false, msg);
        }

        var resource = await _notesCrudService.GetResourceByIdAsync(resourceId);
        if (resource is null)
        {
            _logger.LogWarning("Resource {ResourceId} not found.", resourceId);
            return (false, $"Resource {resourceId} not found.");
        }

        var breadcrumb = await _notesCrudService.GetBreadcrumbAsync(resourceId);
        if (breadcrumb is null) return (false, "Could not resolve resource breadcrumb.");

        await UpsertVectorizationRecord(resourceId, "Processing", 0);
        _notifications.NotifyProgress($"Vectorizing \"{resource.Name}\"…");

        try
        {
            var filePath = resource.Location;
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                _logger.LogWarning("Resource file not found at {Path}", filePath);
                await UpsertVectorizationRecord(resourceId, "Failed", 0);
                return (false, $"Resource file not found at: {filePath}");
            }

            var content = await File.ReadAllTextAsync(filePath);
            var chunks = ChunkMarkdown(content);

            if (chunks.Count == 0)
            {
                await UpsertVectorizationRecord(resourceId, "Completed", 0);
                return (true, null);
            }

            var embeddings = await _embeddingGenerator.GenerateAsync(chunks);

            var noteChunks = new List<NoteChunk>();
            for (int i = 0; i < chunks.Count; i++)
            {
                noteChunks.Add(new NoteChunk
                {
                    Id = Guid.NewGuid().ToString(),
                    ResourceId = resourceId,
                    SectionId = breadcrumb.SectionId,
                    CourseId = breadcrumb.CourseId,
                    Text = chunks[i],
                    SourceName = resource.Name,
                    SourceLink = filePath ?? string.Empty,
                    Embedding = embeddings[i].Vector
                });
            }

            var collection = new SqliteCollection<string, NoteChunk>(
                _vectorDbConnectionString, CollectionName);

            await collection.EnsureCollectionExistsAsync();

            foreach (var chunk in noteChunks)
            {
                await collection.UpsertAsync(chunk);
            }

            await UpsertVectorizationRecord(resourceId, "Completed", noteChunks.Count);
            _logger.LogInformation("Vectorized resource {ResourceId} with {ChunkCount} chunks.", resourceId, noteChunks.Count);
            _notifications.NotifySuccess($"Vectorized \"{resource.Name}\" — {noteChunks.Count} chunks.");
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to vectorize resource {ResourceId}.", resourceId);
            await UpsertVectorizationRecord(resourceId, "Failed", 0);
            _notifications.NotifyError($"Vectorization failed for \"{resource.Name}\": {ex.Message}");
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Search vectorized notes with optional scoping by resource, section, or course.
    /// </summary>
    public async Task<List<NoteSearchResult>> SearchAsync(
        string query,
        int? resourceId = null,
        int? sectionId = null,
        int? courseId = null,
        int top = 5,
        CancellationToken cancellationToken = default)
    {
        if (_embeddingGenerator is null)
        {
            _logger.LogWarning("No embedding generator configured. Cannot search.");
            return [];
        }

        try
        {
            var queryEmbedding = await _embeddingGenerator.GenerateAsync(query, cancellationToken: cancellationToken);

            var collection = new SqliteCollection<string, NoteChunk>(
                _vectorDbConnectionString, CollectionName);

            var searchOptions = new VectorSearchOptions<NoteChunk>();

            // Add filter if scoping is provided
            if (resourceId.HasValue)
            {
                searchOptions.Filter = r => r.ResourceId == resourceId.Value;
            }
            else if (sectionId.HasValue)
            {
                searchOptions.Filter = r => r.SectionId == sectionId.Value;
            }
            else if (courseId.HasValue)
            {
                searchOptions.Filter = r => r.CourseId == courseId.Value;
            }

            var results = collection.SearchAsync(queryEmbedding.Vector, top, searchOptions, cancellationToken);

            var searchResults = new List<NoteSearchResult>();
            await foreach (var result in results.WithCancellation(cancellationToken))
            {
                searchResults.Add(new NoteSearchResult
                {
                    Text = result.Record.Text,
                    SourceName = result.Record.SourceName,
                    SourceLink = result.Record.SourceLink,
                    ResourceId = result.Record.ResourceId,
                    Score = result.Score ?? 0
                });
            }

            // Enrich results with breadcrumb data from the relational DB
            var resourceIds = searchResults.Select(r => r.ResourceId).Distinct().ToList();
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            var resources = await context.Resources
                .Include(r => r.Section)
                    .ThenInclude(s => s.Course)
                .Where(r => resourceIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, cancellationToken);

            foreach (var sr in searchResults)
            {
                if (resources.TryGetValue(sr.ResourceId, out var resource))
                {
                    sr.CourseName = resource.Section.Course.Name;
                    sr.SectionName = resource.Section.Name;
                    sr.Location = resource.Location;
                    sr.LastModified = resource.LastModified;
                }
            }

            return searchResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vector search failed for query: {Query}", query);
            return [];
        }
    }

    private async Task UpsertVectorizationRecord(int resourceId, string status, int chunkCount)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.VectorizedResources
            .FirstOrDefaultAsync(v => v.ResourceId == resourceId);

        if (existing is not null)
        {
            existing.Status = status;
            existing.ChunkCount = chunkCount;
            if (status == "Completed")
                existing.VectorizedAt = DateTime.UtcNow;
        }
        else
        {
            context.VectorizedResources.Add(new VectorizedResource
            {
                ResourceId = resourceId,
                Status = status,
                ChunkCount = chunkCount,
                VectorizedAt = status == "Completed" ? DateTime.UtcNow : default
            });
        }
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Chunks markdown content by headings and paragraphs.
    /// </summary>
    internal static List<string> ChunkMarkdown(string markdown, int maxChunkSize = 1000)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return [];

        var chunks = new List<string>();
        var sections = Regex.Split(markdown, @"(?=^#{1,6}\s)", RegexOptions.Multiline);

        foreach (var section in sections)
        {
            var trimmed = section.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            if (trimmed.Length <= maxChunkSize)
            {
                chunks.Add(trimmed);
            }
            else
            {
                var paragraphs = Regex.Split(trimmed, @"\n\s*\n");
                var currentChunk = string.Empty;

                foreach (var para in paragraphs)
                {
                    var trimmedPara = para.Trim();
                    if (string.IsNullOrWhiteSpace(trimmedPara))
                        continue;

                    if (currentChunk.Length + trimmedPara.Length + 2 <= maxChunkSize)
                    {
                        currentChunk = string.IsNullOrEmpty(currentChunk)
                            ? trimmedPara
                            : $"{currentChunk}\n\n{trimmedPara}";
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(currentChunk))
                            chunks.Add(currentChunk);
                        currentChunk = trimmedPara;
                    }
                }

                if (!string.IsNullOrEmpty(currentChunk))
                    chunks.Add(currentChunk);
            }
        }

        return chunks;
    }
}

/// <summary>
/// Represents a search result from the vector store.
/// </summary>
public class NoteSearchResult
{
    public string Text { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public string SourceLink { get; set; } = string.Empty;
    public int ResourceId { get; set; }
    public double Score { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public DateTime LastModified { get; set; }
}
