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
            return FetchData<MediaRoot>($"https://api.jikan.moe/v3/anime/{id}", cancellationToken);
        }

        public async Task<List<Picture>> GetImagesFromIdAsync(int id, CancellationToken cancellationToken)
        {
            var results = await FetchData<PictureRoot>($"https://api.jikan.moe/v3/anime/{id}/pictures", cancellationToken);

            return results.Pictures;
        }

        private async Task<T> FetchData<T>(string url, CancellationToken cancellationToken)
        {
            var options = new HttpRequestOptions
            {
                Url = url,
                TimeoutMs = 30000,
                CancellationToken = cancellationToken,
                DecompressionMethod = CompressionMethod.Gzip
            };

            var result = await _httpClient.Get(options);

            return await _serializer.DeserializeFromStreamAsync<T>(result);
        }
    }
}