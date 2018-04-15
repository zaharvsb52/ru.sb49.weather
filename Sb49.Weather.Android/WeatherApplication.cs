using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Util;
using Microsoft.Practices.Unity;
using Newtonsoft.Json;
using Sb49.Common.Droid;
using Sb49.Common.Logging;
using Sb49.Common.Logging.Log4Net.Droid;
using Sb49.Security.Core;
using Sb49.Security;
using Sb49.Weather.Droid.Common;
using Sb49.Weather.Droid.Common.Crypto;
using Sb49.Weather.Droid.Common.Exeptions;
using Sb49.Weather.Droid.Model;
using Sb49.Weather.Droid.Service;
using Sb49.Geocoder.Core;
using Sb49.Weather.Core;
using Sb49.Weather.Droid.Common.Impl;
using Sb49.Weather.Droid.Common.Json;
using Sb49.Weather.Droid.Ui.AppWidget.Providers;
using Sb49.Weather.Droid.Ui.AppWidget.Updaters;
using Sb49.Weather.Droid.Ui.AppWidget.Updaters.Core;
using AndroidUtil = Sb49.Common.Droid.Util;

namespace Sb49.Weather.Droid
{
    //http://motzcod.es/post/133609925342/access-the-current-android-activity-from-anywhere
    
#if DEBUG
    [Application(Debuggable = true, Icon = "@mipmap/ic_launcher", Theme = "@style/Theme.Custom")]
#else
    [Application(Debuggable = false, Icon = "@mipmap/ic_launcher", Theme = "@style/Theme.Custom")]
#endif
    //[MetaData("com.google.android.maps.v2.API_KEY", Value = "@string/GoogleMapsGeocodingApiKey")]
    //[MetaData("com.google.android.gms.version", Value = "@integer/google_play_services_version")]
    internal sealed class WeatherApplication : Application
    {
        private ILog _log;

        public WeatherApplication(IntPtr handle, JniHandleOwnership transer)
          : base(handle, transer)
        {
            //AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            //{
            //    _log?.ErrorFormat("Exception caught through Mono: {0}", e.ExceptionObject);
            //};

            AndroidEnvironment.UnhandledExceptionRaiser += (s, e) =>
            {
                e.Handled = true;
                var message = string.Format("Unhandled exception. {0}", e.Exception);

                try
                {
                    _log?.Error(message);
                }
                finally
                {
                    Log.Error(AppSettings.Tag, message);
                }
            };

            if (Build.VERSION.SdkInt <= BuildVersionCodes.Kitkat)
                AppCompatDelegate.CompatVectorFromResourcesEnabled = true;
        }

        public override void OnCreate()
        {
            base.OnCreate();

            // A great place to initialize Xamarin.Insights and Dependency Services!

            // конфигурируем приложение
            var container = new UnityContainer();
            Configure(container);

            // start app
            _log?.InfoFormat("====== Start on '{0}'. OS: '{1}'. Ver. '{2} ({3})'.",
                AndroidUtil.GetDeviceName(),
                AndroidUtil.GetAndroidVersion(),
                AppSettings.Default.VersionName,
                AssemblyAttributeAccessors.GetAssemblyFileVersion(GetType()));

            try
            {
                // json settings
                //http://www.newtonsoft.com/json/help/html/PreserveReferencesHandlingObject.htm
                //http://stackoverflow.com/questions/13510204/json-net-self-referencing-loop-detected
                //https://gist.github.com/carlin-q-scott/4c8a9cce734fa5b10a97
                JsonConvert.DefaultSettings = () => new JsonSerializerSettings
                {
#if DEBUG
                    Formatting = Formatting.Indented,
#endif
                    ContractResolver = ShouldSerializeContractResolver.Instance,
                    TypeNameHandling = TypeNameHandling.Auto
                    //PreserveReferencesHandling = PreserveReferencesHandling.Objects
                    //ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };

                Task.Run(() =>
                {
                    try
                    {
                        var api = new ServiceApi();
                        api.StartService(this, ControlService.ActionAppStart);

                        _log?.Info("------ Init is completed.");
                    }
                    catch (Exception ex)
                    {
                        _log?.Error(ex);
                    }
                });
            }
            catch (Exception ex)
            {
                _log?.Error(ex);
            }
        }

        public override void OnLowMemory()
        {
            _log?.Info("OnLowMemory");
            AppSettings.Default?.ClearCache();
            AppSettings.GcCollect(true, _log); //HARDCODE:

            base.OnLowMemory();
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                OnDispose();
            }
            catch (Exception ex)
            {
                _log?.Debug(ex);
            }

            base.Dispose(disposing);
        }

