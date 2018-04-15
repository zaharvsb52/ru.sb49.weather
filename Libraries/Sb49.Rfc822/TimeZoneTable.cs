using System;
using System.Linq;
using NodaTime;

namespace Sb49.Rfc822
{
    /// <summary>
    /// Maps the time zone identifiers defined by RFC 822 to their corresponding UTC offsets.
    /// </summary>
    public class TimeZoneTable : ITimeZoneMapper
    {
        //protected IDictionary<string, float> Table;

        //https://www.timeanddate.com/time/zones/
        //public TimeZoneTable()
        //{
        //    Table = new ConcurrentDictionary<string, float>
        //    {
        //        // Abbreviated time zone identifiers
        //        ["GMT"] = 0,
        //        ["UT"] = 0,
        //        ["EDT"] = -4,
        //        ["EST"] = -5,
        //        ["CDT"] = -5,
        //        ["CST"] = -6,
        //        ["MDT"] = -6,
        //        ["MST"] = -7,
        //        ["PDT"] = -7,
        //        ["PST"] = -8,
        //        ["MSD"] = 4,
        //        ["MSK"] = 3,
        //        // Military time zone identifiers
        //        ["A"] = 1,
        //        ["B"] = 2,
        //        ["C"] = 3,
        //        ["D"] = 4,
        //        ["E"] = 5,
        //        ["F"] = 6,
        //        ["G"] = 7,
        //        ["H"] = 8,
        //        ["I"] = 9,
        //        ["K"] = 10,
        //        ["L"] = 11,
        //        ["M"] = 12,
        //        ["N"] = -1,
        //        ["O"] = -2,
        //        ["P"] = -3,
        //        ["Q"] = -4,
        //        ["R"] = -5,
        //        ["S"] = -6,
        //        ["T"] = -7,
        //        ["U"] = -8,
        //        ["V"] = -9,
        //        ["W"] = -10,
        //        ["X"] = -11,
        //        ["Y"] = -12,
        //        ["Z"] = 0
        //    };
        //}

        /// <summary>
        /// Maps an RFC 822 time zone identifier to a time zone using an 
        /// internal table of identifiers and offsets.
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public virtual TimeZone Map(string identifier)
        {
            var zoneProvider = DateTimeZoneProviders.Tzdb;
            //var now = SystemClock.Instance.Now;
            var now = SystemClock.Instance.GetCurrentInstant();
            var zoneInterval = zoneProvider.Ids.Select(id => zoneProvider[id].GetZoneInterval(now)).FirstOrDefault(p => p.Name == identifier);
            //var offset = zoneInterval?.StandardOffset.ToTimeSpan() ?? TimeSpan.Zero;
            var offset = zoneInterval?.WallOffset.ToTimeSpan() ?? TimeSpan.Zero;

            //float hours;
            //Table.TryGetValue(identifier.ToUpperInvariant(), out hours);
            //var offset = TimeSpan.FromHours(hours);

            return new TimeZone(identifier, offset);
        }
    }
}
