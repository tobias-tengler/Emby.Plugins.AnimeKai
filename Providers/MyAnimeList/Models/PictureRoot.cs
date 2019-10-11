using System.Collections.Generic;

namespace Emby.Providers.Anime.Providers.MyAnimeList.Models
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