        #region Configure

        private void Configure(IUnityContainer container)
        {
            //https://msdn.microsoft.com/en-us/library/ff660872(v=pandp.20).aspx
            container.RegisterType<ISb49SecureString, Sb49SecureString>(new InjectionConstructor(typeof(string)));
            container.RegisterType<SecureStringConverter>();

            container.RegisterType<ISb49Crypto>(new ContainerControlledLifetimeManager(),
                new InjectionFactory(c => new Sb49Crypto(GetString(Resource.String.Key))));

            // инициализируем AppWidgetUpdater
            var appWidgetProviderClassNames = new List<string>();

            var appWidgetProviderClassName = Java.Lang.Class.FromType(typeof(WeatherAppWidgetProvider)).Name;
            container.RegisterType<IAppWidgetUpdater>(appWidgetProviderClassName,
                new InjectionFactory(c => new WeatherAppWidgetUpdater()));
            appWidgetProviderClassNames.Add(appWidgetProviderClassName);

            appWidgetProviderClassName = Java.Lang.Class.FromType(typeof(WeatherClockAppWidgetProvider)).Name;
            container.RegisterType<IAppWidgetUpdater>(appWidgetProviderClassName,
                new InjectionFactory(c => new WeatherClockAppWidgetUpdater()));
            appWidgetProviderClassNames.Add(appWidgetProviderClassName);

            // инициализируем Weather service
            container.RegisterType<IWeatherProviderServiceFactory, WeatherProviderServiceFactory>(
                new ContainerControlledLifetimeManager());

            var providers = new ConcurrentDictionary<int, Model.Provider>();
            const string delimeter = LocationAddress.SpaceDelimeter;

            var providerId = 0;
            container.RegisterType<IWeatherProviderService>(providerId.ToString(),
                new InjectionFactory((c, t, key) =>
                {
                    var provid = int.Parse(key);
                    return new Provider.YahooWeather.WeatherProviderService(provid, AppSettings.Default.GetRequestCounter(provid))
                    {
                        OptionalParameters =
                            new OptionalParameters
                            {
                                MeasurementUnits = Provider.YahooWeather.WeatherProviderUnits.Imperial.ToString()
                            },
                        ForecastHandler =
                            (provider, address, parameters, token) =>
                            {
                                if (address.HasCoordinatesOnly)
                                {
                                    // ReSharper disable PossibleInvalidOperationException
                                    return provider.GetForecast(address.Latitude.Value, address.Longitude.Value,
                                        parameters, token);
                                    // ReSharper restore PossibleInvalidOperationException
                                }

                                return provider.GetForecast(address.GetAddress(delimeter), parameters, token);
                            }
                    };
                }));
            var defaultProviderId = providerId;
            providers[providerId] = new Model.Provider(providerId)
            {
                ProviderType = ProviderTypes.WeatherProvider,
                IsReadOnly = true,
                TitleId = Resource.String.YahooWeather,
                UrlApiId = Resource.String.YahooWeatherUrl
            };

            container.RegisterType<IWeatherProviderService>((++providerId).ToString(),
                new InjectionFactory((c, o, key) =>
                {
                    var provid = int.Parse(key);
                    return new Provider.DarkSky.WeatherProviderService(provid,
                        AppSettings.Default.GetRequestCounter(provid))
                    {
                        OptionalParameters =
                            new OptionalParameters
                            {
                                MeasurementUnits = Provider.DarkSky.WeatherProviderUnits.Si.ToString()
                            },
                        ForecastHandler = (provider, address, parameters, token) =>
                        {
                            if (!address.HasCoordinates)
                                throw new LocationException(
                                    Context.GetString(Resource.String.UndefinedAddressCoordinates));

                            // ReSharper disable PossibleInvalidOperationException
                            return provider.GetForecast(address.Latitude.Value, address.Longitude.Value, parameters,
                                token);
                            // ReSharper restore PossibleInvalidOperationException
                        }
                    };
                }));
            providers[providerId] = new Model.Provider(providerId)
            {
                ProviderType = ProviderTypes.WeatherProvider,
                TitleId = Resource.String.DarkSkyWeather,
                UrlApiId = Resource.String.DarkSkyWeatherUrlApi
            };

            container.RegisterType<IWeatherProviderService>((++providerId).ToString(),
                new InjectionFactory((c, o, key) =>
                {
                    var provid = int.Parse(key);
                    return new Provider.OpenWeatherMap.WeatherProviderService(provid,
                        AppSettings.Default.GetRequestCounter(provid))
                    {
                        OptionalParameters =
                            new OptionalParameters
                            {
                                MeasurementUnits = Provider.OpenWeatherMap.WeatherProviderUnits.Metric.ToString()
                            },
                        ForecastHandler = (provider, address, parameters, token) =>
                        {
                            if (address.HasCoordinates)
                            {
                                // ReSharper disable PossibleInvalidOperationException
                                return provider.GetForecast(address.Latitude.Value, address.Longitude.Value, parameters,
                                    token);
                                // ReSharper restore PossibleInvalidOperationException
                            }

                            return provider.GetForecast(address.GetAddress(delimeter), parameters, token);
                        }
                    };
                }));
            providers[providerId] = new Model.Provider(providerId)
            {
                ProviderType = ProviderTypes.WeatherProvider,
                TitleId = Resource.String.OpenWeatherMap,
                UrlApiId = Resource.String.OpenWeatherMapUrlApi
            };

            // инициализируем GoogleGeocoder
            providerId = AppSettings.GoogleMapsGeocodingApiProviderId;
            container.RegisterType<IGeocoder>(new InjectionFactory(c =>
                new GoogleGeocoder.Impl.GeocoderImpl(AppSettings.Default.GetRequestCounter(providerId))));
            providers[providerId] = new Model.Provider(providerId)
            {
                ProviderType = ProviderTypes.GoogleApiProvider,
                TitleId = Resource.String.GoogleMapsGeocodingApiTitle,
                UrlApiId = Resource.String.GoogleUrlApi
            };

            // инициализируем settings
            var typeofsettings = typeof(AppSettings);
            AppSettings settings = null;
            var property = typeofsettings.GetProperty("Default", BindingFlags.Static | BindingFlags.Public);
            if (property != null)
            {
                var ctor =
                    typeofsettings.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).FirstOrDefault();
                if (ctor != null)
                {
                    settings = (AppSettings) ctor.Invoke(new object[]
                        {container, providers, defaultProviderId, appWidgetProviderClassNames.ToArray()});
                    property.SetValue(settings, settings);
                }
            }

            // logger
            LogManager.Adapter = new Log4NetLoggerFactoryAdapter();
            AppSettings.Default.LoggingConfigure();
            _log = LogManager.GetLogger<WeatherApplication>();
            var field = typeofsettings.GetField("_log", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
                field.SetValue(settings, LogManager.GetLogger(typeofsettings));
        }

        #endregion Configure

        private void OnDispose()
        {
            AppSettings.Default.Dispose();
        }
    }
}