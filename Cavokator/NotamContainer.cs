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
    class NotamContainer
    {
        public bool ConnectionError;

        public readonly List<bool> NotamQ = new List<bool>();
        public readonly List<bool> NotamD = new List<bool>();

        public readonly List<string> NotamId = new List<string>();

        public readonly List<string> CodeSecondThird = new List<string>();
        public readonly List<string> CodeFourthFifth = new List<string>();

        public readonly List<float> Latitude = new List<float>();
        public readonly List<float> Longitude = new List<float>();
        public readonly List<int> Radius = new List<int>();

        public readonly List<string> NotamFreeText = new List<string>();

        public readonly List<DateTime> StartTime = new List<DateTime>();
        public readonly List<DateTime> EndTime = new List<DateTime>();
        public readonly List<bool> CEstimated = new List<bool>();
        public readonly List<bool> CPermanent = new List<bool>();
        public readonly List<string> Span = new List<string>();

        public readonly List<string> BottomLimit = new List<string>();
        public readonly List<string> TopLimit = new List<string>();

        public readonly List<string> NotamRaw = new List<string>();


    }
}