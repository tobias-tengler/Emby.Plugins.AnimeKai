using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Providers.Anime.Providers.AniList.Models;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;

namespace Emby.Providers.Anime.Providers
{
    public class AniListSeriesProvider : IRemoteMetadataProvider<Series, SeriesInfo>
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

        public async Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
        {
            var results = await Get(info, cancellationToken);

            var media = results.FirstOrDefault();

            if (media == null) return null;

            DateTime? endDate = null, startDate = null;
            if (media.StartDate != null)
                startDate = new DateTime(media.StartDate.Year, media.StartDate.Month, media.StartDate.Day);

            if (media.EndDate != null)
                endDate = new DateTime(media.EndDate.Year, media.EndDate.Month, media.EndDate.Day);

            return new MetadataResult<Series>
            {
                HasMetadata = true,
                Item = new Series
                {
                    Name = media.Title?.Romaji,
                    Overview = media.Description,
                    CommunityRating = (float)media.AverageScore / 10,
                    Genres = media.Genres?.ToArray(),
                    EndDate = endDate,
                    PremiereDate = startDate,
                    ProductionYear = startDate?.Year,
                    Status = media.Status == "RELEASING" ? SeriesStatus.Continuing : SeriesStatus.Ended,
                    ProviderIds = new Dictionary<string, string>
                    {
                        { Name, media.Id.ToString() }
                    }
                }
            };
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo searchInfo, CancellationToken cancellationToken)
        {
            var results = await Get(searchInfo, cancellationToken);

            return results.Select(GetSearchResultFromData);
        }

        private async Task<List<Media>> Get(SeriesInfo info, CancellationToken cancellationToken)
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
                var searchResults = await _api.SearchAsync(info.Name, new List<string> { "TV", "TV_SHORT", "ONA" }, cancellationToken);

                if (searchResults?.Count > 0)
                {
                    foreach (var searchResult in searchResults)
                    {
                        results.Add(searchResult);
                    }
                }
            }

            return results;
        }

        private RemoteSearchResult GetSearchResultFromData(Media data)
        {
            return new RemoteSearchResult
            {
                SearchProviderName = Name,
                Name = data.Title?.Romaji,
                ImageUrl = data.CoverImage?.Large,
                Overview = data.Description,
                ProviderIds = new Dictionary<string, string>
                {
                    { Name, data.Id.ToString() }
                }
            };
        }
    }
}