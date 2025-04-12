using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.JellyFetch.Configuration; // Provides PluginConfiguration type
using MediaBrowser.Common.Configuration; // Provides IApplicationPaths
using MediaBrowser.Common.Plugins; // Provides BasePlugin
using MediaBrowser.Model.Plugins; // Provides IHasWebPages, PluginPageInfo
using MediaBrowser.Model.Serialization; // Provides IXmlSerializer

namespace Jellyfin.Plugin.JellyFetch
{
    /// <summary>
    /// The main entry point for the JellyFetch plugin.
    /// Inherits from BasePlugin for configuration handling and implements IHasWebPages to register configuration pages.
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        // Constants for easy updates and consistency
        private const string PluginName = "JellyFetch";
        private const string PluginGuid = "a8ea81c2-b9a1-499e-ac3d-d9e7a780f6fa";

        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin"/> class.
        /// Note: This constructor signature is required by the BasePlugin class for configuration loading via older mechanisms.
        /// It also sets the static Instance property for easy access from other components like services.
        /// </summary>
        /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface, provided by Jellyfin.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface, provided by Jellyfin.</param>
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer) // Calls the base class constructor for configuration management
        {
            Instance = this; // Set the static instance reference
        }

        /// <inheritdoc />
        public override string Name => PluginName; // Returns the plugin's display name

        /// <inheritdoc />
        public override Guid Id => Guid.Parse(PluginGuid); // Returns the plugin's unique identifier

        /// <summary>
        /// Gets the singleton instance of the plugin.
        /// Used to access the plugin instance (and its configuration) from other parts of the code, like API services.
        /// It's nullable (`Plugin?`) because it's only set after the constructor runs.
        /// </summary>
        public static Plugin? Instance { get; private set; }

        /// <summary>
        /// Gets the configuration pages for this plugin.
        /// This method is called by Jellyfin to discover pages to display in the dashboard.
        /// </summary>
        /// <returns>An enumeration of <see cref="PluginPageInfo"/> objects representing the configuration pages.</returns>
        public IEnumerable<PluginPageInfo> GetPages()
        {
            // Using C# collection expression syntax
            return
            [
                new PluginPageInfo
                {
                    // Name: An internal identifier for this specific page.
                    Name = "JellyFetchConfigurationPage",

                    // EmbeddedResourcePath: The full path to the embedded HTML resource file.
                    // Dynamically constructed using the plugin's namespace and the file location.
                    // Assumes configPage.html is in a 'Configuration' folder marked as Embedded Resource.
                    EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace),

                    // DisplayName: The text displayed in the dashboard menu link.
                    DisplayName = Name, // Uses the plugin's Name property ("JellyFetch")

                    // MenuSection: The section in the dashboard sidebar where the link appears.
                    MenuSection = "Plugins", // Standard section for plugins

                    // MenuIcon: The Material Design icon name to display next to the link.
                    MenuIcon = "cloud_download" // Represents downloading
                }

                // Add more PluginPageInfo objects here if the plugin had multiple pages
            ];
        }
    }
}
