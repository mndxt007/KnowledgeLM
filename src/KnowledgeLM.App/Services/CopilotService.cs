using System.Collections.Concurrent;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace KnowledgeLM.App.Services
{
    /// <summary>
    /// Factory/cache for <see cref="ICopilotSession"/> instances. One session is kept per
    /// <see cref="CopilotPaneKey"/> for the lifetime of the service, so chat history is retained
    /// when Blazor components are torn down and re-created (e.g. when navigating between panes).
    /// </summary>
    public class CopilotService : IAsyncDisposable
    {
        private readonly Kernel _kernel;
        private readonly ILogger<CopilotClient> _logger;
        private readonly ConcurrentDictionary<CopilotPaneKey, CopilotSessionHandle> _sessions = new();
        private readonly SemaphoreSlim _initLock = new(1, 1);

        public CopilotService(Kernel kernel, ILogger<CopilotClient> logger)
        {
            _kernel = kernel;
            _logger = logger;
        }

        /// <summary>
        /// Returns the cached session for <paramref name="key"/>, or creates a new one if none exists.
        /// </summary>
        public async Task<ICopilotSession> GetOrCreateSessionAsync(
            CopilotPaneKey key,
            string systemPrompt,
            Dictionary<string, object>? mcpServers = null)
        {
            if (_sessions.TryGetValue(key, out var existing))
            {
                return existing;
            }

            await _initLock.WaitAsync();
            try
            {
                if (_sessions.TryGetValue(key, out existing))
                {
                    return existing;
                }

                var handle = new CopilotSessionHandle(_kernel, _logger, systemPrompt, mcpServers);
                await handle.InitializeAsync();
                _sessions[key] = handle;
                return handle;
            }
            finally
            {
                _initLock.Release();
            }
        }

        /// <summary>Returns the cached session for <paramref name="key"/> if one exists.</summary>
        public bool TryGetSession(CopilotPaneKey key, out ICopilotSession? session)
        {
            if (_sessions.TryGetValue(key, out var handle))
            {
                session = handle;
                return true;
            }

            session = null;
            return false;
        }

        /// <summary>Disposes and removes the cached session for <paramref name="key"/>, if any.</summary>
        public async Task RemoveSessionAsync(CopilotPaneKey key)
        {
            if (_sessions.TryRemove(key, out var handle))
            {
                await handle.DisposeAsync();
            }
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var handle in _sessions.Values)
            {
                await handle.DisposeAsync();
            }


            _sessions.Clear();
            _initLock.Dispose();
        }
    }
}
