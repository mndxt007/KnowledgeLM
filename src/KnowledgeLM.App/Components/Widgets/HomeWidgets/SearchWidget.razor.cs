using KnowledgeLM.App.Services;
using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;


namespace KnowledgeLM.App.Components.Widgets.HomeWidgets
{
    partial class SearchWidget : IDisposable
    {
        private FluentSearch? searchTest;
        string? searchValue = string.Empty;
        private CancellationTokenSource? _cts;
        private List<NoteSearchResult> searchResults = [];
        private bool isSearching = false;

        private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        [Parameter]
        public EventCallback<bool> HasResultsChanged { get; set; }

        [Parameter]
        public EventCallback<int> OnResourceClicked { get; set; }

        [Inject]
        ILogger<SearchWidget> Logger { get; set; } = default!;

        [Inject]
        VectorStoreService VectorStoreService { get; set; } = default!;

        async Task HandleSearchInput()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            Logger.LogInformation("Search input changed: {SearchValue}", searchValue);
            if (!String.IsNullOrWhiteSpace(searchValue) && searchValue.Trim().Length >= 3)
            {
                try
                {
                    isSearching = true;
                    await InvokeAsync(StateHasChanged);
                    var results = await VectorStoreService.SearchAsync(searchValue, top: 3, cancellationToken: token);
                    searchResults = results;
                    await HasResultsChanged.InvokeAsync(searchResults.Count > 0);
                }
                catch (OperationCanceledException)
                {
                    Logger.LogDebug("Search cancelled for: {SearchValue}", searchValue);
                }
                finally
                {
                    isSearching = false;
                    await InvokeAsync(StateHasChanged);
                }
            }
            else
            {
                searchResults = [];
                await HasResultsChanged.InvokeAsync(false);
            }
        }

        private static string RenderMarkdown(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            return Markdown.ToHtml(text, MarkdownPipeline);
        }

        private static string Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
