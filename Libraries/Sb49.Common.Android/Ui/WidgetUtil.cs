using System;
using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using Java.Lang;

namespace Sb49.Common.Droid.Ui
{
    public class WidgetUtil
    {
        public void ShowMessage(Context context, int? titleId, ICharSequence message, int buttonid = Android.Resource.String.Ok)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var alertDialog = new AlertDialog.Builder(context).Create();
            alertDialog.SetCancelable(false);

            if(titleId.HasValue)
                alertDialog.SetTitle(titleId.Value);

            alertDialog.SetMessage(message);
            alertDialog.SetButton(context.GetString(buttonid), (s, e) =>
            {
                if (e.Which == -1)
                {
                    alertDialog.Dismiss();
                    alertDialog = null;
                }
            });

            alertDialog.Show();
        }

        public void ShowMessage(Context context, int? titleId, string message, int buttonid = Android.Resource.String.Ok)
        {
            ShowMessage(context, titleId, new Java.Lang.String(message), buttonid);
        }

        public void ShowDialog(Context context, int? titleId, ICharSequence message,
            int okId = Android.Resource.String.Ok, int cancelId = Android.Resource.String.Cancel,
            Action okAction = null, Action cancelAction = null)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var alertDialog = new AlertDialog.Builder(context).Create();
            alertDialog.SetCancelable(false);

            if (titleId.HasValue)
                alertDialog.SetTitle(titleId.Value);

            alertDialog.SetMessage(message);

            alertDialog.SetButton(context.GetString(okId), (s, e) =>
            {
                okAction?.Invoke();
                alertDialog.Dismiss();
                alertDialog = null;
            });

            alertDialog.SetButton2(context.GetString(cancelId), (s, e) =>
            {
                cancelAction?.Invoke();
                alertDialog.Dismiss();
                alertDialog = null;
            });

            alertDialog.Show();
        }

        public void ShowDialog(Context context, int? titleId, string message, int okId = Android.Resource.String.Ok,
            int cancelId = Android.Resource.String.Cancel, Action okAction = null, Action cancelAction = null)
        {
            ShowDialog(context, titleId, new Java.Lang.String(message), okId, cancelId, okAction, cancelAction);
        }

        //http://stackoverflow.com/questions/10903754/input-text-dialog-android
        //https://forums.xamarin.com/discussion/12931/prevent-an-alertdialog-from-closing-on-positivebutton-click
        public void InputDialog(Context context, int? titleId, int layoutId, int editTextId,
            Action<View, EditText> createAction = null, Func<View, string, bool> okAction = null, Action cancelAction = null)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            AlertDialog inputDialog;
            View viewInflated;
            EditText input;
            using (var builder = new AlertDialog.Builder(context))
            {
                builder.SetCancelable(true);

                if (titleId.HasValue)
                    builder.SetTitle(titleId.Value);

                //var root = (ViewGroup)inputDialog.FindViewById(Android.Resource.Id.Content);
                //var viewInflated = LayoutInflater.From(context).Inflate(layoutId, root, false);
                viewInflated = LayoutInflater.From(context).Inflate(layoutId, null);
                input = viewInflated.FindViewById<EditText>(editTextId);
                createAction?.Invoke(viewInflated, input);
                builder.SetView(viewInflated);

                builder.SetPositiveButton(context.GetString(Android.Resource.String.Ok),
                    (EventHandler<DialogClickEventArgs>) null);
                builder.SetNegativeButton(context.GetString(Android.Resource.String.Cancel),
                    (EventHandler<DialogClickEventArgs>) null);
                inputDialog = builder.Create();
            }

            inputDialog.Show();
            var okButton = inputDialog.GetButton((int) DialogButtonType.Positive);

            Action disposeHandler = () =>
            {
                inputDialog?.Dismiss();

                input?.Dispose();
                input = null;

                viewInflated?.Dispose();
                viewInflated = null;

                inputDialog?.Dispose();
                inputDialog = null;
            };

            okButton.Click += (s, e) =>
            {
                var result = okAction?.Invoke(viewInflated, input?.Text);
                if (result == true)
                {
                    disposeHandler();
                }
            };

            var cancelButton = inputDialog.GetButton((int) DialogButtonType.Negative);
            cancelButton.Click += (s, e) =>
            {
                cancelAction?.Invoke();
                disposeHandler();
            };
        }
    }
}