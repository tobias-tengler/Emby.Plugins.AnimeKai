using System.Collections.Generic;

namespace Emby.Plugins.AnimeKai.Providers.AniList
{
    public class Page
    {
        public List<Media> Media { get; set; }
    }

    public class Data
    {
        public Page Page { get; set; }
        public Media Media { get; set; }
    }

    public class Root
    {
        public Data Data { get; set; }
    }
}