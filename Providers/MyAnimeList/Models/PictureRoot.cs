using System.Collections.Generic;

namespace Emby.Plugins.AnimeKai.Providers.MyAnimeList
{
    public class Picture
    {
        public string Small { get; set; }
        public string Large { get; set; }
    }

    public class PictureRoot
    {
        public List<Picture> Pictures { get; set; }
    }
}