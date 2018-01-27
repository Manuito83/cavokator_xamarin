using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
        public NotamContainer DecodedNotam = new NotamContainer();

        public NotamFetcher(string icao)
        {
            List<string> notamList = Fetch(icao);

            if (notamList != null)
                Decode(notamList);
            else
                DecodedNotam.ConnectionError = true;
        }

        private void Decode(List<string> notamList)
        {
            // TODO: https://github.com/liviudnistran/vfrviewer/blob/master/class.NOTAM.php
            foreach (string singleNotam in notamList)
            {
                // Try to find what kind of NOTAM we are dealing with
                NotamTypeQ qNotamContainer = AssessNotamTypeQ(singleNotam);
                NotamTypeD dNotamContainer = AssessNotamTypeD(singleNotam);

                if (qNotamContainer == null && dNotamContainer == null)
                {
                    // Pass RAW value to container
                    FillContainerWithRawLines(singleNotam);
                }
                else if (qNotamContainer != null)
                {
                    FillContainerWithNotamQInformation(qNotamContainer);
                    DecodedNotam.NotamRaw.Add(singleNotam);
                }
                else if (dNotamContainer != null)
                {
                    // TODO
                    // FillContainerWithNotamD(dNotamContainer);
                    // DecodedNotam.NotamRaw.Add(singleNotam);
                }
            }
        }

        private NotamTypeQ AssessNotamTypeQ(string singleNotam)
        {
            NotamTypeQ myNotamTypeQ = new NotamTypeQ();

            try
            {
                string[] myNotamSections = Regex.Split((singleNotam), @"\s(?=([A-Z]\)\s))");
                foreach (string line in myNotamSections)
                {
                    // NOTAM ID
                    Regex idRegex = new Regex(@"(^|\s)(?<ID>[A-Z][0-9]{4}\/[0-9]{2}) (NOTAMN|NOTAMR|NOTAMC)");
                    Match idMatch = idRegex.Match(line);
                    if (idMatch.Success)
                        myNotamTypeQ.NotamID = idMatch.Groups["ID"].Value;
                    
                    // GROUP Q)
                    else if (Regex.IsMatch(line, @"(^|\s)Q\) (.*)"))
                    {
                        string shortQline = line.Replace(" ", "");
                        Regex qRegex = new Regex(@"Q\)(?<FIR>[A-Z]{4})\/(?<CODE>[A-Z]{5})\/(?<TRAFFIC>IV|I|V|K)\/(?<PURPOSE>[A-Z]{1,3})\/(?<SCOPE>[A-Z]{1,2})\/(?<LOWER>[0-9]{3})\/(?<UPPER>[0-9]{3})\/(?<LAT>[0-9]{4})(?<LAT_CODE>N|S)(?<LON>[0-9]{5})(?<LONG_CODE>E|W)(?<RADIUS>[0-9]{3})");
                        Match qMatch = qRegex.Match(shortQline);
                        if (qMatch.Success)
                            myNotamTypeQ.QMatch = qMatch;
                    }

                    // GROUP E)
                    else if (Regex.IsMatch(line, @"(^|\s)E\) ([.\n|\W|\w]*)"))
                    {
                        Regex eRegex = new Regex(@"(^|\s)E\) (?<FREE_TEXT>[.\n|\W|\w]*)");
                        Match eMatch = eRegex.Match(line);
                        if (eMatch.Success)
                            myNotamTypeQ.EText = eMatch.Groups["FREE_TEXT"].Value;
                    }
                }
            }
            catch
            {
                return null;
            }

            // If we have filled all the (minimum) required data, pass the NOTAM Q
            if (myNotamTypeQ.NotamID != String.Empty &&
                myNotamTypeQ.QMatch != Match.Empty &&
                myNotamTypeQ.EText != String.Empty)

                return myNotamTypeQ;
            
            // Otherwise, return null (NOTAM to be processed as raw)
            return null;
        }

        private NotamTypeD AssessNotamTypeD(string singleNotam)
        {
            return null;
        }

        private void FillContainerWithRawLines(string singleNotam)
        {
            DecodedNotam.NotamQ.Add(false);
            DecodedNotam.NotamD.Add(false);
            DecodedNotam.NotamRaw.Add(singleNotam);
        }

        private void FillContainerWithNotamQInformation(NotamTypeQ myNotamQ)
        {
            DecodedNotam.NotamQ.Add(true);
            DecodedNotam.NotamD.Add(false);
            DecodedNotam.NotamID.Add(myNotamQ.NotamID);
            DecodedNotam.NotamFreeText.Add(myNotamQ.EText);
        }

        private List<string> Fetch(string icao)
        {
            try
            {
                var htmlSource = RetrieveHtml(GetSourceUrl(icao));

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(htmlSource);

                HtmlNodeCollection notamCollection = doc.DocumentNode.SelectNodes("//pre");

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
            catch
            {
                return null;
            }
        }

        private static string RetrieveHtml(string url)
        {
            // Used to build entire input
            StringBuilder sb = new StringBuilder();

            // Used on each read operation
            byte[] buf = new byte[8192];

            // Prepare the web page we will be asking for
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
            request.Timeout = 10000; // Timeout in milliseconds
            
            try
            {
                using (HttpWebResponse response = (HttpWebResponse) request.GetResponse())
                {
                    // We will read data via the response stream
                    Stream resStream = response.GetResponseStream();

                    string tempString = null;
                    int count = 0;

                    do
                    {
                        // Fill the buffer with data
                        count = resStream.Read(buf, 0, buf.Length);

                        // Make sure we read some data
                        if (count != 0)
                        {
                            // Translate from bytes to ASCII text
                            tempString = Encoding.ASCII.GetString(buf, 0, count);

                            // Continue building the string
                            sb.Append(tempString);
                        }
                    }
                    while (count > 0); // Any more data to read?
                }
            }
            catch (WebException e) when (e.Status == WebExceptionStatus.Timeout)
            {
                // If we got here, it was a timeout exception
            }

            return sb.ToString();
        }

        private static string GetSourceUrl(string icao)
        {
            string url = "https://pilotweb.nas.faa.gov/PilotWeb/notamRetrievalByICAOAction.do?"
                         + "method=displayByICAOs&reportType=RAW&formatType=DOMESTIC&"
                         + $"retrieveLocId={icao}&actionType=notamRetrievalByICAOs";

            return url;
        }

    }

}