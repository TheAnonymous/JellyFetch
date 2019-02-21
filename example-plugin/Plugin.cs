using System;
using System.Collections.Generic;
using Jellyfin.Plugin.ExamplePlugin.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.ExamplePlugin
{
    public class Plugin : BasePlugin<PluginConfiguration>,
    IHasWebPages
    {
        public override string Name => "ExamplePlugin";
        public override Guid Id => Guid.Parse("eb5d7894-8eef-4b36-aa6f-5d124e828ce1");
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public static Plugin Instance { get; private set; }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = this.Name,
                    EmbeddedResourcePath = string.Format("Jellyfin.Plugin.{0}.Configuration.configPage.html",this.Name)
                }
            };
        }
    }
}