using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Sb49.Common.Support.v7.Droid.Ui
{
    public class ToolbarUtil
    {
        //http://stackoverflow.com/questions/26564400/creating-a-preference-screen-with-support-v21-toolbar

        //How to change color of the back arrow in the new material theme?
        //http://gregshackles.com/changing-the-android-toolbars-back-arrow-color/
        //http://stackoverflow.com/questions/26788464/how-to-change-color-of-the-back-arrow-in-the-new-material-theme

        public Toolbar CreateToolbarInDialog(Dialog dialog, Context context, int layoutToolbarId)
        {
            if (dialog == null)
                return null;

            Toolbar result;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.IceCreamSandwich)
            {
                var root = (LinearLayout)dialog.FindViewById(Android.Resource.Id.List).Parent;
                result = (Toolbar)LayoutInflater.From(context).Inflate(layoutToolbarId, root, false);
                root.AddView(result, 0); // insert at top
            }
            else
            {
                var root = (ViewGroup)dialog.FindViewById(Android.Resource.Id.Content);
                var content = (ListView)root.GetChildAt(0);
                root.RemoveAllViews();
                result = (Toolbar)LayoutInflater.From(context).Inflate(layoutToolbarId, root, false);

                int height;
                var tv = new TypedValue();
                if (context.Theme.ResolveAttribute(Android.Resource.Attribute.ActionBarSize, tv, true))
                {
                    height = TypedValue.ComplexToDimensionPixelSize(tv.Data, context.Resources.DisplayMetrics);
                }
                else
                {
                    height = result.Height;
                }

                content.SetPadding(0, height, 0, 0);

                root.AddView(content);
                root.AddView(result);
            }

            return result;
        }

        //Icons
        //1. Стрелка влево - Resource.Drawable.abc_ic_ab_back_mtrl_am_alpha
        //2. Меню - вертикальные точки - Resource.Drawable.abc_ic_menu_moreoverflow_mtrl_alpha
        //3. Меню - гор. полоски - 
        //4  Share - Resource.Drawable.abc_ic_menu_share_mtrl_alpha
    }
}