using System;
using Android.Content;
using Android.Graphics.Drawables;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Sb49.Common.Support.v7.Droid.Ui;

namespace Sb49.Weather.Droid.Ui.Controls
{
    //public class CustomEditText : AppCompatEditText, View.IOnTouchListener, View.IOnFocusChangeListener
    public class CustomEditText : AppCompatAutoCompleteTextView, View.IOnTouchListener, View.IOnFocusChangeListener
    {
        private IOnTouchListener _touchListener;
        private Drawable _xD;

        public CustomEditText(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public CustomEditText(Context context) : base(context)
        {
            Initialize();
        }

        public CustomEditText(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Initialize();
        }

        public CustomEditText(Context context, IAttributeSet attrs, int defStyleAttr)
            : base(context, attrs, defStyleAttr)
        {
            Initialize();
        }

        public override IOnFocusChangeListener OnFocusChangeListener { get; set; }
        public IconLocationType IconLocation { get; set; } = IconLocationType.Right;
        public bool UseDefaultColor { get; set; } = true;

        private void Initialize()
        {
            base.SetOnTouchListener(this);
            base.OnFocusChangeListener = this;
            InitializeIcon();
            SetClearIconVisible(false);
            CompoundDrawablePadding =
                (int) Resources.GetDimension(Resource.Dimension.customEditTextCompoundDrawablePadding);
        }

        private void InitializeIcon()
        {
            _xD = GetCompoundDrawables()[(int)IconLocation];
            if (_xD == null)
            {
                var compatDrawableUtil = new AppCompatDrawableUtil();
                _xD = compatDrawableUtil.GetDrawable(Resources, Resource.Drawable.ic_delete, Context.Theme);
                if (UseDefaultColor)
                    _xD = compatDrawableUtil.ChangeColorTint(_xD, TextColors.DefaultColor);
            }

            //var iconsize = 2 * _xD.IntrinsicHeight;
            _xD.SetBounds(0, 0, _xD.IntrinsicWidth, _xD.IntrinsicHeight);
            //_xD.SetBounds(0, 0, iconsize, iconsize);
            var min = PaddingTop + _xD.IntrinsicHeight + PaddingBottom;
            if (SuggestedMinimumHeight < min)
                SetMinimumHeight(min);
        }

        public override void SetOnTouchListener(IOnTouchListener listener)
        {
            _touchListener = listener;
        }

        protected override void Dispose(bool disposing)
        {
            _xD?.Dispose();
            _xD = null;

            base.Dispose(disposing);
        }

        public bool OnTouch(View v, MotionEvent e)
        {
            if (GetDisplayedDrawable() != null)
            {
                var x = (int) e.GetX();
                var y = (int) e.GetY();

                var left = IconLocation == IconLocationType.Left ? 0 : Width - PaddingRight - _xD.IntrinsicWidth;
                var right = IconLocation == IconLocationType.Left ? PaddingLeft + _xD.IntrinsicWidth : Width;
                var tappedX = x >= left && x <= right && y >= 0 && y <= Bottom - Top;
                if (tappedX)
                {
                    if (e.Action == MotionEventActions.Up)
                        Text = string.Empty;
                    return true;
                }
            }

            if (_touchListener != null)
                return _touchListener.OnTouch(v, e);

            return false;
        }

        public void OnFocusChange(View v, bool hasFocus)
        {
            SetClearIconVisible(hasFocus && !string.IsNullOrEmpty(Text));
            OnFocusChangeListener?.OnFocusChange(v, hasFocus);
        }

        protected override void OnTextChanged(Java.Lang.ICharSequence text, int start, int lengthBefore, int lengthAfter)
        {
            base.OnTextChanged(text, start, lengthBefore, lengthAfter);
            if (IsFocused)
                SetClearIconVisible(!string.IsNullOrEmpty(Text));
        }

        private Drawable GetDisplayedDrawable()
        {
            var drawables = GetCompoundDrawables();
            return drawables?[(int)IconLocation];
        }

        protected void SetClearIconVisible(bool visible)
        {
            var drawables = GetCompoundDrawables();
            var displayed = GetDisplayedDrawable();
            var wasVisible = displayed != null;
            if (visible != wasVisible)
            {
                var x = visible ? _xD : null;
                SetCompoundDrawables(IconLocation == IconLocationType.Left ? x : drawables[0], drawables[1], IconLocation == IconLocationType.Right ? x : drawables[2], drawables[3]);
            }
        }

        public enum IconLocationType
        {
            Left = 0,
            Right = 2
        }
    }
}