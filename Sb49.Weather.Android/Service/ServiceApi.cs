using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Locations;
using Android.Net;
using Sb49.Common.Logging;
using Sb49.Common.Support.v7.Droid.Managers;
using Sb49.Common.Support.v7.Droid.Permissions;
using Sb49.Security.Core;
using Sb49.Weather.Core;
using Sb49.Weather.Droid.Common;
using Sb49.Weather.Droid.Common.Exeptions;
using Sb49.Weather.Droid.Model;
using Sb49.Weather.Droid.Ui.AppWidget.Updaters;
using Sb49.Weather.Exceptions;
using Sb49.Weather.Model;
using AndroidUtil = Sb49.Common.Droid.Util;

namespace Sb49.Weather.Droid.Service
{
    public sealed class ServiceApi
    {
        private static readonly ILog Log = LogManager.GetLogger<ServiceApi>();

        public int WaitMaxMsec { get; set; } = AppSettings.WaitMaxMsec;
        public int WaitGeoMsec { get; set; } = AppSettings.WaitGeoMaxMsec;

        #region Service

        public void StartService(Context context, string action)
        {
            StartService(context, new Intent(action));
        }

        public void StartService(Context context, Intent intent)
        {
            var explicitintent = AndroidUtil.CreateExplicitFromImplicitIntent(context, intent);
            context.StartService(explicitintent);
        }

        public void StopService(Context context, string action)
        {
            context.StopService(new Intent(action));
        }

        public void StopService(Context context, Intent intent)
        {
            var explicitintent = AndroidUtil.CreateExplicitFromImplicitIntent(context, intent);
            context.StopService(explicitintent);
        }

        //public bool IsServiceRunning(Context context, Type type)
        //{
        //    var serviceClassName = Java.Lang.Class.FromType(type).Name;
        //    return IsServiceRunning(context, serviceClassName);
        //}

        //public bool IsServiceRunning(Context context, string javaClassName)
        //{
        //    if (string.IsNullOrEmpty(javaClassName))
        //        return false;

        //    var activityManager = (ActivityManager)context.GetSystemService(Context.ActivityService); 
        //    var services = activityManager.GetRunningServices(int.MaxValue); //is deprecated
        //    return services?.Any(p =>
        //        string.Equals(p.Service.ClassName, javaClassName, StringComparison.OrdinalIgnoreCase)) == true;
        //}

        public void AppWidgetWeatherDataUpdateService(Context context, bool isBootCompletedReceiver, CancellationToken token)
        {
            var appWidgetIds = GetWeatherDataUpdateAppWidgetIds(context, isBootCompletedReceiver, token);
            AppWidgetWeatherDataUpdateService(context, appWidgetIds);
        }

        public void AppWidgetWeatherDataUpdateService(Context context, int[] widgetIds, PendingIntent pendingIntent = null)
        {
            if (widgetIds == null || widgetIds.Length == 0)
                return;

            var intent = new Intent(WidgetService.ActionUpdateWeatherData);
            intent.PutExtra(WidgetService.ExtraAppWidgetIds, widgetIds);
            if (pendingIntent != null)
                intent.PutExtra(WidgetService.ExtraPendingIntent, pendingIntent);
            StartService(context, intent);
        }

        public void AppWidgetUpdateService(Context context, int[] widgetIds)
        {
            var intent = new Intent(WidgetService.ActionWidgetsUpdate);
            if (widgetIds != null && widgetIds.Length > 0)
                intent.PutExtra(WidgetService.ExtraAppWidgetIds, widgetIds);
            StartService(context, intent);
        }

        #endregion Service

        #region AppWidget

        public int[] GetAppWidgetsIds(Context context)
        {
            var updater = new Updater();
            var result = updater.GetAppWidgetsIds(context, AppSettings.Default.AppWidgetProviderClassNames);
            return result;
        }

