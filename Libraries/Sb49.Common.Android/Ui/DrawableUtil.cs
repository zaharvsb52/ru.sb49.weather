using Android.Graphics;

namespace Sb49.Common.Droid.Ui
{
    public class DrawableUtil
    {
        //http://stackoverflow.com/questions/9015372/how-to-rotate-a-bitmap-90-degrees
        public Bitmap RotateBitmap(Bitmap source, float angle)
        {
            var matrix = new Matrix();
            matrix.PostRotate(angle);
            return Bitmap.CreateBitmap(source, 0, 0, source.Width, source.Height, matrix, true);
        }
    }
}