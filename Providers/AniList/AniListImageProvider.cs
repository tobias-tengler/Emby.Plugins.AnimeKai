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

namespace Emby.Providers.Anime.Providers
{
    public class AniListImageProvider : IRemoteImageProvider
    {
        private readonly IHttpClient _httpClient;
        private readonly AniListApi _api;
        private readonly IJsonSerializer serializer;
        private readonly ILogger logger;

        public AniListImageProvider(IHttpClient httpClient, IJsonSerializer serializer, ILogger logger)
        {
            this.logger = logger;
            this.serializer = serializer;
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

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, LibraryOptions libraryOptions, CancellationToken cancellationToken)
        {
            var rawId = item.GetProviderId(Name);

            if (string.IsNullOrEmpty(rawId) || !int.TryParse(rawId, out var id)) throw new InvalidOperationException("Failed to get Id from Media");

            var images = await _api.GetImageFromIdAsync(id, cancellationToken);

            logger.Log(LogSeverity.Info, id + " " + serializer.SerializeToString(images));

            if (images == null || images.Count < 0) throw new InvalidOperationException("No Images found for Id: " + id);

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
            return new[] { ImageType.Primary };
        }

        public bool Supports(BaseItem item)
        {
            return item is Series || item is Movie;
        }
    }
}