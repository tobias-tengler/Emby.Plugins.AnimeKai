using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Emby.Providers.Anime.Providers.AniList.Models;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Serialization;

namespace Emby.Providers.Anime.Providers
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
            var query = @"
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

            var body = _serializer.SerializeToString(new
            {
                query,
                variables = new
                {
                    query = name,
                    formats
                }
            });

            var result = await FetchData<Root>(body, cancellationToken);

            return result?.Data?.Page?.Media;
        }

        public async Task<Media> GetFromIdAsync(int id, CancellationToken cancellationToken)
        {
            var query = @"
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

            var body = _serializer.SerializeToString(new
            {
                query,
                variables = new
                {
                    id
                }
            });

            var result = await FetchData<Root>(body, cancellationToken);

            return result?.Data?.Media;
        }

        public async Task<IDictionary<ImageType, string>> GetImagesFromIdAsync(int id, CancellationToken cancellationToken)
        {
            var query = @"
query ($id: Int!) {
Media(id: $id, type: ANIME) {
coverImage {
large
extraLarge
}
bannerImage
}
}";

            var body = _serializer.SerializeToString(new
            {
                query,
                variables = new
                {
                    id
                }
            });

            var result = await FetchData<Root>(body, cancellationToken);
            var media = result?.Data?.Media;

            return new Dictionary<ImageType, string>
            {
                { ImageType.Primary, media.CoverImage.ExtraLarge },
                { ImageType.Banner, media.BannerImage },
            };
        }

        private async Task<T> FetchData<T>(string body, CancellationToken cancellationToken)
        {
            var options = new HttpRequestOptions
            {
                RequestContent = body.AsMemory(),
                Url = "https://graphql.anilist.co/",
                RequestContentType = "application/json",
                TimeoutMs = 30000,
                CancellationToken = cancellationToken
            };

            var result = await _httpClient.Post(options);

            return await _serializer.DeserializeFromStreamAsync<T>(result.Content);
        }
    }
}