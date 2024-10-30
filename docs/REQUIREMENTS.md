# ClipGitter Requirements

**1. Introduction:**

ClipGitter is a Windows command-line application that monitors the system clipboard and synchronizes its contents with a specified Git repository.  The application offers two modes of operation: history mode and non-history mode, and supports authentication via an `.env` file.

**2. Functional Requirements:**

* **Clipboard Monitoring:** Continuously monitor the clipboard for changes.  Upon detecting new content, the application should save the content to the specified Git repository.
* **Git Repository Interaction:** The application must interact with a Git repository to add, commit, and push changes.
* **Operational Modes:**
    * **History Mode (Default):** Each clipboard change creates a new file with a timestamp and a new commit in the Git repository.
    * **Non-History Mode:** Clipboard content is saved to a single file (`clipboard.txt`), overwriting previous content. Each change creates a new commit.
* **Configuration:** The application must be configurable via command-line arguments:
    * `--repo`: Path to the Git repository (required).
    * `--poll-interval`: Polling interval for remote changes (default: 30 seconds, optional).
    * `--history`: Enable/disable history mode (optional, default: true).
    * `--env-file`: Path to the `.env` file containing authentication credentials (optional).
* **Authentication:** The application supports authentication using a `.env` file containing `USERNAME` and `PASSWORD` variables.  If no `.env` file is specified, the application will attempt to use the system's Git credential helper.
* **Error Handling:** The application should gracefully handle errors and provide informative error messages to the user.
* **Polling:** The application should periodically poll the remote repository for changes.

**3. Non-Functional Requirements:**

* **Platform:** Windows.
* **Reliability:** The application should be reliable and robust, handling unexpected errors gracefully.
* **Performance:** The application should perform efficiently, minimizing resource consumption.
* **Usability:** The application should be easy to use and configure via the command line.

**4. .env File Format:**

The `.env` file, if used, should contain the following variables:

```
USERNAME=<username>
PASSWORD=<password_or_personal_access_token>
