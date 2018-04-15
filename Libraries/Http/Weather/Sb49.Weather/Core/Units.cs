namespace Sb49.Weather.Core
{
    public enum TemperatureUnit
    {
        Celsius,
        Fahrenheit
        //Kelvin
    }

    public enum SpeedUnit
    {
        MeterPerSec,
        KmPerHour,
        MilesPerHour
    }

    //https://en.wikipedia.org/wiki/Pressure
    public enum PressureUnit
    {
        /// <summary>
        /// 1 Hecto Pa (10^2 Pa) or 1 MilliBar.
        /// </summary>
        HectoPa,

        /// <summary>
        /// 1mb == 1hectoPa.
        /// </summary>
        MilliBars,

        MmHg,

        /// <summary>
        /// Pounds per square inch.
        /// </summary>
        Psi
    }

    public enum DistanceUnit
    {
        Meter,
        Kilometer,
        Miles
    }

    public struct Units
    {
        private const double KmPermi = 1.609344;

        public TemperatureUnit TemperatureUnit { get; set; }
        public SpeedUnit WindSpeedUnit { get; set; }
        public PressureUnit PressureUnit { get; set; }
        public DistanceUnit VisibilityUnit { get; set; }

        public static double ConvertTemperature(double temperature, TemperatureUnit from, TemperatureUnit to)
        {
            if (from == to)
                return temperature;

            if (from == TemperatureUnit.Celsius && to == TemperatureUnit.Fahrenheit)
            {
                var result = temperature*1.8 + 32;
                return result;
            }
            if (from == TemperatureUnit.Fahrenheit && to == TemperatureUnit.Celsius)
            {
                var result = (temperature - 32)/1.8;
                return result;
            }

            return temperature;
        }

        public static double ConvertSpeed(double speed, SpeedUnit from, SpeedUnit to)
        {
            if (from == to)
                return speed;
           
            const double kmphPermps = 3.6;

            if (from == SpeedUnit.KmPerHour && to == SpeedUnit.MeterPerSec)
            {
                var result = speed / kmphPermps;
                return result;
            }
            if (from == SpeedUnit.KmPerHour && to == SpeedUnit.MilesPerHour)
            {
                var result = speed/KmPermi;
                return result;
            }

            if (from == SpeedUnit.MeterPerSec && to == SpeedUnit.KmPerHour)
            {
                var result = speed*kmphPermps;
                return result;
            }
            if (from == SpeedUnit.MeterPerSec && to == SpeedUnit.MilesPerHour)
            {
                var result = speed*kmphPermps/KmPermi;
                return result;
            }

            if (from == SpeedUnit.MilesPerHour && to == SpeedUnit.KmPerHour)
            {
                var result = speed*KmPermi;
                return result;
            }
            if (from == SpeedUnit.MilesPerHour && to == SpeedUnit.MeterPerSec)
            {
                var result = speed*KmPermi/kmphPermps;
                return result;
            }

            return speed;
        }

        //https://en.wikipedia.org/wiki/Pressure
        //https://en.wikipedia.org/wiki/Hecto-
        public static double ConvertPressure(double pressure, PressureUnit from, PressureUnit to)
        {
            if (from == to || (from == PressureUnit.HectoPa && to == PressureUnit.MilliBars) ||
                (from == PressureUnit.MilliBars && to == PressureUnit.HectoPa))
            {
                return pressure;
            }

            const double paPermmHg = 133.322;
            const double psiPerpa = 1.450377;

            if ((from == PressureUnit.HectoPa || from == PressureUnit.MilliBars) && to == PressureUnit.MmHg)
            {
                var result = pressure*100/paPermmHg;
                return result;
            }
            if ((from == PressureUnit.HectoPa || from == PressureUnit.MilliBars) && to == PressureUnit.Psi)
            {
                var result = pressure*100*psiPerpa;
                return result;
            }

            if (from == PressureUnit.MmHg && (to == PressureUnit.HectoPa || to == PressureUnit.MilliBars))
            {
                var result = pressure*paPermmHg/100;
                return result;
            }
            if (from == PressureUnit.MmHg && to == PressureUnit.Psi)
            {
                var result = pressure*paPermmHg*psiPerpa;
                return result;
            }

            if (from == PressureUnit.Psi && (to == PressureUnit.HectoPa || to == PressureUnit.MilliBars))
            {
                var result = pressure / psiPerpa /100;
                return result;
            }
            if (from == PressureUnit.Psi && to == PressureUnit.MmHg)
            {
                var result = pressure/psiPerpa/paPermmHg;
                return result;
            }

            return pressure;
        }

        public static double ConvertDistance(double distance, DistanceUnit from, DistanceUnit to)
        {
            if (from == to)
                return distance;

            if (from == DistanceUnit.Meter && to == DistanceUnit.Kilometer)
            {
                var result = distance/1000;
                return result;
            }
            if (from == DistanceUnit.Meter && to == DistanceUnit.Miles)
            {
                var result = distance/1000/KmPermi;
                return result;
            }

            if (from == DistanceUnit.Kilometer && to == DistanceUnit.Meter)
            {
                var result = distance*1000;
                return result;
            }
            if (from == DistanceUnit.Kilometer && to == DistanceUnit.Miles)
            {
                var result = distance / KmPermi;
                return result;
            }

            if (from == DistanceUnit.Miles && to == DistanceUnit.Meter)
            {
                var result = distance*KmPermi*1000;
                return result;
            }
            if (from == DistanceUnit.Miles && to == DistanceUnit.Kilometer)
            {
                var result = distance * KmPermi;
                return result;
            }

            return distance;
        }
    }
}