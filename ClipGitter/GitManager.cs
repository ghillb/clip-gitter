using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using ClipGitter;
using System.Text;
using System.Collections.Generic;

namespace ClipGitter;

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
            if (File.Exists(envFilePath))
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
            else
            {
                _logger.LogWarning(".env file not found. Using default credentials.");
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
                _logger.LogInformation($"Staging file: {filename}");
                Commands.Stage(repo, filename);
                var signature = repo.Config.BuildSignature(DateTimeOffset.Now);
                repo.Commit(message, signature, signature);
                _logger.LogInformation($"Committed changes: {message}");
                var remote = repo.Network.Remotes["origin"];
                if (remote != null)
                {
                    var pushOptions = new PushOptions
                    {
                        CredentialsProvider = (_url, _user, _cred) => GetCredentials(envFilePath)
                    };
                    repo.Network.Push(remote, @"refs/heads/master", pushOptions);
                    _logger.LogInformation($"Pushed changes to remote: {remote.Name}");
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
                _logger.LogInformation($"Staging file: {filename}");
                Commands.Stage(repo, filename);
                var signature = repo.Config.BuildSignature(DateTimeOffset.Now);
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
                _logger.LogInformation($"Committed changes: {message}");
                var remote = repo.Network.Remotes["origin"];
                if (remote != null)
                {
                    var pushOptions = new PushOptions
                    {
                        CredentialsProvider = (_url, _user, _cred) => GetCredentials(envFilePath)
                    };
                    var refSpec = $"+{repo.Head.CanonicalName}";
                    repo.Network.Push(remote, refSpec, pushOptions);
                    _logger.LogInformation($"Force pushed changes to remote: {remote.Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in git operations");
                throw;
            }
        });
    }

    public async Task<string?> PullChangesAsync(string envFilePath, string encryptionPassword)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var repo = new Repository(_repoPath);
                if (repo.Head.RemoteName != null)
                {
                    var oldTip = repo.Head.Tip;
                    var pullOptions = new PullOptions
                    {
                        FetchOptions = new FetchOptions
                        {
                            CredentialsProvider = (_url, _user, _cred) => GetCredentials(envFilePath)
                        }
                    };
                    var signature = repo.Config.BuildSignature(DateTimeOffset.Now);
                    var result = Commands.Pull(repo, signature, pullOptions);
                    var newTip = repo.Head.Tip;

                    if (oldTip.Sha != newTip.Sha)
                    {
                        _logger.LogInformation($"Pulled changes from remote: {repo.Head.RemoteName}");
                        foreach (var change in repo.Diff.Compare<TreeChanges>(oldTip?.Tree, newTip?.Tree))
                        {
                            if (change.Status == ChangeKind.Added || change.Status == ChangeKind.Modified)
                            {
                                try
                                {
                                    var content = File.ReadAllText(Path.Combine(_repoPath, change.Path));
                                    if (!string.IsNullOrEmpty(encryptionPassword))
                                    {
                                        try
                                        {
                                            content = EncryptionManager.Decrypt(content, encryptionPassword);
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogError(ex, $"Error decrypting pulled content for {change.Path}");
                                            continue;
                                        }
                                    }
                                    _logger.LogInformation($"📋Pulled content for {change.Path}:\n{content}");
                                    return content;
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Error reading pulled content for {change.Path}");
                                }
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pulling changes");
                throw;
            }
        });
    }
}
