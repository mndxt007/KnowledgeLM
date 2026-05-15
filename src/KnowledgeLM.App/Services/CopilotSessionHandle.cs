using System.Collections.Concurrent;
using GitHub.Copilot.SDK;
using KnowledgeLM.App.Helpers;
using KnowledgeLM.App.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace KnowledgeLM.App.Services
{
    /// <summary>
    /// Default <see cref="ICopilotSession"/> implementation. Wraps a <see cref="CopilotClient"/>
    /// and <see cref="CopilotSession"/>, maintains the chat history, and forwards SDK events
    /// to subscribers via <see cref="StateChanged"/> / <see cref="OnEvent"/>.
    /// </summary>
    internal sealed class CopilotSessionHandle : ICopilotSession
    {
        private readonly Kernel _kernel;
        private readonly ILogger<CopilotClient> _logger;
        private readonly string _systemPrompt;
        private readonly Dictionary<string, object>? _mcpServers;

        private CopilotClient? _client;
        private CopilotSession? _session;
        private ConcurrentQueue<CoPilotMessages> _messages = new();

        public bool IsWorking { get; private set; }
        public CopilotSessionStatus Status { get; private set; } = CopilotSessionStatus.Idle;
        public IEnumerable<CoPilotMessages> Messages => _messages;
        public bool HasMessages => !_messages.IsEmpty;

        public event Action? StateChanged;
        public event Func<SessionEvent, Task>? OnEvent;

        public CopilotSessionHandle(
            Kernel kernel,
            ILogger<CopilotClient> logger,
            string systemPrompt,
            Dictionary<string, object>? mcpServers)
        {
            _kernel = kernel;
            _logger = logger;
            _systemPrompt = systemPrompt;
            _mcpServers = mcpServers;
        }

        internal async Task InitializeAsync()
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
                    Content = _systemPrompt,
                    Mode = SystemMessageMode.Replace,
                },
                McpServers = _mcpServers,
            });

            Status = CopilotSessionStatus.Active;
            RaiseStateChanged();

            _session.On(async evt => await HandleSdkEventAsync(evt));
        }

        private async Task HandleSdkEventAsync(SessionEvent evt)
        {
            switch (evt)
            {
                case AssistantMessageEvent assistantMsg:
                    _messages.Enqueue(new CoPilotMessages
                    {
                        EventType = evt.Type,
                        Message = string.IsNullOrWhiteSpace(assistantMsg.Data.Content) ? "....." : assistantMsg.Data.Content,
                        ReasoningText = assistantMsg.Data.ReasoningText ?? string.Empty
                    });
                    RaiseStateChanged();
                    break;

                case UserMessageEvent userMsg:
                    _messages.Enqueue(new CoPilotMessages
                    {
                        EventType = evt.Type,
                        Message = userMsg.Data.Content
                    });
                    RaiseStateChanged();
                    break;

                case ToolExecutionStartEvent toolEvt:
                    _messages.Enqueue(new CoPilotMessages
                    {
                        EventType = evt.Type,
                        Message = toolEvt.Data.ToolName
                    });
                    RaiseStateChanged();
                    break;

                case SessionStartEvent:
                case SessionResumeEvent:
                    Status = CopilotSessionStatus.Active;
                    RaiseStateChanged();
                    break;

                case SessionShutdownEvent:
                case SessionErrorEvent:
                    Status = CopilotSessionStatus.Error;
                    RaiseStateChanged();
                    break;

                case AssistantTurnStartEvent:
                    _logger.LogInformation("Assistant started working");
                    IsWorking = true;
                    RaiseStateChanged();
                    break;

                case AssistantTurnEndEvent:
                case AbortEvent:
                    _logger.LogInformation("Assistant turn ended");
                    IsWorking = false;
                    RaiseStateChanged();
                    break;
            }

            var snapshot = OnEvent;
            if (snapshot is not null)
            {
                foreach (var handler in snapshot.GetInvocationList().Cast<Func<SessionEvent, Task>>())
                {
                    try
                    {
                        await handler(evt);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Subscriber event handler threw");
                    }
                }
            }
        }

        public async Task SendAsync(string prompt)
        {
            if (_session is null)
            {
                throw new InvalidOperationException("No active Copilot session.");
            }

            await _session.SendAsync(new MessageOptions
            {
                Prompt = prompt,
                Mode = "immediate"
            });
        }

        public async Task AbortAsync()
        {
            if (_session is null) return;
            await _session.AbortAsync();
        }

        public async Task RestartAsync()
        {
            if (_session is not null)
            {
                try { await _session.AbortAsync(); }
                catch (Exception ex) { _logger.LogWarning(ex, "Abort during restart failed"); }
            }

            _session = null;
            _messages = new ConcurrentQueue<CoPilotMessages>();
            IsWorking = false;
            Status = CopilotSessionStatus.Idle;
            RaiseStateChanged();

            await InitializeAsync();
        }

        private void RaiseStateChanged()
        {
            try { StateChanged?.Invoke(); }
            catch (Exception ex) { _logger.LogError(ex, "StateChanged subscriber threw"); }
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

        public async ValueTask DisposeAsync()
        {
            if (_session is not null)
            {
                try { await _session.AbortAsync(); }
                catch (Exception ex) { _logger.LogWarning(ex, "Abort during dispose failed"); }
            }

            _session = null;
            _client = null;
        }
    }
}
