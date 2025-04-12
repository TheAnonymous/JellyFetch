using System;
using System.Diagnostics;
using System.Globalization; // Namespace seems unused now, consider removing if not needed elsewhere.
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyFetch.Configuration; // Assuming DownloadRequestBody is also in this namespace or its own.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyFetch
{
    /// <summary>
    /// Provides API endpoints for the JellyFetch plugin.
    /// Handles requests for submitting download URLs and serving the submission page.
    /// </summary>
    [ApiController]
    [Route("Plugins/JellyFetch")] // Base route for all endpoints in this controller
    public class JellyFetchService : ControllerBase
    {
        // Constants
        private const string SubmitPageResourcePath = "Jellyfin.Plugin.JellyFetch.Web.submitPage.html";

        private readonly ILogger<JellyFetchService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor; // Used for potential HttpContext access if needed beyond ControllerBase features.

        /// <summary>
        /// Initializes a new instance of the <see cref="JellyFetchService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for logging messages.</param>
        /// <param name="httpContextAccessor">Accessor for the current HTTP context.</param>
        public JellyFetchService(
            ILogger<JellyFetchService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <summary>
        /// Accepts a video URL via POST request and initiates a download using yt-dlp in the background.
        /// Requires authentication via standard Jellyfin API key (X-Emby-Token header).
        /// </summary>
        /// <param name="requestBody">The request body containing the URL to download.</param>
        /// <returns>
        /// Returns HTTP 204 No Content on successful request initiation.
        /// Returns HTTP 400 Bad Request if the URL is missing or invalid.
        /// Returns HTTP 503 Service Unavailable if the plugin instance or configuration is not available or incomplete.
        /// </returns>
        [HttpPost("Download")]
        [Authorize] // Ensures only authenticated users (valid API key) can call this
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status503ServiceUnavailable)]
        public IActionResult PostDownloadRequest([FromBody] DownloadRequestBody requestBody)
        {
            _logger.LogInformation("Processing POST /Download request...");

            // --- Get Configuration via Static Instance ---
            // Accessing configuration through the static Plugin instance.
            // Ensure Plugin.Instance is initialized correctly during plugin startup.
            var config = Plugin.Instance?.Configuration;

            // Validate Plugin instance and its configuration
            if (Plugin.Instance == null || config == null)
            {
                _logger.LogError("Plugin instance or configuration is null. Cannot process download request.");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "Plugin instance or configuration not available.");
            }

            // --- End Configuration Access ---

            // Validate request body and URL
            if (requestBody == null || string.IsNullOrWhiteSpace(requestBody.Url))
            {
                _logger.LogWarning("Download request rejected: Missing or empty URL in request body.");
                return BadRequest("URL is required in the request body.");
            }

            // Validate essential configuration paths
            if (string.IsNullOrWhiteSpace(config.DownloadPath) || string.IsNullOrWhiteSpace(config.YtDlpPath))
            {
                _logger.LogError("Plugin configuration incomplete. DownloadPath='{DownloadPath}', YtDlpPath='{YtDlpPath}'", config.DownloadPath, config.YtDlpPath);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "Plugin configuration is incomplete. Please check DownloadPath and YtDlpPath in JellyFetch settings.");
            }

            // URL is validated non-null/whitespace above, safe to use.
            string urlToDownload = requestBody.Url;
            string downloadPath = config.DownloadPath;
            string ytDlpPath = config.YtDlpPath;

            // --- Initiate Download Asynchronously ---
            _logger.LogInformation("Initiating background download process for URL: {Url}", urlToDownload);
            // Using Task.Run to execute the potentially long-running yt-dlp process
            // without blocking the API request thread ("fire-and-forget").
            _ = Task.Run(() => ExecuteYtDlp(urlToDownload, downloadPath, ytDlpPath));

            _logger.LogInformation("POST /Download request processed, returning NoContent.");
            return NoContent(); // Signal successful request acceptance (download runs in background)
        }

        /// <summary>
        /// Serves the static HTML page used for submitting download URLs.
        /// Allows anonymous access.
        /// </summary>
        /// <returns>
        /// Returns HTTP 200 OK with the HTML content if found.
        /// Returns HTTP 500 Internal Server Error if the embedded resource cannot be loaded.
        /// </returns>
        [HttpGet("SubmitPage")]
        [AllowAnonymous] // Allows access without authentication
        [Produces("text/html")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult GetSubmitPage()
        {
            _logger.LogInformation("Processing GET /SubmitPage request...");
            try
            {
                var assembly = typeof(Plugin).Assembly; // Get the assembly containing the plugin

                _logger.LogDebug("Attempting to load embedded resource: {ResourcePath}", SubmitPageResourcePath);
                using (Stream? resourceStream = assembly.GetManifestResourceStream(SubmitPageResourcePath)) // Resource stream might be null
                {
                    if (resourceStream == null)
                    {
                        _logger.LogError("Could not find embedded resource: {ResourcePath}. Ensure Build Action is 'Embedded Resource' and path is correct.", SubmitPageResourcePath);
                        return StatusCode(StatusCodes.Status500InternalServerError, $"Could not load embedded resource: {SubmitPageResourcePath}.");
                    }

                    using (var reader = new StreamReader(resourceStream))
                    {
                        string htmlContent = reader.ReadToEnd();
                        _logger.LogInformation("Successfully loaded and serving submit page.");
                        return Content(htmlContent, "text/html"); // Return HTML content with correct MIME type
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while serving the submit page.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while serving the page.");
            }
        }

        /// <summary>
        /// Executes the yt-dlp process to download the specified video URL.
        /// This method is designed to be called in a background thread (e.g., via Task.Run).
        /// </summary>
        /// <param name="videoUrl">The URL of the video to download (assumed non-null/whitespace).</param>
        /// <param name="downloadPath">The directory path where the video should be saved.</param>
        /// <param name="ytDlpPath">The path to the yt-dlp executable.</param>
        private void ExecuteYtDlp(string videoUrl, string downloadPath, string ytDlpPath)
        {
            // Note: This method runs in a background thread.
            _logger.LogInformation("[Background Task] Starting yt-dlp execution for URL: {Url}", videoUrl);
            _logger.LogDebug("[Background Task] Using DownloadPath: {DownloadPath}, YtDlpPath: {YtDlpPath}", downloadPath, ytDlpPath);

            Process? process = null; // Declare outside try for logging in finally if needed
            try
            {
                // Ensure the download directory exists before starting the process
                if (!Directory.Exists(downloadPath))
                {
                    _logger.LogInformation("[Background Task] Download directory '{DownloadPath}' does not exist. Attempting to create it.", downloadPath);
                    Directory.CreateDirectory(downloadPath);
                    _logger.LogInformation("[Background Task] Successfully created directory '{DownloadPath}'.", downloadPath);
                }

                // Define output template and arguments for yt-dlp
                // Example: Saves as "Video Title [VideoID].ext"
                // Consider making the output template configurable in future versions.
                string outputTemplate = Path.Combine(downloadPath, "%(title)s [%(id)s].%(ext)s");
                // Ensure arguments containing paths/URLs are quoted correctly.
                // Added --no-progress for cleaner logs, --no-warnings as common warnings are often ignorable. Adjust as needed.
                string arguments = $"-o \"{outputTemplate}\" --no-continue --no-overwrites --no-progress --no-warnings \"{videoUrl}\"";

                _logger.LogInformation("[Background Task] Preparing to execute yt-dlp...");
                _logger.LogDebug("[Background Task] Command: \"{YtDlpPath}\" {Arguments}", ytDlpPath, arguments);

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = ytDlpPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true, // Capture stdout
                    RedirectStandardError = true,  // Capture stderr
                    UseShellExecute = false,     // Required for redirection
                    CreateNoWindow = true,         // Don't show a console window
                    // Set encoding if needed, though ReadToEnd() often handles defaults reasonably.
                    // StandardOutputEncoding = System.Text.Encoding.UTF8,
                    // StandardErrorEncoding = System.Text.Encoding.UTF8,
                };

                process = Process.Start(startInfo); // Start the external process

                if (process == null)
                {
                    // This case is rare but possible if process start fails immediately
                    _logger.LogError("[Background Task] Failed to start yt-dlp process for URL: {Url}. Process.Start returned null.", videoUrl);
                    return; // Exit the method
                }

                _logger.LogInformation("[Background Task] yt-dlp process started (PID: {ProcessId}) for URL: {Url}. Waiting for exit...", process.Id, videoUrl);

                // Reading output *after* waiting avoids potential deadlocks if output buffers fill up on long-running processes.
                // For extremely verbose downloads, async reading might be necessary.
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit(); // Wait for the process to complete

                // Log process completion status and any output/errors
                if (process.ExitCode == 0)
                {
                    _logger.LogInformation("[Background Task] yt-dlp finished successfully for URL: {Url}. Exit Code: {ExitCode}", videoUrl, process.ExitCode);
                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        // Log standard output at debug level on success
                        _logger.LogDebug("[Background Task] yt-dlp Output:\n{Output}", output);
                    }
                }
                else
                {
                    _logger.LogError("[Background Task] yt-dlp failed for URL: {Url}. Exit Code: {ExitCode}", videoUrl, process.ExitCode);
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        // Log standard error at error level on failure
                        _logger.LogError("[Background Task] yt-dlp Error Output:\n{Error}", error);
                    }

                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        // Log standard output at debug level even on failure for context
                        _logger.LogDebug("[Background Task] yt-dlp Standard Output (during error):\n{Output}", output);
                    }
                }
            }
            catch (Exception ex)
            {
                // Catch errors during process setup (e.g., Directory.CreateDirectory, Process.Start) or execution
                _logger.LogError(ex, "[Background Task] An unexpected error occurred while preparing or executing yt-dlp for URL: {Url}", videoUrl);
            }
            finally
            {
                // Ensure process resources are potentially released if needed, though 'using' handles the Process object itself.
                if (process != null && !process.HasExited)
                {
                    // Optional: Attempt to kill runaway process? Risky.
                    _logger.LogWarning("[Background Task] yt-dlp process (PID: {ProcessId}) may not have exited properly after WaitForExit.", process.Id);
                }

                _logger.LogInformation("[Background Task] Finished execution attempt for URL: {Url}", videoUrl);
            }
        }
    }
}
