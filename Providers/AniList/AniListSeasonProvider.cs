using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;

namespace Emby.Plugins.AnimeKai.Providers.AniList
{
    public class AniListSeasonProvider : IRemoteMetadataProvider<Season, SeasonInfo>, IHasOrder
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly AniListSeriesProvider _seriesProvider;

        public AniListSeasonProvider(IHttpClient httpClient, ILogManager logManager, AniListSeriesProvider seriesProvider)
        {
            _httpClient = httpClient;
            _logger = logManager.GetLogger(GetType().Name);
            _seriesProvider = seriesProvider;
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

        public async Task<MetadataResult<Season>> GetMetadata(SeasonInfo info, CancellationToken cancellationToken)
        {
            info.SeriesProviderIds.TryGetValue(Name, out var seriesId);

            MetadataResult<Series> seriesResult = null;
            var seriesInfo = new SeriesInfo();

            if (info.ProviderIds.TryGetValue(Name, out var seasonId))
            {
                _logger.LogCallerInfo("Id: " + seasonId);

                seriesInfo.ProviderIds.Add(Name, seasonId);

                seriesResult = await _seriesProvider.GetMetadata(seriesInfo, cancellationToken).ConfigureAwait(false);
            }
            else if (info.IndexNumber == 1 && !string.IsNullOrEmpty(seriesId))
            {
                // todo load existing series metadata instead of asking the api again

                _logger.LogCallerInfo("Series Id: " + seriesId);

                seriesInfo.ProviderIds.Add(Name, seriesId);

                seriesResult = await _seriesProvider.GetMetadata(seriesInfo, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // todo get series name and append season number, then search api
            }

            var season = new Season
            {
                IndexNumber = info.IndexNumber,
                Name = $"Season {info.IndexNumber}"
            };

            if (seriesResult?.HasMetadata == true)
            {
                var series = seriesResult.Item;

                season.Overview = series.Overview;
                season.PremiereDate = series.PremiereDate;
                season.ProductionYear = series.ProductionYear;
                season.EndDate = series.EndDate;
                season.CommunityRating = series.CommunityRating;
                season.Studios = series.Studios;
                season.Genres = series.Genres;
                season.ProviderIds = series.ProviderIds;
            }
            else
            {
                // todo log series name here for easier reckoning
                _logger.LogCallerWarning($"No Metadata found for Season: \"{info.Name}\" from Series (Id: {seriesId})");
            }

            return new MetadataResult<Season>
            {
                HasMetadata = true,
                Item = season
            };
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeasonInfo searchInfo, CancellationToken cancellationToken)
        {
            _logger.LogCallerInfo($"{nameof(searchInfo.Name)}: \"{searchInfo.Name}\"");

            var results = await _seriesProvider.GetSearchResultsFromInfoAsync(searchInfo, _seriesProvider.SeriesFormats, cancellationToken).ConfigureAwait(false);

            return results.Select(_seriesProvider.GetSearchResultFromMedia);
        }
    }
}