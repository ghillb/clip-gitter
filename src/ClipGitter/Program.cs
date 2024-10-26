﻿using CommandLine;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using TextCopy;
using System.IO;
using ClipGitter;
using System.Security.Cryptography;

namespace ClipGitter
{
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
                    Console.WriteLine($"History mode: {!options.NoHistory}");
                    Console.WriteLine($"Single-file mode: {options.SingleFile}");
                    Console.WriteLine($"Env file path: {options.EnvFilePath}");
                    Console.WriteLine($"Encryption enabled: {!string.IsNullOrEmpty(options.EncryptionPassword)}");
                    Console.WriteLine("Press Ctrl+C to exit");

                    await monitor.StartMonitoringAsync();
                });
        }
    }
}
