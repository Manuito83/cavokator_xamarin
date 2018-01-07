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
            foreach (string a in notamList)
            {
                Console.WriteLine("****");
                //string[] stringSeparators = new string[] { "Q)", "B)", "C)", "D)", "E)" };
                //string[] result;
                //result = a.Split(stringSeparators, StringSplitOptions.None);
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
            var htmlDoc = web.Load(SourceUrl(icao));

            HtmlNodeCollection notamCollection = htmlDoc.DocumentNode.SelectNodes("//pre");

            List<String> myNotams = new List<string>();
            foreach (HtmlNode node in notamCollection)
            {
                myNotams.Add(node.InnerText);
            }

            return myNotams; 
        }

        private string SourceUrl(string icao)
        {
            string url = "https://pilotweb.nas.faa.gov/PilotWeb/notamRetrievalByICAOAction.do?"
                         + "method=displayByICAOs&reportType=RAW&formatType=DOMESTIC&"
                         + $"retrieveLocId={icao}&actionType=notamRetrievalByICAOs";

            return url;
        }
    }
}