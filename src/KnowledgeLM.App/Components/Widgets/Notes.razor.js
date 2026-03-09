function setupTreeContextMenuStopPropagation() {
    document.querySelectorAll('fluent-tree-item fluent-tree-item').forEach(item => {
        if (!item.__ctxStopProp) {
            item.addEventListener('contextmenu', (e) => {
                e.stopPropagation();
            });
            item.__ctxStopProp = true;
        }
    });
}
