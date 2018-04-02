//
// CAVOKATOR APP
// Website: https://github.com/Manuito83/Cavokator
// License GNU General Public License v3.0
// Manuel Ortega, 2018
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Java.Security;

namespace Cavokator
{
    class NotamFetcher
    {
        public NotamContainer DecodedNotam = new NotamContainer();

        public NotamFetcher(string icao)
        {
            List<string> notamList = Fetch(icao);


            // TODO: FETCH AIDAP
            FetchAidapAsync();



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
                    // Placeholder for implementation of USA Notams
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
                        myNotamTypeQ.NotamId = idMatch.Groups["ID"].Value;
                    
                    // GROUP Q)
                    else if (Regex.IsMatch(line, @"(^|\s)Q\) (.*)"))
                    {
                        string shortQline = line.Replace(" ", "");
                        Regex qRegex = new Regex(@"Q\)(?<FIR>[A-Z]{4})\/(?<CODE>[A-Z]{5})\/(?<TRAFFIC>IV|I|V|K)\/(?<PURPOSE>[A-Z]{1,3})\/(?<SCOPE>[A-Z]{1,2})\/(?<LOWER>[0-9]{3})\/(?<UPPER>[0-9]{3})\/(?<LAT>[0-9]{4})(?<LAT_CODE>N|S)(?<LON>[0-9]{5})(?<LON_CODE>E|W)(?<RADIUS>[0-9]{3})");
                        Match qMatch = qRegex.Match(shortQline);
                        if (qMatch.Success)
                            myNotamTypeQ.QMatch = qMatch;
                    }

                    // GROUP B)
                    else if (Regex.IsMatch(line, @"(^|\s)B\) ([0-9]{2})([0-9]{2})([0-9]{2})([0-9]{2})([0-9]{2})"))
                    {
                        Regex bRegex = new Regex(@"(^|\s)B\) (?<TIME_START>([0-9]{2})([0-9]{2})([0-9]{2})([0-9]{2})([0-9]{2}))");
                        Match bMatch = bRegex.Match(line);
                        if (bMatch.Success)
                        {
                            string startTimeRaw = bMatch.Groups["TIME_START"].Value;
                            DateTime myStartTime = DateTime.ParseExact(startTimeRaw, "yyMMddHHmm", null);
                            myNotamTypeQ.StartTime = myStartTime;
                        }
                    }
                    
                    // GROUP C) EST
                    else if (Regex.IsMatch(line, @"(^|\s)C\) ([0-9]{2})([0-9]{2})([0-9]{2})([0-9]{2})([0-9]{2}) (EST)"))
                    {
                        Regex cRegex = new Regex(@"(^|\s)C\) (?<TIME_END>([0-9]{2})([0-9]{2})([0-9]{2})([0-9]{2})([0-9]{2})) (EST)");
                        Match cMatch = cRegex.Match(line);
                        if (cMatch.Success)
                        {
                            string endTimeRaw = cMatch.Groups["TIME_END"].Value;
                            DateTime myEndTime = DateTime.ParseExact(endTimeRaw, "yyMMddHHmm", null);
                            myNotamTypeQ.EndTime = myEndTime;

                            myNotamTypeQ.CEstimated = true;
                        }
                    }
                    
                    // GROUP C) NORMAL
                    else if (Regex.IsMatch(line, @"(^|\s)C\) ([0-9]{2})([0-9]{2})([0-9]{2})([0-9]{2})([0-9]{2})"))
                    {
                        Regex cRegex = new Regex(@"(^|\s)C\) (?<TIME_END>([0-9]{2})([0-9]{2})([0-9]{2})([0-9]{2})([0-9]{2}))");
                        Match cMatch = cRegex.Match(line);
                        if (cMatch.Success)
                        {
                            string endTimeRaw = cMatch.Groups["TIME_END"].Value;
                            DateTime myEndTime = DateTime.ParseExact(endTimeRaw, "yyMMddHHmm", null);
                            myNotamTypeQ.EndTime = myEndTime;
                        }
                    }

                    // GROUP C) PERM
                    else if (Regex.IsMatch(line, @"(^|\s)C\) (PERM)"))
                    {
                        myNotamTypeQ.EndTime = DateTime.MaxValue;
                        myNotamTypeQ.CPermanent = true;
                    }

                    // GROUP D)
                    else if (Regex.IsMatch(line, @"(^|\s)D\) ([.\n|\W|\w]*)"))
                    {
                        Regex dRegex = new Regex(@"(^|\s)D\) (?<SPAN>[.\n|\W|\w]*)");
                        Match dMatch = dRegex.Match(line);
                        if (dMatch.Success)
                        {
                            string spanTimeRaw = dMatch.Groups["SPAN"].Value.Replace('\n', ' ');
                            myNotamTypeQ.SpanTime = spanTimeRaw;
                        }
                    }

                    // GROUP E)
                    else if (Regex.IsMatch(line, @"(^|\s)E\) ([.\n|\W|\w]*)"))
                    {
                        Regex eRegex = new Regex(@"(^|\s)E\) (?<FREE_TEXT>[.\n|\W|\w]*)");
                        Match eMatch = eRegex.Match(line);
                        if (eMatch.Success)
                            myNotamTypeQ.EText = eMatch.Groups["FREE_TEXT"].Value;
                    }

                    // GROUP F)
                    else if (Regex.IsMatch(line, @"(^|\s)F\) ([.\n|\W|\w]*)"))
                    {
                        Regex fRegex = new Regex(@"(^|\s)F\) (?<BOTTOM_LIMIT>[.\n|\W|\w]*)");
                        Match fMatch = fRegex.Match(line);
                        if (fMatch.Success)
                            myNotamTypeQ.BottomLimit = fMatch.Groups["BOTTOM_LIMIT"].Value.Replace('\n', ' ');
                    }

                    // GROUP G)
                    else if (Regex.IsMatch(line, @"(^|\s)G\) ([.\n|\W|\w]*)"))
                    {
                        Regex gRegex = new Regex(@"(^|\s)G\) (?<TOP_LIMIT>[.\n|\W|\w]*)");
                        Match gMatch = gRegex.Match(line);
                        if (gMatch.Success)
                            myNotamTypeQ.TopLimit = gMatch.Groups["TOP_LIMIT"].Value.Replace('\n', ' ');
                    }
                }
            }
            catch
            {
                return null;
            }

