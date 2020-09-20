using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace Emby.Plugins.AnimeKai.Providers.AniList
{
    public class AniListApi
    {
        private readonly IJsonSerializer _serializer;
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;

        public AniListApi(IHttpClient httpClient, IJsonSerializer serializer, ILogManager logManager)
        {
            _httpClient = httpClient;
            _serializer = serializer;
            _logger = logManager.GetLogger(GetType().Name);
        }

        public async Task<List<Media>> SearchAsync(string name, IEnumerable<string> formats, CancellationToken cancellationToken)
        {
            _logger.LogCallerInfo($"{nameof(name)}: \"{name}\"");
            _logger.LogCallerInfo($"{nameof(formats)}: \"{string.Join(",", formats)}\"");

            var body = _serializer.SerializeToString(new
            {
                query = SearchQuery + MediaFragment,
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
            _logger.LogCallerInfo($"{nameof(id)}: {id}");

            var body = _serializer.SerializeToString(new
            {
                query = MediaQuery + MediaFragment,
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
            _logger.LogCallerInfo($"{nameof(id)}: {id}");

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

            if (media == null) return null;

            return new Dictionary<ImageType, string>
            {
                { ImageType.Primary, media.CoverImage?.ExtraLarge },
                { ImageType.Banner, media.BannerImage },
            };
        }

        private async Task<T> FetchDataAsync<T>(string body, CancellationToken cancellationToken) where T : class
        {
            if (body == null) throw new ArgumentNullException(nameof(body));

            var options = new HttpRequestOptions
            {
                RequestContent = body.AsMemory(),
                Url = "https://graphql.anilist.co/",
                RequestContentType = "application/json",
                TimeoutMs = 30000,
                CancellationToken = cancellationToken
            };

            var result = await _httpClient.Post(options).ConfigureAwait(false);

            if (result == null || result.Content == null) return null;

            return await _serializer.DeserializeFromStreamAsync<T>(result.Content).ConfigureAwait(false);
        }

        #region Queries
        private const string SearchQuery = @"
query ($query: String!, $formats: [MediaFormat]) {
    Page {
        media(search: $query, format_in: $formats, type: ANIME) {
            ...mediaFragment
        }
    }
}";

        private const string MediaQuery = @"
query ($id: Int!) {
    Media(id: $id, type: ANIME) {
        ...mediaFragment
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

        private const string MediaFragment = @"
fragment mediaFragment on Media {
  id
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
  studios {
    edges {
      node {
        name
        isAnimationStudio
      }
    }
  }
}";
        #endregion
    }
}