using System.Collections.Generic;

namespace Sb49.Weather.Core
{
    public class OptionalParameters
    {
        public List<string> DataBlocksToExclude { get; set; }
        public bool? ExtendHourly { get; set; }
        public string LanguageCode { get; set; }
        public string MeasurementUnits { get; set; }
        public long? UnixTimeInSeconds { get; set; }
    }
}