        public void AppWidgetUpdate(Context context, int[] widgetsIds)
        {
            var updater = new Updater();
            if (widgetsIds == null || widgetsIds.Length == 0)
            {
                updater.UpdateAppWidget(context, AppSettings.Default.AppWidgetProviderClassNames);
                return;
            }

            updater.UpdateAppWidget(context, widgetsIds);
        }

        public bool ExistsAppWidget(Context context)
        {
            var widgetIds = GetAppWidgetsIds(context);
            return widgetIds != null && widgetIds.Length > 0;
        }

        public void ArrangeAppWidgetSettings(Context context)
        {
            // настройки AppWidget'ов
            var widgetsSettings = AppSettings.Default.GetAppWidgetSettings();
            
            // получаем все AppWidgetIds
            var rightWidgetIds = GetAppWidgetsIds(context);
            var rightWidgetIdsLength = rightWidgetIds?.Length ?? 0;

            void WidgetLogErrorHandler(int count)
            {
                Log.ErrorFormat("Exist{0} {1} AppWidget{2} without settings.", count > 1 ? null : "s", count, count > 1 ? "s" : null);
            }

            if (widgetsSettings == null || widgetsSettings.Count == 0)
            {
                // если AppWidget'ов нет - выходим
                if (rightWidgetIdsLength == 0)
                    return;

                // если AppWidget'ы существуют - пишем лог
                WidgetLogErrorHandler(rightWidgetIdsLength);
                return;
            }

            int[] settingsDeleted;
            int[] widgetIdsDeleted = null;
            if (rightWidgetIdsLength == 0)
            {
                settingsDeleted = widgetsSettings.Values.Where(p => p != null).Select(p => p.WidgetId).ToArray();
            }
            else
            {
                widgetIdsDeleted = rightWidgetIds?.Where(p => !widgetsSettings.ContainsKey(p)).ToArray();
                settingsDeleted =
                    widgetsSettings.Values.Where(p => p?.WidgetId != null && rightWidgetIds != null && !rightWidgetIds.Contains(p.WidgetId))
                        .Select(p => p.WidgetId).ToArray();
            }

            var widgetIdsDeletedLength = widgetIdsDeleted?.Length ?? 0;
            if (widgetIdsDeletedLength > 0)
                WidgetLogErrorHandler(widgetIdsDeletedLength);

            var settingsDeletedLength = settingsDeleted.Length;
            if (settingsDeletedLength > 0)
            {
                AppSettings.Default.DeleteAppWidgetSettings(settingsDeleted);
                Log.DebugFormat("Deleted {0} suspended AppWidget settings.", settingsDeletedLength);
            }
        }

        #endregion AppWidget

        #region Weather

        public int[] GetWeatherDataUpdateAppWidgetIds(Context context, bool isBootCompletedReceiver, CancellationToken token)
        {
            if (IfCancellationRequested(token))
                return null;

            var widgetsSettings = AppSettings.Default.GetAppWidgetSettings();
            if (widgetsSettings == null || widgetsSettings.Count == 0)
                return null;

            if (IfCancellationRequested(token))
                return null;

            var widgetIdsList = new List<int>();
            foreach (var settings in widgetsSettings.Values.Where(p => p != null))
            {
                if (IfCancellationRequested(token))
                    return null;

                if (settings.WeatherDataUpdating)
                    continue;

                if (isBootCompletedReceiver)
                {
                    widgetIdsList.Add(settings.WidgetId);
                    continue;
                }

                var updatedDate = settings.Weather?.UpdatedDate;
                if (!updatedDate.HasValue)
                {
                    widgetIdsList.Add(settings.WidgetId);
                    continue;
                }

                if (settings.WeatherServiceRefreshIntervalMsec <= 0)
                    continue;

                var timerMsec = (DateTime.UtcNow - updatedDate.Value).TotalMilliseconds;
                if (timerMsec >= settings.WeatherServiceRefreshIntervalMsec)
                    widgetIdsList.Add(settings.WidgetId);
            }

            if (IfCancellationRequested(token))
                return null;

            if (widgetIdsList.Count == 0)
                return widgetIdsList.ToArray();

            var widgetIds = widgetIdsList.Distinct().ToArray();

            if (IfCancellationRequested(token))
                return null;

            return widgetIds;
        }

