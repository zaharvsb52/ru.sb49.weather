using System;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Widget;
using Sb49.Weather.Droid.Ui.Controls;

namespace Sb49.Weather.Droid.Ui.Preferences
{
    //https://github.com/Gericop/Android-Support-Preference-V7-Fix/blob/master/preference-v7/src/main/res/layout/preference_dialog_edittext.xml
    //https://android.googlesource.com/platform/frameworks/support/+/master/v7/preference/res/layout/preference_dialog_edittext.xml
    public class CustomEditTextPreference : Android.Support.V7.Preferences.EditTextPreference
    {
        private bool _needDispose;

        protected CustomEditTextPreference(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public CustomEditTextPreference(Context context) : this(context, null)
        {
        }

        public CustomEditTextPreference(Context context, IAttributeSet attrs) : this(context, attrs, Android.Resource.Attribute.EditTextPreferenceStyle)
        {
        }

        public CustomEditTextPreference(Context context, IAttributeSet attrs, int defStyleAttr) : this(context, attrs, defStyleAttr, 0)
        {
        }

        public CustomEditTextPreference(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
        {
            Init(context, attrs);
        }

        public EditText EditText { get; protected set; }

        protected void Init(Context context, IAttributeSet attrs)
        {
            //preference_dialog_edittext
            //DialogLayoutResource = Resource.Layout.preference_dialog_edittext;
            //EditText = new AppCompatEditText(context, attrs) { Id = Android.Resource.Id.Edit };
            EditText = new CustomEditText(context, attrs) { Id = Android.Resource.Id.Edit };
            _needDispose = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (_needDispose)
            {
                EditText?.Dispose();
                EditText = null;
            }

            base.Dispose(disposing);
        }
    }
}