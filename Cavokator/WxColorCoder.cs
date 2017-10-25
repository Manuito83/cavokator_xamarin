using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Android.Graphics;
using Android.Text;
using Android.Text.Style;
using Android.Views;

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

        // RVR
        private int _regularRvr = 1000;
        private int _badRvr = 600;




        private List<string> GoodWeather { get; } = new List<string>
        {
            "CAVOK", "NOSIG", "NSC", "00000KT"
        };


        private List<string> RegularWeather { get; } = new List<string>
        {
            @"[-]RA(([A-Z]+)|(\z))",                // -RA, -RA(whatever), including last word in string
            @"\sRA(([A-Z]+)|(\z))",                 // RA, RA(whatever), including last word in string

            @"\sSH(([A-Z]+)|(\z))",                 // SH, SH(whatever), including last word in string
            @"[-]SH(([A-Z]+)|(\z))",                // -SH, -SH(whatever), including last word in string

            @"[-]TS(([A-Z]+)|(\z))",                // -TS, -TS(whatever), including last word in string
            @"\sTS(([A-Z]+)|(\z))",                 // TS, TS(whatever), including last word in string
               
            @"[-]FZ(([A-Z]+)|(\z))",                // -FZ, -FZ(whatever), including last word in string

            @"[-]RA", @"\sRA",                      // Rain
            @"[-]DZ", @"\sDZ",                      // Drizzle
            @"[-]SG", @"\sSG",                      // Snow Grains
            @"\sIC",                                // Ice Crystals
            @"[-]PE", @"\sPE",                      // Ice Pellets

            @"[-]SN", "DRSN", "DRSN",               // Snow

            @"[-]GR", @"\sGR",                      // Hail
            @"[-]GS", @"\sGS",                      // Small Hail

            @"\sBR+(\s|\b)", @"\sFU+(\s|\b)",       // Visibility
            @"\sDU+(\s|\b)", @"\sSA+(\s|\b)",       // Visibility
            @"\sHZ+(\s|\b)", @"\sPY+(\s|\b)",       // Visibility
            "VCFG", "MIFG", "PRFG", "BCFG",
            "DRDU", "BLDU", "DRSA", "BLSA", "BLPY",

            "RERA", "VCSH", "VCTS", "SHRA"          // Some others
        };


        private List<string> BadWeather { get; } = new List<string>
        {
            @"\s[+](([A-Z]+)|(\z))",                // ANYTHING WITH A "+" 
            @"[+]TS(([A-Z]+)|(\z))",                // +TS, +TS(whatever), including last word in string
            @"[+]SH(([A-Z]+)|(\z))",                // +SH, +SH(whatever), including last word in string
            @"[+]FZ(([A-Z]+)|(\z))",                // +FZ, +FZ(whatever), including last word in string
            @"\sFZ(([A-Z]+)|(\z))",                 // FZ, FZ(whatever), including last word in string

            @"[+]RA",                               // Rain
            @"[+]DZ",                               // Drizzle
            @"[+]SG",                               // Snow Grains
            @"[+]PE",                               // Ice Pellets
            @"\sSN", @"[+]SN", "BLSN",              // Snow

            "SHSN", "SHPE", "SHGR", "SHGS",         // Red Showers

            @"\sFG", @"\sVA+(\s|\b)",               // Visibility

            @"\sPO", @"\sSQ", @"\sFC", @"\sSS",     // Sand/Dust Whirls, Squalls, Funnel Cloud, Sandstorm
            @"\sDS+(\s|\z)",                        // Trying to avoid american "distant" (DSNT)
            @"[+]FC",@"[+]SS",@"[+]DS",
            @"\sVCPO", @"\sVCSS", @"\sVCDS"

        };


        /// <summary>
        /// Provide a string with METAR or TAFOR for color coding.
        /// </summary>
        /// <param name="rawMetar"></param>
        /// <returns></returns>
        public SpannableString ColorCodeMetar(string rawMetar)
        {

            // ** CAUTION: USE ONLY FOR TESTING **
            // rawMetar = "LBBG 041600Z 12012G07MPS 0500 SHRA 12015G20KT R04/P1500N R22/P0800U R22L/P0500U 8849//91= ";
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
            var visibilityRegex = new Regex(@"(?<=\s)([0-9]{4})(?=\s)");
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



            // RVR
            var rvrRegex = new Regex(@"R[0-9]+.\057[P|M][0-9]+[U|D|N]");
            var rvrMatches = rvrRegex.Matches(rawMetar);
            foreach (var match in rvrMatches.Cast<Match>())
            {

                var rvrValue = rawMetar.Substring(match.Index + match.Length - 5, 4);

                try
                {
                    if (Int32.Parse(rvrValue) <= _regularRvr && Int32.Parse(rvrValue) > _badRvr)
                    {
                        coloredMetar = SpanRegularMetar(coloredMetar, match.Index + match.Length - 6, 5);
                    }
                    else if (Int32.Parse(rvrValue) <= _badRvr)
                    {
                        coloredMetar = SpanBadMetar(coloredMetar, match.Index + match.Length - 6, 5);
                    }
                }
                catch
                {
                    // ignored
                }

                var rvrTrend = rawMetar.Substring(match.Index + match.Length - 1, 1);

                try
                {
                    switch (rvrTrend)
                    {
                        case "U":
                            coloredMetar = SpanGoodMetar(coloredMetar, match.Index + match.Length - 1, 1);
                            break;
                        case "N":
                            coloredMetar = SpanRegularMetar(coloredMetar, match.Index + match.Length - 1, 1);
                            break;
                        case "D":
                            coloredMetar = SpanBadMetar(coloredMetar, match.Index + match.Length - 1, 1);
                            break;
                    }
                }
                catch
                {
                    // ignored
                }
                try
                {
                    if (rawMetar.Substring(match.Index + 3, 1) == "/")
                    {
                        coloredMetar = SpanInfoColor(coloredMetar, match.Index, 4);
                    }
                    else
                    {
                        coloredMetar = SpanInfoColor(coloredMetar, match.Index, 5);
                    }
                }
                catch
                {
                    // ignored
                }
            }



            // TEMPORARY
            var tempoRegex = new Regex(@"(PROB[0-9]{2} TEMPO)|(BECMG)|(TEMPO)|(FM)[0-9]{6}");
            var tempoMatches = tempoRegex.Matches(rawMetar);
            foreach (var match in tempoMatches.Cast<Match>())
            {
                try
                {
                    coloredMetar = SpanInfoColor(coloredMetar, match.Index, match.Length);
                }
                catch
                {
                    // ignored
                }

            }




            //TODO: **EXAMPLE FOR UNDERLINE**
            // RUNWAY CONDITION / MOTNE
            var conditionRegex = new Regex(@"((\b)+(R[0-9]{2})+(R|L|C|\/)+(([0-9]|\/){6})+(\b))|((\b)+(([0-9]|\/){8})+(\b))");
            var conditionMatches = conditionRegex.Matches(rawMetar);
            foreach (var match in conditionMatches.Cast<Match>())
            {
                try
                {
                    coloredMetar = SpanConditionColor(coloredMetar, match.Index, match.Length, match.ToString());
                }
                catch
                {
                    // ignored
                }

            }
            //*******************************





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


        // Apply information color
        private SpannableString SpanInfoColor(SpannableString entryColoredMetar, int index, int length)
        {
            entryColoredMetar.SetSpan(new ForegroundColorSpan(Color.Cyan), index, index + length, 0);
            return entryColoredMetar;
        }



        // BEGIN CONDITION COLOR 
        
        // First, declare an event to pass the clicked condition to main activity
        public event EventHandler<WxColorCoderArgs> ClickedRunwayCondition;

        // Save the actual text that is being highlighted, so that we can pass it as a parameter
        // to the WxColorCoderArgs for each object created
        private string matched_clickkable_condition;
        
        // Apply condition color
        private SpannableString SpanConditionColor(SpannableString entryColoredMetar, int index, int length, string matched_text)
        {
            // Update text directly from the match
            matched_clickkable_condition = matched_text;

            // Create instance of ClickableSpan and assign field for actual text that was clicked
            var clickableRunwayCondition = new MyClickableSpan(matched_clickkable_condition);
            
            // Subscribe to the actual click for each instance
            clickableRunwayCondition.ClickedMyClickableSpan += OnClickedRunwayCondition;

            entryColoredMetar.SetSpan(clickableRunwayCondition, index, index + length, 0);
            entryColoredMetar.SetSpan(new UnderlineSpan(), index, index + length, 0);
            entryColoredMetar.SetSpan(new BackgroundColorSpan(Color.Yellow), index, index + length, 0);
            entryColoredMetar.SetSpan(new ForegroundColorSpan(Color.Black), index, index + length, 0);

            return entryColoredMetar;
        }

        private void OnClickedRunwayCondition(object source, MyClickableSpanArgs e)
        {
            ClickedRunwayCondition?.Invoke(this, new WxColorCoderArgs() { RunwayCondition = e.Clicklable_Text });
        }

    }


    public class WxColorCoderArgs : EventArgs
    {
        public string RunwayCondition { get; set; }
    }


}