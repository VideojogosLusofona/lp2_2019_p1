using System.Collections.Generic;

namespace SampleLP2p1
{
    public struct Title
    {
        public string PrimaryTitle { get; }
        public short? StartYear { get; }
        public IEnumerable<string> Genres { get; }

        public Title(
            string primaryTitle, short? startYear, IEnumerable<string> genres)
        {
            PrimaryTitle = primaryTitle;
            StartYear = startYear;
            Genres = genres;
        }
    }
}
