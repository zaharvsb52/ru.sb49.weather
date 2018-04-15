using System;
using Android.Content;
using Android.Runtime;
using Android.Util;

namespace Sb49.Weather.Droid.Ui.Preferences
{
    public class ImagePreference : Android.Support.V7.Preferences.Preference
    {
        public event EventHandler BindViewHolder;

        protected ImagePreference(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public ImagePreference(Context context, IAttributeSet attrs) : this(context, attrs, 0)
        {
        }

        public ImagePreference(Context context, IAttributeSet attrs, int defStyleAttr)
            : base(context, attrs, defStyleAttr)
        {
        }

        public Android.Support.V7.Preferences.PreferenceViewHolder ViewHolder { get; private set; }

        public override void OnBindViewHolder(Android.Support.V7.Preferences.PreferenceViewHolder holder)
        {
            base.OnBindViewHolder(holder);
            ViewHolder = holder;
            BindViewHolder?.Invoke(this, EventArgs.Empty);
        }
    }
}