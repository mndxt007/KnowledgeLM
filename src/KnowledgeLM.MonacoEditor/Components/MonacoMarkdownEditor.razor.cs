using KnowledgeLM.MonacoEditor.Markdown;
using KnowledgeLM.MonacoEditor.Memento;
using KnowledgeLM.MonacoEditor.Monaco;
using Ganss.Xss;
using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace KnowledgeLM.MonacoEditor.Components
{
    /// <summary>
    /// Monaco-based Markdown editor component using the official Monaco Editor from Microsoft CDN.
    /// Provides syntax highlighting, IntelliSense, and advanced editing features for Markdown.
    /// </summary>
    public partial class MonacoMarkdownEditor : ComponentBase, IAsyncDisposable
    {
        [Inject]
        public IHtmlSanitizer? HtmlSanitizer { get; set; }
        
        [Inject]
        public required IJSRuntime JSRuntime { get; set; }

        private TextEditorSession? editorSession_;
        private DotNetObjectReference<MonacoMarkdownEditor>? dotNetRef_;
        private string editorId_ = $"monaco-markdown-editor-{Guid.NewGuid():N}";
        private bool isInitialized_ = false;

        // =======================================================
        // CONTENT & DATA BINDING
        // =======================================================

        [Parameter]
        public string? Markdown
        {
            get => editorSession_?.Content;
            set
            {
                if (editorSession_ == null)
                {
                    editorSession_ = new TextEditorSession(value);
                }
                else if (editorSession_.Content != value)
                {
                    editorSession_ = new TextEditorSession(value);

                    if (isInitialized_)
                    {
                        _ = JSRuntime.InvokeVoidAsync(
                            "monacoMarkdownEditor.setContent",
                            editorId_,
                            value ?? string.Empty);
                    }
                }
            }
        }

        [Parameter]
        public EventCallback<string> MarkdownChanged { get; set; }

        [Parameter]
        public string? Id { get; set; }

        // =======================================================
        // EDITOR CONFIGURATION
        // =======================================================

        [Parameter]
        public MarkdownOptions? MarkdownOptions { get; set; }

        [Parameter]
        public MonacoOptions Options { get; set; } = new();

        // =======================================================
        // CSS INTELLISENSE INPUTS
        // =======================================================

        [Parameter]
        public string? ExternalCss { get; set; }

        [Parameter]
        public string? AdditionalCss { get; set; }

        // =======================================================
        // UI COMPOSITION
        // =======================================================

        [Parameter]
        public RenderFragment? LeftContent { get; set; }

        [Parameter]
        public RenderFragment? RightContent { get; set; }

        [Parameter]
        public EventCallback OnCancelled { get; set; }

        // =======================================================
        // LIFECYCLE EVENTS
        // =======================================================

        [Parameter]
        public Func<IJSRuntime, Task>? BeforeCreated { get; set; }

        [Parameter]
        public Func<IJSObjectReference, Task>? AfterInitialized { get; set; }

        private CancellationTokenSource? cancellationTokenSource_;
        private const int ThrottleDelayMs = 500;
        private List<string> contentChunks_ = new();
        private int expectedTotalChunks_ = 0;

        public string EditorId => editorId_;
        public bool IsModified => editorSession_?.IsModified ?? false;
        public bool CanUndo => editorSession_?.CanUndo ?? false;
        public bool CanRedo => editorSession_?.CanRedo ?? false;
        public MarkupString Html { get; private set; }

        protected override void OnParametersSet()
        {
            Html = CreateMarkupString();
            base.OnParametersSet();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    if (BeforeCreated is not null)
                    {
                        await BeforeCreated.Invoke(JSRuntime);
                    }

                    await ApplyMonacoOptionsAsync();

                    dotNetRef_ = DotNetObjectReference.Create(this);

                    isInitialized_ = await JSRuntime.InvokeAsync<bool>(
                        "monacoMarkdownEditor.init",
                        editorId_,
                        editorSession_?.Content ?? string.Empty,
                        dotNetRef_);

                    if (isInitialized_)
                    {
                        await UpdateCssIntelliSenseAsync();
                        await InvokeAsync(StateHasChanged);

                        if (AfterInitialized is not null)
                        {
                            var editorRef = await JSRuntime.InvokeAsync<IJSObjectReference>(
                                "monacoMarkdownEditor.getEditor",
                                editorId_);

                            await AfterInitialized.Invoke(editorRef);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MonacoMarkdownEditor] Error initializing: {ex.Message}");
                }
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        private async Task ApplyMonacoOptionsAsync()
        {
            if (Options is null)
                return;

            if (Options.EnableFrontMatter)
            {
                await JSRuntime.InvokeVoidAsync("monacoMarkdownEditor.enableFrontMatter", true);
            }

            await JSRuntime.InvokeVoidAsync("monacoMarkdownEditor.setOption", "wordWrap", Options.WordWrap);
            await JSRuntime.InvokeVoidAsync("monacoMarkdownEditor.setOption", "minimap", Options.Minimap);
            await JSRuntime.InvokeVoidAsync("monacoMarkdownEditor.setOption", "lineNumbers", Options.LineNumbers);
            await JSRuntime.InvokeVoidAsync("monacoMarkdownEditor.setOption", "fontSize", Options.FontSize);

            if (!string.IsNullOrWhiteSpace(Options.FontFamily))
            {
                await JSRuntime.InvokeVoidAsync("monacoMarkdownEditor.setOption", "fontFamily", Options.FontFamily);
            }

            if (Options.LineHeight is not null)
            {
                await JSRuntime.InvokeVoidAsync("monacoMarkdownEditor.setOption", "lineHeight", Options.LineHeight);
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
            
            if (isInitialized_)
            {
                await UpdateCssIntelliSenseAsync();
            }
        }

        private async Task UpdateCssIntelliSenseAsync()
        {
            if (!isInitialized_) return;
            
            try
            {
                await JSRuntime.InvokeVoidAsync(
                    "monacoMarkdownEditor.updateCssClasses",
                    editorId_,
                    ExternalCss ?? "");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MonacoMarkdownEditor] Error updating CSS IntelliSense: {ex.Message}");
            }
        }

        [JSInvokable]
        public async Task OnContentChunk(string chunk, int chunkIndex, int totalChunks)
        {
            if (editorSession_ == null) return;

            if (chunkIndex == 0)
            {
                contentChunks_.Clear();
                expectedTotalChunks_ = totalChunks;
                
                if (!editorSession_.IsModified)
                {
                    editorSession_.SaveState();
                    await InvokeAsync(StateHasChanged);
                }
            }

            contentChunks_.Add(chunk);

            if (contentChunks_.Count == expectedTotalChunks_)
            {
                var newContent = string.Concat(contentChunks_);
                contentChunks_.Clear();
                
                editorSession_.Content = newContent;
                editorSession_.UpdateModifiedState();

                cancellationTokenSource_?.Cancel();
                cancellationTokenSource_?.Dispose();
                cancellationTokenSource_ = new CancellationTokenSource();
                var token = cancellationTokenSource_.Token;

                try
                {
                    await Task.Delay(ThrottleDelayMs, token);
                    
                    await InvokeAsync(() =>
                    {
                        Html = CreateMarkupString();
                        StateHasChanged();
                    });

                    if (MarkdownChanged.HasDelegate)
                    {
                        await MarkdownChanged.InvokeAsync(newContent);
                    }
                }
                catch (TaskCanceledException)
                {
                    // User is still typing
                }
            }
        }

        private static MarkdownPipeline? defaultMarkdownPipeline_;

        private MarkdownPipeline GetMarkdownPipeline()
        {
            if (MarkdownOptions is null)
            {
                return defaultMarkdownPipeline_ ??= CreateLegacyDefaultPipeline();
            }

            return MarkdownPipelineFactory.BuildPipeline(MarkdownOptions);
        }

        private static MarkdownPipeline CreateLegacyDefaultPipeline()
        {
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseGenericAttributes()
                .UseYamlFrontMatter()
                .Build();

            return pipeline;
        }

        private MarkupString CreateMarkupString()
        {
            var content = editorSession_?.Content ?? string.Empty;
            
            if (string.IsNullOrWhiteSpace(content))
            {
                return new MarkupString(string.Empty);
            }

            var html = Markdig.Markdown.ToHtml(content, GetMarkdownPipeline());
            
            if (HtmlSanitizer != null)
            {
                html = HtmlSanitizer.Sanitize(html);
            }

            var cssToApply = BuildCascadedCss();
            var wrapped = $"<div{(Id != null ? $" id=\"{Id}\"" : "")}>";
            
            if (!string.IsNullOrWhiteSpace(cssToApply))
            {
                wrapped += $"<style>{cssToApply}</style>";
            }
            
            wrapped += html + "</div>";
            return new MarkupString(wrapped);
        }

        private string BuildCascadedCss()
        {
            var cssBuilder = new System.Text.StringBuilder();
            
            if (!string.IsNullOrWhiteSpace(ExternalCss))
            {
                cssBuilder.AppendLine("/* External CSS */");
                cssBuilder.AppendLine(CleanCss(ExternalCss));
            }
            
            if (!string.IsNullOrWhiteSpace(AdditionalCss))
            {
                if (cssBuilder.Length > 0)
                    cssBuilder.AppendLine();
                cssBuilder.AppendLine("/* Additional CSS */");
                cssBuilder.AppendLine(CleanCss(AdditionalCss));
            }
            
            return cssBuilder.ToString();
        }

        private static string CleanCss(string css)
        {
            if (string.IsNullOrWhiteSpace(css))
                return string.Empty;

            var cleaned = System.Text.RegularExpressions.Regex.Replace(
                css, 
                @"<\s*style[^>]*>|<\s*/\s*style\s*>", 
                string.Empty, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            return cleaned.Trim();
        }

        public async Task CancelAsync()
        {
            if (editorSession_ != null && isInitialized_)
            {
                editorSession_.CancelEdit();
                
                await JSRuntime.InvokeVoidAsync(
                    "monacoMarkdownEditor.setContent", 
                    editorId_, 
                    editorSession_.Content);
                
                Html = CreateMarkupString();
                StateHasChanged();

                if (OnCancelled.HasDelegate)
                {
                    await OnCancelled.InvokeAsync();
                }
            }
        }

        public void Commit()
        {
            editorSession_?.CommitEdit();
            StateHasChanged();
        }

        public async Task<bool> Undo()
        {
            if (editorSession_?.CanUndo == true && isInitialized_)
            {
                var content = await JSRuntime.InvokeAsync<string>("monacoMarkdownEditor.undo", editorId_);
                
                if (content != null)
                {
                    editorSession_.Undo();
                    editorSession_.Content = content;
                    Html = CreateMarkupString();
                    StateHasChanged();
                    return true;
                }
            }
            
            return false;
        }

        public async Task<bool> Redo()
        {
            if (editorSession_?.CanRedo == true && isInitialized_)
            {
                var content = await JSRuntime.InvokeAsync<string>("monacoMarkdownEditor.redo", editorId_);
                
                if (content != null)
                {
                    editorSession_.Redo();
                    editorSession_.Content = content;
                    Html = CreateMarkupString();
                    StateHasChanged();
                    return true;
                }
            }
            
            return false;
        }

        public async ValueTask DisposeAsync()
        {
            if (isInitialized_ && dotNetRef_ != null)
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("monacoMarkdownEditor.dispose", editorId_);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MonacoMarkdownEditor] Error disposing: {ex.Message}");
                }
                finally
                {
                    dotNetRef_.Dispose();
                    dotNetRef_ = null;
                    isInitialized_ = false;
                }
            }
        }
    }
}
