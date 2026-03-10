using KnowledgeLM.App.Models.Notes.DTOs;
using KnowledgeLM.App.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace KnowledgeLM.App.Components.Widgets;

public partial class HarvestNotesPane : IDisposable
{
    [Inject] private HarvestStateService HarvestState { get; set; } = default!;
    [Inject] private NotesCRUDService NotesCrudService { get; set; } = default!;
    [Inject] private StateService StateService { get; set; } = default!;
    [Inject] private VectorStoreService VectorStoreService { get; set; } = default!;
    [Inject] private FlashcardService FlashcardService { get; set; } = default!;
    [Inject] private PodcastService PodcastService { get; set; } = default!;
    [Inject] private ILogger<HarvestNotesPane> Logger { get; set; } = default!;

    private EventCallback<ResourceResponse> _callback;

    protected override void OnInitialized()
    {
        _callback = EventCallback.Factory.Create<ResourceResponse>(this, OnResourceSelected);
        StateService.OnResourceSelectedCallBack = _callback;
    }

    private async Task OnResourceSelected(ResourceResponse resource)
    {
        Logger.LogInformation("Harvest: Selected resource {Name} (ID: {Id})", resource.Name, resource.Id);

        // Get breadcrumb and set scope
        var breadcrumb = await NotesCrudService.GetBreadcrumbAsync(resource.Id);
        if (breadcrumb is not null)
        {
            HarvestState.SetScope(breadcrumb);
        }

        // Check vectorization status and start if needed
        var isVectorized = await VectorStoreService.IsResourceVectorizedAsync(resource.Id);
        if (!isVectorized)
        {
            Logger.LogInformation("Starting vectorization for resource {Id}", resource.Id);
            _ = Task.Run(async () =>
            {
                await VectorStoreService.VectorizeResourceAsync(resource.Id);
                await InvokeAsync(StateHasChanged);
            });
        }

        // Check for existing flashcards and podcasts
        var hasFlashcards = await FlashcardService.HasFlashcardsAsync(resource.Id);
        var flashcardCount = hasFlashcards ? await FlashcardService.GetFlashcardCountAsync(resource.Id) : 0;
        var hasPodcast = await PodcastService.HasPodcastAsync(resource.Id);

        HarvestState.UpdateContentAvailability(hasFlashcards, flashcardCount, hasPodcast);
    }

    public void Dispose()
    {
        if (StateService?.OnResourceSelectedCallBack.Equals(_callback) == true)
        {
            StateService.OnResourceSelectedCallBack = default;
        }
    }
}
