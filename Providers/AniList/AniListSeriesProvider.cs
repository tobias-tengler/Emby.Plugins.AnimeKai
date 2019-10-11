using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Providers.Anime.Providers.AniList.Models;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
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

        public async Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
        {
            var results = await GetSearchResultsFromSeriesInfoAsync(info, cancellationToken);

            var media = results.FirstOrDefault();

            if (media == null) return null;

            return new MetadataResult<Series>
            {
                HasMetadata = true,
                Item = GetSeriesFromMedia(media)
            };
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo searchInfo, CancellationToken cancellationToken)
        {
            var results = await GetSearchResultsFromSeriesInfoAsync(searchInfo, cancellationToken);

            return results.Select(GetSearchResultFromMedia);
        }

        public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            var results = await GetSearchResultsFromMovieInfoAsync(info, cancellationToken);

            var media = results.FirstOrDefault();

            if (media == null) return null;

            return new MetadataResult<Movie>
            {
                HasMetadata = true,
                Item = GetMovieFromMedia(media)
            };
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
        {
            var results = await GetSearchResultsFromMovieInfoAsync(searchInfo, cancellationToken);

            return results.Select(GetSearchResultFromMedia);
        }

        private async Task<List<Media>> GetSearchResultsFromSeriesInfoAsync(SeriesInfo info, CancellationToken cancellationToken)
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

        private async Task<List<Media>> GetSearchResultsFromMovieInfoAsync(MovieInfo info, CancellationToken cancellationToken)
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
                var searchResults = await _api.SearchAsync(info.Name, new List<string> { "MOVIE" }, cancellationToken);

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

        private Series GetSeriesFromMedia(Media media)
        {
            DateTime? endDate = null, startDate = null;
            if (media.StartDate != null)
                startDate = new DateTime(media.StartDate.Year, media.StartDate.Month, media.StartDate.Day);

            if (media.EndDate != null)
                endDate = new DateTime(media.EndDate.Year, media.EndDate.Month, media.EndDate.Day);

            return new Series
            {
                Name = media.Title?.Romaji,
                Overview = CleanDescription(media.Description),
                CommunityRating = (float)media.AverageScore / 10,
                Genres = media.Genres?.ToArray(),
                EndDate = endDate,
                PremiereDate = startDate,
                ProductionYear = startDate?.Year,
                Status = media.Status == "RELEASING" ? SeriesStatus.Continuing : SeriesStatus.Ended,
                ProviderIds = new Dictionary<string, string>
                {
                    { Name, media.Id.ToString() },
                    { MyAnimeListExternalId.ProviderName, media.IdMal.ToString() }
                }
            };
        }

        private Movie GetMovieFromMedia(Media media)
        {
            DateTime? startDate = null;
            if (media.StartDate != null)
                startDate = new DateTime(media.StartDate.Year, media.StartDate.Month, media.StartDate.Day);

            return new Movie
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
    }
}