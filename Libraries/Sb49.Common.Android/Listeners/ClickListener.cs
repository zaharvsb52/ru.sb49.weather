using System;
using Android.Views;

namespace Sb49.Common.Droid.Listeners
{
    public class ClickListener : Java.Lang.Object, View.IOnClickListener
    {
        private Action<View> _onclickhandler;
        public ClickListener(Action<View> clickhandler)
        {
            _onclickhandler = clickhandler;
        }

        public void OnClick(View view)
        {
            _onclickhandler?.Invoke(view);
        }
    }
}