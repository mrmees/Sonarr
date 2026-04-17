using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.IndexerSearch
{
    public class SeasonEpisodeSearchCommand : Command
    {
        public int SeriesId { get; set; }
        public int SeasonNumber { get; set; }

        public override bool SendUpdatesToClient => true;

        public SeasonEpisodeSearchCommand()
        {
        }

        public SeasonEpisodeSearchCommand(int seriesId, int seasonNumber)
        {
            SeriesId = seriesId;
            SeasonNumber = seasonNumber;
        }
    }
}
