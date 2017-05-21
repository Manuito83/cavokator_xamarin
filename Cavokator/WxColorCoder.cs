using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Text.RegularExpressions;
using Android.Graphics;
using Android.Text;
using Android.Text.Style;

namespace Cavokator
{
    class WxColorCoder
    {
        private List<string> GoodWeather { get; } = new List<string>
        {
            "CAVOK", "Q1012"
        };

        
        public SpannableString ColorCodeMetar(string rawMetar)
        {
            var coloredMetar = new SpannableString(rawMetar);
            var regex = new Regex(string.Join("|", GoodWeather), RegexOptions.Compiled);
            var matches = regex.Matches(rawMetar);

            foreach (var match in matches.Cast<Match>())
            {
                coloredMetar = SpanMetar(rawMetar, match.Index, match.Length);
            }

            return coloredMetar;
        }


        private SpannableString SpanMetar(string originalMetar, int index, int length)
        {
            SpannableString spannableMetar = new SpannableString(originalMetar);
            spannableMetar.SetSpan(new ForegroundColorSpan(Color.Green),index, index + length, 0);
            return spannableMetar;

            // TODO: borrar
            //var builder = new StringBuilder();
            //builder.Insert(0, originalMetar.Substring(0, index));
            //builder.Insert(index, replacement);
            //builder.Insert(index+replacement.Length, originalMetar.Substring(index+length));
            //return builder.ToString();
        }

        internal void SetSpan(ForegroundColorSpan foregroundColorSpan, int v1, int v2, int v3)
        {
            throw new NotImplementedException();
        }
    }
}