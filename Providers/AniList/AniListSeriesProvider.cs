using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using Emby.Plugins.AnimeKai.Providers.MyAnimeList;
using MediaBrowser.Model.Logging;
using System.Runtime.CompilerServices;

namespace Emby.Plugins.AnimeKai.Providers.AniList
{
    public class AniListSeriesProvider : IRemoteMetadataProvider<Series, SeriesInfo>, IRemoteMetadataProvider<Movie, MovieInfo>, IHasOrder
    {
        private readonly IHttpClient _httpClient;
        private readonly AniListApi _api;
        private readonly ILogger _logger;

        public AniListSeriesProvider(IHttpClient httpClient, IJsonSerializer serializer, ILogManager logManager)
        {
            _httpClient = httpClient;
            _api = new AniListApi(httpClient, serializer, logManager);
            _logger = logManager.GetLogger(GetType().Name);
        }

        public string Name => AniListExternalId.ProviderName;

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
            var results = await GetSearchResultsFromInfoAsync(info, _seriesFormats, cancellationToken).ConfigureAwait(false);

            var media = results?.FirstOrDefault();

            if (media == null)
            {
                _logger.LogCallerWarning($"No Media found for {nameof(info)}.{nameof(info.Name)}: \"{info.Name}\"");
                return new MetadataResult<Series>();
            }

            LogMediaFound(media, info);

            var seriesItem = GetItemFromMedia<Series>(media);

            seriesItem.Status = media.Status == "RELEASING" ? SeriesStatus.Continuing : SeriesStatus.Ended;

            int? endYear = media.EndDate?.Year, endMonth = media.EndDate?.Month, endDay = media.EndDate?.Day;

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
            var results = await GetSearchResultsFromInfoAsync(searchInfo, _seriesFormats, cancellationToken).ConfigureAwait(false);

            return results.Select(GetSearchResultFromMedia);
        }
        #endregion

        #region Movie
        public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            var results = await GetSearchResultsFromInfoAsync(info, _movieFormats, cancellationToken).ConfigureAwait(false);

            var media = results?.FirstOrDefault();

            if (media == null)
            {
                _logger.LogCallerWarning($"No Media found for Name: \"{info.Name}\"");
                return new MetadataResult<Movie>();
            }

            LogMediaFound(media, info);

            return new MetadataResult<Movie>
            {
                HasMetadata = true,
                Item = GetItemFromMedia<Movie>(media)
            };
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
        {
            var results = await GetSearchResultsFromInfoAsync(searchInfo, _movieFormats, cancellationToken).ConfigureAwait(false);

            return results.Select(GetSearchResultFromMedia);
        }
        #endregion

        #region Helper
        private readonly List<string> _seriesFormats = new List<string> { "TV", "TV_SHORT", "ONA" };
        private readonly List<string> _movieFormats = new List<string> { "MOVIE" };

        private async Task<List<Media>> GetSearchResultsFromInfoAsync(ItemLookupInfo info, IEnumerable<string> formats, CancellationToken cancellationToken)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));

            _logger.LogCallerInfo($"{nameof(info)}.{nameof(info.Name)}: \"{info.Name}\"");
            _logger.LogCallerInfo($"{nameof(formats)}: \"{string.Join(",", formats)}\"");

            var results = new List<Media>();

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
                _logger.LogCallerWarning($"No Id found for {nameof(info)}.{nameof(info.Name)}: \"{info.Name}\"");

            if (!string.IsNullOrEmpty(info.Name))
            {
                var searchResults = await _api.SearchAsync(info.Name, formats, cancellationToken).ConfigureAwait(false);

                if (searchResults?.Count > 0)
                {
                    results.AddRange(searchResults);
                    return results;
                }

                _logger.LogCallerWarning($"No Results found for {nameof(info)}.{nameof(info.Name)}: \"{info.Name}\"");
            }

            return new List<Media>();
        }

        private T GetItemFromMedia<T>(Media media) where T : BaseItem, new()
        {
            if (media == null) throw new ArgumentNullException(nameof(media));

            var item = new T
            {
                Name = media.Title?.Romaji,
                Overview = Sanitizer.SanitizeDescription(media.Description),
                ProviderIds = new Dictionary<string, string>
                {
                    { Name, media.Id.ToString() },
                    { MyAnimeListExternalId.ProviderName, media.IdMal?.ToString() }
                }
            };

            if (media.Genres?.Count > 0)
                item.Genres = media.Genres.ToArray();

            if (media.Studios?.Edges?.Count > 0)
                item.Studios = media.Studios.Edges.Where(i => i.Node?.IsAnimationStudio == true)
                                                  .Select(i => i.Node.Name).ToArray();

            int? startYear = media.StartDate?.Year, startMonth = media.StartDate?.Month, startDay = media.StartDate?.Day;

            if (startYear.HasValue && startMonth.HasValue && startDay.HasValue)
            {
                item.PremiereDate = new DateTime(startYear.Value, startMonth.Value, startDay.Value);
                item.ProductionYear = startYear.Value;
            }

            if (media.AverageScore.HasValue)
                item.CommunityRating = media.AverageScore / 10;

            return item;
        }

        private RemoteSearchResult GetSearchResultFromMedia(Media media)
        {
            if (media == null) throw new ArgumentNullException(nameof(media));

            return new RemoteSearchResult
            {
                SearchProviderName = Name,
                Name = media.Title?.Romaji,
                ImageUrl = media.CoverImage?.Large,
                Overview = Sanitizer.SanitizeDescription(media.Description),
                ProviderIds = new Dictionary<string, string>
                {
                    { Name, media.Id.ToString() },
                    { MyAnimeListExternalId.ProviderName, media.IdMal?.ToString() }
                }
            };
        }

        private void LogMediaFound(Media media, ItemLookupInfo info, [CallerMemberName] string caller = null)
        {
            _logger.LogCallerInfo($"Media found for {nameof(info)}.{nameof(info.Name)}: \"{info.Name}\":", caller);
            _logger.LogCallerInfo($"{nameof(media.Id)}: {media.Id}", caller);
            _logger.LogCallerInfo($"{nameof(media.IdMal)}: {media.IdMal}", caller);
            _logger.LogCallerInfo($"{nameof(media.Title)}: \"{media.Title?.Romaji}\"", caller);

            if (media.Genres?.Count > 0)
                _logger.LogCallerInfo($"{nameof(media.Genres)}: \"{string.Join(",", media.Genres)}\"", caller);
            else
                _logger.LogCallerWarning($"No {nameof(media.Genres)} found", caller);
        }
        #endregion
    }
}