# JellyFetch Plugin for Jellyfin


[![Latest Release](https://img.shields.io/github/v/release/TheAnonymous/JellyFetch)](https://github.com/TheAnonymous/JellyFetch/releases/latest)
[![License](PLACEHOLDER_URL_LICENSE_BADGE)](LICENSE)

JellyFetch is a plugin for the Jellyfin media server that allows you to easily download videos from various web sources directly into your library using the powerful `yt-dlp` command-line tool.

It provides a simple, standalone web interface to submit video URLs. Downloads are processed in the background and saved to a configurable directory, ideally making them readily available in your Jellyfin library after a scan.

## Features

* **Download from Numerous Sites:** Leverages `yt-dlp` to support downloading from hundreds of websites. See the full list [here](https://github.com/yt-dlp/yt-dlp/blob/master/supportedsites.md).
* **Simple Submission UI:** Provides a dedicated web page (served by the plugin) to paste video URLs and initiate downloads.
* **Configurable Download Location:** Set a specific directory on your server where downloaded files should be saved.
* **Configurable `yt-dlp` Path:** Specify the exact path to your `yt-dlp` executable or just use the command name if it's in your system's PATH.
* **Secure Submission:** Uses standard Jellyfin API Keys for authenticating download requests via the web UI.
* **Background Processing:** Downloads run as background tasks, allowing you to submit multiple URLs.

## Requirements

* **Jellyfin Server:** Version 10.9.0 or later (adjust `targetAbi` in `manifest.json` if targeting a different version).
* **`yt-dlp`:** The `yt-dlp` command-line tool **must be installed** on the same machine as your Jellyfin server. You can find installation instructions [here](https://github.com/yt-dlp/yt-dlp#installation).
* **`ffmpeg` (Highly Recommended):** `yt-dlp` often requires `ffmpeg` to merge separate video and audio streams (common on sites like YouTube) or for format conversions. It's strongly recommended to install `ffmpeg` alongside `yt-dlp`.
* **Network Access:** Your Jellyfin server needs outbound internet access to download videos from source websites.
* **Permissions:** The user account running the Jellyfin server process must have **write permissions** to the configured download directory.

### Installation Recommendation (`yt-dlp` + `ffmpeg`)

* **Linux (using Python PIP):** `python3 -m pip install -U yt-dlp` and use your distribution's package manager for `ffmpeg` (e.g., `sudo apt update && sudo apt install ffmpeg` on Debian/Ubuntu).
* **Windows (using Chocolatey):** Open PowerShell as Administrator and run:
    ```powershell
    choco install yt-dlp ffmpeg
    ```
* **Other:** Refer to the `yt-dlp` and `ffmpeg` documentation for other operating systems and installation methods.

## Installation

There are two methods to install the JellyFetch plugin:

**Method 1: Using the Jellyfin Plugin Catalog (Recommended)**

1.  Go to your Jellyfin Dashboard -> Administration -> Catalog.
2.  Click the '+' button to add a repository.
3.  Enter the following URL:
    ```
    [https://raw.githubusercontent.com/TheAnonymous/JellyFetch/main/manifest.json](https://raw.githubusercontent.com/TheAnonymous/JellyFetch/main/manifest.json)
    ```
    *(Note: Using the `main` branch URL directly means you always get the latest commit's manifest, which might not correspond to a stable release. Ideally, link to a manifest associated with a specific release tag).*
4.  Click "Save".
5.  Find "JellyFetch" in the catalog list.
6.  Click on it and select the desired version to install.
7.  **Restart your Jellyfin server** to load the plugin.

**Method 2: Manual Installation**

1.  Go to the [Releases page](https://github.com/TheAnonymous/JellyFetch/releases) of the JellyFetch repository.
2.  Download the `.zip` file for the desired release version (e.g., `Jellyfin.Plugin.JellyFetch_1.0.0.0.zip`).
3.  Extract the contents of the `.zip` file. You should have at least `Jellyfin.Plugin.JellyFetch.dll`.
4.  Create a folder named `JellyFetch` inside your Jellyfin server's plugin directory (usually located at `/config/plugins/` or `/var/lib/jellyfin/plugins/` on Linux, or `C:\ProgramData\Jellyfin\Server\plugins\` on Windows).
5.  Copy the extracted files (e.g., the `.dll`) into the newly created `JellyFetch` folder.
6.  **Restart your Jellyfin server** to load the plugin.

## Configuration

After installing and restarting Jellyfin:

1.  Go to your Jellyfin Dashboard -> Administration -> Plugins.
2.  Find the "JellyFetch" plugin in the list. Click on it to open the configuration page.
3.  **Download Directory:** Enter the full path to the directory where you want downloaded videos to be saved (e.g., `/data/downloads/jellyfetch` or `D:\JellyfinDownloads\JellyFetch`). **Ensure the Jellyfin server process has write permissions here!** It's recommended to use a directory monitored by a Jellyfin library.
4.  **Path to yt-dlp:** Enter the name or full path of the `yt-dlp` executable (e.g., `yt-dlp` or `/usr/local/bin/yt-dlp` or `C:\tools\yt-dlp\yt-dlp.exe`).
5.  Click **Save**.

## Usage

1.  Navigate to the plugin's configuration page (Dashboard -> Plugins -> JellyFetch).
2.  Click the link "Open URL Submission Page (New Tab)". This will open the dedicated web interface.
3.  On the submission page:
    * Enter the full **Video URL** you want to download.
    * Enter a valid **Jellyfin API Key**. You can generate API keys in your Jellyfin Dashboard under Administration -> API Keys.
    * Optionally, check the box to remember the API key in your browser (use with caution, as this is insecure).
4.  Click **Start Download**.
5.  You should see a confirmation message if the request was sent successfully. The download will proceed in the background on the server. Monitor the Jellyfin logs or your download directory for progress/completion.

### Concurrent Downloads

Submitting multiple URLs quickly will likely result in **parallel downloads**, each handled by a separate `yt-dlp` process. This can consume significant server resources (CPU, RAM, network). Be mindful of your server's capacity and potential throttling from video source websites.

## Troubleshooting

* **Plugin not visible after installation:** Ensure you restarted the Jellyfin server. Check the Jellyfin logs for loading errors during startup.
* **Configuration page not loading:** Check Jellyfin logs for errors. Check browser developer console (F12) for network or JavaScript errors when clicking the plugin link. Clear browser cache. Ensure the plugin DLL was copied correctly.
* **Submit Page gives 500 error:** Check Jellyfin logs for `InvalidOperationException` or other server-side errors when accessing the `/Plugins/JellyFetch/SubmitPage` URL.
* **Submit Page gives 404 error when submitting:** Ensure the JavaScript on the submit page points to the correct endpoint (`/Plugins/JellyFetch/Download`). Rebuild the plugin if you modified `submitPage.html`.
* **Downloads fail or don't start:**
    * Check Jellyfin server logs *after* submitting a URL. Look for errors mentioning "JellyFetch", "yt-dlp", "ExecuteYtDlp", or file/permission issues.
    * Verify the "Path to yt-dlp" in the plugin configuration is correct and the executable works.
    * Verify the Jellyfin server process has write permissions to the configured "Download Directory".
    * Test `yt-dlp` manually from your server's command line with the same URL to rule out issues with `yt-dlp` itself or the specific video source.

## Contributing

Contributions are welcome! Please feel free to open an issue on GitHub for bug reports or feature requests. Pull requests are also appreciated.

## License

This project is licensed under the [GPL3] License - see the [LICENSE](LICENSE) file for details.

## Acknowledgements

* This plugin heavily relies on the fantastic work of the [yt-dlp team](https://github.com/yt-dlp/yt-dlp).
* The Jellyfin team and community.
