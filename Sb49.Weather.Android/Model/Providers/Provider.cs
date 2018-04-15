namespace Sb49.Weather.Droid.Model
{
    public sealed class Provider
    {
        public Provider(int id)
        {
            Id = id;
        }

        public int Id { get; }
        public ProviderTypes ProviderType { get; set; }
        public int TitleId { get; set; }
        public int? UrlApiId { get; set; }
        public bool IsReadOnly { get; set; }
    }
}