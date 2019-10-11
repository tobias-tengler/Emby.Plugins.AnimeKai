using System.Collections.Generic;

namespace Emby.Providers.Anime.Providers.AniList.Models
{
    public class Title
    {
        public string Romaji { get; set; }
    }

    public class CoverImage
    {
        public string Large { get; set; }
        public string ExtraLarge { get; set; }
    }

    public class Date
    {
        public int? Year { get; set; }
        public int? Month { get; set; }
        public int? Day { get; set; }
    }

    public class Media
    {
        public int Id { get; set; }
        public int IdMal { get; set; }
        public Title Title { get; set; }
        public CoverImage CoverImage { get; set; }
        public int AverageScore { get; set; }
        public Date StartDate { get; set; }
        public Date EndDate { get; set; }
        public string BannerImage { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public List<string> Genres { get; set; }
    }
}