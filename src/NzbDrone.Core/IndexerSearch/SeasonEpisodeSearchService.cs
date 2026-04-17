using NLog;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.IndexerSearch
{
    public class SeasonEpisodeSearchService : IExecute<SeasonEpisodeSearchCommand>
    {
        private readonly ISeriesService _seriesService;
        private readonly EpisodeSearchService _episodeSearchService;
        private readonly Logger _logger;

        public SeasonEpisodeSearchService(ISeriesService seriesService,
                                          EpisodeSearchService episodeSearchService,
                                          Logger logger)
        {
            _seriesService = seriesService;
            _episodeSearchService = episodeSearchService;
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

            _episodeSearchService.SearchForBulkEpisodes(eligibleEpisodes, true, message.Trigger == CommandTrigger.Manual).GetAwaiter().GetResult();
        }
    }
}
