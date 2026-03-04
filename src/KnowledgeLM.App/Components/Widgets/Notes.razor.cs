using KnowledgeLM.App.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace KnowledgeLM.App.Components.Widgets
{
    partial class Notes : IDisposable
    {
        private List<KnowledgeLM.App.Models.Notes.DTOs.CourseDetailResponse>? courses;
        private bool loading = true;
        [Inject]
        private StateService StateService { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            await LoadCoursesAsync();
            StateService.OnRefreshRequested += OnRefreshRequested;
        }

        private async void OnRefreshRequested()
        {
            await LoadCoursesAsync();
            await InvokeAsync(StateHasChanged);
        }

        private async Task LoadCoursesAsync()
        {
            loading = true;
            var allCourses = await CrudService.GetAllCoursesAsync();
            courses = [];
            foreach (var c in allCourses)
            {
                var detail = await CrudService.GetCourseByIdAsync(c.Id);
                if (detail is not null)
                    courses.Add(detail);
            }
            loading = false;
        }

        public void Dispose()
        {
            StateService.OnRefreshRequested -= OnRefreshRequested;
        }

        private static Icon GetResourceIcon(KnowledgeLM.App.Models.Notes.ResourceType type) =>
            type switch
            {
                KnowledgeLM.App.Models.Notes.ResourceType.Image => new Icons.Regular.Size20.Image(),
                KnowledgeLM.App.Models.Notes.ResourceType.Video => new Icons.Regular.Size20.Video(),
                _ => new Icons.Regular.Size20.Document(),
            };
    }
}
