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
        
        // Wind Intensity
        private int _regularWindIntensity = 20;
        private int _badWindIntensity = 30;

        // Gust Intensity
        private int _regularGustIntensity = 30;
        private int _badGustIntensity = 40;

        // Wind Meters Per Second Intensity
        private int _regularMpsWindIntensity = 10;
        private int _badMpsWindIntensity = 15;

        // Wind Meters Per Second Gust
        private int _regularMpsGustIntensity = 15;
        private int _badMpsGustIntensity = 20;



        // Visibility
        private int _regularVisibility = 6000;
        private int _badVisibility = 1000;

        
        
        private List<string> GoodWeather { get; } = new List<string>
        {
            "CAVOK", "NOSIG", "NSC", "00000KT"
        };


        private List<string> RegularWeather { get; } = new List<string>
        {
            @"[-]RA", @"\sRA",                      // Rain
            @"[-]DZ", @"\sDZ",                      // Drizzle
            @"[-]SG", @"\sSG",                      // Snow Grains
            @"\sIC",                                // Ice Crystals
            @"[-]PE", @"\sPE",                      // Ice Pellets

            @"[-]SN", "DRSN", "DRSN",               // Snow

            @"[-]GR", @"\sGR",                      // Hail
            @"[-]GS", @"\sGS",                      // Small Hail

            @"[-]SH(([A-Z]+)|(\z))",                // -SH, -SH(whatever), including last word in string
            @"[-]TS(([A-Z]+)|(\z))",                // -TS, -TS(whatever), including last word in string
            @"\sTS(([A-Z]+)|(\z))",                 // TS, TS(whatever), including last word in string
               
            @"[-]FZ(([A-Z]+)|(\z))",                // -FZ, -FZ(whatever), including last word in string

            "BR", "FU", "DU", "SA", "HZ", "PY",     // Visibility
            "VCFG", "MIFG", "PRFG", "BCFG",
            "DRDU", "BLDU", "DRSA", "BLSA", "BLPY",

            "RERA", "VCSH", "VCTS", "SHRA"          // Some others
        };


        private List<string> BadWeather { get; } = new List<string>
        {
            @"\s[+](([A-Z]+)|(\z))",                // ANYTHING WITH A "+" 

            @"[+]RA",                               // Rain
            @"[+]DZ",                               // Drizzle
            @"[+]SG",                               // Snow Grains
            @"[+]PE",                               // Ice Pellets
            @"\sSN", @"[+]SN", "BLSN",              // Snow

            "SHSN", "SHPE", "SHGR", "SHGS",         // Red Showers

            @"\sFG", "VA",                          // Visibility

            @"[+]TS(([A-Z]+)|(\z))",                // +TS, +TS(whatever), including last word in string
            @"[+]SH(([A-Z]+)|(\z))",                // +SH, +SH(whatever), including last word in string
            @"[+]FZ(([A-Z]+)|(\z))",                // +FZ, +FZ(whatever), including last word in string
            @"\sFZ(([A-Z]+)|(\z))",                 // FZ, FZ(whatever), including last word in string

            @"\sPO", @"\sSQ", @"\sFC", @"\sSS", @"\sDS",    // Sand/Dust Whirls, Squalls, Funnel Cloud, Sandstorm, Duststorm
            @"[+]FC",@"[+]SS",@"[+]DS",
            @"\sVCPO", @"\sVCSS", @"\sVCDS"

        };


        public SpannableString ColorCodeMetar(string rawMetar)
        {

            // TODO: DELETE WHEN TESTING IS OVER!!
            rawMetar = "LBBG 041600Z 12008MPS 12012MPS 12018MPS 12008G18MPS 12012G07MPS 12016G16MPS 12018G20MPS" +
                " 0500 1000 2000 5000 5005" +
                " SHSN SHRA " +
                "12015G20KT 12015G30KT 12015G40KT 12025G30KT 12025G40KT 12035G40KT 20015KT 20025KT 20035KT" +
                " 090V150 1400 R04/P1500N R22/P1500U +SN BKN022 OVC050 M04/M07 Q1020 NOSIG 8849//91= +SN";
            // TEST**TEST**TEST**

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


            
            // WIND KNOTS - e.g.: 25015KT
            var windRegex = new Regex(@"\s[0-9]+KT");
            var windMatches = windRegex.Matches(rawMetar);
            foreach (var match in windMatches.Cast<Match>())
            {
                var windIntensity = rawMetar.Substring(match.Index + 4, 2);

                try
                {
                    if (Int32.Parse(windIntensity) >= _regularWindIntensity && Int32.Parse(windIntensity) < _badWindIntensity)
                    {
                        coloredMetar = SpanRegularMetar(coloredMetar, match.Index, 8);
                    }
                    else if (Int32.Parse(windIntensity) >= _badWindIntensity)
                    {
                        coloredMetar = SpanBadMetar(coloredMetar, match.Index, 8);
                    }
                }
                catch
                {
                    // ignored
                }
            }



            // WIND KNOTS 2 - e.g.: 25015G30KT
            var wind2Regex = new Regex(@"\s[0-9]+G[0-9]+KT");
            var wind2Matches = wind2Regex.Matches(rawMetar);
            foreach (var match in wind2Matches.Cast<Match>())
            {

                // INTENSITY
                var windIntensity = rawMetar.Substring(match.Index + 4, 2);
                try
                {
                    if (Int32.Parse(windIntensity) >= _regularWindIntensity && Int32.Parse(windIntensity) < _badWindIntensity)
                    {
                        coloredMetar = SpanRegularMetar(coloredMetar, match.Index, 6);
                        coloredMetar = SpanRegularMetar(coloredMetar, match.Index + 9, 2);
                    }
                    else if (Int32.Parse(windIntensity) >= _badWindIntensity)
                    {
                        coloredMetar = SpanBadMetar(coloredMetar, match.Index, 6);
                        coloredMetar = SpanRegularMetar(coloredMetar, match.Index + 9, 2);
                    }
                }
                catch
                {
                    // ignored
                }
                

                // GUST
                var windGust = rawMetar.Substring(match.Index + 7, 2);
                try
                {
                    if (Int32.Parse(windGust) >= _regularGustIntensity && Int32.Parse(windGust) < _badGustIntensity)
                    {
                        coloredMetar = SpanRegularMetar(coloredMetar, match.Index + 6, 5);
                    }
                    else if (Int32.Parse(windGust) >= _badGustIntensity)
                    {
                        coloredMetar = SpanBadMetar(coloredMetar, match.Index + 6, 5);
                    }
                }
                catch
                {
                    // ignored
                }
            }



            // WIND MPS - e.g.: 13030MPS
            var windMpsRegex = new Regex(@"\s[0-9]+MPS");
            var windMpsMatches = windMpsRegex.Matches(rawMetar);
            foreach (var match in windMpsMatches.Cast<Match>())
            {
                var windIntensity = rawMetar.Substring(match.Index + 4, 2);

                try
                {
                    if (Int32.Parse(windIntensity) >= _regularMpsWindIntensity && Int32.Parse(windIntensity) < _badMpsWindIntensity)
                    {
                        coloredMetar = SpanRegularMetar(coloredMetar, match.Index, 9);
                    }
                    else if (Int32.Parse(windIntensity) >= _badMpsWindIntensity)
                    {
                        coloredMetar = SpanBadMetar(coloredMetar, match.Index, 9);
                    }
                }
                catch
                {
                    // ignored
                }
            }



            // WIND MPS 2 - e.g.: 13030G20MPS
            var windMps2Regex = new Regex(@"\s[0-9]+G[0-9]+MPS");
            var windMps2Matches = windMps2Regex.Matches(rawMetar);
            foreach (var match in windMps2Matches.Cast<Match>())
            {
                // INTENSITY
                var windIntensity = rawMetar.Substring(match.Index + 4, 2);
                try
                {
                    if (Int32.Parse(windIntensity) >= _regularMpsWindIntensity && Int32.Parse(windIntensity) < _badMpsWindIntensity)
                    {
                        coloredMetar = SpanRegularMetar(coloredMetar, match.Index, 6);
                        coloredMetar = SpanRegularMetar(coloredMetar, match.Index + 9, 3);
                    }
                    else if (Int32.Parse(windIntensity) >= _badMpsWindIntensity)
                    {
                        coloredMetar = SpanBadMetar(coloredMetar, match.Index, 6);
                        coloredMetar = SpanRegularMetar(coloredMetar, match.Index + 9, 3);
                    }
                }
                catch
                {
                    // ignored
                }


                // GUST
                var windGust = rawMetar.Substring(match.Index + 7, 2);
                try
                {
                    if (Int32.Parse(windGust) >= _regularMpsGustIntensity && Int32.Parse(windGust) < _badMpsGustIntensity)
                    {
                        coloredMetar = SpanRegularMetar(coloredMetar, match.Index + 6, 6);
                    }
                    else if (Int32.Parse(windGust) >= _badMpsGustIntensity)
                    {
                        coloredMetar = SpanBadMetar(coloredMetar, match.Index + 6, 6);
                    }
                }
                catch
                {
                    // ignored
                }
            }



            // VISIBILITY
            var visibilityRegex = new Regex(@"(?<=\s)([0-9]+)(?=\s)");
            var visibiltyMatches = visibilityRegex.Matches(rawMetar);
            foreach (var match in visibiltyMatches.Cast<Match>())
            {

                var visibilityValue = rawMetar.Substring(match.Index, 4);

                try
                {
                    if (Int32.Parse(visibilityValue) <= _regularVisibility && Int32.Parse(visibilityValue) > _badVisibility)
                    {
                        coloredMetar = SpanRegularMetar(coloredMetar, match.Index, 4);
                    }
                    else if (Int32.Parse(visibilityValue) <= _badVisibility)
                    {
                        coloredMetar = SpanBadMetar(coloredMetar, match.Index, 4);
                    }
                }
                catch
                {
                    // ignored
                }
            }



            // TODO: RVR
            var rvrRegex = new Regex(@"R[0-9]+.\057[P|M][0-9]+[U|D|N]");





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