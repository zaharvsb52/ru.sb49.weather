using Android.Content;
using Android.Util;
using Android.Widget;

namespace Sb49.Weather.Droid.Ui.Preferences
{
    //https://github.com/android/platform_frameworks_support/blob/master/v7/preference/src/android/support/v7/preference/UnPressableLinearLayout.java

    public class UnPressableLinearLayout : LinearLayout
    {
        public UnPressableLinearLayout(Context context) : this(context, null)
        {
        }
        public UnPressableLinearLayout(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        protected override void DispatchSetPressed(bool pressed)
        {
            // Skip dispatching the pressed key state to the children so that they don't trigger any
            // pressed state animation on their stateful drawables.
        }
    }
}