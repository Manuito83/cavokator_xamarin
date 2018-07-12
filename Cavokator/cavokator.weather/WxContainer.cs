//
// CAVOKATOR APP
// Website: https://github.com/Manuito83/Cavokator
// License GNU General Public License v3.0
// Manuel Ortega, 2018
//

using System;
using System.Collections.Generic;

namespace Cavokator
{
    public class WxContainer
    {
        public List<bool> AirportErrors { get; set; } = new List<bool>();
        public List<string> AirportIDs { get; set; } = new List<string>();
        public List<List<string>> AirportMetars { get; set; } = new List<List<string>>();
        public List<List<string>> AirportTafors { get; set; } = new List<List<string>>();
        public List<List<DateTime>> AirportMetarsUtc { get; set; } = new List<List<DateTime>>();
        public List<List<DateTime>> AirportTaforsUtc { get; set; } = new List<List<DateTime>>();
    }
}