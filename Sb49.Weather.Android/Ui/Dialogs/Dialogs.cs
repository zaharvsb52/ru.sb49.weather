using System;
using Android.App;
using Android.Gms.Common;
using Android.Text;
using Android.Widget;
using Sb49.Common.Droid.Ui;
using Sb49.Common.Logging;
using Sb49.Http.Provider.Exeptions;
using Sb49.Weather.Droid.Common;
using Sb49.Weather.Droid.Model;
using Sb49.Weather.Exceptions;

namespace Sb49.Weather.Droid.Ui
{
    public sealed class Dialogs
    {
        public bool GoogleApiAvailabilityDialog(Activity activity, int requestCode)
        {
            if (activity == null)
                throw new ArgumentNullException(nameof(activity));

            int errorCode;
            var result = AppSettings.Default.CheckIsGooglePlayServicesInstalled(out errorCode);
            if (!result)
            {
                var dialog = GoogleApiAvailability.Instance.GetErrorDialog(activity, errorCode, requestCode);
                dialog.Show();
            }

            return result;
        }

        public bool ApiKeyDialog(Activity activity, ProviderTypes providerType, int providerId,
            bool alwaysShowDialog, Action okAction, Action cancelAction, ToastLength toastLength, ILog log)
        {
            if (activity == null)
                throw new ArgumentNullException(nameof(activity));

            var isValidServiceProvider = true;
            var isValidApiKey = true;

            switch (providerType)
            {
                case ProviderTypes.WeatherProvider:
                    isValidServiceProvider = AppSettings.Default.ValidateWeatherProvider(providerId);
                    break;
            }

            if (isValidServiceProvider)
                isValidApiKey = AppSettings.Default.ValidateApiKey(providerId);

            var result = isValidServiceProvider && isValidApiKey;
            if (alwaysShowDialog || !result)
            {
                int providerNameId;
                switch (providerType)
                {
                    case ProviderTypes.WeatherProvider:
                        providerNameId = AppSettings.Default.GetWeatherProviderNameById(providerId);
                        break;
                    default:
                        providerNameId = AppSettings.Default.GetProviderNameById(providerId);
                        break;
                }
                
                string message = null;

                activity.RunOnUiThread(() =>
                {
                    if (!isValidServiceProvider)
                    {
                        message = string.Format(activity.GetString(Resource.String.WeatherProviderIsNotImplemented),
                            providerNameId);
                    }
                    else if (alwaysShowDialog || !isValidApiKey)
                    {
                        string GetErrorHandler()
                        {
                            switch (providerType)
                            {
                                case ProviderTypes.WeatherProvider:
                                    return Properties.Resources.UndefinedWeatherProviderApiKey;
                                default:
                                    return Http.Properties.Resources.UndefinedServerApiKey;
                            }
                        }

                        Exception GetExceptionHandler()
                        {
                            switch (providerType)
                            {
                                case ProviderTypes.WeatherProvider:
                                    return new WeatherApiKeyException();
                                default:
                                    return new ApiKeyException();
                            }
                        }

                        var dlg = new WidgetUtil();
                        dlg.InputDialog(context: activity, titleId: providerNameId,
                            layoutId: Resource.Layout.text_input, editTextId: Resource.Id.txtInput,
                            createAction: (view, input) =>
                            {
                                if (input == null)
                                    return;

                                // android:inputType="textPassword"
                                input.InputType = InputTypes.ClassText | InputTypes.TextVariationPassword;
                            },
                            okAction: (view, value) =>
                            {
                                if (string.IsNullOrEmpty(value))
                                    return false;

                                try
                                {
                                    AppSettings.Default.SaveApiKey(providerId, value);
                                    if (!AppSettings.Default.ValidateApiKey(providerId))
                                        throw GetExceptionHandler();

                                    okAction?.Invoke();
                                }
                                catch (Exception ex)
                                {
                                    log?.ErrorFormat("{0} {1}", "OkAction", ex);
                                    Toast.MakeText(activity, activity.GetString(Resource.String.InternalError),
                                        toastLength).Show();
                                }

                                return true;
                            },
                            cancelAction: () =>
                            {
                                cancelAction?.Invoke();
                                if (!AppSettings.Default.ValidateApiKey(providerId))
                                {
                                    message = GetErrorHandler();
                                    if (!string.IsNullOrEmpty(message))
                                        Toast.MakeText(activity, message, toastLength).Show();
                                }
                            });

                        return;
                    }

                    if (!string.IsNullOrEmpty(message))
                        Toast.MakeText(activity, message, toastLength).Show();
                });
            }

            return result;
        }
    }
}