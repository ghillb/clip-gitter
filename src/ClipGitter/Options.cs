using CommandLine;

namespace ClipGitter;

public class Options
{
    [Option('r', "repo", Required = true, HelpText = "Path to the Git repository")]
    public string RepoPath { get; set; } = string.Empty;

    [Option('p', "poll-interval", Default = 30, HelpText = "Polling interval in seconds")]
    public int PollInterval { get; set; }

    [Option('n', "no-history", HelpText = "Disable history mode")]
    public bool NoHistory { get; set; }

    [Option('e', "env-file", HelpText = "Path to the .env file")]
    public string EnvFilePath { get; set; } = string.Empty;
}