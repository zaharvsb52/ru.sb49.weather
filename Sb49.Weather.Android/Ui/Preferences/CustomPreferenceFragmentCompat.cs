using Android.App;

namespace Sb49.Weather.Droid.Ui.Preferences
{
    public abstract class CustomPreferenceFragmentCompat : Android.Support.V7.Preferences.PreferenceFragmentCompat
    {
        protected const string FragmentDialogTag = ".preference.PreferenceFragment.DIALOG";

        public override void OnDisplayPreferenceDialog(Android.Support.V7.Preferences.Preference preference)
        {
            // check if dialog is already showing
            if (FragmentManager.FindFragmentByTag(FragmentDialogTag) != null)
                return;

            if (preference is CustomEditTextPreference)
            {
                var fragment = new CustomEditTextPreferenceDialogFragmentCompat(preference.Key);
                OnShow(fragment);
                return;
            }

            if (preference is CustomMultiSelectListPreference)
            {
                var fragment = new CustomMultiSelectListPreferenceDialogFragment(preference.Key);
                OnShow(fragment);
                return;
            }

            if (preference is SeekBarPreferenceCompat)
            {
                var fragment = new SeekBarPreferenceDialogFragmentCompat(preference.Key);
                OnShow(fragment);
                return;
            }

            base.OnDisplayPreferenceDialog(preference);
        }

        protected void OnShow(Android.Support.V7.Preferences.PreferenceDialogFragmentCompat fragment)
        {
            if (fragment == null)
                return;

            fragment.SetTargetFragment(this, 0);
            fragment.Show(FragmentManager, Application.Context.PackageName + FragmentDialogTag);
        }
    }
}