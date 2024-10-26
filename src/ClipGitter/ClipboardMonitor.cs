using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TextCopy;
using ClipGitter;

namespace ClipGitter;

public class ClipboardMonitor : IDisposable
{
    private readonly ILogger<ClipboardMonitor> _logger;
    private readonly GitManager _gitManager;
    private readonly Options _options;
    private string? _lastClipboardContent;
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;
    private readonly IClipboard _clipboard;

    public ClipboardMonitor(ILogger<ClipboardMonitor> logger, GitManager gitManager, Options options)
    {
        _logger = logger;
        _gitManager = gitManager;
        _options = options;
        _clipboard = new Clipboard();
    }

    public async Task StartMonitoringAsync()
    {
        try
        {
            // Start polling for clipboard changes
            _ = Task.Run(MonitorClipboardAsync);

            // Start polling for git changes
            _ = Task.Run(PollGitChangesAsync);

            // Keep the application running
            await Task.Delay(-1, _cts.Token);
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("Monitoring stopped");
        }
    }

    private async Task MonitorClipboardAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var clipboardText = await _clipboard.GetTextAsync();

                if (!string.IsNullOrEmpty(clipboardText) && clipboardText != _lastClipboardContent)
                {
                    _lastClipboardContent = clipboardText;
                    await SaveClipboardContentAsync(clipboardText);
                }

                await Task.Delay(1000, _cts.Token); // Check every second
            }
            catch (Exception ex) when (ex is not TaskCanceledException)
            {
                _logger.LogError(ex, "Error in clipboard monitoring");
                await Task.Delay(5000, _cts.Token); // Wait before retrying
            }
        }
    }

    private async Task SaveClipboardContentAsync(string content)
    {
        try
        {
            bool historyMode = !_options.NoHistory; // Use NoHistory flag

            if (historyMode)
            {
                var filename = $"clipboard_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                await File.WriteAllTextAsync(Path.Combine(_options.RepoPath, filename), content);
                await _gitManager.CommitAndPushAsync(filename, "Add new clipboard content", _options.EnvFilePath);
            }
            else
            {
                var filename = "clip.txt";
                await File.WriteAllTextAsync(Path.Combine(_options.RepoPath, filename), content);
                await _gitManager.CommitAndForcePushAsync(filename, _options.EnvFilePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving clipboard content");
        }
    }

    private async Task PollGitChangesAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                await _gitManager.PullChangesAsync(_options.EnvFilePath);
                await Task.Delay(TimeSpan.FromSeconds(_options.PollInterval), _cts.Token);
            }
            catch (Exception ex) when (ex is not TaskCanceledException)
            {
                _logger.LogError(ex, "Error polling git changes");
                await Task.Delay(5000, _cts.Token); // Wait before retrying
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cts.Cancel();
            _cts.Dispose();
            _disposed = true;
        }
    }
}
