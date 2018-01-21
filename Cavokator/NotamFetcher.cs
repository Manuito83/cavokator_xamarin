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
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace Cavokator
{
    class NotamFetcher
    {
        public NotamContainer DecodedNotam { get; } = new NotamContainer();

        public NotamFetcher(string icao)
        {
            List<string> notamList = Fetch(icao);

            Decode(notamList);

        }

        private void Decode(List<string> notamList)
        {
            // TODO: https://github.com/liviudnistran/vfrviewer/blob/master/class.NOTAM.php
            foreach (string singleNotam in notamList)
            {
                string myNotamFull = singleNotam;
                //myNotamFull = singleNotam.Replace('\r', ' ');

                ResultRaw(myNotamFull);

                string[] myNotamSections = Regex.Split((myNotamFull), @"\s(?=([A-Z]\)\s))");
                foreach (string line_match in myNotamSections)
                {
                    if (Regex.IsMatch(line_match, @"(^|\s)Q\) (.*)"))
                    {
                        string shortQline = line_match.Replace(" ", "");

                        string qStructure = @"Q\)(?<FIR>[A-Z]{4})\/(?<CODE>[A-Z]{5})\/(?<TRAFFIC>IV|I|V|K)\/(?<PURPOSE>[A-Z]{1,3})\/(?<SCOPE>[A-Z]{1,2})\/(?<LOWER>[0-9]{3})\/(?<UPPER>[0-9]{3})\/(?<LAT>[0-9]{4})(?<LAT_CODE>N|S)(?<LON>[0-9]{5})(?<LONG_CODE>E|W)(?<RADIUS>[0-9]{3})";


                        Regex regexQ = new Regex(qStructure);
                        Match qMatches = regexQ.Match(shortQline);

                        if (qMatches.Success)
                        {
                            Console.WriteLine("*****fir: " + qMatches.Groups["FIR"].Value);
                        }

                        // TODO: decoding

                    }
                }
            }





        }

        private void ResultRaw(string myNotamFull)
        {
            DecodedNotam.NotamRaw.Add(myNotamFull);
        }

        private List<string> Fetch(string icao)
        {
            HtmlWeb web = new HtmlWeb();
            var htmlDoc = web.Load(GetSourceUrl(icao));

            HtmlNodeCollection notamCollection = htmlDoc.DocumentNode.SelectNodes("//pre");

            List<String> myNotams = new List<string>();
            if (notamCollection != null)
            {
                foreach (HtmlNode node in notamCollection)
                {
                    myNotams.Add(node.InnerText);
                }
            }
            
            return myNotams; 
        }

        private string GetSourceUrl(string icao)
        {
            string url = "https://pilotweb.nas.faa.gov/PilotWeb/notamRetrievalByICAOAction.do?"
                         + "method=displayByICAOs&reportType=RAW&formatType=DOMESTIC&"
                         + $"retrieveLocId={icao}&actionType=notamRetrievalByICAOs";

            return url;
        }
    }
}