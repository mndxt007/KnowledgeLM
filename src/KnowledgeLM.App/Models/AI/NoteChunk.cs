using Microsoft.Extensions.VectorData;

namespace KnowledgeLM.App.Models.AI;

/// <summary>
/// Vector store data model for text search. Used with SqliteVec, not managed by EF Core.
/// </summary>
public class NoteChunk
{
    [VectorStoreKey]
    public string Id { get; set; } = string.Empty;

    [VectorStoreData(IsIndexed = true)]
    public int ResourceId { get; set; }

    [VectorStoreData(IsIndexed = true)]
    public int SectionId { get; set; }

    [VectorStoreData(IsIndexed = true)]
    public int CourseId { get; set; }

    [VectorStoreData]
    public string Text { get; set; } = string.Empty;

    [VectorStoreData]
    public string SourceName { get; set; } = string.Empty;

    [VectorStoreData]
    public string SourceLink { get; set; } = string.Empty;

    [VectorStoreVector(Dimensions: 1536)]
    public ReadOnlyMemory<float> Embedding { get; set; }
}
