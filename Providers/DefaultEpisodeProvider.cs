using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;

namespace Emby.Providers.Anime.Providers
{
    public class DefaultEpisodeProvider : IRemoteMetadataProvider<Episode, EpisodeInfo>
    {
        private readonly string[] _episodePatterns;

        public DefaultEpisodeProvider()
        {
            _episodePatterns = new string[]
            {
                @"(\.|\s|_)-(\.|\s|_)(?<number>\d+)",
                @"s(\.|\s|-|_)?\d+e(\.|\s|-|_)?(?<number>\d+)",
                @"episode(\.|\s|-|_)(?<number>\d+)",
                @"ep?(\.|\s|-|_)?(?<number>\d+)",
                @"(?<number>\d+)"
            };
        }

        public string Name => Plugin.PluginName;

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo info, CancellationToken cancellationToken)
        {
            var episodeNumber = GetEpisodeNumber(info);

            return Task.FromResult(new MetadataResult<Episode>
            {
                HasMetadata = true,
                Item = new Episode
                {
                    IndexNumber = episodeNumber,
                    Name = $"Episode {episodeNumber}"
                }
            });
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo searchInfo, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        private int? GetEpisodeNumber(EpisodeInfo info)
        {
            foreach (var pattern in _episodePatterns)
            {
                var match = Regex.Match(info.Name, pattern);

                if (!match.Success) continue;

                if (!string.IsNullOrEmpty(match.Groups?["number"]?.Value) && int.TryParse(match.Groups["number"].Value, out var episodeNumber))
                    return episodeNumber;
            }

            return null;
        }
    }
}