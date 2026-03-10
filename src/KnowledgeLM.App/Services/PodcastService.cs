using System.ComponentModel;
using KnowledgeLM.App.Data;
using KnowledgeLM.App.Models.AI;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace KnowledgeLM.App.Services;

/// <summary>
/// Service for generating and managing AI-generated podcasts using Azure Speech Service.
/// Generates conversational audio with two speakers (male/female) from SSML content.
/// </summary>
public class PodcastService
{
    private readonly IDbContextFactory<NotesContext> _contextFactory;
    private readonly NotesCRUDService _notesCrudService;
    private readonly ILogger<PodcastService> _logger;
    private readonly string? _speechKey;
    private readonly string? _speechRegion;

    public PodcastService(
        IDbContextFactory<NotesContext> contextFactory,
        NotesCRUDService notesCrudService,
        ILogger<PodcastService> logger,
        IConfiguration configuration)
    {
        _contextFactory = contextFactory;
        _notesCrudService = notesCrudService;
        _logger = logger;
        _speechKey = configuration["AzureSpeech:Key"];
        _speechRegion = configuration["AzureSpeech:Region"];
    }

    /// <summary>
    /// Get the podcast for a resource, if it exists.
    /// </summary>
    public async Task<Podcast?> GetPodcastByResourceIdAsync(int resourceId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Podcasts
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ResourceId == resourceId);
    }

    /// <summary>
    /// Check if a podcast exists for a resource.
    /// </summary>
    public async Task<bool> HasPodcastAsync(int resourceId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Podcasts.AnyAsync(p => p.ResourceId == resourceId);
    }

    /// <summary>
    /// Generate podcast audio from SSML content using Azure Speech Service.
    /// </summary>
    [KernelFunction("generate_podcast_audio")]
    [Description("Generates podcast audio from SSML content. The SSML should use two voices for a conversational format: en-US-AndrewMultilingualNeural (male) and en-US-AvaMultilingualNeural (female). Returns the path to the generated audio file.")]
    public async Task<string> GeneratePodcastAudioAsync(
        [Description("The SSML content with two speaker voices")] string ssml,
        [Description("The absolute file path where the audio should be saved (e.g., D:\\Dev\\LearnLoom\\podcast.wav)")] string outputPath)
    {
        if (string.IsNullOrWhiteSpace(_speechKey) || string.IsNullOrWhiteSpace(_speechRegion))
        {
            _logger.LogWarning("Azure Speech Service credentials not configured.");
            return "Error: Azure Speech Service credentials not configured. Set AZURE_SPEECH_KEY and AZURE_SPEECH_REGION environment variables.";
        }

        try
        {
            var speechConfig = SpeechConfig.FromSubscription(_speechKey, _speechRegion);
            speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio16Khz128KBitRateMonoMp3);

            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            using var audioConfig = AudioConfig.FromWavFileOutput(outputPath);
            using var synthesizer = new SpeechSynthesizer(speechConfig, audioConfig);

            var result = await synthesizer.SpeakSsmlAsync(ssml);

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                _logger.LogInformation("Podcast audio generated successfully at {Path}", outputPath);
                return outputPath;
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                _logger.LogError("Speech synthesis canceled: {Reason} - {Details}", cancellation.Reason, cancellation.ErrorDetails);
                return $"Error: Speech synthesis canceled - {cancellation.ErrorDetails}";
            }

            return $"Error: Unexpected result - {result.Reason}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate podcast audio.");
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Save podcast metadata to the database after audio generation.
    /// </summary>
    [KernelFunction("save_podcast")]
    [Description("Saves podcast metadata after audio has been generated. Call this after generate_podcast_audio succeeds.")]
    public async Task<bool> SavePodcastAsync(
        [Description("The resource ID this podcast belongs to")] int resourceId,
        [Description("The title of the podcast")] string title,
        [Description("The absolute path to the generated audio file")] string audioFilePath,
        [Description("The SSML content used to generate the podcast")] string ssmlContent)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var existing = await context.Podcasts
                .FirstOrDefaultAsync(p => p.ResourceId == resourceId);
            if (existing is not null)
                context.Podcasts.Remove(existing);

            var podcast = new Podcast
            {
                ResourceId = resourceId,
                Title = title,
                AudioFilePath = audioFilePath,
                SsmlContent = ssmlContent,
                DurationTicks = 0
            };

            context.Podcasts.Add(podcast);
            await context.SaveChangesAsync();

            _logger.LogInformation("Saved podcast for resource {ResourceId}: {Title}", resourceId, title);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save podcast for resource {ResourceId}.", resourceId);
            return false;
        }
    }

    /// <summary>
    /// Delete podcast for a resource.
    /// </summary>
    public async Task DeletePodcastForResourceAsync(int resourceId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var podcast = await context.Podcasts
            .FirstOrDefaultAsync(p => p.ResourceId == resourceId);

        if (podcast is not null)
        {
            if (!string.IsNullOrWhiteSpace(podcast.AudioFilePath) && File.Exists(podcast.AudioFilePath))
            {
                try { File.Delete(podcast.AudioFilePath); }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete audio file {Path}", podcast.AudioFilePath); }
            }

            context.Podcasts.Remove(podcast);
            await context.SaveChangesAsync();
            _logger.LogInformation("Deleted podcast for resource {ResourceId}.", resourceId);
        }
    }

    /// <summary>
    /// Generate a sample SSML template for a two-speaker podcast conversation.
    /// </summary>
    public static string GetSsmlTemplate(string maleContent, string femaleContent)
    {
        return $"""
            <speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis" xml:lang="en-US">
                <voice name="en-US-AndrewMultilingualNeural">
                    {maleContent}
                </voice>
                <voice name="en-US-AvaMultilingualNeural">
                    {femaleContent}
                </voice>
            </speak>
            """;
    }
}
