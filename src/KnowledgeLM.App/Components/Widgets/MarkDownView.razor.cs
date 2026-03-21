using KnowledgeLM.App.Models.Notes.DTOs;
using KnowledgeLM.App.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeLM.App.Components.Widgets
{
    partial class MarkDownView : IDisposable
    {
        [Inject]
        private StateService StateService { get; set; } = default!;
        [Inject]
        private NotesCRUDService NotesCRUDService { get; set; } = default!;
        [Parameter]
        public int? ResourceId { get; set; }
        private int? _loadedResourceId;
        private ResourceResponse ResourceResponse { get; set; } = default!;
        private SectionResponse? SectionResponse { get; set; }
        private CourseResponse? CourseResponse { get; set; }
        private string MarkDownContent = "# Hello World\nThis is a markdown content.";
        private bool _hasSearchResults;
        private EventCallback<ResourceResponse> _callback;

        private void OnSearchResultsChanged(bool hasResults)
        {
            _hasSearchResults = hasResults;
        }

        protected override async Task OnAfterRenderAsync(bool first)
        {
            if (ResourceId.HasValue && ResourceId != _loadedResourceId)
            {
                var resource = await NotesCRUDService.GetResourceByIdAsync(ResourceId.Value);
                if (resource != null)
                {
                    await OnResourceSelected(resource);
                    _loadedResourceId = ResourceId.Value;
                }
            }
        }

        protected override void OnInitialized()
        {
            _callback = EventCallback.Factory.Create<ResourceResponse>(this, OnResourceSelected);
            StateService.OnResourceSelectedCallBack = _callback;
        }

        public void Dispose()
        {
            if (StateService?.OnResourceSelectedCallBack.Equals(_callback) == true)
            {
                StateService.OnResourceSelectedCallBack = default;
            }
        }

        private async Task OnResourceSelected(ResourceResponse resourceResponse)
        {
            // Handle resource selection
            ResourceResponse = resourceResponse;
            _loadedResourceId = resourceResponse.Id;

            //To:DO can be optimizedby caching section and course details in StateService to avoid multiple API calls when user selects resources from the same section/course

            // Fetch Section information
            var sectionDetail = await NotesCRUDService.GetSectionByIdAsync(resourceResponse.SectionId);
            if (sectionDetail != null)
            {
                SectionResponse = new SectionResponse
                {
                    Id = sectionDetail.Id,
                    CourseId = sectionDetail.CourseId,
                    Name = sectionDetail.Name,
                    Description = sectionDetail.Description,
                    Location = sectionDetail.Location,
                    CreatedAt = sectionDetail.CreatedAt,
                    LastModified = sectionDetail.LastModified,
                    ResourceCount = sectionDetail.Resources.Count
                };

                // Fetch Course information
                var courses = await NotesCRUDService.GetAllCoursesAsync();
                CourseResponse = courses.FirstOrDefault(c => c.Id == sectionDetail.CourseId);
            }

            await ExtractContent(resourceResponse?.Location!);
            await InvokeAsync(StateHasChanged);
        }

        private async Task ExtractContent(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                MarkDownContent = "No content available.";
                return;
            }
            
            try
            {
                MarkDownContent = await File.ReadAllTextAsync(filePath);
            }
            catch (Exception ex)
            {
                MarkDownContent = $"Error loading content: {ex.Message}";
            }

        }

        private Task OnChanged(string newContent)
        {
            MarkDownContent = newContent;
            return Task.CompletedTask;
        }
    }
}
