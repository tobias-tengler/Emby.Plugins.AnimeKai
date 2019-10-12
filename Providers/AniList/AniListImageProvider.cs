using System;
using System.Collections.Generic;
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

namespace Emby.Plugins.AnimeKai.Providers.AniList
{
    public class AniListImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IHttpClient _httpClient;
        private readonly AniListApi _api;
        private readonly ILogger _logger;

        public AniListImageProvider(IHttpClient httpClient, IJsonSerializer serializer, ILogManager logManager)
        {
            _httpClient = httpClient;
            _api = new AniListApi(httpClient, serializer, logManager);
            _logger = logManager.GetLogger(GetType().Name);
        }

        public string Name => AniListExternalId.ProviderName;

        public int Order => 0;

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
                _logger.LogCallerWarning($"No Id found for {nameof(item)}.{nameof(item.Name)}: \"{item.Name}\"");
                return new List<RemoteImageInfo>();
            }

            _logger.LogCallerInfo($"Id: {id.ToString()}");

            var images = await _api.GetImagesFromIdAsync(id, cancellationToken).ConfigureAwait(false);

            if (images == null || images.Count < 1)
            {
                _logger.LogCallerWarning($"No Images found for Id: {id}");
                return new List<RemoteImageInfo>();
            }

            var results = new List<RemoteImageInfo>();

            if (images.TryGetValue(ImageType.Primary, out var primaryPath))
                results.Add(new RemoteImageInfo
                {
                    Url = primaryPath,
                    Type = ImageType.Primary,
                    ProviderName = Name
                });

            if (images.TryGetValue(ImageType.Banner, out var bannerPath))
                results.Add(new RemoteImageInfo
                {
                    Url = bannerPath,
                    Type = ImageType.Banner,
                    ProviderName = Name
                });

            return results;
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new[] { ImageType.Primary, ImageType.Banner };
        }

        public bool Supports(BaseItem item)
        {
            return item is Series || item is Movie;
        }
    }
}