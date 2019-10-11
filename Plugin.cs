using System;
using MediaBrowser.Common.Plugins;

namespace Emby.Plugins.AnimeKai
{
    public class Plugin : BasePlugin
    {
        public const string PluginName = "AnimeKai";

        public override Guid Id => new Guid("ce69a5ea-14b6-44a3-b75a-9d22dd92a7cf");

        public override string Name => PluginName;

        public override string Description => "Improved Metadata Provider, specifically designed for Anime.";
    }
}