namespace Sb49.GoogleGeocoder
{
    public class GoogleComponentFilter
    {
        public GoogleComponentFilter(string component, string value)
        {
            ComponentFilter = string.Format("{0}:{1}", component, value);
        }

        public string ComponentFilter { get; set; }
    }
}