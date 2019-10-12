using System.Collections.Generic;

namespace Emby.Plugins.AnimeKai.Providers.AniList
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

    public class StudioNode
    {
        public string Name { get; set; }
        public bool IsAnimationStudio { get; set; }
    }

    public class StudioEdge
    {
        public StudioNode Node { get; set; }
    }

    public class StudioRoot
    {
        public List<StudioEdge> Edges { get; set; }
    }

    public class Media
    {
        public int Id { get; set; }
        public int? IdMal { get; set; }
        public Title Title { get; set; }
        public CoverImage CoverImage { get; set; }
        public float? AverageScore { get; set; }
        public Date StartDate { get; set; }
        public Date EndDate { get; set; }
        public string BannerImage { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public List<string> Genres { get; set; }
        public StudioRoot Studios { get; set; }
    }
}