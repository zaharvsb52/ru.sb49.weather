namespace Sb49.Provider.DarkSky.Model
{
	public class DarkSkyResponse
	{
		public string AttributionLine => "Powered by Dark Sky";

		public string DataSource => "https://darksky.net/poweredby/";

		public ResponseHeaders Headers { get; set; }

		public string Response { get; set; }
	}
}