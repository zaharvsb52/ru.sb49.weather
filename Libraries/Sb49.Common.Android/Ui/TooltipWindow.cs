using System;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Sb49.Common.Droid.Ui
{
    //http://androidsrc.net/android-popupwindow-show-tooltip/
    //https://github.com/sephiroth74/android-target-tooltip
    //https://github.com/nhaarman/supertooltips/blob/master/lib/src/main/java/com/nhaarman/supertooltips/ToolTipView.java

    public class TooltipWindow : IDisposable
    {
        private const int MessageDismissTooltip = 100;
        private const int DefaultDelayedMsec = 4000;

        private readonly WeakReference<Context> _context;
        private PopupWindow _tipWindow;
        private View _contentView;
        private Handler _handler;
        private TextView _tooltipTextView;

        public TooltipWindow(Context context, int tooltipLayoutId, int tooltipId, string tooltipText = null, int delayedMsec = DefaultDelayedMsec)
        {
            _context = new WeakReference<Context>(context);
            var inflater = (LayoutInflater)context.GetSystemService(Context.LayoutInflaterService);
            _contentView = inflater.Inflate(tooltipLayoutId, null);
            _tooltipTextView = _contentView.FindViewById<TextView>(tooltipId);
            if (!string.IsNullOrEmpty(tooltipText))
                TooltipText = tooltipText;
            DelayedMsec = delayedMsec;
        }

        ~TooltipWindow()
        {
            OnDispose();
        }

        public bool IsDisposed { get; private set; }

        public bool IsTooltipShown => _tipWindow?.IsShowing == true;

        public string TooltipText
        {
            get => IsDisposed ? null : _tooltipTextView?.Text;
            set
            {
                if (!IsDisposed && _tooltipTextView != null)
                    _tooltipTextView.Text = value ?? string.Empty;
            }
        }

        public Java.Lang.ICharSequence TooltipTextFormatted
        {
            get => IsDisposed ? null : _tooltipTextView?.TextFormatted;
            set
            {
                if (!IsDisposed && _tooltipTextView != null)
                    _tooltipTextView.TextFormatted = value;
            }
        }
        
        public int DelayedMsec { get; set; }

        public void ShowToolTip(View anchor)
        {
            OnPopupWindowDispose();
            _tipWindow = new PopupWindow(GetContext())
            {
                Height = ViewGroup.LayoutParams.WrapContent,
                Width = ViewGroup.LayoutParams.WrapContent,
                OutsideTouchable = true,
                Touchable = true,
                Focusable = true,
                ContentView = _contentView
            };
            _tipWindow.SetBackgroundDrawable(new BitmapDrawable());


            // Call view measure to calculate how big your view should be.
            _contentView.Measure(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
            ApplyToolTipPosition(anchor, _contentView.MeasuredHeight, _contentView.MeasuredWidth, out int positionX,
                out int positionY);
           _tipWindow.ShowAtLocation(anchor, GravityFlags.NoGravity, positionX, positionY);

            OnHandlerDispose();
            if (DelayedMsec >= 1000)
            {
                _handler = new Handler(msg =>
                {
                    switch (msg.What)
                    {
                        case MessageDismissTooltip:
                            try
                            {
                                Dismiss();
                            }
                            // ReSharper disable once EmptyGeneralCatchClause
                            catch{ }
                            break;
                    }
                });

                // send message to handler to dismiss tipWindow after X milliseconds
                _handler.SendEmptyMessageDelayed(MessageDismissTooltip, DelayedMsec);
            }
        }

        public void Dismiss()
        {
            if (!IsDisposed && _tipWindow != null && _tipWindow.Handle != IntPtr.Zero && _tipWindow.IsShowing)
                _tipWindow.Dismiss();
        }

        private void ApplyToolTipPosition(View anchor, int contentViewHeight, int contentViewWidth, out int positionX, out int positionY)
        {
            const int offset = 0;

            var masterViewScreenPosition = new int[2];
            anchor.GetLocationOnScreen(masterViewScreenPosition);

            var viewDisplayFrame = new Rect();
            anchor.GetWindowVisibleDisplayFrame(viewDisplayFrame);

            var anchorWidth = anchor.Width;
            var anchorHeight = anchor.Height;

            var mRelativeMasterViewX = masterViewScreenPosition[0];
            var mRelativeMasterViewY = masterViewScreenPosition[1];
            var relativeMasterViewCenterX = mRelativeMasterViewX + anchorWidth / 2;

            var toolTipViewAboveY = mRelativeMasterViewY - contentViewHeight;
            var toolTipViewBelowY = Math.Max(offset, mRelativeMasterViewY + anchorHeight);

            var toolTipViewX = Math.Max(offset, relativeMasterViewCenterX - contentViewWidth / 2);
            if (toolTipViewX + contentViewWidth > viewDisplayFrame.Right)
                toolTipViewX = viewDisplayFrame.Right - contentViewWidth;
            
            var showBelow = toolTipViewAboveY < 0;
            var toolTipViewY = showBelow
                ? toolTipViewBelowY
                : toolTipViewAboveY -
                  (int) TypedValue.ApplyDimension(ComplexUnitType.Dip, 4, anchor.Resources.DisplayMetrics); // - anchorHeight; 

            positionX = toolTipViewX;
            positionY = toolTipViewY;
        }

        private Context GetContext()
        {
            if (_context == null)
                return null;

            return _context.TryGetTarget(out Context context) ? context : null;
        }

        #region . IDisposable .

        private void OnDispose()
        {
            try
            {
                OnHandlerDispose();
                OnPopupWindowDispose();
            }
            finally
            {
                IsDisposed = true;
            }
        }

        private void OnPopupWindowDispose()
        {
            if (_tipWindow != null && _tipWindow.Handle != IntPtr.Zero)
            {
                Dismiss();
                _tipWindow.Dispose();
                _tipWindow = null;
            }
        }

        private void OnHandlerDispose()
        {
            if (_handler != null && _handler.Handle != IntPtr.Zero)
            {
                //_handler.RemoveMessages(MessageDismissTooltip);
                _handler.RemoveCallbacksAndMessages(null);
                _handler.Dispose();
                _handler = null;
            }
        }

        public void Dispose()
        {
            OnDispose();
            GC.SuppressFinalize(this);
        }
        #endregion . IDisposable .
    }
}