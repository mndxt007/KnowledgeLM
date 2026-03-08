namespace KnowledgeLM.MonacoEditor.Memento;

/// <summary>
/// Manages the history of text mementos for undo/redo operations.
/// </summary>
public class TextHistory
{
    private readonly Stack<IMemento> history_ = new();
    private readonly Stack<IMemento> redoStack_ = new();

    public bool CanUndo => history_.Count > 0;
    public bool CanRedo => redoStack_.Count > 0;
    public int HistoryCount => history_.Count;

    public void Save(IMemento memento)
    {
        if (memento == null)
            throw new ArgumentNullException(nameof(memento));

        history_.Push(memento);
        redoStack_.Clear();
    }

    public IMemento? Peek()
    {
        return history_.Count > 0 ? history_.Peek() : null;
    }

    public IMemento? Undo()
    {
        if (history_.Count == 0)
            return null;

        var memento = history_.Pop();
        redoStack_.Push(memento);
        return history_.Count > 0 ? history_.Peek() : null;
    }

    public IMemento? Redo()
    {
        if (redoStack_.Count == 0)
            return null;

        var memento = redoStack_.Pop();
        history_.Push(memento);
        return memento;
    }

    public void Clear()
    {
        history_.Clear();
        redoStack_.Clear();
    }

    public IMemento? GetOriginal()
    {
        return history_.Count > 0 ? history_.ToArray()[^1] : null;
    }
}
