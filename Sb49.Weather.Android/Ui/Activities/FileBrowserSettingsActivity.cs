using System;
using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Sb49.Weather.Droid.Ui.Fragments;

namespace Sb49.Weather.Droid.Ui.Activities
{
    //https://stackoverflow.com/questions/2176922/how-do-i-create-a-transparent-activity-on-android
    //https://stackoverflow.com/questions/26012722/how-to-make-activity-transparent-in-xamarin-android

    [Activity(Theme = "@style/Theme.Custom.Translucent")]
    public class FileBrowserSettingsActivity : AppCompatActivity
    {
        private View _viewRoot;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_file_browser_settings);

            _viewRoot = FindViewById(Resource.Id.viewRootSettings);

            if (savedInstanceState == null)
            {
                var fragment = new FileBrowserSettingsFragment();
                using (var transaction = SupportFragmentManager.BeginTransaction())
                {
                    transaction.Replace(Resource.Id.settingsContent, fragment);
                    transaction.Commit();
                }
            }
        }

        protected override void OnResume()
        {
            base.OnResume();

            SubscribeEvents();
        }

        protected override void OnPause()
        {
            base.OnPause();
         
            UnsubscribeEvents();
        }

        private void OnClick(object sender, EventArgs eventArgs)
        {
            Finish();
        }

        private void SubscribeEvents()
        {
            UnsubscribeEvents();

            if (_viewRoot != null)
                _viewRoot.Click += OnClick;
        }

        private void UnsubscribeEvents()
        {
            if (_viewRoot != null && _viewRoot.Handle != IntPtr.Zero)
                _viewRoot.Click -= OnClick;
        }
    }
}