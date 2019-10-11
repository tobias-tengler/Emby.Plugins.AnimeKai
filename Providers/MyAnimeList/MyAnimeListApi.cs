using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace Emby.Plugins.AnimeKai.Providers.MyAnimeList
{
    public class MyAnimeListApi
    {
        private readonly IJsonSerializer _serializer;
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;

        public MyAnimeListApi(IHttpClient httpClient, IJsonSerializer serializer, ILogManager logManager)
        {
            _httpClient = httpClient;
            _serializer = serializer;
            _logger = logManager.GetLogger(GetType().Name);
        }

        public Task<MediaRoot> GetFromIdAsync(int id, CancellationToken cancellationToken)
        {
            _logger.LogCallerInfo($"{nameof(id)}: {id}");

            return FetchDataAsync<MediaRoot>(id.ToString(), cancellationToken);
        }

        public async Task<List<Picture>> GetImagesFromIdAsync(int id, CancellationToken cancellationToken)
        {
            _logger.LogCallerInfo($"{nameof(id)}: {id}");

            var results = await FetchDataAsync<PictureRoot>($"{id}/pictures", cancellationToken).ConfigureAwait(false);

            return results?.Pictures;
        }

        private async Task<T> FetchDataAsync<T>(string endpoint, CancellationToken cancellationToken) where T : class
        {
            var options = new HttpRequestOptions
            {
                Url = $"https://api.jikan.moe/v3/anime/{endpoint}",
                TimeoutMs = 30000,
                CancellationToken = cancellationToken,
                DecompressionMethod = CompressionMethod.Gzip
            };

            var result = await _httpClient.Get(options).ConfigureAwait(false);

            if (result == null) return null;

            return await _serializer.DeserializeFromStreamAsync<T>(result).ConfigureAwait(false);
        }
    }
}