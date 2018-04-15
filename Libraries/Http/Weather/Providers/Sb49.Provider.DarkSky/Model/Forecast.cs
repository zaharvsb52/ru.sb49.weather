using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sb49.Provider.DarkSky.Model
{
	public class Forecast
	{
		[JsonProperty(PropertyName = "alerts")]
		public List<Alert> Alerts { get; set; }

		[JsonProperty(PropertyName = "currently")]
		public DataPoint Currently { get; set; }

		[JsonProperty(PropertyName = "daily")]
		public DataBlock Daily { get; set; }

        /// <summary>
        /// A Flags object containing miscellaneous metadata about the request.
        /// </summary>
        [JsonProperty(PropertyName = "flags")]
		public Flags Flags { get; set; }

		[JsonProperty(PropertyName = "hourly")]
		public DataBlock Hourly { get; set; }

		[JsonProperty(PropertyName = "latitude")]
		public double Latitude { get; set; }

		[JsonProperty(PropertyName = "longitude")]
		public double Longitude { get; set; }

		[JsonProperty(PropertyName = "minutely")]
		public DataBlock Minutely { get; set; }

		//[Obsolete("Use Timezone instead")]
		//[JsonProperty(PropertyName = "offset")]
		//public string Offset { get; set; }

        /// <summary>
        /// The IANA timezone name for the requested location. This is used for text summaries and for determining when hourly and daily data block objects begin.
        /// </summary>
		[JsonProperty(PropertyName = "timezone")]
		public string Timezone { get; set; }
	}
}