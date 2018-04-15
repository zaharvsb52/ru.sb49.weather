using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using AlertDialog = Android.Support.V7.App.AlertDialog;

namespace Sb49.Weather.Droid.Ui.Preferences
{
    //https://android.googlesource.com/platform/frameworks/support/+/0112bac/v14/preference/src/android/support/v14/preference/MultiSelectListPreferenceDialogFragment.java
    public class CustomMultiSelectListPreferenceDialogFragment : Android.Support.V7.Preferences.PreferenceDialogFragmentCompat
    {
        private Java.Util.HashSet _newValues;
        private bool _preferenceChanged;
        private ListView _listView;
        private ICollection<int> _readOnlyEntriesPosition;

        public CustomMultiSelectListPreferenceDialogFragment(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var bundle = new Bundle(1);
            bundle.PutString(ArgKey, key);
            Arguments = bundle;

            _newValues = new Java.Util.HashSet();
        }

        protected override void OnPrepareDialogBuilder(AlertDialog.Builder builder)
        {
            base.OnPrepareDialogBuilder(builder);

            var preference = GetPreference();
            if (preference.GetEntries() == null || preference.GetEntryValues() == null)
            {
                throw new Java.Lang.IllegalStateException(
                    "MultiSelectListPreference requires an entries array and an entryValues array.");
            }

            var checkedItems = preference.GetSelectedItems();
            var values = preference.GetEntryValues();
            var entries = preference.GetEntries();


            builder.SetMultiChoiceItems(entries, checkedItems, (s, e) =>
            {
                if (_readOnlyEntriesPosition?.Count > 0 &&  _listView != null && _readOnlyEntriesPosition.Contains(e.Which))
                {
                    _listView.SetItemChecked(e.Which, !e.IsChecked);
                    return;    
                }

                _preferenceChanged = true;
                if (e.IsChecked)
                    _newValues.Add(values[e.Which]);
                else
                    _newValues.Remove(values[e.Which]);
            });

            if (preference.ReadOnlyEntries?.Count > 0)
            {
                var entriesList = preference.GetEntries().ToList();
                if (entriesList.Count > 0)
                {
                    var readOnlyEntries = (from entry in entriesList
                        from readOnlyEntry in preference.ReadOnlyEntries
                        where string.Equals(entry, readOnlyEntry, StringComparison.OrdinalIgnoreCase)
                        select entry).ToArray();

                    if (readOnlyEntries.Length > 0)
                        _readOnlyEntriesPosition = readOnlyEntries.Select(p => entriesList.IndexOf(p)).ToArray();
                }
            }

            _newValues.Clear();
            for (var i = 0; i < checkedItems.Length; i++)
            {
                if(!checkedItems[i])
                    continue;
                _newValues.Add(values[i]);
            }
        }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            var dialog = base.OnCreateDialog(savedInstanceState);
            if (_readOnlyEntriesPosition == null || _readOnlyEntriesPosition.Count == 0)
                return dialog;

            _listView = (dialog as AlertDialog)?.ListView;
            if(_listView != null)
            {
                _listView.ChildViewAdded -= OnChildViewAdded;
                _listView.ChildViewAdded += OnChildViewAdded;
            }

            return dialog;
        }

        private void OnChildViewAdded(object sender, ViewGroup.ChildViewAddedEventArgs e)
        {
            if(_readOnlyEntriesPosition == null || _listView == null || e.Child == null)
                return;

            var position = _listView.GetPositionForView(e.Child);
            if(position < 0)
                return;

            if (_readOnlyEntriesPosition.Contains(position))
            {
                e.Child.Enabled = false;
                e.Child.Clickable = false;
                e.Child.Focusable = false;
            }
        }

        public override void OnDialogClosed(bool positiveResult)
        {
            if(_listView != null)
                _listView.ChildViewAdded -= OnChildViewAdded;
            _listView = null;

            var preference = GetPreference();
            if (positiveResult && _preferenceChanged)
            {
                if (preference.CallChangeListener(_newValues))
                    preference.Values = _newValues.ToArray().Select(p => p.ToString()).ToArray();
            }
            _preferenceChanged = false;
        }

        private CustomMultiSelectListPreference GetPreference()
        {
            return (CustomMultiSelectListPreference) Preference;
        }
    }
}