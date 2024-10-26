using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using ClipGitter;

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
