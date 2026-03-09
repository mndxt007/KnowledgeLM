namespace KnowledgeLM.MonacoEditor.Memento;

/// <summary>
/// High-level text editor session that combines the Memento pattern components
/// for easy text editing with rollback capability.
/// </summary>
public class TextEditorSession
{
    private readonly TextOriginator originator_;
    private readonly TextHistory history_;
    private IMemento? originalState_;
    private bool hasUnsavedChanges_ = false;

    public string Content
    {
        get => originator_.Content;
        set => originator_.Content = value;
    }

    public bool CanUndo => history_.CanUndo;
    public bool CanRedo => history_.CanRedo;
    public bool IsModified => hasUnsavedChanges_;

    public TextEditorSession(string? initialContent = null)
    {
        originator_ = new TextOriginator { Content = initialContent ?? string.Empty };
        history_ = new TextHistory();
        BeginEdit();
    }

    public void BeginEdit()
    {
        originalState_ = originator_.Save();
        history_.Clear();
        hasUnsavedChanges_ = false;
    }

    public void SaveState()
    {
        var memento = originator_.Save();
        history_.Save(memento);
        hasUnsavedChanges_ = true;
    }

    public void UpdateModifiedState()
    {
        hasUnsavedChanges_ = originalState_ != null &&
                             originalState_.GetState() != Content;
    }

    public bool Undo()
    {
        var memento = history_.Undo();
        if (memento != null)
        {
            originator_.Restore(memento);
            UpdateModifiedState();
            return true;
        }
        return false;
    }

    public bool Redo()
    {
        var memento = history_.Redo();
        if (memento != null)
        {
            originator_.Restore(memento);
            UpdateModifiedState();
            return true;
        }
        return false;
    }

    public void CancelEdit()
    {
        if (originalState_ != null)
        {
            originator_.Restore(originalState_);
            history_.Clear();
            hasUnsavedChanges_ = false;
        }
    }

    public void CommitEdit()
    {
        originalState_ = originator_.Save();
        history_.Clear();
        hasUnsavedChanges_ = false;
    }

    public void RollbackToOriginal()
    {
        if (originalState_ != null)
        {
            originator_.Restore(originalState_);
            hasUnsavedChanges_ = false;
        }
    }
}
