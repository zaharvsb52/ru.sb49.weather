using System;
using Android.Content;
using Android.Runtime;
using Android.Util;

namespace Sb49.Weather.Droid.Ui.Preferences
{
    public class CustomPreference : Android.Support.V7.Preferences.Preference
    {
        protected CustomPreference(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public CustomPreference(Context context) : base(context)
        {
        }

        public CustomPreference(Context context, IAttributeSet attrs) : base(context, attrs, 0)
        {
            Init(context, attrs);
        }

        public CustomPreference(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
            Init(context, attrs);
        }

        public CustomPreference(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
        {
            Init(context, attrs);
        }

        public bool NeedStartNewActivity { get; set; }
        public int? ActivityTitleResource { get; set; }
        public int? ActivityFragmentLayoutResource { get; set; }
        public string Values { get; set; }

        private void Init(Context context, IAttributeSet attrs)
        {
            var attr = context.ObtainStyledAttributes(attrs, Resource.Styleable.CustomPreference);
            try
            {
                NeedStartNewActivity = attr.GetBoolean(Resource.Styleable.CustomPreference_needStartNewActivity, false);

                var activityTitleResource = attr.GetResourceId(Resource.Styleable.CustomPreference_activityTitle, 0);
                if (activityTitleResource > 0)
                    ActivityTitleResource = activityTitleResource;

                var activityFragmentLayout =
                    attr.GetResourceId(Resource.Styleable.CustomPreference_activityFragmentLayout, 0);
                if(activityFragmentLayout > 0)
                    ActivityFragmentLayoutResource = activityFragmentLayout;

                Values = attr.GetString(Resource.Styleable.CustomPreference_extraValues);
            }
            finally
            {
                attr?.Recycle();
            }
        }
    }
}