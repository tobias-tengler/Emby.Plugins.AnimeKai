using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Emby.Plugins.AnimeKai.Providers.MyAnimeList
{
    public class MyAnimeListExternalId : IExternalId
    {
        public const string ProviderName = "MyAnimeList";

        public string Name => ProviderName;

        public string Key => ProviderName;

        public string UrlFormatString => "https://myanimelist.net/anime/{0}";

        public bool Supports(IHasProviderIds item)
        {
            return item is Series || item is Movie;
        }
    }
}