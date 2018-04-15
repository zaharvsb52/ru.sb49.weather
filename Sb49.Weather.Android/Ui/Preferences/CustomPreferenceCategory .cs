using System;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Widget;

namespace Sb49.Weather.Droid.Ui.Preferences
{
    //https://github.com/Gericop/Android-Support-Preference-V7-Fix/blob/master/preference-v7/src/main/java/android/support/v7/preference/PreferenceCategoryFix.java
    public class CustomPreferenceCategory : Android.Support.V7.Preferences.PreferenceCategory
    {
        protected CustomPreferenceCategory(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public CustomPreferenceCategory(Context context) : base(context)
        {
        }

        public CustomPreferenceCategory(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public CustomPreferenceCategory(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
        }

        public CustomPreferenceCategory(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
        {
        }

        public override void OnBindViewHolder(Android.Support.V7.Preferences.PreferenceViewHolder holder)
        {
            base.OnBindViewHolder(holder);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
                return;

            var titleView = (TextView)holder.FindViewById(Android.Resource.Id.Title);
            if(titleView == null)
                return;

            TypedArray typedArray = null;
            try
            {
                //http://stackoverflow.com/questions/27611173/how-to-get-accent-color-programmatically
                var colorAttrId = Context.Resources.GetIdentifier("colorAccent", "attr", Context.PackageName);
                if(colorAttrId <= 0)
                    return;

                typedArray = Context.ObtainStyledAttributes(new[] {colorAttrId});
                if (typedArray == null || typedArray.Length() <= 0)
                    return;

                var accentColor = typedArray.GetColor(0, 0xff4081); // defaults to pink
                titleView.SetTextColor(accentColor);
            }
            finally
            {
                typedArray?.Recycle();
            }
        }
    }
}