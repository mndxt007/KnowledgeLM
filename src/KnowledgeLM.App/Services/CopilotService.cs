using GitHub.Copilot.SDK;
using KnowledgeLM.App.Helpers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace KnowledgeLM.App.Services
{
    public class CopilotService : IAsyncDisposable
    {
        private readonly Kernel _kernel;
        private readonly ILogger<CopilotClient> _logger;
        private CopilotClient? _client;
        private CopilotSession? _session;

        public bool HasActiveSession => _session is not null;
        public bool IsWorking { get; private set; }

        public CopilotService(Kernel kernel, ILogger<CopilotClient> logger)
        {
            _kernel = kernel;
            _logger = logger;

            _client = new CopilotClient(new()
            {
                UseLoggedInUser = true,
                Logger = _logger,
            });
        }

        private IList<AIFunction> GetAIFunctions()
        {
            var aiFunctions = new List<AIFunction>();

            foreach (var plugin in _kernel.Plugins)
            {
                foreach (var function in plugin)
                {
                    aiFunctions.Add(function);
                }
            }

            var getNotesFolderTool = AIFunctionFactory.Create(
                () => AgentStatics.LearnLoomRoot,
                name: "get_notes_folder",
                description: "Returns the absolute folder path on disk where all resources (courses, sections, notes, images) must be stored. Call this before creating any files or folders.");

            aiFunctions.Add(getNotesFolderTool);

            return aiFunctions;
        }

        public async Task CreateSessionAsync(
            string systemPrompt,
            Dictionary<string, object>? mcpServers = null,
            Func<SessionEvent, Task>? onEvent = null)
        {
            _client ??= new CopilotClient(new()
            {
                UseLoggedInUser = true,
                Logger = _logger,
            });

            _session = await _client.CreateSessionAsync(new SessionConfig
            {
                Tools = GetAIFunctions(),
                OnPermissionRequest = PermissionHandler.ApproveAll,
                SystemMessage = new()
                {
                    Content = systemPrompt,
                    Mode = SystemMessageMode.Replace,
                },
                McpServers = mcpServers,
            });

            _session.On(async evt =>
            {
                switch (evt)
                {
                    case AssistantTurnStartEvent:
                        _logger.LogInformation("Assistant Started Working");
                        IsWorking = true;
                        break;
                    case AssistantTurnEndEvent:
                    case AbortEvent:
                        _logger.LogInformation("Assistant Turn ended");
                        IsWorking = false;
                        break;
                }

                if (onEvent is not null)
                {
                    await onEvent(evt);
                }
            });
        }

        public async Task SendAsync(string prompt)
        {
            if (_session is not null)
            {
                await _session.SendAsync(new MessageOptions
                {
                    Prompt = prompt,
                    Mode = "immediate"
                });
            }
        }

        public async Task AbortAsync()
        {
            if (_session is not null)
            {
                await _session.AbortAsync();
            }
        }

        public void ResetSession()
        {
            _session = null;
        }

        public async ValueTask DisposeAsync()
        {
            _session = null;
            _client = null;
            await ValueTask.CompletedTask;
        }
    }
}
