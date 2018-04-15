using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Support.V4.Content;
using Android.Support.V4.Content.Res;
using Android.Support.V4.Graphics.Drawable;

namespace Sb49.Common.Support.v7.Droid.Ui
{
    public class AppCompatDrawableUtil
    {
        //http://stackoverflow.com/questions/29041027/android-getresources-getdrawable-deprecated-api-22
        public Drawable GetDrawable(Resources resources, int id, Resources.Theme theme)
        {
            return ResourcesCompat.GetDrawable(resources, id, theme);
        }

        //http://stackoverflow.com/questions/26788464/how-to-change-color-of-the-back-arrow-in-the-new-material-theme
        //http://gregshackles.com/changing-the-android-toolbars-back-arrow-color/
        public Drawable ChangeColorTint(Drawable drawable, int colorTint)
        {
            if (drawable == null)
                return null;

            var wrapped = DrawableCompat.Wrap(drawable);
             
            // This should avoid tinting all the arrows
            wrapped = wrapped.Mutate();
            DrawableCompat.SetTint(wrapped, colorTint);
            return wrapped;
        }

        public Drawable ChangeColorTint(Drawable drawable, Color colorTint)
        {
            return ChangeColorTint(drawable, colorTint.ToArgb());
        }

        public Drawable ChangeColorTintResourceId(Context context, Drawable drawable, int colorTintResourceId)
        {
            var tintColor = ContextCompat.GetColor(context, colorTintResourceId);
            return ChangeColorTint(drawable, tintColor);
        }

        public Drawable ChangeColorTint(Drawable drawable, ColorStateList tint)
        {
            if (drawable == null || tint == null)
                return null;

            var wrapped = DrawableCompat.Wrap(drawable);

            // This should avoid tinting all the arrows
            wrapped = wrapped.Mutate();
            DrawableCompat.SetTintList(wrapped, tint);
            return wrapped;
        }

        //http://stackoverflow.com/questions/31953503/how-to-set-icon-color-of-menuitem
        public Drawable ChangeColorFilter(Drawable drawable, Color newcolor, PorterDuff.Mode mode)
        {
            if (drawable == null)
                return null;

            var wrapped = DrawableCompat.Wrap(drawable);

            // This should avoid tinting all the arrows
            wrapped = wrapped.Mutate();

            //PorterDuff.Mode.SrcAtop
            //PorterDuff.Mode.SrcIn
            wrapped.SetColorFilter(newcolor, mode);
            return wrapped;
        }

        public Drawable ChangeColorFilter(Drawable drawable, int newColor, PorterDuff.Mode mode)
        {
            return ChangeColorFilter(drawable, new Color(newColor), mode);
        }
    }
}