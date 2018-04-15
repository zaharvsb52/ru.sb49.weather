using System;
using Android.Content;
using Android.Content.Res;
using Android.Runtime;
using Android.Support.V7.Preferences;
using Android.Support.V7.Widget;
using Android.Text;
using Android.Util;
using Android.Widget;

namespace Sb49.Weather.Droid.Ui.Preferences
{
    //https://github.com/DreaminginCodeZH/SeekBarPreference/blob/master/library/src/main/java/me/zhanghai/android/seekbarpreference/SeekBarPreference.java

    public class SeekBarPreferenceCompat : DialogPreference
    {
        private const int MinDefault = 0;
        private const string FormatDefault = "{0}";

        private int? _defaultValue;
        private int _progress;
        private int _min;
        private int _max;
        private int? _pluralsUnitResourceId;
        private bool _needDispose;

        protected SeekBarPreferenceCompat(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public SeekBarPreferenceCompat(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Init(context, attrs);
        }

        public SeekBarPreferenceCompat(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
            Init(context, attrs);
        }

        public SeekBarPreferenceCompat(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
        {
            Init(context, attrs);
        }

        public SeekBar SeekBar { get; protected set; }

        public int Min
        {
            get => _min;
            set
            {
                _min = value;
                if (_min >= Max)
                {
                    throw new ArgumentOutOfRangeException(nameof(Min),
                        string.Format("{0} must be less than {1}", nameof(Min), nameof(Max)));
                }
                UpdateSeekBarMax();
            }
        }

        public int Max
        {
            get => _max;
            set
            {
                _max = value;
                ValidateMax();
                UpdateSeekBarMax();
            }
        }

        public string Format { get; set; } = FormatDefault;

        public string Unit { get; set; }

        public int Progress
        {
            get => _progress;
            set
            {
                if (value != _progress)
                {
                    ValidateMax();
                    var progress = value;
                    if (progress > Max)
                    {
                        progress = Max;
                    }
                    else if (progress < Min)
                    {
                        progress = Min;
                    }
                    _progress = progress;
                    if (ShouldPersist())
                        PersistInt(_progress);
                    NotifyChanged();
                }
            }
        }

        public override Java.Lang.ICharSequence SummaryFormatted
        {
            get => string.IsNullOrEmpty(Format) ? base.SummaryFormatted : new SpannableString(GetFormattedValue());
            set => base.SummaryFormatted = value;
        }

        private void Init(Context context, IAttributeSet attrs)
        {
            if (DialogLayoutResource == 0)
                DialogLayoutResource = Resource.Layout.preference_widget_seekbar_material;

            SeekBar = new AppCompatSeekBar(context, attrs) {Id = Resource.Id.seekbar, Enabled = true};
            _needDispose = true;

            var attr = context.ObtainStyledAttributes(attrs, Resource.Styleable.CustomSeekBarPreference);
            try
            {
                _min = attr.GetInt(Resource.Styleable.CustomSeekBarPreference_minimum, MinDefault);
                Max = attr.GetInt(Resource.Styleable.CustomSeekBarPreference_maximum, SeekBar.Max);
                var format = attr.GetString(Resource.Styleable.CustomSeekBarPreference_format);
                if (!string.IsNullOrEmpty(format))
                    Format = format;
                Unit = attr.GetString(Resource.Styleable.CustomSeekBarPreference_unit);
                if (!string.IsNullOrEmpty(Unit))
                {
                    var resid = Context.Resources.GetIdentifier(Unit, "plurals", Context.PackageName);
                    if (resid > 0)
                        _pluralsUnitResourceId = resid;
                }
            }
            finally
            {
                attr?.Recycle();
            }
        }

        protected override Java.Lang.Object OnGetDefaultValue(TypedArray aValue, int index)
        {
            _defaultValue = aValue.GetInt(index, Min);
            return _defaultValue.Value;
        }

        protected override void OnSetInitialValue(bool restorePersistedValue, Java.Lang.Object defaultValue)
        {
            _progress = restorePersistedValue ? GetPersistedInt(_defaultValue ?? MinDefault) : (int)defaultValue;
            Progress = _progress;
        }

        protected override void Dispose(bool disposing)
        {
            if (_needDispose)
            {
                SeekBar?.Dispose();
                SeekBar = null;
            }

            base.Dispose(disposing);
        }

        private void ValidateMax()
        {
            if (Max <= Min)
            {
                throw new ArgumentOutOfRangeException(nameof(Max),
                    string.Format("{0} must be greater than {1}", nameof(Max), nameof(Min)));
            }
        }

        private void UpdateSeekBarMax()
        {
            if (SeekBar != null)
                SeekBar.Max = Max - Min;
        }

        public string GetFormattedValue(int value)
        {
            var format = string.IsNullOrEmpty(Format) ? FormatDefault : Format;
            if (_pluralsUnitResourceId.HasValue)
                return Context.Resources.GetQuantityString(_pluralsUnitResourceId.Value, value, value);
            return string.Format(format, value);
        }

        public string GetFormattedValue()
        {
            return GetFormattedValue(Progress);
        }
    }
}