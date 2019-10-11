using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Emby.Plugins.AnimeKai.Providers.AniList
{
    public class AniListExternalId : IExternalId
    {
        public const string ProviderName = "AniList";

        public string Name => ProviderName;

        public string Key => ProviderName;

        public string UrlFormatString => "http://anilist.co/anime/{0}/";

        public bool Supports(IHasProviderIds item)
        {
            return item is Series || item is Movie;
        }
    }
}