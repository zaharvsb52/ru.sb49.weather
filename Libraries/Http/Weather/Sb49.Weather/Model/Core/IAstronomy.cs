namespace Sb49.Weather.Model.Core
{
    public interface IAstronomy : IAstronomyInfo
    {
        IAstronomyInfo PreviousInfo { get; }
        IAstronomyInfo NextInfo { get; }
    }
}
