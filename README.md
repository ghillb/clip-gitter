# ClipGitter

A command-line tool for Windows that automatically syncs your clipboard content with a Git repository. It monitors your clipboard for changes and saves the content to a specified Git repository, either preserving history or maintaining only the latest version.

## Features

- Automatic clipboard monitoring from the command line
- Two operating modes: History (default) and Non-History
- Configurable polling interval for remote changes
- Automatic Git operations (commit, push, pull)
- Supports authentication using username/password from a .env file
- **AES-256 Encryption using PBKDF2HMAC key derivation:** Encrypt clipboard content using a password provided via the `--encryption-pw` argument.
- **Pull-only mode:**  Pull changes from the repository periodically without monitoring the clipboard.


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


## Building from Source

### Requirements for Building
- .NET 8.0 SDK

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

## Usage

Run the application from the command line with the following syntax:

```powershell
.\ClipGitter.exe --repo <repository-path> [--poll-interval <seconds>] [--no-history] [--env-file <path_to_env_file>] [--encryption-pw <password>] [--pull-only]
```

### Command Line Arguments

- `--repo` (Required): Path to the Git repository where clipboard content will be saved.
- `--poll-interval` (Optional): How often to check for remote changes, in seconds (default: 30).
- `--no-history`: Disable history mode (default: history mode enabled).
- `--env-file` (Optional): Path to the `.env` file containing your GitHub credentials.
- `--encryption-pw` (Optional): Password used to encrypt the clipboard content before saving.  If provided, encryption is enabled.
- `--pull-only` (Optional): Only pull changes from the repository; do not monitor the clipboard.


### Examples

1. Basic usage with default settings:
   ```powershell
   .\ClipGitter.exe --repo "C:\...\clipboard-repo"
   ```

2. Non-history mode with custom polling interval and .env file:
   ```powershell
   .\ClipGitter.exe --repo "C:\Projects\my-clipboard-repo" --poll-interval 45 --no-history --env-file "C:\path\to\your\.env\file"
   ```

3. Encryption enabled:
   ```powershell
   .\ClipGitter.exe --repo "C:\...\clipboard-repo" --encryption-pw "MyPassword"
   ```

4. Pull-only mode:
    ```powershell
    .\ClipGitter.exe --repo "C:\...\clipboard-repo" --pull-only
    ```

## Operating Modes

### History Mode (Default)

- Each clipboard change creates a new file with timestamp
- Example: `clipboard_20240126_123456.txt`
- All changes are preserved in Git history
- Normal Git push operations

### Non-History Mode

- All clipboard content saved to `clipboard.txt`
- File is overwritten with each new clipboard content
- Each change creates a new commit

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
- Asynchronous operations and error handling
- Clipboard access using TextCopy library
- AES-256 Encryption
