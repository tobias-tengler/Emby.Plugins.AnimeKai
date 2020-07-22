using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;

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

            if (!string.IsNullOrEmpty(seriesId))
                _logger.LogCallerInfo("Series Id: " + seriesId);
            if (!string.IsNullOrEmpty(info.SeriesName))
                _logger.LogCallerInfo("Series Name: " + info.SeriesName);
            if (info.IndexNumber > 0)
                _logger.LogCallerInfo("Season Number: " + info.IndexNumber);
            if (!string.IsNullOrEmpty(info.Name))
                _logger.LogCallerInfo("Season Name: " + info.Name);

            MetadataResult<Series> seriesResult;
            var seriesInfo = new SeriesInfo();

            if (info.ProviderIds.TryGetValue(Name, out var seasonId))
            {
                _logger.LogCallerInfo("Season Id: " + seasonId);

                seriesInfo.ProviderIds.Add(Name, seasonId);

                seriesResult = await _seriesProvider.GetMetadata(seriesInfo, cancellationToken).ConfigureAwait(false);
            }
            else if (info.IndexNumber == 1 && !string.IsNullOrEmpty(seriesId))
            {
                // todo load existing series metadata instead of querying the API again

                seriesInfo.ProviderIds.Add(Name, seriesId);

                seriesResult = await _seriesProvider.GetMetadata(seriesInfo, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                seriesInfo.Name = $"{info.SeriesName} {info.IndexNumber}";

                seriesResult = await _seriesProvider.GetMetadata(seriesInfo, cancellationToken).ConfigureAwait(false);
            }

            string seasonName;

            var nameMatch = Regex.Match(info.Name, @"\((.+)\)");

            if (nameMatch.Success)
                seasonName = nameMatch.Groups[1]?.Value;
            else
                seasonName = $"Season {info.IndexNumber}";

            var season = new Season
            {
                IndexNumber = info.IndexNumber,
                Name = seasonName
            };

            var hasMetadata = seriesResult?.HasMetadata == true;

            if (hasMetadata)
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
                _logger.LogCallerWarning($"No Metadata found for Season: \"{info.Name}\" from Series (Name: \"{info.SeriesName}\", Id: {seriesId})");

            return new MetadataResult<Season>
            {
                HasMetadata = hasMetadata,
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