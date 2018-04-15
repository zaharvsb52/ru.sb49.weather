using System;

namespace Sb49.Weather.Droid.Common
{
    [Flags]
    public enum SettingsChangeStatus
    {
        None = 0,
        Changed = 0x1,
        LogChanged=0x2,
        NeedAppWidgetUpdate = 0x4,
        NeedAppWidgetWeatherDataUpdate = 0x8,
        NeedRequestCounterUpdate = 0x10,
        Skip = 0x20
    }
}