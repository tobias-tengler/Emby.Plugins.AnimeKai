using System.Collections.Generic;
using System.Linq;
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

        public async Task<List<MediaRoot>> SearchAsync(string name, string format, CancellationToken cancellationToken)
        {
            _logger.LogCallerInfo($"{nameof(name)}: \"{name}\"");
            _logger.LogCallerInfo($"{nameof(format)}: \"{format}\"");

            var result = await FetchDataAsync<SearchResultRoot>($"search/anime?q={name}&page=1&type={format}", cancellationToken).ConfigureAwait(false);

            return result.Results?.Select(i => new MediaRoot
            {
                Mal_Id = i.Mal_Id,
                Airing = i.Airing,
                Image_Url = i.Image_Url,
                Score = i.Score,
                Synopsis = i.Synopsis,
                Title = i.Title,
                Aired = new Aired
                {
                    From = i.Start_Date,
                    To = i.End_Date,
                }
            }).ToList();
        }

        public Task<MediaRoot> GetFromIdAsync(int id, CancellationToken cancellationToken)
        {
            _logger.LogCallerInfo($"{nameof(id)}: {id}");

            return FetchDataAsync<MediaRoot>($"anime/{id}", cancellationToken);
        }

        public async Task<List<Picture>> GetImagesFromIdAsync(int id, CancellationToken cancellationToken)
        {
            _logger.LogCallerInfo($"{nameof(id)}: {id}");

            var results = await FetchDataAsync<PictureRoot>($"anime/{id}/pictures", cancellationToken).ConfigureAwait(false);

            return results?.Pictures;
        }

        private async Task<T> FetchDataAsync<T>(string endpoint, CancellationToken cancellationToken) where T : class
        {
            var options = new HttpRequestOptions
            {
                Url = $"https://api.jikan.moe/v3/{endpoint}",
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