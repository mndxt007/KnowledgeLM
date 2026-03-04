namespace KnowledgeLM.App.Services
{
    public class StateService
    {
        public event Action? OnRefreshRequested;

        public void RequestRefresh()
        {
            OnRefreshRequested?.Invoke();
        }
    }
}
