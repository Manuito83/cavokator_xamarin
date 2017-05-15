using System.IO;
using Android.App;
using CsvHelper;

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
    /// Takes IATA and/or ICAO code.
    /// If IATA is given, converts to ICAO as well.
    /// </summary>
    public class AirportConverter
    {
        private string Iata { get; }
        public string Icao { get; private set; }
        public string Description { get; private set; }


        /// <summary>
        /// Fill properties with ICAO code and Description.
        /// </summary>
        /// <param name="icao">ICAO code in order to get airport description.</param>
        /// <param name="iata">IATA code to get ICAO + airport description.</param>
        public AirportConverter(string icao, string iata)
        {

            if (icao != null)
            {
                Icao = icao.ToUpper();
                GetDescription(Icao);
            }

            if (iata != null)
            {
                Iata = iata.ToUpper();
                GetIcao(Iata);
                GetDescription(Icao);
            }


        }


        private void GetIcao(string requestedIata)
        {
            var assets = Application.Context.Assets;
            var sr = new StreamReader(assets.Open("airport_codes.csv"));
            var csv = new CsvReader(sr);
            csv.Configuration.Delimiter = ";";

            while (csv.Read())
            {
                var record = csv.GetRecord<AirportCsvDefinition>();

                if (record.iata == requestedIata)
                {
                    Icao = record.icao;
                    break;
                }
                Icao = null;
            }
        }


        private void GetDescription(string requestedIcao)
        {
            var assets = Application.Context.Assets;
            var sr = new StreamReader(assets.Open("airport_codes.csv"));
            var csv = new CsvReader(sr);
            csv.Configuration.Delimiter = ";";

            while (csv.Read())
            {
                var record = csv.GetRecord<AirportCsvDefinition>();

                if (record.icao == requestedIcao)
                {
                    Description = record.description;
                    break;
                }
                Description = null;
            }
        }
    }
}