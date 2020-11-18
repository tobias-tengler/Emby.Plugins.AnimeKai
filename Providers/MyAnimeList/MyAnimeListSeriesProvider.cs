using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;

namespace Emby.Plugins.AnimeKai.Providers.MyAnimeList
{
    public class MyAnimeListSeriesProvider : IRemoteMetadataProvider<Series, SeriesInfo>, IRemoteMetadataProvider<Movie, MovieInfo>, IHasOrder
    {
        private readonly IHttpClient _httpClient;
        private readonly MyAnimeListApi _api;
        private readonly ILogger _logger;

        public MyAnimeListSeriesProvider(IHttpClient httpClient, IJsonSerializer serializer, ILogManager logManager)
        {
            _httpClient = httpClient;
            _api = new MyAnimeListApi(httpClient, serializer, logManager);
            _logger = logManager.GetLogger(GetType().Name);
        }

        public string Name => MyAnimeListExternalId.ProviderName;

        public int Order => 0;

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
            var results = await GetSearchResultsFromInfoAsync(info, "tv", cancellationToken).ConfigureAwait(false);

            var media = results?.FirstOrDefault();

            if (media == null)
            {
                _logger.LogCallerWarning($"No Media found for {nameof(info.Name)}: \"{info.Name}\"");
                return new MetadataResult<Series>();
            }

            LogMediaFound(media);

            var seriesItem = GetItemFromMedia<Series>(media);

            seriesItem.Status = media.Airing ? SeriesStatus.Continuing : SeriesStatus.Ended;

            int? endYear = media.Aired?.To.Year, endMonth = media.Aired?.To.Month, endDay = media.Aired?.To.Day;

            if (endYear.HasValue && endMonth.HasValue && endDay.HasValue)
                seriesItem.EndDate = new DateTime(endYear.Value, endMonth.Value, endDay.Value);

            return new MetadataResult<Series>
            {
                HasMetadata = true,
                Item = seriesItem
            };
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo searchInfo, CancellationToken cancellationToken)
        {
            var results = await GetSearchResultsFromInfoAsync(searchInfo, "tv", cancellationToken).ConfigureAwait(false);

            return results.Select(GetSearchResultFromMedia);
        }
        #endregion

        #region Movie
        public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            var results = await GetSearchResultsFromInfoAsync(info, "movie", cancellationToken).ConfigureAwait(false);

            var media = results?.FirstOrDefault();

            if (media == null)
            {
                _logger.LogCallerWarning($"No Media found for {nameof(info.Name)}: \"{info.Name}\"");
                return new MetadataResult<Movie>();
            }

            LogMediaFound(media);

            return new MetadataResult<Movie>
            {
                HasMetadata = true,
                Item = GetItemFromMedia<Movie>(media)
            };
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
        {
            var results = await GetSearchResultsFromInfoAsync(searchInfo, "movie", cancellationToken).ConfigureAwait(false);

            return results.Select(GetSearchResultFromMedia);
        }
        #endregion

        #region Helper
        public async Task<List<MediaRoot>> GetSearchResultsFromInfoAsync(ItemLookupInfo info, string format, CancellationToken cancellationToken)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));

            var results = new List<MediaRoot>();

            if (info.ProviderIds != null && info.ProviderIds.TryGetValue(Name, out var rawId) && int.TryParse(rawId, out var id))
            {
                _logger.LogCallerInfo($"Id: {id}");

                var media = await _api.GetFromIdAsync(id, cancellationToken).ConfigureAwait(false);

                if (media != null)
                {
                    results.Add(media);

                    return results;
                }

                _logger.LogCallerWarning($"No Media with Id: {id} found");
            }
            else
                _logger.LogCallerWarning($"No Id found for {nameof(info.Name)}: \"{info.Name}\"");

            if (!string.IsNullOrEmpty(info.Name))
            {
                var searchResults = await _api.SearchAsync(info.Name, format, cancellationToken).ConfigureAwait(false);

                if (searchResults?.Count > 0)
                {
                    results.AddRange(searchResults);
                    return results;
                }

                _logger.LogCallerWarning($"No Results found for {nameof(info.Name)}: \"{info.Name}\"");
            }
            else
                _logger.LogCallerWarning($"No {nameof(info.Name)} found");

            return results;
        }

        private T GetItemFromMedia<T>(MediaRoot media) where T : BaseItem, new()
        {
            if (media == null) throw new ArgumentNullException(nameof(media));

            var item = new T
            {
                Name = media.Title,
                Overview = Sanitizer.SanitizeDescription(media.Synopsis),
                ProviderIds = new Dictionary<string, string>
                {
                    { Name, media.Mal_Id.ToString() },
                }
            };

            if (media.Genres?.Count > 0)
                item.Genres = media.Genres.Select(i => i.Name).ToArray();

            int? startYear = media.Aired?.From.Year, startMonth = media.Aired?.From.Month, startDay = media.Aired?.From.Day;

            if (startYear.HasValue && startMonth.HasValue && startDay.HasValue)
            {
                item.PremiereDate = new DateTime(startYear.Value, startMonth.Value, startDay.Value);
                item.ProductionYear = startYear.Value;
            }

            item.CommunityRating = (float)Math.Round(media.Score, 1);

            return item;
        }

        public RemoteSearchResult GetSearchResultFromMedia(MediaRoot media)
        {
            if (media == null) throw new ArgumentNullException(nameof(media));

            return new RemoteSearchResult
            {
                SearchProviderName = Name,
                Name = media.Title,
                ImageUrl = media.Image_Url,
                Overview = Sanitizer.SanitizeDescription(media.Synopsis),
                ProviderIds = new Dictionary<string, string>
                {
                    { Name, media.Mal_Id.ToString() }
                }
            };
        }

        private void LogMediaFound(MediaRoot media, [CallerMemberName] string caller = null)
        {
            _logger.LogCallerInfo($"Media found:", caller);
            _logger.LogCallerInfo($"{nameof(media.Mal_Id)}: {media.Mal_Id}", caller);
            _logger.LogCallerInfo($"{nameof(media.Title)}: \"{media.Title}\"", caller);

            if (media.Genres?.Count > 0)
                _logger.LogCallerInfo($"{nameof(media.Genres)}: \"{string.Join(",", media.Genres.Select(i => i.Name))}\"", caller);
            else
                _logger.LogCallerWarning($"No {nameof(media.Genres)} found", caller);
        }
        #endregion
    }
}