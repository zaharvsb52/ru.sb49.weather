using System;

namespace Sb49.Weather.Code
{
    [Flags]
    public enum WeatherCodes : long
    {
        Undefined = 0,
        Error = -1,
        ClearSky = 0x1, //sun
        Clouds = 0x2,
        Rain = 0x4,
        Thunderstorm = 0x8, //гроза
        Snow = 0x10,
        Fog = 0x20,
        Hail = 0x40,
        Light = 0x10000000,
        Heavy = 0x20000000,
        Storm = 0x40000000,
        Extreme = 0x80000000, //tornado, tropical storm, hurricane 
        FewClouds = ClearSky | Clouds,
        LightRain = Light | Rain,
        HeavyRain = Heavy | Rain,
        LightSnow = Light | Snow,
        HeavySnow = Heavy | Snow,
        SnowStorm = Storm | Snow,
        RainAndSnow = Rain | Snow,
        FreezingRain = RainAndSnow,
        HeavyRainAndSnow = Heavy | Rain | Snow
    }
}