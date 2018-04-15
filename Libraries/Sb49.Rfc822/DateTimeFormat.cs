using System.Text;

namespace Sb49.Rfc822
{
    /// <summary>
    /// Utility class to create a format specifier for parsing a DateTime object 
    /// from a date time string that is based on RFC 822 syntax rules.
    /// </summary>
    public class DateTimeFormat
    {
        /// <summary>
        /// Creates a new DateTimeFormat object based on the syntax rules 
        /// provided in the syntax parameter.
        /// </summary>
        /// <param name="syntax">The syntax rules that should apply when using 
        /// this DateTimeFormat object.</param>
        public DateTimeFormat(DateTimeSyntax syntax)
        {
            Syntax = syntax;
            Init();
        }

        /// <summary>
        /// The syntax rules that should apply when using this DateTimeFormat object.
        /// </summary>
        public DateTimeSyntax Syntax { get; }

        /// <summary>
        /// Format specifier that can be used to parse or format a 
        /// DateTime object according to RFC 822 date time syntax rules. 
        /// </summary>
        public string FormatSpecifier { get; protected set; }

        private void Init()
        {
            FormatSpecifier = OnFormatSpecifier(Syntax);
        }

        protected virtual string OnFormatSpecifier(DateTimeSyntax syntax)
        {
            var specifier = new StringBuilder();

            specifier.Append(syntax.HasFlag(DateTimeSyntax.WithDayName) ? "ddd, " : string.Empty);
            specifier.Append(syntax.HasFlag(DateTimeSyntax.TwoDigitDay) ? "dd" : "d");
            specifier.Append(" MMM");
            specifier.Append(syntax.HasFlag(DateTimeSyntax.FourDigitYear) ? " yyyy" : " yy");
            if (syntax.HasFlag(DateTimeSyntax.UseAmPm))
            {
                if (syntax.HasFlag(DateTimeSyntax.TwoDigitTime))
                    specifier.Append(syntax.HasFlag(DateTimeSyntax.WithSeconds) ? " hh:mm:ss tt" : " hh:mm tt");
                else
                    specifier.Append(syntax.HasFlag(DateTimeSyntax.WithSeconds) ? " h:m:s tt" : " h:m tt");
            }
            else
            {
                if (syntax.HasFlag(DateTimeSyntax.TwoDigitTime))
                    specifier.Append(syntax.HasFlag(DateTimeSyntax.WithSeconds) ? " HH:mm:ss" : " HH:mm");
                else
                    specifier.Append(syntax.HasFlag(DateTimeSyntax.WithSeconds) ? " H:m:s" : " H:m");
            }

            return specifier.ToString();
        }
    }
}
