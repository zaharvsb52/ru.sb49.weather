using System;
using Android.Content;
using Android.Views;

namespace Sb49.Common.Droid.Listeners
{
    public class KeyListener : Java.Lang.Object, IDialogInterfaceOnKeyListener
    {
        private Func<IDialogInterface, Keycode, KeyEvent, bool> _keyhandler;

        public KeyListener(Func<IDialogInterface, Keycode, KeyEvent, bool> keyhandler)
        {
            _keyhandler = keyhandler;
        }

        public bool OnKey(IDialogInterface dialog, Keycode keyCode, KeyEvent e)
        {
            if (_keyhandler != null)
                return _keyhandler(dialog, keyCode, e);
            return false;
        }
    }
}