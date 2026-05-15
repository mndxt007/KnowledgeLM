using GitHub.Copilot.SDK;
using KnowledgeLM.App.Models;

namespace KnowledgeLM.App.Services
{
    /// <summary>
    /// Owns a single GitHub Copilot SDK session along with its chat history and UI-facing state.
    /// One handle is cached per <see cref="CopilotPaneKey"/> by <see cref="CopilotService"/>, so
    /// the chat is retained across component re-mounts (e.g. navigating between panes).
    /// </summary>
    public interface ICopilotSession : IAsyncDisposable
    {
        bool IsWorking { get; }
        CopilotSessionStatus Status { get; }
        IEnumerable<CoPilotMessages> Messages { get; }
        bool HasMessages { get; }

        /// <summary>Fired whenever any observable state changes (messages, IsWorking, Status).</summary>
        event Action? StateChanged;

        /// <summary>Raw SDK event feed. Subscribers receive every event after the handle has updated its own state.</summary>
        event Func<SessionEvent, Task>? OnEvent;

        Task SendAsync(string prompt);
        Task AbortAsync();

        /// <summary>Aborts the current SDK session, clears messages, and starts a fresh one with the original config.</summary>
        Task RestartAsync();
    }
}