            // If we have filled all the (minimum) required data, pass the NOTAM Q
            if (myNotamTypeQ.NotamId != String.Empty &&
                myNotamTypeQ.QMatch != Match.Empty &&
                myNotamTypeQ.StartTime != DateTime.MinValue &&
                myNotamTypeQ.EndTime != DateTime.MinValue &&
                myNotamTypeQ.EText != String.Empty)

                return myNotamTypeQ;
            
            // Otherwise, return null (NOTAM to be processed as raw)
            return null;
        }

        private void FillContainerWithRawLines(string singleNotam)
        {
            // Valid values
            DecodedNotam.NotamRaw.Add(singleNotam);
            
            // IMPORTANT: fill all in order to avoid errors
            DecodedNotam.NotamId.Add(String.Empty);
            DecodedNotam.CodeSecondThird.Add(String.Empty);
            DecodedNotam.CodeFourthFifth.Add(String.Empty);
            DecodedNotam.Latitude.Add(0);
            DecodedNotam.Longitude.Add(0);
            DecodedNotam.Radius.Add(0);
            DecodedNotam.NotamFreeText.Add(String.Empty);
            DecodedNotam.StartTime.Add(DateTime.MinValue);
            DecodedNotam.EndTime.Add(DateTime.MinValue);
            DecodedNotam.CEstimated.Add(false);
            DecodedNotam.CPermanent.Add(false);
            DecodedNotam.Span.Add(String.Empty);
            DecodedNotam.BottomLimit.Add(String.Empty);
            DecodedNotam.TopLimit.Add(String.Empty);

            DecodedNotam.NotamQ.Add(false);
            DecodedNotam.NotamD.Add(false);
            
        }

        private void FillContainerWithNotamQInformation(NotamTypeQ myNotamQ)
        {
            DecodedNotam.NotamQ.Add(true);
            DecodedNotam.NotamD.Add(false);
            DecodedNotam.NotamId.Add(myNotamQ.NotamId);
            DecodedNotam.StartTime.Add(myNotamQ.StartTime);
            DecodedNotam.EndTime.Add(myNotamQ.EndTime);
            DecodedNotam.Span.Add(myNotamQ.SpanTime);
            DecodedNotam.NotamFreeText.Add(myNotamQ.EText);

            // Notam Codes
            string secondThird = myNotamQ.QMatch.Groups["CODE"].Value.Substring(1, 2);
            DecodedNotam.CodeSecondThird.Add(secondThird);

            string fourthFifth = myNotamQ.QMatch.Groups["CODE"].Value.Substring(3, 2);
            DecodedNotam.CodeFourthFifth.Add(fourthFifth);


            // Try to pass coordinates
            try
            {
                string latitude = myNotamQ.QMatch.Groups["LAT"].Value + myNotamQ.QMatch.Groups["LAT_CODE"].Value;
                string latitudeCode = myNotamQ.QMatch.Groups["LAT_CODE"].Value;
                string longitude = myNotamQ.QMatch.Groups["LON"].Value + myNotamQ.QMatch.Groups["LON_CODE"].Value;
                string longitudeCode = myNotamQ.QMatch.Groups["LON_CODE"].Value;
                string radius = myNotamQ.QMatch.Groups["RADIUS"].Value;
                Int32.TryParse(radius, out int radiusInt);
                DecodedNotam.Radius.Add(radiusInt);

                string degreesLatString = latitude.Substring(0, 2);
                string minutesLatString = latitude.Substring(2, 2);
                float.TryParse(degreesLatString, out var degreesLat);
                float.TryParse(minutesLatString, out var minutesLat);
                minutesLat = minutesLat / 60;
                float finalLat = degreesLat + minutesLat;
                if (latitudeCode == "S")
                    finalLat = -finalLat;
                DecodedNotam.Latitude.Add(finalLat);

                string degreesLonString = longitude.Substring(0, 3);
                string minutesLonString = longitude.Substring(3, 2);
                float.TryParse(degreesLonString, out float degreesLon);
                float.TryParse(minutesLonString, out float minutesLon);
                minutesLon = minutesLon / 60;
                float finalLon = degreesLon + minutesLon;
                if (longitudeCode == "W")
                    finalLon = -finalLon;
                DecodedNotam.Longitude.Add(finalLon);
            }
            catch
            {
                DecodedNotam.Latitude.Add(9999);
                DecodedNotam.Longitude.Add(9999);
                DecodedNotam.Radius.Add(9999);
            }

            // Estimated time?
            if (myNotamQ.CEstimated)
                DecodedNotam.CEstimated.Add(true);
            else
                DecodedNotam.CEstimated.Add(false);

            // Permanent time?
            if (myNotamQ.CPermanent)
                DecodedNotam.CPermanent.Add(true);
            else
                DecodedNotam.CPermanent.Add(false);

            // Top and bottom limits?
            if (myNotamQ.BottomLimit != String.Empty || myNotamQ.TopLimit != String.Empty)
            {
                if (myNotamQ.BottomLimit != String.Empty)
                {
                    DecodedNotam.BottomLimit.Add(myNotamQ.BottomLimit);
                }
                else
                {
                    DecodedNotam.BottomLimit.Add("(not reported)");
                }

                if (myNotamQ.TopLimit != String.Empty)
                {
                    DecodedNotam.TopLimit.Add(myNotamQ.TopLimit);
                }
                else
                {
                    DecodedNotam.TopLimit.Add("(not reported)");
                }
            }
            else
            {
                DecodedNotam.BottomLimit.Add(String.Empty);
                DecodedNotam.TopLimit.Add(String.Empty);
            }
        
        }

        private NotamTypeD AssessNotamTypeD(string singleNotam)
        {
            // Placeholder for implementation of USA Notams
            return null;
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

        // TODO: implement
        private void FetchAidapAsync()
        {
            // https://stackoverflow.com/questions/13533351/curl-request-using-net-using-security-certificate?rq=1
            // http://cemtopkaya.blogspot.com.es/2013/12/pfx-dosyasyla-webrequest-yaparak-c-ile.html
            
            // https://forums.xamarin.com/discussion/35598/android-p12-certificate-authentication-issue


            try
            {
                //**** TEST
                var myPath = "android.resource://" + Application.Context.PackageName + "/" + Application.Context.Resources.OpenRawResource(Resource.Raw.test);
                var text = File.ReadAllText(myPath);
                Console.WriteLine("Contents of example.txt = {0}", text);
                //*** TEST

                HttpWebRequest talep = (HttpWebRequest)WebRequest.Create("https://www.aidap.naimes.faa.gov/aidap/XmlNotamServlet");

                var path = "android.resource://" + Application.Context.PackageName + "/" + Resource.Raw.test;
                //var path = Android.Net.Uri.Parse("android.resource://" + "com.github.manuito83.cavokator" + "/" + Resource.Raw.aidapuser_cert_2018).Path;

                X509Certificate2 clientCertificate = new X509Certificate2(path, "start123");

                talep.ClientCertificates.Add(clientCertificate);
                talep.Credentials = CredentialCache.DefaultCredentials;
                talep.PreAuthenticate = true;

                talep.KeepAlive = true;
                talep.ContentType = "application/x-www-form-urlencoded";
                talep.Method = "POST";

                string formunIcerigi = "uid=kuid=usename&password=mypass&location_id=LEZL";

                var encoder = new ASCIIEncoding();
                var requestData = encoder.GetBytes(formunIcerigi);

                talep.ContentLength = requestData.Length;

                var gidenAkis = talep.GetRequestStream();
                gidenAkis.Write(requestData, 0, requestData.Length);
                gidenAkis.Close();

                var cevap = (HttpWebResponse)talep.GetResponse();
                Stream gelen_akisi = cevap.GetResponseStream();

                StreamReader sr = new StreamReader(gelen_akisi);
                String result = sr.ReadToEnd();
                Console.WriteLine(result);

                sr.Close();
                gelen_akisi.Close();
            }
            catch (Exception ex)
            {
                throw (ex);
            }



        }

        private string RetrieveHtml(string url)
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

        private string GetSourceUrl(string icao)
        {
            string url = "https://pilotweb.nas.faa.gov/PilotWeb/notamRetrievalByICAOAction.do?"
                         + "method=displayByICAOs&reportType=RAW&formatType=DOMESTIC&"
                         + $"retrieveLocId={icao}&actionType=notamRetrievalByICAOs";

            return url;
        }

    }

}