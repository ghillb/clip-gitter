using CommandLine;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using TextCopy;
using System.IO;

namespace ClipGitter;

public class Options
{
    [Option('r', "repo", Required = true, HelpText = "Path to the Git repository")]
    public string RepoPath { get; set; } = string.Empty;

    [Option('p', "poll-interval", Default = 30, HelpText = "Polling interval in seconds")]
    public int PollInterval { get; set; }

    [Option('h', "history", HelpText = "Enable history mode")]
    public bool? HistoryMode { get; set; }

    [Option('e', "env-file", HelpText = "Path to the .env file")]
    public string EnvFilePath { get; set; } = string.Empty;
}

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
            bool historyMode = _options.HistoryMode ?? true; // Default to true if not specified

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

public class GitManager
{
    private readonly ILogger<GitManager> _logger;
    private readonly string _repoPath;

    public GitManager(ILogger<GitManager> logger, string repoPath)
    {
        _logger = logger;
        _repoPath = repoPath;
    }

    private UsernamePasswordCredentials GetCredentials(string envFilePath)
    {
        string username = "";
        string password = "";

        try
        {
            foreach (string line in File.ReadAllLines(envFilePath))
            {
                if (line.StartsWith("USERNAME="))
                {
                    username = line.Substring("USERNAME=".Length);
                }
                else if (line.StartsWith("PASSWORD="))
                {
                    password = line.Substring("PASSWORD=".Length);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading .env file");
        }

        return new UsernamePasswordCredentials { Username = username, Password = password };
    }

    public async Task CommitAndPushAsync(string filename, string message, string envFilePath)
    {
        await Task.Run(() =>
        {
            try
            {
                using var repo = new Repository(_repoPath);
                Commands.Stage(repo, filename);
                var signature = repo.Config.BuildSignature(DateTimeOffset.Now);
                repo.Commit(message, signature, signature);
                var remote = repo.Network.Remotes["origin"];
                if (remote != null)
                {
                    var pushOptions = new PushOptions
                    {
                        CredentialsProvider = (_url, _user, _cred) => GetCredentials(envFilePath)
                    };
                    repo.Network.Push(remote, @"refs/heads/master", pushOptions);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in git operations");
                throw;
            }
        });
    }

    public async Task CommitAndForcePushAsync(string filename, string envFilePath)
    {
        await Task.Run(() =>
        {
            try
            {
                using var repo = new Repository(_repoPath);
                Commands.Stage(repo, filename);
                var signature = repo.Config.BuildSignature(DateTimeOffset.Now);

                // Amend the last commit if it exists, otherwise create a new one
                var message = "Update clipboard content";
                if (repo.Head.Tip != null)
                {
                    var commitOptions = new CommitOptions { AmendPreviousCommit = true };
                    repo.Commit(message, signature, signature, commitOptions);
                }
                else
                {
                    repo.Commit(message, signature, signature);
                }

                var remote = repo.Network.Remotes["origin"];
                if (remote != null)
                {
                    var pushOptions = new PushOptions
                    {
                        CredentialsProvider = (_url, _user, _cred) => GetCredentials(envFilePath)
                    };
                    var refSpec = $"+{repo.Head.CanonicalName}";
                    repo.Network.Push(remote, refSpec, pushOptions);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in git operations");
                throw;
            }
        });
    }

    public async Task PullChangesAsync(string envFilePath)
    {
        await Task.Run(() =>
        {
            try
            {
                using var repo = new Repository(_repoPath);
                if (repo.Head.RemoteName != null)
                {
                    var pullOptions = new PullOptions
                    {
                        FetchOptions = new FetchOptions
                        {
                            CredentialsProvider = (_url, _user, _cred) => GetCredentials(envFilePath)
                        }
                    };

                    var signature = repo.Config.BuildSignature(DateTimeOffset.Now);
                    Commands.Pull(repo, signature, pullOptions);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pulling changes");
                throw;
            }
        });
    }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });

        await Parser.Default.ParseArguments<Options>(args)
            .WithParsedAsync(async options =>
            {
                if (!Directory.Exists(options.RepoPath))
                {
                    Console.WriteLine($"Error: Repository path '{options.RepoPath}' does not exist.");
                    return;
                }

                if (!Directory.Exists(Path.Combine(options.RepoPath, ".git")))
                {
                    Console.WriteLine($"Error: '{options.RepoPath}' is not a git repository.");
                    return;
                }


                var gitLogger = loggerFactory.CreateLogger<GitManager>();
                var monitorLogger = loggerFactory.CreateLogger<ClipboardMonitor>();

                var gitManager = new GitManager(gitLogger, options.RepoPath);
                using var monitor = new ClipboardMonitor(monitorLogger, gitManager, options);

                Console.WriteLine($"Starting clipboard monitor...");
                Console.WriteLine($"Repository: {options.RepoPath}");
                Console.WriteLine($"Poll interval: {options.PollInterval} seconds");
                Console.WriteLine($"History mode: {options.HistoryMode ?? true}");
                Console.WriteLine($"Env file path: {options.EnvFilePath}");
                Console.WriteLine("Press Ctrl+C to exit");

                await monitor.StartMonitoringAsync();
            });
    }
}
