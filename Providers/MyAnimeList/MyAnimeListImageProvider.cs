using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;

namespace Emby.Plugins.AnimeKai.Providers.MyAnimeList
{
    public class MyAnimeListImageProvider : IRemoteImageProvider
    {
        private readonly IHttpClient _httpClient;
        private readonly MyAnimeListApi _api;
        private readonly ILogger _logger;

        public MyAnimeListImageProvider(IHttpClient httpClient, IJsonSerializer serializer, ILogManager logManager)
        {
            _httpClient = httpClient;
            _api = new MyAnimeListApi(httpClient, serializer, logManager);
            _logger = logManager.GetLogger(GetType().Name);
        }

        public string Name => MyAnimeListExternalId.ProviderName;

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url
            });
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, LibraryOptions libraryOptions, CancellationToken cancellationToken)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            _logger.LogCallerInfo($"{nameof(item)}.{nameof(item.Name)}: \"{item.Name}\"");

            var rawId = item.GetProviderId(Name);

            if (string.IsNullOrEmpty(rawId) || !int.TryParse(rawId, out var id))
            {
                _logger.LogCallerWarning($"No Provider ({Name}) Id found for {nameof(item)}.{nameof(item.Name)}: \"{item.Name}\"");
                return new List<RemoteImageInfo>();
            }

            _logger.LogCallerInfo($"Id: {id.ToString()}");

            var images = await _api.GetImagesFromIdAsync(id, cancellationToken).ConfigureAwait(false);

            if (images == null || images.Count < 1)
            {
                _logger.LogCallerWarning($"No Images found for Id: {id}");
                return new List<RemoteImageInfo>();
            }

            return images.Select(i => new RemoteImageInfo
            {
                Url = i.Large,
                Type = ImageType.Primary,
                ProviderName = Name
            });
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new[] { ImageType.Primary };
        }

        public bool Supports(BaseItem item)
        {
            return item is Series || item is Movie;
        }
    }
}