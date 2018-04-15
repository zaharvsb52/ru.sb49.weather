using System;
using System.Collections.Generic;
using System.Linq;
using Android.Content;
using Android.Runtime;
using Android.Text;
using Android.Util;
using Java.Lang;

namespace Sb49.Weather.Droid.Ui.Preferences
{
    public class CustomMultiSelectListPreference : Android.Support.V14.Preferences.MultiSelectListPreference
    {
        private const string FormatSummary = "%s";
        private bool? _useFormatSummary;

        protected CustomMultiSelectListPreference(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public CustomMultiSelectListPreference(Context context) : base(context)
        {
        }

        public CustomMultiSelectListPreference(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Init(context, attrs);
        }

        public CustomMultiSelectListPreference(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
            Init(context, attrs);
        }

        public CustomMultiSelectListPreference(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
        {
            Init(context, attrs);
        }

        public ICollection<string> ReadOnlyEntries { get; set; }

        private void Init(Context context, IAttributeSet attrs)
        {
            if(attrs == null)
                return;

            var attr = context.ObtainStyledAttributes(attrs, Resource.Styleable.CustomMultiSelectListPreference);
            try
            {
                ReadOnlyEntries = attr.GetTextArray(Resource.Styleable.CustomMultiSelectListPreference_readOnlyEntries);
            }
            finally
            {
                attr?.Recycle();
            }
        }

        public new virtual bool[] GetSelectedItems()
        {
            return base.GetSelectedItems();
        }

        public override ICharSequence SummaryFormatted
        {
            get
            {
                if (!_useFormatSummary.HasValue && CheckFormatSummary(base.SummaryFormatted?.ToString()))
                    _useFormatSummary = true;

                if (_useFormatSummary == true)
                {
                    var entries = GetEntries();
                    if (entries != null && entries.Length > 0)
                    {
                        var checkedItems = GetSelectedItems();
                        if (checkedItems != null && checkedItems.Length > 0)
                        {
                            var selected = new List<string>();
                            for (var i = 0; i < checkedItems.Length; i++)
                            {
                                if (!checkedItems[i])
                                    continue;
                                selected.Add(entries[i]);
                            }
                            return new SpannableString(string.Format(string.Join(", ", selected.Distinct())));
                        }
                    }
                }

                return base.SummaryFormatted;
            }
            set
            {
                _useFormatSummary = CheckFormatSummary(value?.ToString());
                base.SummaryFormatted = value;
            }
        }

        public override ICollection<string> Values
        {
            get { return base.Values; }
            set
            {
                base.Values = value;
                if (_useFormatSummary == true)
                    NotifyChanged();
            }
        }

        private bool CheckFormatSummary(string value)
        {
            return !string.IsNullOrEmpty(value) && value.ToLower() == FormatSummary;
        }
    }
}