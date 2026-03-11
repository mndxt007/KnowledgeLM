namespace KnowledgeLM.App.Services;

/// <summary>
/// Centralized notification service for long-running operations.
/// Services fire notifications; UI components subscribe and display toasts.
/// </summary>
public class AppNotificationService
{
    public event Action<AppNotification>? OnNotification;

    public void NotifyProgress(string message)
        => OnNotification?.Invoke(new AppNotification(AppNotificationType.Progress, message));

    public void NotifySuccess(string message)
        => OnNotification?.Invoke(new AppNotification(AppNotificationType.Success, message));

    public void NotifyError(string message)
        => OnNotification?.Invoke(new AppNotification(AppNotificationType.Error, message));

    public void NotifyInfo(string message)
        => OnNotification?.Invoke(new AppNotification(AppNotificationType.Info, message));
}

public enum AppNotificationType
{
    Info,
    Success,
    Error,
    Progress
}

public record AppNotification(AppNotificationType Type, string Message);
