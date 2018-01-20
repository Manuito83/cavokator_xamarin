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

namespace Cavokator
{
    static class AirportDefinitions
    {
        /// <summary>
        /// Information from Airport's CVS file
        /// </summary>
        public static List<AirportCsvDefinition> _myAirportDefinitions = new List<AirportCsvDefinition>();

        static AirportDefinitions()
        {
            // Object to store List downloaded at OnCreate from a CAV file with IATA, ICAO and Airport Names
            AirportConverter iataConverter = new AirportConverter();
            _myAirportDefinitions = iataConverter.GetCodeList();
        }
    }
}