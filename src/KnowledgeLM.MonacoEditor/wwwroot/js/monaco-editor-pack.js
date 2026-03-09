// Monaco Editor Pack - Single script loader for Blazor apps
// Loads all required Monaco Editor Pack scripts in the correct order
// Vendored from: https://github.com/gerfen/FluentUI.Blazor.Monaco.EditorPack (MIT)

(function() {
    'use strict';
    
    function getBasePath() {
        const baseElement = document.querySelector('base');
        const baseHref = baseElement ? baseElement.getAttribute('href') : '/';
        
        if (baseHref === '/') {
            return '/_content/KnowledgeLM.MonacoEditor/js/';
        }
        
        return '_content/KnowledgeLM.MonacoEditor/js/';
    }
    
    const basePath = getBasePath();
    console.log('[Monaco Editor Pack] Using base path:', basePath);
    
    // Only markdown-related scripts (CSS editor removed)
    const scripts = [
        'fluentUIDesignTokens.js',
        'cssClassHarvester.js',
        'monacoMarkdownEditor.js',
        'monacoMarkdownToolbar.js'
    ];
    
    let loadedCount = 0;
    
    function loadScript(index) {
        if (index >= scripts.length) {
            console.log(`[Monaco Editor Pack] All ${loadedCount} scripts loaded successfully`);
            window.dispatchEvent(new CustomEvent('monaco-editor-pack-loaded', {
                detail: {
                    scriptsLoaded: loadedCount,
                    totalScripts: scripts.length
                }
            }));
            return;
        }
        
        const script = document.createElement('script');
        script.src = basePath + scripts[index];
        script.async = false;
        
        script.onload = () => {
            loadedCount++;
            console.log(`[Monaco Editor Pack] Loaded ${scripts[index]} (${loadedCount}/${scripts.length})`);
            loadScript(index + 1);
        };
        
        script.onerror = () => {
            console.error(`[Monaco Editor Pack] Failed to load: ${scripts[index]}`);
            loadScript(index + 1);
        };
        
        document.head.appendChild(script);
    }
    
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => {
            console.log('[Monaco Editor Pack] Starting script loading...');
            loadScript(0);
        });
    } else {
        console.log('[Monaco Editor Pack] Starting script loading...');
        loadScript(0);
    }
})();
