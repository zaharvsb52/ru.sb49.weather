using System;
using Android.OS;

namespace Sb49.Common.Droid.CallBacks
{
    public class HandlerCallback : Java.Lang.Object, Handler.ICallback
    {
        private readonly Func<Message, bool> _handler;

        public HandlerCallback(Func<Message, bool> handler)
        {
            _handler = handler;
        }

        public bool HandleMessage(Message msg)
        {
            return _handler?.Invoke(msg) == true;
        }
    }
}