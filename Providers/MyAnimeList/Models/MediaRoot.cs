using System;
using System.Collections.Generic;

namespace Emby.Providers.Anime.Providers.MyAnimeList.Models
{
    public class Genre
    {
        public string Name { get; set; }
    }

    public class Aired
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }

    public class MediaRoot
    {
        public int Mal_Id { get; set; }
        public string Title { get; set; }
        public string Image_Url { get; set; }
        public string Synopsis { get; set; }
        public bool Airing { get; set; }
        public Aired Aired { get; set; }
        public float Score { get; set; }
        public List<Genre> Genres { get; set; }
    }
}