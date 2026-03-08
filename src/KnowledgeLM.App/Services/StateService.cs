using KnowledgeLM.App.Models.Notes.DTOs;
using Microsoft.AspNetCore.Components;

namespace KnowledgeLM.App.Services
{
    public class StateService
    {
        public event Action? OnRefreshRequested;
        public EventCallback<ResourceResponse> OnResourceSelectedCallBack { get; set; }

        public void RequestRefresh()
        {
            OnRefreshRequested?.Invoke();
        }

        public async Task OnResourceSelected(ResourceResponse resourceResponse)
        {
            await OnResourceSelectedCallBack.InvokeAsync(resourceResponse);
        }
    }
}
