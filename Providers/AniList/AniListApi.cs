using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Serialization;

namespace Emby.Plugins.AnimeKai.Providers.AniList
{
    public class AniListApi
    {
        private readonly IJsonSerializer _serializer;
        private readonly IHttpClient _httpClient;

        public AniListApi(IHttpClient httpClient, IJsonSerializer serializer)
        {
            _httpClient = httpClient;
            _serializer = serializer;
        }

        public async Task<List<Media>> SearchAsync(string name, IEnumerable<string> formats, CancellationToken cancellationToken)
        {
            var body = _serializer.SerializeToString(new
            {
                query = SearchQuery,
                variables = new
                {
                    query = name,
                    formats
                }
            });

            var result = await FetchDataAsync<Root>(body, cancellationToken).ConfigureAwait(false);

            return result?.Data?.Page?.Media;
        }

        public async Task<Media> GetFromIdAsync(int id, CancellationToken cancellationToken)
        {
            var body = _serializer.SerializeToString(new
            {
                query = MediaQuery,
                variables = new
                {
                    id
                }
            });

            var result = await FetchDataAsync<Root>(body, cancellationToken).ConfigureAwait(false);

            return result?.Data?.Media;
        }

        public async Task<IDictionary<ImageType, string>> GetImagesFromIdAsync(int id, CancellationToken cancellationToken)
        {
            var body = _serializer.SerializeToString(new
            {
                query = ImageQuery,
                variables = new
                {
                    id
                }
            });

            var result = await FetchDataAsync<Root>(body, cancellationToken).ConfigureAwait(false);

            var media = result?.Data?.Media;

            return new Dictionary<ImageType, string>
            {
                { ImageType.Primary, media.CoverImage.ExtraLarge },
                { ImageType.Banner, media.BannerImage },
            };
        }

        private async Task<T> FetchDataAsync<T>(string body, CancellationToken cancellationToken)
        {
            var options = new HttpRequestOptions
            {
                RequestContent = body.AsMemory(),
                Url = "https://graphql.anilist.co/",
                RequestContentType = "application/json",
                TimeoutMs = 30000,
                CancellationToken = cancellationToken
            };

            var result = await _httpClient.Post(options).ConfigureAwait(false);

            return await _serializer.DeserializeFromStreamAsync<T>(result.Content).ConfigureAwait(false);
        }

        #region Queries
        private const string SearchQuery = @"
query ($query: String!, $formats: [MediaFormat]) {
Page(perPage: 10) {
media(search: $query, format_in: $formats, type: ANIME) {
id
idMal
title {
romaji
}
description
coverImage {
large
}
}
}
}";

        private const string MediaQuery = @"
query ($id: Int!) {
Media(id: $id, type: ANIME) {
idMal
title {
romaji
}
startDate {
year
month
day
}
endDate {
year
month
day
}
coverImage {
large
extraLarge
}
bannerImage
status
episodes
description
averageScore
genres
}
}";

        private const string ImageQuery = @"
query ($id: Int!) {
Media(id: $id, type: ANIME) {
coverImage {
large
extraLarge
}
bannerImage
}
}";
        #endregion
    }
}