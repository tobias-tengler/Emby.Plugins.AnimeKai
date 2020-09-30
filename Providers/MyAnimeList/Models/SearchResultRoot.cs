using System;
using System.Collections.Generic;

namespace Emby.Plugins.AnimeKai.Providers.MyAnimeList
{
    public class SearchResult
    {
        public int Mal_Id { get; set; }
        public string Title { get; set; }
        public string Image_Url { get; set; }
        public string Synopsis { get; set; }
        public bool Airing { get; set; }
        public DateTime Start_Date { get; set; }
        public DateTime End_Date { get; set; }
        public float Score { get; set; }

    }

    public class SearchResultRoot
    {
        public SearchResultRoot()
        {
            Results = new List<SearchResult>();
        }

        public List<SearchResult> Results { get; set; }
    }
}