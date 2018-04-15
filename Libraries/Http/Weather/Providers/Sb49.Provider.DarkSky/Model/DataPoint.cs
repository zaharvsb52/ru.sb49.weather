using Newtonsoft.Json;

namespace Sb49.Provider.DarkSky.Model
{
	public class DataPoint
	{
		[JsonProperty(PropertyName = "apparentTemperature")]
		public double? ApparentTemperature { get; set; }

		[JsonProperty(PropertyName = "apparentTemperatureMax")]
		public double? ApparentTemperatureMax { get; set; }

		[JsonProperty(PropertyName = "apparentTemperatureMaxTime")]
		public long? ApparentTemperatureMaxTime { get; set; }

		[JsonProperty(PropertyName = "apparentTemperatureMin")]
		public double? ApparentTemperatureMin { get; set; }

		[JsonProperty(PropertyName = "apparentTemperatureMinTime")]
		public long? ApparentTemperatureMinTime { get; set; }

		[JsonProperty(PropertyName = "cloudCover")]
		public double? CloudCover { get; set; }

        /// <summary>
        /// The dew point in degrees.
        /// </summary>
		[JsonProperty(PropertyName = "dewPoint")]
		public double? DewPoint { get; set; }

		[JsonProperty(PropertyName = "humidity")]
		public double? Humidity { get; set; }

		[JsonProperty(PropertyName = "icon")]
		public string Icon { get; set; }

		[JsonProperty(PropertyName = "moonPhase")]
		public double? MoonPhase { get; set; }

		[JsonProperty(PropertyName = "nearestStormBearing")]
		public int? NearestStormBearing { get; set; }

		[JsonProperty(PropertyName = "nearestStormDistance")]
		public double? NearestStormDistance { get; set; }

		[JsonProperty(PropertyName = "ozone")]
		public double? Ozone { get; set; }

		[JsonProperty(PropertyName = "precipAccumulation")]
		public double? PrecipAccumulation { get; set; }

		[JsonProperty(PropertyName = "precipIntensity")]
		public double? PrecipIntensity { get; set; }

		[JsonProperty(PropertyName = "precipIntensityError")]
		public double? PrecipIntensityError { get; set; }

		[JsonProperty(PropertyName = "precipIntensityMax")]
		public double? PrecipIntensityMax { get; set; }

		[JsonProperty(PropertyName = "precipIntensityMaxTime")]
		public long? PrecipIntensityMaxTime { get; set; }

		[JsonProperty(PropertyName = "precipProbability")]
		public double? PrecipProbability { get; set; }

		[JsonProperty(PropertyName = "precipType")]
		public string PrecipType { get; set; }

		[JsonProperty(PropertyName = "pressure")]
		public double? Pressure { get; set; }

		[JsonProperty(PropertyName = "summary")]
		public string Summary { get; set; }

		[JsonProperty(PropertyName = "sunriseTime")]
		public long? SunriseTime { get; set; }

		[JsonProperty(PropertyName = "sunsetTime")]
		public long? SunsetTime { get; set; }

		[JsonProperty(PropertyName = "temperature")]
		public double? Temperature { get; set; }

		[JsonProperty(PropertyName = "temperatureMax")]
		public double? TemperatureMax { get; set; }

		[JsonProperty(PropertyName = "temperatureMaxTime")]
		public long? TemperatureMaxTime { get; set; }

		[JsonProperty(PropertyName = "temperatureMin")]
		public double? TemperatureMin { get; set; }

		[JsonProperty(PropertyName = "temperatureMinTime")]
		public long? TemperatureMinTime { get; set; }

        /// <summary>
        /// The UNIX time at which this data point begins. Minutely data point are always aligned to the top of the minute, 
        /// hourly data point objects to the top of the hour, and daily data point objects to midnight of the day, all according to the local time zone.
        /// </summary>
		[JsonProperty(PropertyName = "time")]
		public long Time { get; set; }

		[JsonProperty(PropertyName = "visibility")]
		public double? Visibility { get; set; }

		[JsonProperty(PropertyName = "windBearing")]
		public int? WindBearing { get; set; }

		[JsonProperty(PropertyName = "windSpeed")]
		public double? WindSpeed { get; set; }
	}
}