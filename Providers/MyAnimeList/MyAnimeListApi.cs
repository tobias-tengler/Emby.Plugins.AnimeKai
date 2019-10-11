using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;

namespace Emby.Plugins.AnimeKai.Providers.MyAnimeList
{
    public class MyAnimeListApi
    {
        private readonly IJsonSerializer _serializer;
        private readonly IHttpClient _httpClient;

        public MyAnimeListApi(IHttpClient httpClient, IJsonSerializer serializer)
        {
            _httpClient = httpClient;
            _serializer = serializer;
        }

        public Task<MediaRoot> GetFromIdAsync(int id, CancellationToken cancellationToken)
        {
            return FetchDataAsync<MediaRoot>($"https://api.jikan.moe/v3/anime/{id}", cancellationToken);
        }

        public async Task<List<Picture>> GetImagesFromIdAsync(int id, CancellationToken cancellationToken)
        {
            var results = await FetchDataAsync<PictureRoot>($"https://api.jikan.moe/v3/anime/{id}/pictures", cancellationToken).ConfigureAwait(false);

            return results.Pictures;
        }

        private async Task<T> FetchDataAsync<T>(string url, CancellationToken cancellationToken)
        {
            var options = new HttpRequestOptions
            {
                Url = url,
                TimeoutMs = 30000,
                CancellationToken = cancellationToken,
                DecompressionMethod = CompressionMethod.Gzip
            };

            var result = await _httpClient.Get(options).ConfigureAwait(false);

            return await _serializer.DeserializeFromStreamAsync<T>(result).ConfigureAwait(false);
        }
    }
}