using System.IO;
using System.Linq;
using Android.App;
using CsvHelper;
using System.Collections.Generic;

namespace Cavokator
{
    public class AirportCsvDefinition
    {
        // CAUTION: Field names match those in the CSV
        // DO NOT change to standard namming
        public string description { get; set; }
        public string icao { get; set; }
        public string iata { get; set; }
    }

    
    /// <summary>
    /// Returns ICAO code and/or airport description. 
    /// </summary>
    public class AirportConverter
    {


        /// <summary>
        /// Returns list with all codes present in CSV
        /// </summary>
        public List<AirportCsvDefinition> GetCodeList()
        {
            var records = new List<AirportCsvDefinition>();

            var assets = Application.Context.Assets;
            var sr = new StreamReader(assets.Open("airport_codes.csv"));
            var csv = new CsvReader(sr);
            csv.Configuration.Delimiter = ";";

            records = csv.GetRecords<AirportCsvDefinition>().ToList();

            //while (csv.Read())
            //{
            //    records = csv.GetRecords<AirportCsvDefinition>().ToList();
            //}

            return records;
        }




        /// <summary>
        /// Returns ICAO code when provided with a valid IATA code
        /// </summary>
        /// <param name="requestedIata">The IATA code you want to transform to ICAO</param>
        /// <returns></returns>
        public string GetIcao(string requestedIata)
        {
            if (requestedIata != null)
            {
                requestedIata = requestedIata.ToUpper();

                var assets = Application.Context.Assets;
                var sr = new StreamReader(assets.Open("airport_codes.csv"));
                var csv = new CsvReader(sr);
                csv.Configuration.Delimiter = ";";

                while (csv.Read())
                {
                    var record = csv.GetRecord<AirportCsvDefinition>();

                    if (record.iata == requestedIata)
                    {
                        return record.icao;

                    }
                }
            }

            return null;
        }


        /// <summary>
        /// Returns airport name when provided with a valid ICAO code
        /// </summary>
        /// <param name="requestedIcao">The ICAO airport you want to get the name from</param>
        /// <returns></returns>
        public string GetDescription(string requestedIcao)
        {
            if (requestedIcao != null)
            {
                requestedIcao = requestedIcao.ToUpper();

                var assets = Application.Context.Assets;
                var sr = new StreamReader(assets.Open("airport_codes.csv"));
                var csv = new CsvReader(sr);
                csv.Configuration.Delimiter = ";";

                while (csv.Read())
                {
                    var record = csv.GetRecord<AirportCsvDefinition>();

                    if (record.icao == requestedIcao)
                    {
                        return record.description;
                    }
                }
            }

            return null;
        }
    }
}