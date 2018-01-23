using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Cavokator
{

    public class WxFetcher
    {

        // ** TAFOR CONFIGURATION (ONLY IMPLEMENTED FOR LATEST VERSION) **
        private const int TaforHours = 24;
        private const bool TaforLast = true;

        // WxContainer to pass the result of the job
        private readonly WxContainer _wxinfo = new WxContainer();

        public event EventHandler<WxGetEventArgs> WorkRunning;
        public event EventHandler ConnectionError;

        /// <summary>
        /// Returns event args Airport (string) and PercentageCompleted (int)
        /// Returns 999 if error exists
        /// </summary>
        public event EventHandler<WxGetEventArgs> PercentageCompleted;


        private string _metarOrTafor;

        // Error handling for http timeout
        private bool _connectionErrorException;


        /// <summary>
        /// Gets weather and returns WXInfo object.
        /// Note: Only last TAFOR is retrieved 
        /// </summary>
        /// <param name="icaoIDlist">List of airport ID</param>
        /// <param name="hoursBefore">Hours before to look for metar</param>
        /// <param name="metarOrTafor">Accepts "metar_and_tafor", "only_metar", "only_tafor"</param>
        /// <param name="mostRecent">In case we just need the last one</param>
        public WxContainer Fetch(List<string> icaoIDlist, int hoursBefore, string metarOrTafor, bool mostRecent)
        {

            _metarOrTafor = metarOrTafor;

            // Call event raiser
            OnWorkStarted();
            
            // Process weather for each requested airport
            for (var i = 0; i < icaoIDlist.Count(); i++)
            {
                int airportNumber = i;

                if (_metarOrTafor == "only_metar")
                {
                    {
                        // Decode weather
                        var metarDecodedXml = GetMetar(icaoIDlist[i], hoursBefore, mostRecent);

                        // We continue working ONLY if no http error (no info)
                        // This is where the non-async task would stop if async task is not finished
                        if (!_connectionErrorException)
                        {
                            // Pass ShowWX the airport number (requested), available XML and airport ID
                            ProcessWx(airportNumber, metarDecodedXml, null, icaoIDlist[i]);
                        }
                        else
                        {
                            _connectionErrorException = true;
                        }
                    }
                }
                else if (_metarOrTafor == "only_tafor")
                {
                    {
                        var taforDecodedXml = GetTafor(icaoIDlist[i]);

                        if (!_connectionErrorException)
                        {
                            // Pass ShowWX the airport number (requested), available XML and airport ID
                            ProcessWx(airportNumber, null, taforDecodedXml, icaoIDlist[i]);
                        }
                        else
                        {
                            _connectionErrorException = true;
                        }
                    }
                }
                else if (_metarOrTafor == "metar_and_tafor")
                {
                    {
                        var metarDecodedXml = GetMetar(icaoIDlist[i], hoursBefore, mostRecent);
                        var taforDecodedXml = GetTafor(icaoIDlist[i]);

                        if (!_connectionErrorException)
                        {
                            ProcessWx(airportNumber, metarDecodedXml, taforDecodedXml, icaoIDlist[i]);
                        }
                        else
                        {
                            _connectionErrorException = true;
                        }
                    }
                }

                // Call event raiser with airport name and percentage completed
                // so that we can follow the progress
                if (!_connectionErrorException)
                {
                    int percentageCompleted = (i + 1) * 100 / icaoIDlist.Count();
                    OnPercentageCompleted(icaoIDlist[i], percentageCompleted);
                }
                else
                {
                    OnPercentageCompleted(icaoIDlist[i], 999);
                    break;
                }
            }

            // Call event raiser
            OnWorkFinished();

            return _wxinfo;

        }

        /// <summary>
        /// Gets Metar from previously formed URL based on AviationWeather.gov
        /// Passes Cancelation Token to async task where HttpRequest is
        /// Parses and returns XML received
        /// </summary>
        /// <param name="icaoId"></param>
        /// <param name="hoursBefore"></param>
        /// <param name="mostRecent"></param>
        /// <returns></returns>
        private XDocument GetMetar(string icaoId, int hoursBefore, bool mostRecent)
        {
            // Form URL
            var metarUrl = GetMetarUrl(icaoId, hoursBefore, mostRecent);

            // Get source html
            var metarRawData = RetrieveHtml(metarUrl);

            // Try to parse html into xml
            try
            {
                var parsedMetarXml = XDocument.Parse(metarRawData);
                return parsedMetarXml;
            }
            catch
            {
                // If empty (timeout or invalid), try with HTTPS address in Aviation Weather
                try
                {
                    metarUrl = GetMetarUrlHTTPS(icaoId, hoursBefore, mostRecent);
                    metarRawData = RetrieveHtml(metarUrl);

                    try
                    {
                        var parsedMetarXml = XDocument.Parse(metarRawData);
                        return parsedMetarXml;
                    }
                    catch
                    {
                        _connectionErrorException = true;
                        OnConnectionError();
                        return null;
                    }
                }
                catch
                {
                    _connectionErrorException = true;
                    OnConnectionError();
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets Tafor from previously formed URL based on AviationWeather.gov
        /// Passes Cancelation Token to async task where HttpRequest is
        /// Parses and returns XML received
        /// </summary>
        /// <param name="icaoId"></param>
        /// <returns></returns>
        private XDocument GetTafor(string icaoId)
        {
            // Form URL
            var taforUrl = GetTaforUrl(icaoId);

            // Get source html
            var taforRawData = RetrieveHtml(taforUrl);

            // Try to parse html into xml
            try
            {
                var parsedTaforXml = XDocument.Parse(taforRawData);
                return parsedTaforXml;
            }
            catch
            {
                // If empty (timeout or invalid), try with HTTPS address in Aviation Weather
                try
                {
                    taforUrl = GetTaforUrlHTTPS(icaoId);
                    taforRawData = RetrieveHtml(taforUrl);

                    try
                    {
                        var parsedTaforXml = XDocument.Parse(taforRawData);
                        return parsedTaforXml;
                    }
                    catch
                    {
                        _connectionErrorException = true;
                        OnConnectionError();
                        return null;
                    }
                }
                catch
                {
                    _connectionErrorException = true;
                    OnConnectionError();
                    return null;
                }
            }
        }

        /// <summary>
        /// Takes parsed XML and fills public List<T/> with information
        /// </summary>
        /// <param name="airportNumber"></param>
        /// <param name="metarParsedXml"></param>
        /// <param name="taforParsedXml"></param>
        /// <param name="airportId"></param>
        private void ProcessWx(int airportNumber, XContainer metarParsedXml, XContainer taforParsedXml, string airportId)
        {

            List<IGrouping<string, XElement>> airport;

            _wxinfo.AirportIDs.Add(airportId);
            
            // We get the airport ID either from the metar XML or tafor XML, one of the two has to be valid
            if (metarParsedXml != null)
            {
                airport = (from n in metarParsedXml.Descendants("METAR")
                                     group n by n.Element("station_id").Value).ToList();
            }
            else
            {
                // Group parsed xml by airports
                airport = (from n in taforParsedXml.Descendants("TAF")
                                 group n by n.Element("station_id").Value).ToList();
            }


            // If there is no airport_group, we have no airport, ID is incorrect and we show an error
            // Also, if length is below 4, we will give an empty airport, otherwise we could be getting
            // a list of airports which meet the firt 3 letters
            if (airport.Count == 0 || airportId.Length < 4)
            {
                _wxinfo.AirportErrors.Add(true);

                // We need to fill our public variables no matter what
                _wxinfo.AirportMetars.Insert(airportNumber, null);
                _wxinfo.AirportTafors.Insert(airportNumber, null);
                _wxinfo.AirportMetarsUtc.Insert(airportNumber, null);
                _wxinfo.AirportTaforsUtc.Insert(airportNumber, null);
            }
            else
            {
                _wxinfo.AirportErrors.Add(false);

                // Iterate each airport that we find "station_id" and show on TextView
                for (var i = 0; i < airport.Count(); i++)
                {
                    if (_metarOrTafor == "only_metar")
                    {
                        FillValidMetar(airportNumber, metarParsedXml, airport, i);
                        FillNullTafor(airportNumber);
                    }
                    else if (_metarOrTafor == "only_tafor")
                    {
                        FillNullMetar(airportNumber);
                        FillValidTafor(airportNumber, taforParsedXml, airport, i, airportId);
                    }
                    else if (_metarOrTafor == "metar_and_tafor")
                    {
                        FillValidMetar(airportNumber, metarParsedXml, airport, i);
                        FillValidTafor(airportNumber, taforParsedXml, airport, i, airportId);
                    }

                }
            }
        }

        private void FillValidMetar(int airportNumber, XContainer metarParsedXml, List<IGrouping<string, XElement>> airport, int i)
        {
            // Group its own weather information (METAR)
            List<XElement> metarsGroup = (from n in metarParsedXml.Descendants("METAR")
                                          where n.Element("station_id").Value == airport[i].Key
                                          select n.Element("raw_text")).ToList();


            // METARS
            var metarList = new List<string>();
            for (var j = 0; j < metarsGroup.Count(); j++)
            {
                metarList.Add(metarsGroup[j].Value);
            }
            _wxinfo.AirportMetars.Insert(airportNumber, metarList);

            // DATETIME METAR
            var metarUtcList = new List<DateTime>();
            List<XElement> metarUtcGroup = (from n in metarParsedXml.Descendants("METAR")
                                            where n.Element("station_id").Value == airport[i].Key
                                            select n.Element("observation_time")).ToList();
            
            if (metarUtcGroup.Count() == 0)
            {
                metarUtcList.Add(DateTime.MinValue);
            }
            else
            {
                for (var j = 0; j < metarUtcGroup.Count(); j++)
                {
                    metarUtcList.Add(Convert.ToDateTime(metarUtcGroup[j].Value).ToUniversalTime());
                }

            }
            _wxinfo.AirportMetarsUtc.Insert(airportNumber, metarUtcList);
        }

        private void FillValidTafor(int airportNumber, XContainer taforParsedXml, List<IGrouping<string, XElement>> airport, int i, string airportId)
        {
            // TAFOR XML
            List<XElement> taforsGroup = (from n in taforParsedXml.Descendants("TAF")
                                          where n.Element("station_id").Value == airport[i].Key
                                          select n.Element("raw_text")).ToList();


            // DATETIME TAFOR
            var taforUtcList = new List<DateTime>();
            List<XElement> taforUtcGroup = (from n in taforParsedXml.Descendants("TAF")
                                            where n.Element("station_id").Value == airport[i].Key
                                            select n.Element("issue_time")).ToList();


            // PROCESS TAFOR
            var taforList = new List<string>();
            for (var j = 0; j < taforsGroup.Count(); j++)
            {
                taforList.Add(taforsGroup[j].Value);

                taforUtcList.Add(Convert.ToDateTime(taforUtcGroup[j].Value).ToUniversalTime());
            }

            // If the TAFOR could not be found, we'll try an alternate website
            // This could happen as the AviationWeather TextServer is not always updating correctly
            if (taforsGroup.Count() == 0)
            {
                string alternateTaforString = String.Empty;
                DateTime alternateTaforUtc = DateTime.MinValue;
                    
                TryToGetAlternateTaforString(ref alternateTaforString, ref alternateTaforUtc, airportId);

                if (alternateTaforString != String.Empty 
                    && alternateTaforString != @"<html><head>" 
                    && alternateTaforUtc != DateTime.MinValue)
                {
                    taforList.Add(alternateTaforString);
                    taforUtcList.Add(alternateTaforUtc);
                }
                else
                {
                    taforUtcList.Add(DateTime.MinValue);
                }
                
            }

            // Fill the information in the WxContainer class
            _wxinfo.AirportTaforsUtc.Insert(airportNumber, taforUtcList);
            _wxinfo.AirportTafors.Insert(airportNumber, taforList);

        }

        private void TryToGetAlternateTaforString(ref string inputString, ref DateTime inputDateTime, string airportId)
        {
            // Form TAFOR URL
            string taforUrl = GetAlternateTaforUrl(airportId);

            // Get source html
            var taforRawData = RetrieveHtml(taforUrl);

            try
            {
                string[] alternateTaforRaw = taforRawData.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                // Assess if the TAFOR is very old, in that case just return String.Empty
                string firstLine = alternateTaforRaw[0];
                DateTime originalDate;
                DateTime.TryParseExact(firstLine, "yyyy/MM/dd HH:mm", null, DateTimeStyles.None, out originalDate);
                
                if ((DateTime.UtcNow - originalDate).TotalDays < 30)
                {
                    // Split and get the tafor string (second line)
                    string alternateTaforString = alternateTaforRaw[1];

                    string utcRaw = String.Empty;

                    var utcRegex = new Regex(@"(\b)+[0-3][0-9][0-2][0-9][0-5][0-9]Z+(?=\b)");
                    var utcMatches = utcRegex.Matches(alternateTaforString);
                    foreach (var match in utcMatches.Cast<Match>())
                        utcRaw = alternateTaforString.Substring(match.Index, 6);

                    DateTime tryInputDateTime;
                    DateTime.TryParseExact(utcRaw, "ddHHmm", null, DateTimeStyles.None, out tryInputDateTime);
                    inputDateTime = tryInputDateTime;

                    var monthDiff = DateTime.UtcNow.Month - inputDateTime.Month;

                    // Return DateTime by reference
                    if (inputDateTime.Day > DateTime.UtcNow.Day)
                        inputDateTime = inputDateTime.AddMonths(monthDiff - 1);
                    else
                        inputDateTime = inputDateTime.AddMonths(monthDiff);

                    // Build string from RAW to return by reference
                    // but do NOT include the first line (omited in the for loop), as it's the date
                    StringBuilder builder = new StringBuilder();
                    for (int i = 1; i < alternateTaforRaw.Length; i++)
                        builder.Append(alternateTaforRaw[i]);
                    inputString = builder.ToString().Replace('\n', ' ');
                }
                else
                {
                    inputDateTime = DateTime.MinValue;
                    inputString = String.Empty;
                }
            }
            catch (OperationCanceledException)
            {
                // Call event raiser
                OnConnectionError();

                // Connection error var in order to stop the work
                _connectionErrorException = true;
            }

        }

        private void FillNullMetar(int airportNumber)
        {
            // METAR (null)
            var metarList = new List<string> { null };
            _wxinfo.AirportMetars.Insert(airportNumber, metarList);

            // DATETIME METAR (non-existant)
            var metarUtcList = new List<DateTime> { DateTime.MinValue };
            _wxinfo.AirportMetarsUtc.Insert(airportNumber, metarUtcList);
        }

        private void FillNullTafor(int airportNumber)
        {
            // TAFORS (null)
            var taforList = new List<string> { null };
            _wxinfo.AirportTafors.Insert(airportNumber, taforList);

            // DATETIME TAFOR (non-existant)
            var taforUTC_list = new List<DateTime> { DateTime.MinValue };
            _wxinfo.AirportTaforsUtc.Insert(airportNumber, taforUTC_list);
        }

        private static string GetMetarUrl(string icaoId, int hoursBefore, bool mostRecent)
        {
            var url = "http://www.aviationweather.gov/adds/dataserver_current/httpparam?"
                            + "dataSource=metars"
                            + "&requestType=retrieve"
                            + "&format=xml"
                            + "&stationString=" + icaoId
                            + "&hoursBeforeNow=" + hoursBefore
                            + "&mostRecent=" + mostRecent;

            return url;
        }

        private static string GetMetarUrlHTTPS(string icaoId, int hoursBefore, bool mostRecent)
        {

            var url = "https://www.aviationweather.gov/adds/dataserver_current/httpparam?"
                      + "dataSource=metars"
                      + "&requestType=retrieve"
                      + "&format=xml"
                      + "&stationString=" + icaoId
                      + "&hoursBeforeNow=" + hoursBefore
                      + "&mostRecent=" + mostRecent;

            return url;
        }

        private string GetTaforUrl(string icaoId)
        {
            var url = "http://www.aviationweather.gov/adds/dataserver_current/httpparam?"
                            + "dataSource=tafs"
                            + "&requestType=retrieve"
                            + "&format=xml"
                            + "&stationString=" + icaoId
                            + "&hoursBeforeNow=" + TaforHours
                            + "&mostRecent=" + TaforLast
                            + "&timeType=issue";

            return url;
        }

        private string GetTaforUrlHTTPS(string icaoId)
        {
            var url = "https://www.aviationweather.gov/adds/dataserver_current/httpparam?"
                      + "dataSource=tafs"
                      + "&requestType=retrieve"
                      + "&format=xml"
                      + "&stationString=" + icaoId
                      + "&hoursBeforeNow=" + TaforHours
                      + "&mostRecent=" + TaforLast
                      + "&timeType=issue";

            return url;
        }

        private string GetAlternateTaforUrl(string icaoId)
        {
            var url = $"http://tgftp.nws.noaa.gov/data/forecasts/taf/stations/{icaoId}.TXT";

            return url;
        }

        private static string RetrieveHtml(string url)
        {
            // Used to build entire input
            StringBuilder sb = new StringBuilder();

            // Used on each read operation
            byte[] buf = new byte[8192];

            // Prepare the web page we will be asking for
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = 10000; // Timeout in milliseconds

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
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

        // Event raiser
        protected virtual void OnWorkStarted()
            {
                WorkRunning?.Invoke(this, new WxGetEventArgs() { Running = true });
            }

        // Event raiser
        protected virtual void OnWorkFinished()
        {
            WorkRunning?.Invoke(this, new WxGetEventArgs() { Running = false });
        }

        // Event raiser
        protected virtual void OnConnectionError()
        {
            ConnectionError?.Invoke(this, EventArgs.Empty);
        }

        // Event raiser
        protected virtual void OnPercentageCompleted(string airportId, int percentageCompleted)
        {
            PercentageCompleted?.Invoke(this, new WxGetEventArgs() { Airport = airportId, 
                PercentageCompleted = percentageCompleted });
        }

    }


    public class WxGetEventArgs : EventArgs
    {
        public bool Running { get; set; }
        public bool Error { get; private set; }

        public string Airport { get; set; }
        public int PercentageCompleted { get; set; }

    }



}