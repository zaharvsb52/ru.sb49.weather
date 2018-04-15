using System;
using Sb49.Weather.Code;

namespace Sb49.Weather.Model.Core
{
    public interface IWeatherDataPoint : IDisposable
    {
        double? ApparentTemperature { get;  }
        double? Temperature { get;  }
        double? MinTemperature { get;  }
        double? MaxTemperature { get;  }
        double? Pressure { get;  }
        double? Humidity { get;  }
        double? Visibility { get;  }
        double? WindDirection { get;  }
        double? WindSpeed { get;  }
        double? DewPoint { get;  }
        WeatherCodes WeatherCode { get;  }
        string WeatherUnrecognizedCode { get;  }
        string Condition { get;  }
        string Icon { get;  }

        /// <summary>
        /// The Utc date of calculation.
        /// </summary>
        DateTime? Date { get; }

        IAstronomy Astronomy { get; }
        
        object Clone();
    }
}
