using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Providers.Anime.Providers.AniList.Models;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;

namespace Emby.Providers.Anime.Providers
{
    public class AniListSeriesProvider : IRemoteMetadataProvider<Series, SeriesInfo>, IRemoteMetadataProvider<Movie, MovieInfo>
    {
        private readonly IHttpClient _httpClient;
        private readonly AniListApi _api;

        public AniListSeriesProvider(IHttpClient httpClient, IJsonSerializer serializer)
        {
            _httpClient = httpClient;
            _api = new AniListApi(httpClient, serializer);
        }

        public string Name => AniListExternalId.ProviderName;

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url
            });
        }

        #region Series
        public async Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
        {
            var results = await GetSearchResultsFromInfoAsync(info, _seriesFormats, cancellationToken);

            var media = results.FirstOrDefault();

            if (media == null) return null;

            var seriesItem = GetItemFromMedia<Series>(media);

            seriesItem.Status = media.Status == "RELEASING" ? SeriesStatus.Continuing : SeriesStatus.Ended;

            if (media.EndDate != null && media.EndDate.Year.HasValue && media.EndDate.Month.HasValue && media.EndDate.Day.HasValue)
                seriesItem.EndDate = new DateTime(media.EndDate.Year.Value, media.EndDate.Month.Value, media.EndDate.Day.Value);

            return new MetadataResult<Series>
            {
                HasMetadata = true,
                Item = seriesItem
            };
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo searchInfo, CancellationToken cancellationToken)
        {
            var results = await GetSearchResultsFromInfoAsync(searchInfo, _seriesFormats, cancellationToken);

            return results.Select(GetSearchResultFromMedia);
        }
        #endregion

        #region Movie
        public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            var results = await GetSearchResultsFromInfoAsync(info, _movieFormats, cancellationToken);

            var media = results.FirstOrDefault();

            if (media == null) return null;

            var movieItem = GetItemFromMedia<Movie>(media);

            return new MetadataResult<Movie>
            {
                HasMetadata = true,
                Item = movieItem
            };
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
        {
            var results = await GetSearchResultsFromInfoAsync(searchInfo, _movieFormats, cancellationToken);

            return results.Select(GetSearchResultFromMedia);
        }
        #endregion

        #region Helper
        private readonly List<string> _seriesFormats = new List<string> { "TV", "TV_SHORT", "ONA" };
        private readonly List<string> _movieFormats = new List<string> { "MOVIE" };

        private async Task<List<Media>> GetSearchResultsFromInfoAsync(ItemLookupInfo info, IEnumerable<string> formats, CancellationToken cancellationToken)
        {
            var results = new List<Media>();

            if (info.ProviderIds.TryGetValue(Name, out var rawId) && int.TryParse(rawId, out var id))
            {
                var media = await _api.GetFromIdAsync(id, cancellationToken);

                if (media != null)
                {
                    results.Add(media);

                    return results;
                }
            }

            if (!string.IsNullOrEmpty(info.Name))
            {
                var searchResults = await _api.SearchAsync(info.Name, formats, cancellationToken);

                if (searchResults?.Count > 0)
                    results.AddRange(searchResults);
            }

            return results;
        }

        private T GetItemFromMedia<T>(Media media) where T : BaseItem, new()
        {
            DateTime? startDate = null;
            if (media.StartDate != null && media.StartDate.Year.HasValue && media.StartDate.Month.HasValue && media.StartDate.Day.HasValue)
                startDate = new DateTime(media.StartDate.Year.Value, media.StartDate.Month.Value, media.StartDate.Day.Value);

            return new T
            {
                Name = media.Title?.Romaji,
                Overview = CleanDescription(media.Description),
                CommunityRating = (float)media.AverageScore / 10,
                Genres = media.Genres?.ToArray(),
                PremiereDate = startDate,
                ProductionYear = startDate?.Year,
                ProviderIds = new Dictionary<string, string>
                {
                    { Name, media.Id.ToString() },
                    { MyAnimeListExternalId.ProviderName, media.IdMal.ToString() }
                }
            };
        }

        private RemoteSearchResult GetSearchResultFromMedia(Media media)
        {
            return new RemoteSearchResult
            {
                SearchProviderName = Name,
                Name = media.Title?.Romaji,
                ImageUrl = media.CoverImage?.Large,
                Overview = CleanDescription(media.Description),
                ProviderIds = new Dictionary<string, string>
                {
                    { Name, media.Id.ToString() },
                    { MyAnimeListExternalId.ProviderName, media.IdMal.ToString() }
                }
            };
        }

        private string CleanDescription(string description)
        {
            var sourceIndex = description.IndexOf("(Source:");

            if (sourceIndex > 0)
                description = description.Substring(0, sourceIndex);

            var writtenByIndex = description.IndexOf("[Written by");

            if (writtenByIndex > 0)
                description = description.Substring(0, writtenByIndex);

            description = description.Replace("—", " - ").Trim();

            while (description.EndsWith("<br>"))
                description = description.Remove(description.Length - 4).TrimEnd();

            return description;
        }
        #endregion
    }
}