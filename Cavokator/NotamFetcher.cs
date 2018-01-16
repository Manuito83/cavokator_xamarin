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
        public NotamContainer Notams = new NotamContainer();

        public NotamFetcher(string icao)
        {
            List<string> notamList = Fetch(icao);




            // TODO: https://github.com/liviudnistran/vfrviewer/blob/master/class.NOTAM.php
            foreach (string singleNotam in notamList)
            {
                Console.WriteLine("****");

                string myNotamFull = singleNotam;
                myNotamFull = singleNotam.Replace('\r', ' ');

                string[] myNotamSections = Regex.Split((myNotamFull), @"\s(?=([A-Z]\)\s))");

                foreach (string line_match in myNotamSections)
                {
                    if (Regex.IsMatch(line_match, @"(^|\s)Q\) (.*)"))
                    {
                        string lineQ = line_match.Replace(" ", "");
                        
                        if (Regex.IsMatch(lineQ, @"Q\)([A-Z]{4})\/([A-Z]{5})\/(IV|I|V)\/([A-Z]{1,3})\/([A-Z]{1,2})\/([0-9]{3})\/([0-9]{3})\/([0-9]{4})(N|S)([0-9]{5})(E|W)([0-9]{3})"))
                        {

                        }

                    }
                }


                //foreach (string s in result)
                //{
                //    Console.WriteLine(s);
                //}

                Console.WriteLine("****");
            }




        }

        private List<string> Fetch(string icao)
        {
            HtmlWeb web = new HtmlWeb();
            var htmlDoc = web.Load(GetSourceUrl(icao));

            HtmlNodeCollection notamCollection = htmlDoc.DocumentNode.SelectNodes("//pre");

            List<String> myNotams = new List<string>();
            foreach (HtmlNode node in notamCollection)
            {
                myNotams.Add(node.InnerText);
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