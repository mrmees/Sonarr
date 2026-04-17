using NLog;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.IndexerSearch
{
    public class SeasonEpisodeSearchService : IExecute<SeasonEpisodeSearchCommand>
    {
        private readonly ISeriesService _seriesService;
        private readonly EpisodeSearchService _episodeSearchService;
        private readonly ISearchForReleases _releaseSearchService;
        private readonly IProcessDownloadDecisions _processDownloadDecisions;
        private readonly Logger _logger;

        public SeasonEpisodeSearchService(ISeriesService seriesService,
                                          EpisodeSearchService episodeSearchService,
                                          ISearchForReleases releaseSearchService,
                                          IProcessDownloadDecisions processDownloadDecisions,
                                          Logger logger)
        {
            _seriesService = seriesService;
            _episodeSearchService = episodeSearchService;
            _releaseSearchService = releaseSearchService;
            _processDownloadDecisions = processDownloadDecisions;
            _logger = logger;
        }

        public void Execute(SeasonEpisodeSearchCommand message)
        {
            var series = _seriesService.GetSeries(message.SeriesId);
            if (series == null)
            {
                _logger.ProgressInfo("Series {0} was not found. Skipping season episode search.", message.SeriesId);
                return;
            }

            if (!series.Monitored)
            {
                _logger.ProgressInfo("No eligible episodes found for {0} season {1}.", series.Title, message.SeasonNumber);
                return;
            }

            var eligibleEpisodes = _episodeSearchService.GetSeasonSearchEpisodes(message.SeriesId, message.SeasonNumber);

            if (eligibleEpisodes.Count == 0)
            {
                _logger.ProgressInfo("No eligible episodes found for {0} season {1}.", series.Title, message.SeasonNumber);
                return;
            }

            var decisions = _releaseSearchService.SeasonSearch(message.SeriesId, message.SeasonNumber, eligibleEpisodes, true, message.Trigger == CommandTrigger.Manual, false).GetAwaiter().GetResult();
            var processed = _processDownloadDecisions.ProcessDecisions(decisions).GetAwaiter().GetResult();

            _logger.ProgressInfo("Season search completed. {0} reports downloaded.", processed.Grabbed.Count);
        }
    }
}
