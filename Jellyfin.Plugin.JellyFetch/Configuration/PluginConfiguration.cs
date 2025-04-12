using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.JellyFetch.Configuration
{
    /// <summary>
    /// Represents the configuration for the JellyFetch plugin.
    /// It stores settings that users can modify in the Jellyfin dashboard.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class
        /// and sets default values.
        /// </summary>
        public PluginConfiguration()
        {
            // Set default values for the configuration options.
            // Users can override these in the Jellyfin dashboard.

            // It's crucial the user sets a valid path here.
            // Providing a specific default might lead to errors if the path doesn't exist or isn't writable.
            // An empty string or a placeholder encourages the user to configure it.
            DownloadPath = "/media/jellyfetch_downloads"; // Example placeholder - User MUST change this

            // Assume yt-dlp is in PATH by default, which is common.
            YtDlpPath = "yt-dlp";
        }

        /// <summary>
        /// Gets or sets the directory path where downloaded videos will be saved.
        /// This path must be writable by the Jellyfin server process.
        /// It should ideally be included in a Jellyfin library for automatic discovery.
        /// </summary>
        public string DownloadPath { get; set; }

        /// <summary>
        /// Gets or sets the path to the yt-dlp executable.
        /// This can be the full path or just "yt-dlp" if it's available in the system's PATH environment variable.
        /// </summary>
        public string YtDlpPath { get; set; }
    }
}
