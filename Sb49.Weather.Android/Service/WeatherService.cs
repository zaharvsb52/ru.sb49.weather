using System;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using Sb49.Common.Logging;
using Sb49.Weather.Droid.Common;

namespace Sb49.Weather.Droid.Service
{
    [Service(Name = AppSettings.AppPackageName + ".WeatherService")]
    public class WeatherService : Android.App.Service
    {
        private CancellationTokenSource _cancellationTokenSource;
        private Timer _timer;
        private int _dueTimeMsec;
        private int _timerPeriodMsec;
        private uint _count;
        private readonly ILog _log = LogManager.GetLogger<WeatherService>();
        internal static bool IsServiceRunning;

        public override void OnCreate()
        {
            try
            {
                base.OnCreate();

                OnDispose();
                _cancellationTokenSource = new CancellationTokenSource();
                _dueTimeMsec = AppSettings.SchedulerDelayMsec;
                _timerPeriodMsec = AppSettings.SchedulerPeriodMsec;
                TimerStart(_dueTimeMsec);
                _log.Info("Service is started.");
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            IsServiceRunning = true;
            return StartCommandResult.Sticky;
        }

        public override void OnLowMemory()
        {
            _log.Info("OnLowMemory");
            AppSettings.GcCollect(true, _log); //HARDCODE:

            base.OnLowMemory();
        }

        public override void OnDestroy()
        {
            _log.Info("Service is stopped.");

            try
            {
                IsServiceRunning = false;
                OnDispose();
            }
            catch (Exception ex)
            {
                _log.Debug(ex);
            }

            base.OnDestroy();
        }

        private void OnTimerTick(object state)
        {
            if(IfCancellationRequested() || _timer == null)
                return;

            try
            {
                _log.DebugFormat("Scheduler (Count = '{0}').", _count);

                var api = new ServiceApi();
                api.ArrangeAppWidgetSettings(this);

                if(IfCancellationRequested())
                    return;

                if (!api.ExistsAppWidget(this))
                {
                    StopSelf();
                    return;
                }

                api.AppWidgetUpdateService(this, null);

                if (IfCancellationRequested())
                    return;

                var isBootCompletedReceiver = AppSettings.Default.IsBootCompletedReceiver;
                if (isBootCompletedReceiver)
                    AppSettings.Default.IsBootCompletedReceiver = false;

                api.AppWidgetWeatherDataUpdateService(this, isBootCompletedReceiver, _cancellationTokenSource.Token);
            }
            catch (System.OperationCanceledException ex)
            {
                _log.Debug(ex);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
            finally
            {
                if (_count + 1 == uint.MaxValue)
                    _count = 0;
                _count++;
            }
        }

        private void TimerStart(int dueTimeMsec)
        {
            TimerDispose();
            _timer = new Timer(OnTimerTick, null, dueTimeMsec, _timerPeriodMsec);
            _count = 0;
        }

        private void TimerStop()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void TimerDispose()
        {
            if (_timer != null)
            {
                TimerStop();
                _timer.Dispose();
                _timer = null;
            }
        }

        private void OnDispose()
        {
            _cancellationTokenSource?.Cancel();
            TimerDispose();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        private bool IfCancellationRequested()
        {
            return _cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested;
        }
    }
}