// CSS Class Harvester for Monaco IntelliSense
// Parses CSS text and extracts class names for autocomplete

window.cssClassHarvester = {
    classes: new Map(),
    
    parseCssText: function(cssText, source) {
        this.classes.clear();
        
        if (!cssText || typeof cssText !== 'string') {
            return;
        }
        
        const cleanCss = cssText.replace(/\/\*[\s\S]*?\*\//g, '');
        
        const rulePattern = /([^{}]+)\{([^{}]*)\}/g;
        let match;
        
        while ((match = rulePattern.exec(cleanCss)) !== null) {
            const selectorText = match[1].trim();
            const propertiesText = match[2].trim();
            
            const classMatches = selectorText.matchAll(/\.([a-zA-Z0-9_-]+)/g);
            
            for (const classMatch of classMatches) {
                const className = classMatch[1];
                const properties = this.parseProperties(propertiesText);
                
                this.classes.set(className, {
                    className: className,
                    selector: selectorText,
                    source: source,
                    properties: properties
                });
            }
        }
        
        console.log(`[CssHarvester] Parsed ${this.classes.size} CSS classes from ${source}`);
    },
    
    parseProperties: function(propertiesText) {
        if (!propertiesText) return [];
        
        const props = [];
        const propPattern = /([a-zA-Z-]+)\s*:\s*([^;]+)/g;
        let match;
        
        while ((match = propPattern.exec(propertiesText)) !== null) {
            props.push(`${match[1].trim()}: ${match[2].trim()}`);
            if (props.length >= 5) break;
        }
        
        return props;
    },
    
    updateFromSources: function(cssContent) {
        this.classes.clear();
        
        if (cssContent.global) {
            this.parseCssText(cssContent.global, 'Global CSS');
        }
        if (cssContent.page) {
            this.parseCssText(cssContent.page, 'Page CSS');
        }
        if (cssContent.project) {
            this.parseCssText(cssContent.project, 'Project CSS');
        }
        if (cssContent.market) {
            this.parseCssText(cssContent.market, 'Market CSS');
        }
        
        console.log(`[CssHarvester] Parsed ${this.classes.size} CSS classes from ${Object.keys(cssContent).length} sources`);
    },
    
    getClassNames: function() {
        const uniqueClasses = new Set();
        this.classes.forEach((info, key) => {
            uniqueClasses.add(info.className);
        });
        return Array.from(uniqueClasses);
    },
    
    getClassInfo: function(className) {
        return this.classes.get(className) || null;
    },
    
    toMonacoCompletionItems: function() {
        const items = [];
        
        this.classes.forEach((info, className) => {
            items.push({
                label: info.className,
                kind: monaco.languages.CompletionItemKind.Class,
                detail: `CSS Class (${info.source})`,
                documentation: {
                    value: this.formatDocumentation(info)
                },
                insertText: info.className,
                sortText: 'a' + info.className
            });
        });
        
        return items;
    },
    
    formatDocumentation: function(classInfo) {
        if (!classInfo) {
            return '';
        }
        
        let doc = `**Source:** ${classInfo.source}\n\n`;
        doc += `**Selector:** \`${classInfo.selector}\`\n\n`;
        
        if (classInfo.properties && classInfo.properties.length > 0) {
            doc += `**Properties:**\n\n`;
            classInfo.properties.forEach(prop => {
                const propMatch = prop.match(/^([^:]+):\s*(.+)$/);
                if (!propMatch) {
                    doc += `- \`${prop}\`\n`;
                    return;
                }
                
                const propName = propMatch[1].trim();
                const propValue = propMatch[2].trim();
                
                const colorMatch = propValue.match(/^(#[0-9a-f]{3,8}|rgba?\([^)]+\)|hsla?\([^)]+\)|var\(--[^)]+\))$/i);
                
                if (colorMatch) {
                    let colorValue = colorMatch[0];
                    
                    if (colorValue.startsWith('var(')) {
                        const varName = colorValue.match(/var\(\s*(--[\w-]+)\s*\)/);
                        if (varName && window.fluentUIDesignTokens && window.fluentUIDesignTokens._harvestedTokens) {
                            const token = window.fluentUIDesignTokens._harvestedTokens.find(t => t.name === varName[1]);
                            if (token && token.value) {
                                const resolvedColor = token.value;
                                doc += `- **${propName}:** \`${colorValue}\` → \`${resolvedColor}\`\n`;
                                return;
                            }
                        }
                        doc += `- **${propName}:** \`${colorValue}\` (CSS variable)\n`;
                    } else {
                        doc += `- **${propName}:** \`${colorValue}\`\n`;
                    }
                } else {
                    doc += `- **${propName}:** \`${propValue}\`\n`;
                }
            });
        }
        
        return doc;
    },
    
    getColorEmoji: function(colorValue) {
        return '';
    },
    
    getStats: function() {
        return {
            total: this.classes.size,
            classes: this.getClassNames(),
            sampleClasses: this.getClassNames().slice(0, 10)
        };
    }
};

console.log('[CssHarvester] Module loaded');
