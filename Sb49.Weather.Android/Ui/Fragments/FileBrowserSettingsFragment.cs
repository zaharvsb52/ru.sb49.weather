using Android.App;
using Android.Content;
using Android.OS;
using Sb49.Weather.Droid.Common;
using Sb49.Weather.Droid.Ui.Preferences;

namespace Sb49.Weather.Droid.Ui.Fragments
{
    public class FileBrowserSettingsFragment : CustomPreferenceFragmentCompat, ISharedPreferencesOnSharedPreferenceChangeListener
    {
        private bool _hasChanged;

        public override void OnCreatePreferences(Bundle savedInstanceState, string rootKey)
        {
            PreferenceManager.SharedPreferencesName = AppSettings.SharedPreferencesFileName;
            PreferenceManager.SharedPreferencesMode = (int)FileCreationMode.Private;

            SetPreferencesFromResource(Resource.Xml.preferences_file_browser, rootKey);
        }

        public override void OnResume()
        {
            base.OnResume();

            SubscribeEvents();
        }

        public override void OnPause()
        {
            base.OnPause();

            UnsubscribeEvents();
        }

        void ISharedPreferencesOnSharedPreferenceChangeListener.OnSharedPreferenceChanged(ISharedPreferences sharedPreferences, string key)
        {
            if (!_hasChanged)
            {
                _hasChanged = true;
                Activity?.SetResult(Result.Ok);
            }
        }

        private void SubscribeEvents()
        {
            UnsubscribeEvents();
            PreferenceScreen?.SharedPreferences.RegisterOnSharedPreferenceChangeListener(this);
        }

        private void UnsubscribeEvents()
        {
            PreferenceScreen?.SharedPreferences.UnregisterOnSharedPreferenceChangeListener(this);
        }
    }
}