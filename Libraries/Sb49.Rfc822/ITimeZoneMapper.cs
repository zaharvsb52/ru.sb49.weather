namespace Sb49.Rfc822
{
    /// <summary>
    /// Maps an RFC 822 time zone identifier to a time zone.
    /// </summary>
    public interface ITimeZoneMapper
    {
        TimeZone Map(string identifier);
    }
}
