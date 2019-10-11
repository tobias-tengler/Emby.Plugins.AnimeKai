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
    public class MyAnimeListImageProvider : IRemoteImageProvider
    {
        private readonly IHttpClient _httpClient;
        private readonly MyAnimeListApi _api;

        public MyAnimeListImageProvider(IHttpClient httpClient, IJsonSerializer serializer)
        {
            _httpClient = httpClient;
            _api = new MyAnimeListApi(httpClient, serializer);
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
            var rawId = item.GetProviderId(Name);

            if (string.IsNullOrEmpty(rawId) || !int.TryParse(rawId, out var id)) throw new InvalidOperationException("Failed to get Id from Media");

            var images = await _api.GetImagesFromIdAsync(id, cancellationToken);

            if (images == null || images.Count < 0) throw new InvalidOperationException("No Images found for Id: " + id);

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