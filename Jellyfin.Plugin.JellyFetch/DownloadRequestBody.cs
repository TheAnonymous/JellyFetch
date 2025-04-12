namespace Jellyfin.Plugin.JellyFetch
{
    /// <summary>
    /// DTO for the download request body.
    /// </summary>
    public partial class DownloadRequestBody
    {
        /// <summary>
        /// Gets or sets the URL of the video to download.
        /// </summary>
        public string? Url { get; set; } // Nullable gemacht
    }
}
