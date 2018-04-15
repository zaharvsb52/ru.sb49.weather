using System;

namespace Sb49.Common.Logging
{
    public class LogManager : ILogManager
    {
        private static ILoggerFactoryAdapter _adapter;
        private static readonly object LoadLock = new object();

        static LogManager()
        {
            Reset();
        }

        public static ILoggerFactoryAdapter Adapter
        {
            get
            {
                if (_adapter == null)
                {
                    lock (LoadLock)
                    {
                        if (_adapter == null)
                        {
                            _adapter = BuildLoggerFactoryAdapter();
                        }
                    }
                }
                return _adapter;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(Adapter));
                }
                lock (LoadLock)
                {
                    _adapter = value;
                }
            }
        }

        ILoggerFactoryAdapter ILogManager.Adapter
        {
            get => Adapter;
            set => Adapter = value;
        }

        private static ILoggerFactoryAdapter BuildLoggerFactoryAdapter()
        {
            throw new NotImplementedException();
        }

        public static ILog GetLogger<T>(string tag = null) 
        {
            return GetLogger(typeof(T), tag);
        }

        ILog ILogManager.GetLogger<T>(string tag)
        {
            return GetLogger(typeof(T), tag);
        }

        public static ILog GetLogger(Type type, string tag = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return Adapter?.GetLogger(type, tag);
        }

        ILog ILogManager.GetLogger(Type type, string tag)
        {
            return GetLogger(type, tag);
        }

        public static void Reset()
        {
            _adapter = null;
        }

        void ILogManager.Reset()
        {
            Reset();
        }
    }
}