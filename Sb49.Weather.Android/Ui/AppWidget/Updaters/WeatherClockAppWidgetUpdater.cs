using System;
using Android.Content;
using Android.Text;
using Android.Text.Style;
using Android.Widget;
using Java.Lang;
using Sb49.Weather.Droid.Common;

namespace Sb49.Weather.Droid.Ui.AppWidget.Updaters
{
    public class WeatherClockAppWidgetUpdater : WeatherAppWidgetUpdater
    {
        private const string ApPmFormat = "a";

        public override bool HasClock => true;

        public override int GetHeightDp(bool landOrientation)
        {
            return landOrientation ? 135 : 188;
        }

        protected override RemoteViews CreateRemoteViews(Context context)
        {
            return new RemoteViews(context.PackageName, Resource.Layout.widget_weather_clock);
        }

        protected override void OnUpdateCustomPart(Context context, AppWidgetSettings settings, RemoteViews view)
        {
            var use12HourFormat = settings.Use12HourFormat;
            var offset = use12HourFormat ? 1 : 0;
            var format = settings.GetHourFormat(ApPmFormat);
            var clockLength = format.Length;

            var dateformat = Environment.NewLine + settings.GetDateFormat();
            //var lengthdate = dateformat.Length;
            format += dateformat + Environment.NewLine;

            var spannable = new SpannableString(format);
            spannable.SetSpan(new AbsoluteSizeSpan(settings.ClockTextSizeSp, true), offset, clockLength, SpanTypes.ExclusiveExclusive);
            spannable.SetSpan(new TextAppearanceSpan(context, Resource.Style.wgClockTextStyle), offset,
                clockLength, SpanTypes.ExclusiveExclusive);
            spannable.SetSpan(new AlignmentSpanStandard(Layout.Alignment.AlignCenter), 0, clockLength,
                SpanTypes.ExclusiveExclusive);

            SetClockProperties(view, Resource.Id.txtDateTime, spannable);
        }

        protected override void UpdateWeatherPart(Context context, RemoteViews view, AppWidgetSettings settings, bool isDemoMode)
        {
            if (!AppSettings.Default.LandOrientation)
            {
                var topId = settings.WidgetIconStyle == IconStyles.FancyWidgets
                    ? Resource.Dimension.wgviewImgConditionPaddingFancyWidgets
                    : Resource.Dimension.wgviewImgConditionPadding;
                view.SetViewPadding(Resource.Id.viewImgCondition, 0, (int) context.Resources.GetDimension(topId),
                    0, 0);
            }

            base.UpdateWeatherPart(context, view, settings, isDemoMode);
        }

        private void SetClockProperties(RemoteViews view, int id, ICharSequence spannable)
        {
            view.SetCharSequence(id, "setFormat12Hour", spannable);
            view.SetCharSequence(id, "setFormat24Hour", spannable);
        }
    }
}