        public void UpdateWeatherData(Context context, int[] widgetIds, CancellationToken token)
        {
            if (IfCancellationRequested(token))
                return;

            if (widgetIds == null || widgetIds.Length == 0)
                return;

            foreach (var widgetId in widgetIds)
            {
                if (IfCancellationRequested(token))
                    return;

                var settings = AppSettings.Default.FindAppWidgetSettings(widgetId);
                if (settings == null || settings.WeatherDataUpdating)
                    continue;

                var data = GetWeatherData(context, settings, token) ?? throw new NothingForecastException();

                if (IfCancellationRequested(token))
                    return;

                settings.Weather = data;

                AppSettings.Default.SaveAppWidgetSettings(settings);
            }
        }

        public WeatherForecast GetWeatherData(Context context, IAppSettings settings, CancellationToken token)
        {
            if (IfCancellationRequested(token))
                return null;

            if (settings == null || settings.WeatherDataUpdating)
                return null;

            try
            {
                settings.BeginWeatherDataUpdate();

                if (settings.UseTrackCurrentLocation)
                {
                    try
                    {
                        var currentLocationAddress = GetCurrentLocation(context, token);
                        settings.LocationAddress = currentLocationAddress ??
                                                   throw new ArgumentNullException(nameof(currentLocationAddress),
                                                       @"Location address is null");
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Can't get current location (GetCurrentLocation).", ex);
                    }
                }

                if (IfCancellationRequested(token))
                    return null;

                var address = settings.LocationAddress;
                var hasCoordinates = address?.HasCoordinates;
                if (IfCancellationRequested(token))
                    return null;

                var task = Task.Run(
                    () => GetWeatherData(context, settings.WeatherApiKey, settings.WeatherProviderService,
                        address, AppSettings.Default.Language, token), token);
                TaskWait(task, WaitMaxMsec, token);

                var data = task.Result;
                if (data == null)
                    throw new NothingForecastException();

                if (IfCancellationRequested(token))
                    return null;

                if (address != null && hasCoordinates == false && address.HasCoordinates)
                {
                    if (settings.IsNotAppWidget)
                        AppSettings.Default.SaveLocationAddress(address, AppSettings.Default.SelectedLocationAddressId);
                    else
                        settings.LocationAddress = address;
                }

                return data;
            }
            finally
            {
                settings.EndWeatherDataUpdate();
            }
        }

