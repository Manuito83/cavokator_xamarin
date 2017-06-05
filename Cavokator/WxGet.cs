using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Cavokator
{
    public class WxGet
    {

        // Configuration
        private readonly TimeSpan _connectionTimeOutSeconds = TimeSpan.FromSeconds(15);

        // ** TAFOR CONFIGURATON (ONLY IMPLEMENTED FOR LATEST VERSION) **
        private const int TaforHours = 6;
        private const bool TaforLast = true;


        private readonly WxInfo _wxinfo = new WxInfo();

        public event EventHandler<WxGetEventArgs> WorkRunning;
        public event EventHandler ConnectionTimeOut;
        public event EventHandler ConnectionError;

        /// <summary>
        /// Returns event args Airport (string) and PercentageCompleted (int)
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
        public WxInfo Fetch(List<string> icaoIDlist, int hoursBefore, string metarOrTafor, bool mostRecent)
        {

            _metarOrTafor = metarOrTafor;

            // Call event raiser
            OnWorkStarted();


            // For each requested airport
            for (var i = 0; i < icaoIDlist.Count(); i++)
            {
                int airportNumber = i;

                if (_metarOrTafor == "only_metar")
                {
                    {
                        // Decode weather
                        var metarDecodedXml = ParseMetar(icaoIDlist[i], hoursBefore, mostRecent);

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
                        var taforDecodedXml = ParseTafor(icaoIDlist[i]);

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
                        var metarDecodedXml = ParseMetar(icaoIDlist[i], hoursBefore, mostRecent);
                        var taforDecodedXml = ParseTafor(icaoIDlist[i]);

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
                int percentageCompleted = (i+1) * 100 / icaoIDlist.Count();
                OnPercentageCompleted(icaoIDlist[i], percentageCompleted);
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
        private XDocument ParseMetar(string icaoId, int hoursBefore, bool mostRecent)
        {
            // Form METAR URL
            var metarUrl = GetMetarUrl(icaoId, hoursBefore, mostRecent);

            // Cancellation Token to set timeout of async task (http request, mainly)
            CancellationTokenSource cts = new CancellationTokenSource(_connectionTimeOutSeconds);

            // Get async data and pass cancellation token
            var metarRawData = Task.Run(async () => await GetRawData(metarUrl, cts.Token));

            try
            {
                metarRawData.Wait(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Call event raiser
                OnConnectionTimeOut();
                
                // Connection error var in order to stop the work
                _connectionErrorException = true;
                return null;
            }
                
            var metarXmLraw = metarRawData.Result;

            if (metarXmLraw == string.Empty)
            {
                // Connection error var in order to stop the work
                _connectionErrorException = true;
                return null;
            }
            else
            {
                // Parse xml string
                var parsedMetarXml = XDocument.Parse(metarXmLraw);
                return parsedMetarXml;
            }

        }


        /// <summary>
        /// Gets Tafor from previously formed URL based on AviationWeather.gov
        /// Passes Cancelation Token to async task where HttpRequest is
        /// Parses and returns XML received
        /// </summary>
        /// <param name="icaoId"></param>
        /// <returns></returns>
        private XDocument ParseTafor(string icaoId)
        {
            // Form TAFOR URL
            string taforUrl = GetTaforUrl(icaoId);

            // Cancellation Token to set timeout of async task (http request, mainly)
            CancellationTokenSource cts = new CancellationTokenSource(_connectionTimeOutSeconds);

            // Get async data and pass cancellation token
            Task<string> taforRawData = Task.Run(async () => await GetRawData(taforUrl, cts.Token));

            try
            {
                taforRawData.Wait(cts.Token);
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

            // Parse xml string
            XDocument parsedTaforXml = XDocument.Parse(taforXmlRaw);

            return parsedTaforXml;
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

                if (_metarOrTafor == "only_metar")
                {
                    // Iterate each airport that we find "station_id" and show on TextView
                    for (var i = 0; i < airport.Count(); i++)
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


                        // TAFORS (null)
                        var taforList = new List<string> {null};
                        _wxinfo.AirportTafors.Insert(airportNumber, taforList);

                    
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


                        // DATETIME TAFOR (non-existant)
                        var taforUTC_list = new List<DateTime> {DateTime.MinValue};
                        _wxinfo.AirportTaforsUtc.Insert(airportNumber, taforUTC_list);


                    }

                }
                else if (_metarOrTafor == "only_tafor")
                {
                    // Iterate each airport that we find "station_id" and show on TextView
                    for (var i = 0; i < airport.Count(); i++)
                    {

                        // Group its own weather information (TAFOR)
                        List<XElement> taforsGroup = (from n in taforParsedXml.Descendants("TAF")
                                            where n.Element("station_id").Value == airport[i].Key
                                            select n.Element("raw_text")).ToList();


                        // METAR (null)
                        var metarList = new List<string> {null};
                        _wxinfo.AirportMetars.Insert(airportNumber, metarList);

                    
                        // TAFORS
                        var taforList = new List<string>();
                        for (var j = 0; j < taforsGroup.Count(); j++)
                        {
                            taforList.Add(taforsGroup[j].Value);
                        }
                        _wxinfo.AirportTafors.Insert(airportNumber, taforList);



                        // DATETIME METAR (non-existant)
                        var metarUtcList = new List<DateTime> {DateTime.MinValue};
                        _wxinfo.AirportMetarsUtc.Insert(airportNumber, metarUtcList);



                        // DATETIME TAFOR
                        var taforUtcList = new List<DateTime>();
                        List<XElement> taforUtcGroup = (from n in taforParsedXml.Descendants("TAF")
                                              where n.Element("station_id").Value == airport[i].Key
                                              select n.Element("issue_time")).ToList();


                        if (taforUtcGroup.Count() == 0)
                        {
                            taforUtcList.Add(DateTime.MinValue);
                        }
                        else
                        {
                            for (var j = 0; j < taforUtcGroup.Count(); j++)
                            {
                                taforUtcList.Add(Convert.ToDateTime(taforUtcGroup[j].Value).ToUniversalTime());
                            }

                        }

                        _wxinfo.AirportTaforsUtc.Insert(airportNumber, taforUtcList);


                    }
                }
                else if (_metarOrTafor == "metar_and_tafor")
                {
                    // Iterate each airport that we find "station_id" and show on TextView
                    for (var i = 0; i < airport.Count(); i++)
                    {

                        // Group its own weather information (METAR)
                        List<XElement> metarsGroup = (from n in metarParsedXml.Descendants("METAR")
                                            where n.Element("station_id").Value == airport[i].Key
                                            select n.Element("raw_text")).ToList();


                        // Group its own weather information (TAFOR)
                        List<XElement> taforsGroup = (from n in taforParsedXml.Descendants("TAF")
                                            where n.Element("station_id").Value == airport[i].Key
                                            select n.Element("raw_text")).ToList();


                        // METARS
                        var metarList = new List<string>();
                        for (var j = 0; j < metarsGroup.Count(); j++)
                        {
                            metarList.Add(metarsGroup[j].Value);
                        }
                        _wxinfo.AirportMetars.Insert(airportNumber, metarList);


                        // TAFORS
                        var taforList = new List<string>();
                        for (var j = 0; j < taforsGroup.Count(); j++)
                        {
                            taforList.Add(taforsGroup[j].Value);
                        }
                        _wxinfo.AirportTafors.Insert(airportNumber, taforList);



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



                        // DATETIME TAFOR
                        var taforUtcList = new List<DateTime>();
                        List<XElement> taforUtcGroup = (from n in taforParsedXml.Descendants("TAF")
                                              where n.Element("station_id").Value == airport[i].Key
                                              select n.Element("issue_time")).ToList();


                        if (taforUtcGroup.Count() == 0)
                        {
                            taforUtcList.Add(DateTime.MinValue);
                        }
                        else
                        {
                            for (var j = 0; j < taforUtcGroup.Count(); j++)
                            {
                                taforUtcList.Add(Convert.ToDateTime(taforUtcGroup[j].Value).ToUniversalTime());
                            }

                        }

                        _wxinfo.AirportTaforsUtc.Insert(airportNumber, taforUtcList);
                    }
                }
            }
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
            
            // HttpClient + Timeout property
            var httpClient = new HttpClient();

            try
            {
                var response = await httpClient.GetAsync(url, token);
                content = await response.Content.ReadAsStringAsync();
                return content;
            }
            catch (HttpRequestException)
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
            WorkRunning?.Invoke(this, new WxGetEventArgs(_connectionErrorException) { Running = false });
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

        public WxGetEventArgs()
        {

        }

        public WxGetEventArgs(bool error)
        {
            Error = error;
        }

    }


}