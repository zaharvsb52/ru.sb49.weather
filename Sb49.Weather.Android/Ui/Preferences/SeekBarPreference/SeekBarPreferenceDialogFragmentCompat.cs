using System;
using Android.App;
using Android.OS;
using Android.Support.V7.Preferences;
using Android.Views;
using Android.Widget;
using Sb49.Common.Droid.Listeners;
using AlertDialog = Android.Support.V7.App.AlertDialog;

namespace Sb49.Weather.Droid.Ui.Preferences
{
    //https://github.com/DreaminginCodeZH/SeekBarPreference/blob/master/library/src/main/java/me/zhanghai/android/seekbarpreference/SeekBarPreferenceDialogFragment.java

    public class SeekBarPreferenceDialogFragmentCompat : PreferenceDialogFragmentCompat
    {
        private SeekBar _seekBar;
        private TextView _seekbarValue;

        public SeekBarPreferenceDialogFragmentCompat(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var bundle = new Bundle(1);
            bundle.PutString(ArgKey, key);
            Arguments = bundle;
        }

        protected override void OnPrepareDialogBuilder(AlertDialog.Builder builder)
        {
            base.OnPrepareDialogBuilder(builder);

            builder.SetOnKeyListener(new KeyListener((dialog, keyCode, e) =>
            {
                if (e.Action != KeyEventActions.Up)
                {
                    if (keyCode == Keycode.Plus || keyCode == Keycode.Equals)
                    {
                        _seekBar.Progress = _seekBar.Progress + 1;
                        return true;
                    }

                    if (keyCode == Keycode.Minus)
                    {
                        _seekBar.Progress = _seekBar.Progress - 1;
                        return true;
                    }
                }

                return false;
            }));
        }

        protected override void OnBindDialogView(View view)
        {
            base.OnBindDialogView(view);

            var preference = GetPreference();
            _seekBar = preference.SeekBar;
            _seekBar.Progress = preference.Progress - preference.Min;

            _seekbarValue = (TextView) view.FindViewById(Resource.Id.seekbar_value);
            if (_seekbarValue != null)
            {
                _seekbarValue.Text = preference.GetFormattedValue();
                _seekBar.ProgressChanged -= OnProgressChanged;
                _seekBar.ProgressChanged += OnProgressChanged;
            }

            var oldParent = _seekBar.Parent;
            if (oldParent != view)
            {
                ((ViewGroup) oldParent)?.RemoveView(_seekBar);
                OnAddSeekBarToDialogView(view, _seekBar);
            }
        }

        private void OnProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            if(_seekbarValue == null)
                return;

            var preference = GetPreference();
            var value = e.Progress + preference.Min;
            _seekbarValue.Text = preference.GetFormattedValue(value);
        }

        public override void OnDialogClosed(bool positiveResult)
        {
            _seekBar.ProgressChanged -= OnProgressChanged;

            if (positiveResult)
            {
                var preference = GetPreference();
                var value = _seekBar.Progress + preference.Min;
                if (preference.CallChangeListener(value))
                    preference.Progress = value;
            }
        }

        private void OnAddSeekBarToDialogView(View dialogView, View seekBar)
        {
            var oldSeekBar = dialogView.FindViewById(seekBar.Id);
            var container = (ViewGroup)oldSeekBar?.Parent;
            if (container != null)
            {
                container.RemoveView(oldSeekBar);
                container.AddView(seekBar, 0,
                    new ActionBar.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
            }
        }

        private SeekBarPreferenceCompat GetPreference()
        {
            return (SeekBarPreferenceCompat) Preference;
        }
    }
}