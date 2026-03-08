// Fluent UI Design Tokens for Monaco CSS Editor IntelliSense
// Dynamically harvested from the browser's computed styles
// https://github.com/microsoft/fluentui-blazor

window.fluentUIDesignTokens = {
    _harvestedTokens: null,
    _harvestInProgress: false,

    async harvestTokens() {
        if (this._harvestInProgress) {
            console.log('[FluentUI Tokens] Harvest already in progress, waiting...');
            while (this._harvestInProgress) {
                await new Promise(resolve => setTimeout(resolve, 100));
            }
            return this._harvestedTokens;
        }

        if (this._harvestedTokens) {
            console.log('[FluentUI Tokens] Using cached tokens:', this._harvestedTokens.length);
            return this._harvestedTokens;
        }

        this._harvestInProgress = true;
        console.log('[FluentUI Tokens] Starting harvest from DOM...');

        try {
            const tokens = new Map();
            const rootStyles = getComputedStyle(document.documentElement);
            const bodyStyles = getComputedStyle(document.body);
            
            const styleSources = [
                { styles: rootStyles, element: 'document root' },
                { styles: bodyStyles, element: 'body' }
            ];

            for (const { styles, element } of styleSources) {
                for (let i = 0; i < styles.length; i++) {
                    const propName = styles[i];
                    
                    if (propName.startsWith('--')) {
                        const propValue = styles.getPropertyValue(propName).trim();
                        
                        if (!propValue || tokens.has(propName)) {
                            continue;
                        }

                        const type = this._inferTokenType(propName, propValue);
                        const category = this._categorizeToken(propName);
                        const description = this._generateDescription(propName, category);

                        tokens.set(propName, {
                            name: propName,
                            value: propValue,
                            type: type,
                            category: category,
                            description: description
                        });
                    }
                }
            }

            this._harvestedTokens = Array.from(tokens.values()).sort((a, b) => 
                a.name.localeCompare(b.name)
            );

            console.log(`[FluentUI Tokens] Harvested ${this._harvestedTokens.length} tokens from ${styleSources.length} sources`);
            
            return this._harvestedTokens;
        } catch (error) {
            console.error('[FluentUI Tokens] Harvest failed:', error);
            this._harvestedTokens = [];
            return [];
        } finally {
            this._harvestInProgress = false;
        }
    },

    _inferTokenType(name, value) {
        if (
            /^#[0-9a-f]{3,8}$/i.test(value) ||
            /^rgb\(/.test(value) ||
            /^rgba\(/.test(value) ||
            /^hsl\(/.test(value) ||
            /^hsla\(/.test(value) ||
            name.includes('color') ||
            name.includes('fill') ||
            name.includes('stroke') ||
            name.includes('foreground') ||
            name.includes('background') ||
            name.includes('border')
        ) {
            return 'color';
        }

        if (
            /^\d+(\.\d+)?(px|rem|em|%|vh|vw)$/.test(value) ||
            name.includes('size') ||
            name.includes('height') ||
            name.includes('width') ||
            name.includes('radius') ||
            name.includes('spacing') ||
            name.includes('gap')
        ) {
            return 'length';
        }

        if (name.includes('font-family') || name.includes('font')) {
            return 'font-family';
        }

        if (name.includes('weight')) {
            return 'font-weight';
        }

        if (/^-?\d+(\.\d+)?$/.test(value)) {
            return 'number';
        }

        if (name.includes('shadow') || /^\d+px\s+\d+px/.test(value)) {
            return 'shadow';
        }

        if (/^\d+m?s$/.test(value)) {
            return 'time';
        }

        if (name.includes('direction')) {
            return 'direction';
        }

        return 'value';
    },

    _categorizeToken(name) {
        if (name.includes('neutral')) return 'Neutral Colors';
        if (name.includes('accent')) return 'Accent Colors';
        if (name.includes('font') || name.includes('type-ramp') || name.includes('body-font')) return 'Typography';
        if (name.includes('layer')) return 'Layers';
        if (name.includes('stroke')) return 'Strokes & Borders';
        if (name.includes('fill')) return 'Fills';
        if (name.includes('foreground')) return 'Foreground Colors';
        if (name.includes('focus')) return 'Focus States';
        if (name.includes('elevation') || name.includes('shadow')) return 'Elevation & Shadows';
        if (name.includes('corner') || name.includes('radius')) return 'Border Radius';
        if (name.includes('spacing') || name.includes('gap') || name.includes('design-unit')) return 'Spacing';
        if (name.includes('height') || name.includes('width') || name.includes('multiplier')) return 'Sizing';
        if (name.includes('opacity')) return 'Opacity';
        if (name.includes('direction')) return 'Direction';
        if (name.includes('duration') || name.includes('timing')) return 'Animation';
        if (name.includes('Status') || name.includes('Success') || name.includes('Warning') || name.includes('Danger') || name.includes('Error')) return 'Status Colors';
        
        return 'Other';
    },

    _generateDescription(name, category) {
        const cleanName = name.replace(/^--/, '');
        const words = cleanName.split('-').map(word => 
            word.charAt(0).toUpperCase() + word.slice(1)
        );
        const joinedWords = words.join(' ');
        const categoryPrefix = category ? `[${category}] ` : '';
        return `${categoryPrefix}${joinedWords}`;
    },

    async getAllTokens() {
        if (!this._harvestedTokens) {
            await this.harvestTokens();
        }
        return this._harvestedTokens || [];
    },

    async toMonacoCompletionItems() {
        const tokens = await this.getAllTokens();
        
        return tokens.map(token => ({
            label: token.name,
            kind: monaco.languages.CompletionItemKind.Variable,
            detail: `${token.type} (${token.category})`,
            documentation: {
                value: `**${token.description}**\n\n` +
                       `**Type:** ${token.type}\n\n` +
                       `**Category:** ${token.category}\n\n` +
                       `**Current Value:** \`${token.value}\``,
                isTrusted: true
            },
            insertText: token.name,
            sortText: `${token.category}_${token.name}`
        }));
    },

    async getTokenInfo(tokenName) {
        const tokens = await this.getAllTokens();
        return tokens.find(t => t.name === tokenName);
    },

    async getTokensByCategory(category) {
        const tokens = await this.getAllTokens();
        return tokens.filter(t => t.category === category);
    },

    async getCategories() {
        const tokens = await this.getAllTokens();
        const categories = new Set(tokens.map(t => t.category));
        return Array.from(categories).sort();
    },

    async searchTokens(pattern) {
        const tokens = await this.getAllTokens();
        const regex = new RegExp(pattern, 'i');
        return tokens.filter(t => regex.test(t.name) || regex.test(t.description));
    },

    async refreshTokens() {
        console.log('[FluentUI Tokens] Refreshing tokens...');
        this._harvestedTokens = null;
        this._harvestInProgress = false;
        return await this.harvestTokens();
    },

    async getTokenValue(tokenName) {
        const tokens = await this.getAllTokens();
        const token = tokens.find(t => t.name === tokenName);
        return token ? token.value : null;
    },
    
    async getStats() {
        const tokens = await this.getAllTokens();
        const categories = await this.getCategories();
        
        const stats = {
            total: tokens.length,
            categories: categories.length,
            byCategory: {},
            byType: {}
        };

        for (const token of tokens) {
            stats.byCategory[token.category] = (stats.byCategory[token.category] || 0) + 1;
            stats.byType[token.type] = (stats.byType[token.type] || 0) + 1;
        }

        return stats;
    }
};

console.log('[FluentUI Tokens] Module loaded and ready');