        private WeatherForecast GetWeatherData(Context context, ISb49SecureString apiKey, IWeatherProviderService weatherProvider,
            IWeatherLocationAddress address, string languageCode, CancellationToken token)
        {
            if (weatherProvider == null)
                throw new WeatherProviderException();

            if (apiKey == null || !apiKey.Validate())
                throw new WeatherApiKeyException();

            if (address == null || !address.IsValid())
                throw new LocationException();

            var permissionChecker = new Sb49PermissionChecker();
            var permissions = new[] { Manifest.Permission.Internet, Manifest.Permission.AccessNetworkState };
            var status = permissionChecker.CheckPermissionAsync(context, permissions).Result;
            if (status != PermissionStatus.Granted)
            {
                throw new Java.Lang.SecurityException(string.Format("Permission denied. Missing {0} permissions.",
                    string.Join(", ", permissions.Select(p => string.Format("'{0}'", p)))));
            }

            weatherProvider.ApiKey = apiKey;
            if (weatherProvider.OptionalParameters == null)
                weatherProvider.OptionalParameters = new OptionalParameters();
            weatherProvider.OptionalParameters.LanguageCode = languageCode;

            using (var connectivityManager = (ConnectivityManager)context.GetSystemService(Context.ConnectivityService))
            {
                if (!AndroidUtil.IsNetworkAvailable(connectivityManager))
                    throw new Sb49HttpRequestException();

                Log.DebugFormat(
                    "Weather provider: '{0}', Uri: '{1}', Lan: '{2}', Count: '{3}', Connectivity type: '{4}', Address: '{5}'.",
                    weatherProvider.ProviderId, weatherProvider.BaseUri, languageCode,
                    weatherProvider.RequestCounter?.Count,
                    connectivityManager.ActiveNetworkInfo?.Type,
                    address);
            }

            // TwilightCache
            var twilightCacheManager = new TwilightCacheManager();
            try
            {
                var twilightCache = twilightCacheManager.RetrieveData(context, AppSettings.TwilightCacheKey);
                weatherProvider.SetSunInfoCache(twilightCache);
            }
            catch (Newtonsoft.Json.JsonSerializationException ex)
            {
                Log.Error(ex);
                twilightCacheManager.Remove(context, AppSettings.TwilightCacheKey);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            var result = weatherProvider.GetForecast(address, token: token);

            if (result != null)
            {
                if (!address.Latitude.HasValue)
                    address.Latitude = weatherProvider.Latitude;
                if (!address.Longitude.HasValue)
                    address.Longitude = weatherProvider.Longitude;

                result.AddressHashCode = address.GetMd5Code();

                // TwilightCache
                try
                {
                    var cache = weatherProvider.GetSunInfoCache(TwilightCacheManager.MaxCount);
                    twilightCacheManager.CacheData(context, AppSettings.TwilightCacheKey, cache);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }

            Log.Debug("Weather data updated.");

            return result;
        }

        #endregion Weather

        #region Location

        public Address[] GetCurrentLocations(Context context, CancellationToken token, int geoMaxResult)
        {
            if (IfCancellationRequested(token))
                return null;

            var currentLocation = AppSettings.Default.CurrentLocation;
            if (currentLocation == null || !currentLocation.HasCoordinates)
                throw new LocationException(context.GetString(Resource.String.UndefinedCurrentLocation));

            var geo = new Sb49Geocoder();
            // ReSharper disable PossibleInvalidOperationException
            var task = geo.GetFromLocationAsync(context, currentLocation.Latitude.Value, currentLocation.Longitude.Value,
                geoMaxResult, token);
            // ReSharper restore PossibleInvalidOperationException
            TaskWait(task, WaitGeoMsec, token);
            var addrs = task.Result?.ToArray();

            if (IfCancellationRequested(token))
                return null;

            return addrs == null || addrs.Length == 0
                ? new[]
                {
                    new Address(Java.Util.Locale.Default)
                    {
                        Latitude = currentLocation.Latitude.Value,
                        Longitude = currentLocation.Longitude.Value
                    }
                }
                : addrs;
        }

        public LocationAddress GetCurrentLocation(Context context, CancellationToken token)
        {
            var address = GetCurrentLocations(context: context, token: token, geoMaxResult: 1)?.FirstOrDefault();
            return address == null ? null : new LocationAddress(address);
        }

        public Location GetLastKnownLocation(Context context)
        {
            using (var locationManager = new Sb49LocationManager(context))
            {
                return locationManager.GetLastKnownLocation();
            }
        }

        public bool UseTrackCurrentLocation(Context context)
        {
            if (AppSettings.Default.UseTrackCurrentLocation)
                return true;

            var settings = AppSettings.Default.GetAppWidgetSettings();
            var result = settings?.Count > 0 && settings.Any(p => p.Value?.UseTrackCurrentLocation == true);
            return result;
        }

        #endregion Location

        public void TaskWait(Task task, int waitMsec, CancellationToken token)
        {
            if (IfCancellationRequested(token))
                return;

            var date0 = DateTime.Now;
            task.Wait(waitMsec, token);
            if(!task.IsCompleted && !task.IsCanceled && !task.IsFaulted &&
                (DateTime.Now - date0).TotalMilliseconds >= waitMsec)
            {
                throw new TimeoutException();
            }

            if (task.Exception != null && !task.IsCanceled)
                throw task.Exception;
        }

        private static bool IfCancellationRequested(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return false;
        }
    }
}