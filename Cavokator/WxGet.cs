using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Cavokator
{

    public class WxGet
    {

        // Configuration
        private readonly int _connectionTimeOutSeconds = 10;

        // ** TAFOR CONFIGURATION (ONLY IMPLEMENTED FOR LATEST VERSION) **
        private const int TaforHours = 24;
        private const bool TaforLast = true;


        private readonly WxInfoContainer _wxinfo = new WxInfoContainer();

        public event EventHandler<WxGetEventArgs> WorkRunning;
        public event EventHandler ConnectionTimeOut;
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
        public WxInfoContainer Fetch(List<string> icaoIDlist, int hoursBefore, string metarOrTafor, bool mostRecent)
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
            // Form METAR URL
            var metarUrl = GetMetarUrl(icaoId, hoursBefore, mostRecent);
            
            // Cancellation Token to set timeout of async task (http request, mainly)
            var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(_connectionTimeOutSeconds));
            var token = tokenSource.Token;

            // Get async data and pass cancellation token
            var metarRawData = Task.Run(async () => await GetRawData(metarUrl, token), token);

            try
            {
                metarRawData.Wait(token);
            }
            catch
            {
                // Call event raiser
                OnConnectionTimeOut();
                
                // Connection error var in order to stop the work
                _connectionErrorException = true;
                return null;
            }
                
            var metarXmlRaw = metarRawData.Result;

            if (metarXmlRaw == string.Empty)
            {
                // Connection error var in order to stop the work
                _connectionErrorException = true;
                return null;
            }
            else
            {
                // Parse xml string
                try
                {
                    var parsedMetarXml = XDocument.Parse(metarXmlRaw);
                    return parsedMetarXml;
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
            // Form TAFOR URL
            string taforUrl = GetTaforUrl(icaoId);

            // Cancellation Token to set timeout of async task (http request, mainly)
            var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(_connectionTimeOutSeconds));
            var token = tokenSource.Token;

            // Get async data and pass cancellation token
            Task<string> taforRawData = Task.Run(async () => await GetRawData(taforUrl, token), token);

            try
            {
                taforRawData.Wait(token);
            }
            catch (OperationCanceledException)
            {
                // Call event raiser
                OnConnectionTimeOut();

                // Connection error var in order to stop the work
                _connectionErrorException = true;
                return null;
            }
            
            var taforXmlRaw = taforRawData.Result;
            
            if (taforXmlRaw == string.Empty)
            {
                // Connection error var in order to stop the work
                _connectionErrorException = true;
                return null;
            }
            else
            {
                // Parse xml string
                try
                {
                    var parsedTaforXml = XDocument.Parse(taforXmlRaw);
                    return parsedTaforXml;
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


            // If there is no airpor_group, we have no airport, ID is incorrect and we show an error
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
                        FillValidTafor(airportNumber, taforParsedXml, airport, i);
                    }
                    else if (_metarOrTafor == "metar_and_tafor")
                    {
                        FillValidMetar(airportNumber, metarParsedXml, airport, i);
                        FillValidTafor(airportNumber, taforParsedXml, airport, i);
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

        private void FillValidTafor(int airportNumber, XContainer taforParsedXml, List<IGrouping<string, XElement>> airport, int i)
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


            // If the TAFOR could not be found, we'll try an alternate way
            if (taforsGroup.Count() == 0)  // TODO: (NEW TAFOR) add condition in case METAR was not found
            {
                string alternateTaforString = String.Empty;
                DateTime alternateTaforUtc = DateTime.MinValue;
                    
                TryToGetAlternateTaforString(ref alternateTaforString, ref alternateTaforUtc);

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
                    
                
                // TODO: (NEW TAFOR) TRY WITH "DSS"/"GOBD"!
                // TODO: (NEW TAFOR) RETHINK HOW TO CAPTURE TIME AS WELL
                
            }


            // Fill the information in the WxInfoContainer class
            _wxinfo.AirportTaforsUtc.Insert(airportNumber, taforUtcList);
            _wxinfo.AirportTafors.Insert(airportNumber, taforList);

        }

        private void TryToGetAlternateTaforString(ref string inputString, ref DateTime inputDateTime)
        {
            // Form TAFOR URL
            string taforUrl = "http://tgftp.nws.noaa.gov/data/forecasts/taf/stations/GOBD.TXT";

            // Cancellation Token to set timeout of async task (http request, mainly)
            var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(_connectionTimeOutSeconds));
            var token = tokenSource.Token;

            // Get async data and pass cancellation token
            Task<string> taforRawData = Task.Run(async () => await GetRawData(taforUrl, token), token);

            try
            {
                taforRawData.Wait(token);

                string[] alternateTaforRaw = taforRawData.Result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                // Split and get the string
                string alternateTaforString = alternateTaforRaw[1];

                // TODO: Split and get the time
                string utcRaw = String.Empty;
                
                var utcRegex = new Regex(@"(\b)+[0-3][0-9][0-2][0-9][0-5][0-9]Z+(?=\b)");
                var utcMatches = utcRegex.Matches(alternateTaforString);
                foreach (var match in utcMatches.Cast<Match>())
                    utcRaw = alternateTaforString.Substring(match.Index, 6);

                inputDateTime = DateTime.ParseExact(utcRaw, "ddHHmm", null);

                Console.WriteLine("\n\t******* DATETIME INIT ********: " + inputDateTime + "\n\n");

                var monthDiff = DateTime.UtcNow.Month - inputDateTime.Month;

                if (inputDateTime.Day > DateTime.UtcNow.Day)
                    inputDateTime = inputDateTime.AddMonths(monthDiff - 1);
                else
                    inputDateTime = inputDateTime.AddMonths(monthDiff);

                Console.WriteLine("\n\t******* DATETIME END ********: " + inputDateTime + "\n\n");





                
                                
                // Return variables by reference
                //inputDateTime = DateTime.MinValue; // TODO: Change to pass UTC
                inputString = alternateTaforString;
            }
            catch (OperationCanceledException)
            {
                // Call event raiser
                OnConnectionTimeOut();

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

        

        /// <summary>
        /// Returns string with Metar URL
        /// </summary>
        /// <param name="icaoId"></param>
        /// <param name="hoursBefore"></param>
        /// <param name="mostRecent"></param>
        /// <returns></returns>
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


        /// <summary>
        /// Returns string with Tafor URL
        /// </summary>
        /// <param name="icaoId"></param>
        /// <returns></returns>
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


        /// <summary>
        /// Async Task to get data information from website
        /// </summary>
        /// <param name="url"></param>
        /// <param name="token">Cancelation Token</param>
        /// <returns></returns>
        private async Task<string> GetRawData(string url, CancellationToken token)
        {

            string content;
            
            // HttpClient
            var httpClient = new HttpClient {Timeout = TimeSpan.FromSeconds(10)};

            try
            {
                var response = await httpClient.GetAsync(url, token);
                content = await response.Content.ReadAsStringAsync();
                return content;
            }
            catch
            {
                // Call event raiser
                OnConnectionError();
                content = "";
                return content;
                
            }
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
        protected virtual void OnConnectionTimeOut()
        {
            ConnectionTimeOut?.Invoke(this, EventArgs.Empty);
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