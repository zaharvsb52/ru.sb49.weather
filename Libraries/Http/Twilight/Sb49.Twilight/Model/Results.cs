using Newtonsoft.Json;

namespace Sb49.Twilight.Model
{
    //NOTE: All times are in UTC and summer time adjustments are not included in the returned data.
    public class Results
    {
        [JsonProperty(PropertyName = "sunrise")]
        public string Sunrise { get; set; }

        [JsonProperty(PropertyName = "sunset")]
        public string Sunset { get; set; }

        [JsonProperty(PropertyName = "solar_noon")]
        public string SolarNoon { get; set; }

        [JsonProperty(PropertyName = "day_length")]
        public string DayLength { get; set; }

        [JsonProperty(PropertyName = "civil_twilight_begin")]
        public string CivilTwilightBegin { get; set; }

        [JsonProperty(PropertyName = "civil_twilight_end")]
        public string CivilTwilightEnd { get; set; }

        [JsonProperty(PropertyName = "nautical_twilight_begin")]
        public string NauticalTwilightBegin { get; set; }

        [JsonProperty(PropertyName = "nautical_twilight_end")]
        public string NauticalTwilightEnd { get; set; }

        [JsonProperty(PropertyName = "astronomical_twilight_begin")]
        public string AstronomicalTwilightBegin { get; set; }

        [JsonProperty(PropertyName = "astronomical_twilight_end")]
        public string AstronomicalTwilightEnd { get; set; }
    }
}