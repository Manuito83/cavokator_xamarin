using System;
using System.Collections.Generic;
using System.Linq;
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
            "CAVOK", "NOSIG"
        };


        private List<string> RegularWeather { get; } = new List<string>
        {
            "-RA", "-SHRA", 
            "TS", "TSRA", "VCTS", "SHRA", "BR"
        };


        private List<string> BadWeather { get; } = new List<string>
        {
            "TSRA", @"\+SHRA"
        };


        public SpannableString ColorCodeMetar(string rawMetar)
        {
            var coloredMetar = new SpannableString(rawMetar);

            // GOOD WEATHER
            var goodRegex = new Regex(string.Join("|", GoodWeather));
            var goodMatches = goodRegex.Matches(rawMetar);
            foreach (var match in goodMatches.Cast<Match>())
            {
                coloredMetar = SpanGoodMetar(coloredMetar, match.Index, match.Length);
            }

            // REGULAR WEATHER
            var regularRegex = new Regex(string.Join("|", RegularWeather));
            var regularMatches = regularRegex.Matches(rawMetar);
            foreach (var match in regularMatches.Cast<Match>())
            {
                coloredMetar = SpanRegularMetar(coloredMetar, match.Index, match.Length);
            }

            // BAD WEATHER
            var badRegex = new Regex(string.Join("|", BadWeather));
            var badMatches = badRegex.Matches(rawMetar);
            foreach (var match in badMatches.Cast<Match>())
            {
                coloredMetar = SpanBadMetar(coloredMetar, match.Index, match.Length);
            }

            return coloredMetar;
        }

        // Take raw weather and apply green color
        private SpannableString SpanGoodMetar(SpannableString rawMetar, int index, int length)
        {
            rawMetar.SetSpan(new ForegroundColorSpan(Color.Green),index, index + length, 0);
            return rawMetar;
        }

        // Take already green-colored weather and apply yellow color
        private SpannableString SpanRegularMetar(SpannableString goodColoredMetar, int index, int length)
        {
            goodColoredMetar.SetSpan(new ForegroundColorSpan(Color.Yellow), index, index + length, 0);
            return goodColoredMetar;
        }

        // Take alredy yellow-colored weather and apply red color
        private SpannableString SpanBadMetar(SpannableString regularColoredMetar, int index, int length)
        {
            regularColoredMetar.SetSpan(new ForegroundColorSpan(Color.Red), index, index + length, 0);
            return regularColoredMetar;
        }

    }
}