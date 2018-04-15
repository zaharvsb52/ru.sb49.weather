using System;
using Android.OS;
using Android.Support.V7.Preferences;
using Android.Views;
using Android.Widget;

namespace Sb49.Weather.Droid.Ui.Preferences
{
    public class CustomEditTextPreferenceDialogFragmentCompat : PreferenceDialogFragmentCompat
    {
        public CustomEditTextPreferenceDialogFragmentCompat(string key)
        {
            if(string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var bundle = new Bundle(1);
            bundle.PutString(ArgKey, key);
            Arguments = bundle;
        }

        protected EditText EditText { get; set; }
        protected CustomEditTextPreference EditTextPreference => (CustomEditTextPreference) Preference;

        protected override void OnBindDialogView(View view)
        {
            base.OnBindDialogView(view);

            EditText = EditTextPreference.EditText;
            EditText.Text = EditTextPreference.Text;

            var text = EditText.Text;
            if (text != null)
            {
                var length = text.Length;
                EditText.SetSelection(length, length);
            }

            var oldParent = EditText.Parent;
            if (oldParent != view)
            {
                ((ViewGroup) oldParent)?.RemoveView(EditText);
                OnAddEditTextToDialogView(view, EditText);
            }
        }

        public override void OnDialogClosed(bool positiveResult)
        {
            if (positiveResult)
            {
                var value = EditText?.Text ?? string.Empty;
                if (EditTextPreference.CallChangeListener(value))
                    EditTextPreference.Text = value;
            }
        }

        private void OnAddEditTextToDialogView(View dialogView, View editText)
        {
            var oldEditText = dialogView.FindViewById(editText.Id);
            var container = (ViewGroup) oldEditText?.Parent;
            if (container != null)
            {
                container.RemoveView(oldEditText);
                container.AddView(editText, ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            }
        }
    }
}