using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace KnowledgeLM.MonacoEditor.Components
{
    /// <summary>
    /// Toolbar component for markdown formatting commands in Monaco Editor
    /// </summary>
    public partial class MarkdownToolbar : ComponentBase
    {
        [Inject]
        public required IJSRuntime JSRuntime { get; set; }

        [Parameter]
        public required string EditorId { get; set; }

        private async Task ExecuteCommand(string commandId)
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("monacoMarkdownToolbar.executeCommand", EditorId, commandId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MarkdownToolbar] Error executing command '{commandId}': {ex.Message}");
            }
        }

        private string GetTooltip(string label, string shortcut)
        {
            var isMac = JSRuntime is IJSInProcessRuntime jsInProcess 
                ? jsInProcess.Invoke<bool>("eval", "navigator.platform.toUpperCase().indexOf('MAC') >= 0")
                : false;

            var displayShortcut = shortcut;
            if (isMac)
            {
                displayShortcut = shortcut
                    .Replace("Ctrl", "⌘")
                    .Replace("Alt", "⌥")
                    .Replace("Shift", "⇧");
            }

            return $"{label} ({displayShortcut})";
        }
    }
}
