using System;

namespace Sb49.Weather
{
    //http://climate.umn.edu/snow_fence/components/winddirectionanddegreeswithouttable3.htm
    //http://stackoverflow.com/questions/7490660/converting-wind-direction-in-angles-to-text-words

    public sealed class WindTools
    {
        public WindCardinalDirection DegreesToCardinal(double degrees)
        {
            //string[] caridnals = { "N", "NE", "E", "SE", "S", "SW", "W", "NW", "N" };
            var index = (int) Math.Round((degrees%360)/45);
            if (index > (int) WindCardinalDirection.Nw)
                index = 0;
            return (WindCardinalDirection) index;
        }

        public string GetString(WindCardinalDirection cardinal)
        {
            return GetString((Enum) cardinal);
        }

        public WindCardinalDetailedDirection DegreesToCardinalDetailed(double degrees)
        {
            degrees *= 10;

            //string[] caridnals = { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW", "N" };
            var index = (int) Math.Round((degrees%3600)/225);
            if (index > (int) WindCardinalDetailedDirection.Nnw)
                index = 0;
            return (WindCardinalDetailedDirection)index;
        }

        public string GetString(WindCardinalDetailedDirection cardinal)
        {
            return GetString((Enum) cardinal);
        }

        public double DegreeToCompass(double degrees)
        {
            return Rotate(degrees, 180);
        }

        public double Rotate(double degrees, double angle)
        {
            return (degrees + angle) % 360;
        }

        private static string GetString(Enum cardinal)
        {
            if(cardinal == null)
                throw new ArgumentNullException();

            return Properties.Resources.ResourceManager.GetString(string.Format("{0}", cardinal));
        }
    }

    public enum WindCardinalDirection
    {
        N = 0,
        Ne = 1,
        E = 2,
        Se = 3,
        S = 4,
        Sw = 5,
        W = 6,
        Nw = 7
    }

    public enum WindCardinalDetailedDirection
    {
        N = 0,
        Nne = 1,
        Ne = 2,
        Ene = 3,
        E = 4,
        Ese = 5,
        Se = 6,
        Sse = 7,
        S = 8,
        Ssw = 9,
        Sw = 10,
        Wsw = 11,
        W = 12,
        Wnw = 13,
        Nw = 14,
        Nnw = 15
    }
}