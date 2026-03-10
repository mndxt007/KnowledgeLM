using System.Timers;
using KnowledgeLM.App.Models.AI;
using Microsoft.AspNetCore.Components;
using Plugin.Maui.Audio;

namespace KnowledgeLM.App.Components.Widgets;

public partial class PodcastPlayer : IAsyncDisposable
{
    [Parameter]
    public Podcast? Podcast { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Inject]
    private IAudioManager AudioManager { get; set; } = default!;

    private IAudioPlayer? audioPlayer;
    private System.Timers.Timer? progressTimer;
    private bool isPlaying = false;
    private double currentTime = 0;
    private double totalDuration = 1;
    private double sliderValue = 0;
    private bool isSeeking = false;
    private bool isPlaybackCompleted = false;
    private bool disposed = false;
    private Podcast? _lastPodcast;

    protected override async Task OnParametersSetAsync()
    {
        if (disposed) return;

        // Auto-play when a new podcast is set
        if (Podcast is not null && Podcast != _lastPodcast
            && !string.IsNullOrWhiteSpace(Podcast.AudioFilePath))
        {
            _lastPodcast = Podcast;
            await LoadAndPlay(Podcast.AudioFilePath);
        }
    }

    private async Task LoadAndPlay(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || disposed) return;

        try
        {
            await InvokeAsync(StateHasChanged);

            await DisposeAudioPlayer();

            if (File.Exists(filePath))
            {
                var fileStream = File.OpenRead(filePath);
                audioPlayer = AudioManager.CreatePlayer(fileStream);
                audioPlayer.PlaybackEnded += OnNativePlaybackEnded;

                totalDuration = audioPlayer.Duration > 0 ? audioPlayer.Duration : 1;
                isPlaybackCompleted = false;
                SetupProgressTimer();

                // Auto-play
                audioPlayer.Play();
                isPlaying = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading audio: {ex.Message}");
        }
        finally
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    private void SetupProgressTimer()
    {
        progressTimer?.Dispose();
        progressTimer = new System.Timers.Timer(250);
        progressTimer.Elapsed += UpdateProgress;
        progressTimer.AutoReset = true;
        progressTimer.Start();
    }

    private void UpdateProgress(object? sender, ElapsedEventArgs e)
    {
        if (disposed || audioPlayer == null || !isPlaying || isSeeking || isPlaybackCompleted)
            return;

        try
        {
            var pos = audioPlayer.CurrentPosition;
            if (audioPlayer.Duration > 0 && totalDuration <= 1)
            {
                InvokeAsync(() =>
                {
                    totalDuration = audioPlayer.Duration;
                    StateHasChanged();
                });
            }

            InvokeAsync(() =>
            {
                currentTime = pos;
                sliderValue = pos;
                StateHasChanged();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Progress timer error: {ex.Message}");
        }
    }

    private void OnNativePlaybackEnded(object? sender, EventArgs e)
    {
        InvokeAsync(() =>
        {
            progressTimer?.Stop();
            isPlaying = false;
            isPlaybackCompleted = true;
            currentTime = totalDuration;
            sliderValue = totalDuration;
            StateHasChanged();
        });
    }

    private async Task TogglePlayPause()
    {
        if (audioPlayer == null || disposed) return;

        if (isPlaying)
        {
            audioPlayer.Pause();
            isPlaying = false;
            progressTimer?.Stop();
        }
        else
        {
            if (isPlaybackCompleted || (audioPlayer.Duration > 0 && currentTime >= audioPlayer.Duration))
            {
                audioPlayer.Seek(0);
                currentTime = 0;
                sliderValue = 0;
                isPlaybackCompleted = false;
            }

            audioPlayer.Play();
            isPlaying = true;
            progressTimer?.Start();
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task OnSeek(double value)
    {
        if (audioPlayer == null || disposed) return;

        isSeeking = true;
        isPlaybackCompleted = false;

        var clampedValue = Math.Clamp(value, 0, totalDuration);
        audioPlayer.Seek(clampedValue);
        currentTime = clampedValue;
        sliderValue = clampedValue;

        await InvokeAsync(StateHasChanged);
        await Task.Delay(50);
        isSeeking = false;
    }

    private async Task Close()
    {
        await DisposeAudioPlayer();
        isPlaying = false;
        currentTime = 0;
        sliderValue = 0;
        totalDuration = 1;
        _lastPodcast = null;
        await OnClose.InvokeAsync();
    }

    private static string FormatTime(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.TotalHours >= 1
            ? ts.ToString(@"h\:mm\:ss")
            : ts.ToString(@"m\:ss");
    }

    private async Task DisposeAudioPlayer()
    {
        if (audioPlayer != null)
        {
            try
            {
                audioPlayer.PlaybackEnded -= OnNativePlaybackEnded;
                if (isPlaying)
                {
                    audioPlayer.Stop();
                    isPlaying = false;
                }
                audioPlayer.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing audio player: {ex.Message}");
            }
            finally
            {
                audioPlayer = null;
            }
        }

        progressTimer?.Stop();
        progressTimer?.Dispose();
        progressTimer = null;

        currentTime = 0;
        totalDuration = 1;
        sliderValue = 0;
        isPlaybackCompleted = false;

        if (!disposed)
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    public async ValueTask DisposeAsync()
    {
        disposed = true;
        await DisposeAudioPlayer();
    }
}
