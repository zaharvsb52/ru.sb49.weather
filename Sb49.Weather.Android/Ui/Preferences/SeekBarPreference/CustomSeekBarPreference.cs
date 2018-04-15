using System;
using Android.Content;
using Android.Content.Res;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Sb49.Weather.Droid.Ui.Preferences
{
    //https://github.com/DreaminginCodeZH/SeekBarPreference/blob/master/library/src/main/java/me/zhanghai/android/seekbarpreference/SeekBarPreference.java
    //http://stackoverflow.com/questions/16108609/android-creating-custom-preference
    //https://chromium.googlesource.com/android_tools/+/master/sdk/extras/android/support/v14/preference/res/layout/preference_material.xml
    //https://github.com/Gericop/Android-Support-Preference-V7-Fix/blob/master/preference-v7/src/main/res/layout/preference_widget_seekbar.xml

    public class CustomSeekBarPreference : Android.Support.V7.Preferences.Preference
    {
        public const int Min = 0;

        private int? _defaultValue;
        private int _progress;
        private int _max;
        private bool _needDispose;

        public new event EventHandler<PreferenceChangeEventArgs> PreferenceChange;

        protected CustomSeekBarPreference(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public CustomSeekBarPreference(Context context, IAttributeSet attrs) : this(context, attrs, 0)
        {
        }

        public CustomSeekBarPreference(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
            //if (LayoutResource <= 0)
            //    LayoutResource = Android.Resource.Layout.sbp_preference_dialog_seekbar;
            LayoutResource = Resource.Layout.custom_preference_widget_seekbar;

            SeekBar = new AppCompatSeekBar(context, attrs) {Id = Resource.Id.seekbar, Enabled = true};
            _needDispose = true;

            var attr = context.ObtainStyledAttributes(attrs, Resource.Styleable.CustomSeekBarPreference);
            try
            {
                Max = attr.GetInt(Resource.Styleable.CustomSeekBarPreference_maximum, SeekBar.Max);
                MinTitle = attr.GetString(Resource.Styleable.CustomSeekBarPreference_minTitle);
                MaxTitle = attr.GetString(Resource.Styleable.CustomSeekBarPreference_maxTitle);
            }
            finally
            {
                attr?.Recycle();
            }
        }

        public SeekBar SeekBar { get; protected set; }

        public int Max
        {
            get { return _max; }
            set
            {
                if (value <= Min)
                {
                    throw new ArgumentOutOfRangeException(nameof(Max),
                        string.Format("{0} must be greater than {1}", nameof(Max), Min));
                }
                _max = value;
                if (SeekBar != null)
                    SeekBar.Max = value;
            }
        }

        public string MinTitle { get; set; }
        public string MaxTitle { get; set; }

        public int Progress
        {
            get { return _progress; }
            set
            {
                var progress = value;
                if (progress > SeekBar.Max)
                {
                    progress = SeekBar.Max;
                }
                else if (progress < Min)
                {
                    progress = Min;
                }

                if (progress != _progress)
                {
                    var e = new PreferenceChangeEventArgs(true, this, progress);
                    PreferenceChange?.Invoke(this, e);
                    if (e.Handled)
                    {
                        _progress = progress;
                        if (ShouldPersist())
                            PersistInt(progress);
                    }
                    //NotifyChanged();
                }
            }
        }

        public override void OnBindViewHolder(Android.Support.V7.Preferences.PreferenceViewHolder holder)
        {
            base.OnBindViewHolder(holder);

            if(holder == null)
                return;

            var widgetFrame = holder.FindViewById(Android.Resource.Id.WidgetFrame);
            if(widgetFrame != null)
                widgetFrame.Visibility = ViewStates.Visible;

            var mintitle = (TextView) holder.FindViewById(Resource.Id.mintitle);
            if (mintitle != null)
            {
                mintitle.Text = MinTitle ?? string.Empty;
                mintitle.Visibility = string.IsNullOrEmpty(MinTitle) ? ViewStates.Gone : ViewStates.Visible;
            }
            var maxtitle = (TextView)holder.FindViewById(Resource.Id.maxtitle);
            if (maxtitle != null)
            {
                maxtitle.Text = MaxTitle ?? string.Empty;
                maxtitle.Visibility = string.IsNullOrEmpty(MinTitle) ? ViewStates.Gone : ViewStates.Visible;
            }

            SeekBar.Progress = Progress;

            var oldseekBar = holder.FindViewById(SeekBar.Id);
            if (oldseekBar != null && oldseekBar != SeekBar)
            {
                var container = (ViewGroup) oldseekBar.Parent;
                if (container != null)
                {
                    container.RemoveView(oldseekBar);
                    container.AddView(SeekBar, ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
                }
            }

            SubscribeEvents();
        }

        protected override Java.Lang.Object OnGetDefaultValue(TypedArray aValue, int index)
        {
            _defaultValue = aValue.GetInt(index, Min);
            return _defaultValue.Value;
        }

        protected override void OnSetInitialValue(bool restorePersistedValue, Java.Lang.Object defaultValue)
        {
            _progress = restorePersistedValue ? GetPersistedInt(_defaultValue ?? Min) : (int)defaultValue;
            Progress = _progress;
        }

        protected override void Dispose(bool disposing)
        {
            UnsubscribeEvents();

            if (_needDispose)
            {
                SeekBar?.Dispose();
                SeekBar = null;
            }

            base.Dispose(disposing);
        }

        private void OnProgressChanged(object sender, SeekBar.StopTrackingTouchEventArgs e)
        {
            if (e?.SeekBar == null)
                return;

            Progress = e.SeekBar.Progress;
        }

        private void SubscribeEvents()
        {
            UnsubscribeEvents();
            if (SeekBar != null)
                SeekBar.StopTrackingTouch += OnProgressChanged;
        }

        private void UnsubscribeEvents()
        {
            if (SeekBar != null)
                SeekBar.StopTrackingTouch -= OnProgressChanged;
        }
    }
}