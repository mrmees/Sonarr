using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Queue;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.IndexerSearchTests
{
    [TestFixture]
    public class SeasonEpisodeSearchServiceFixture : CoreTest<SeasonEpisodeSearchService>
    {
        private const int SeriesId = 1;
        private const int SeasonNumber = 2;
        private static readonly DateTime AiredDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime UnairedDate = new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly List<Episode> _episodes = new();
        private readonly List<Episode> _searchedEpisodes = new();
        private readonly List<Queue> _queue = new();

        [SetUp]
        public void SetUp()
        {
            var series = new Series
            {
                Id = SeriesId,
                Title = "Series",
                Monitored = true
            };

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.GetSeries(SeriesId))
                  .Returns(series);

            Mocker.GetMock<IEpisodeService>()
                  .Setup(s => s.GetEpisodesBySeason(SeriesId, SeasonNumber))
                  .Returns(() => _episodes);

            Mocker.GetMock<IQueueService>()
                  .Setup(s => s.GetQueue())
                  .Returns(() => _queue);

            Mocker.GetMock<ISearchForReleases>()
                  .Setup(s => s.SeasonSearch(SeriesId, SeasonNumber, It.IsAny<List<Episode>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                  .Returns(Task.FromResult(new List<DownloadDecision>()))
                  .Callback<int, int, List<Episode>, bool, bool, bool>((_, _, episodes, _, _, _) =>
                  {
                      _searchedEpisodes.Clear();
                      _searchedEpisodes.AddRange(episodes);
                  });

            Mocker.GetMock<IProcessDownloadDecisions>()
                  .Setup(s => s.ProcessDecisions(It.IsAny<List<DownloadDecision>>()))
                  .Returns(Task.FromResult(new ProcessedDecisions(new List<DownloadDecision>(), new List<DownloadDecision>(), new List<DownloadDecision>())));
        }

        [Test]
        public void should_search_only_monitored_episodes_and_skip_episodes_with_files_unaired_episodes_and_queued_episodes()
        {
            var monitoredEpisode = CreateEpisode(1, monitored: true, hasFile: false, aired: true);
            var unmonitoredEpisode = CreateEpisode(2, monitored: false, hasFile: false, aired: true);
            var episodeWithFile = CreateEpisode(3, monitored: true, hasFile: true, aired: true);
            var unairedEpisode = CreateEpisode(4, monitored: true, hasFile: false, aired: false);
            var queuedEpisode = CreateEpisode(5, monitored: true, hasFile: false, aired: true);

            _episodes.AddRange(new[]
            {
                monitoredEpisode,
                unmonitoredEpisode,
                episodeWithFile,
                unairedEpisode,
                queuedEpisode
            });

            _queue.Add(new Queue
            {
                Episodes = new List<Episode> { queuedEpisode }
            });

            Subject.Execute(new SeasonEpisodeSearchCommand
            {
                SeriesId = SeriesId,
                SeasonNumber = SeasonNumber,
                Trigger = CommandTrigger.Manual
            });

            _searchedEpisodes.Select(e => e.Id).Should().BeEquivalentTo(new[] { monitoredEpisode.Id });
        }

        [Test]
        public void should_not_use_the_broad_season_search_path()
        {
            _episodes.Add(CreateEpisode(1, monitored: true, hasFile: false, aired: true));

            Subject.Execute(new SeasonEpisodeSearchCommand
            {
                SeriesId = SeriesId,
                SeasonNumber = SeasonNumber,
                Trigger = CommandTrigger.Manual
            });

            Mocker.GetMock<ISearchForReleases>()
                  .Verify(v => v.SeasonSearch(SeriesId, SeasonNumber, false, true, It.IsAny<bool>(), It.IsAny<bool>()), Times.Never());
        }

        private Episode CreateEpisode(int episodeNumber, bool monitored, bool hasFile, bool aired)
        {
            var airDate = aired ? AiredDate : UnairedDate;

            return new Episode
            {
                Id = episodeNumber,
                SeriesId = SeriesId,
                Series = new Series
                {
                    Id = SeriesId,
                    Title = "Series",
                    Monitored = true
                },
                SeasonNumber = SeasonNumber,
                EpisodeNumber = episodeNumber,
                Monitored = monitored,
                EpisodeFileId = hasFile ? 1 : 0,
                AirDateUtc = airDate,
                AirDate = airDate.ToString(Episode.AIR_DATE_FORMAT)
            };
        }
    }
}
