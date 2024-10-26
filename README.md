# ClipGitter

A command-line tool for Windows that automatically syncs your clipboard content with a Git repository. It monitors your clipboard for changes and saves the content to a specified Git repository, either preserving history or maintaining only the latest version.

## Features

- Automatic clipboard monitoring from the command line
- Two operating modes: History (default) and Non-History
- Configurable polling interval for remote changes
- Automatic Git operations (commit, push, pull)
- Supports authentication using username/password from a .env file

## Prerequisites

- Windows operating system
- Git installed and configured
- A Git repository with push access

Note: .NET runtime is not required as the application is published as a self-contained executable.

## Git Configuration

Before running the application, ensure Git is properly configured:

```powershell
# Configure Git user information
git config user.name "Your Name"
git config user.email "your.email@example.com"
```

## .env File Configuration

To use username/password authentication, create a `.env` file in your project directory or any location you prefer. The file should contain your credentials in the following format:

```
USERNAME=your_username
PASSWORD=your_password_or_personal_access_token
```

**Important:** Replace `your_github_username` and `your_github_password_or_personal_access_token` with your actual GitHub username and password or personal access token. Using a personal access token is recommended for security.

## Building from Source

### Requirements for Building
- .NET 8.0 SDK
- Git installed

### Build Steps

1. Clone the repository
2. Run the publish command:
   ```bash
   dotnet publish ClipGitter.csproj -c Release -r win-x64
   ```
3. The executable `ClipGitter.exe` will be created in:
   ```
   bin/Release/net8.0/win-x64/publish/ClipGitter.exe
   ```

Important: Make sure to use the executable from the `publish` directory, as this contains all required dependencies.

## Usage

Run the application from the command line with the following syntax:

```powershell
.\ClipGitter.exe --repo <repository-path> [--poll-interval <seconds>] [--no-history] [--env-file <path_to_env_file>]
```

### Command Line Arguments

- `--repo` (Required): Path to the Git repository where clipboard content will be saved.
- `--poll-interval` (Optional): How often to check for remote changes, in seconds (default: 30).
- `--no-history`: Disable history mode (default: history mode enabled).
- `--env-file` (Optional): Path to the `.env` file containing your GitHub credentials.

### Examples

1. Basic usage with default settings:
   ```powershell
   .\ClipGitter.exe --repo "C:\Projects\my-clipboard-repo"
   ```

2. Non-history mode with custom polling interval and .env file:
   ```powershell
   .\ClipGitter.exe --repo "C:\Projects\my-clipboard-repo" --poll-interval 45 --no-history --env-file "C:\path\to\your\.env\file"
   ```

## Operating Modes

### History Mode (Default)
- Each clipboard change creates a new file with timestamp
- Example: `clipboard_20240126_123456.txt`
- All changes are preserved in Git history
- Normal Git push operations

### Non-History Mode
- All clipboard content saved to `clip.txt`
- File is overwritten with each new clipboard content
- Git history is not preserved (uses commit amend)
- Force pushes to keep repository clean

## Error Handling

- Application logs errors to the console
- Automatic retry for transient failures
- Graceful handling of Git operation failures

## Notes

- The application requires write access to the specified Git repository
- Repository should have a remote named 'origin' configured
- Application runs in the console and can be terminated using Ctrl+C

## Development

The source code includes:
- Command line argument parsing using CommandLineParser
- Git operations using LibGit2Sharp
- Cross-platform clipboard access using TextCopy
- Asynchronous operations and error handling
