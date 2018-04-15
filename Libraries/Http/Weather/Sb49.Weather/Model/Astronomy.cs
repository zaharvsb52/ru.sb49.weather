using Sb49.Weather.Model.Core;

namespace Sb49.Weather.Model
{
    public class Astronomy : AstronomyInfo, IAstronomy
    {
        public IAstronomyInfo PreviousInfo { get; set; }
        public IAstronomyInfo NextInfo { get; set; }
   }
}