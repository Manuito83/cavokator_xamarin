//
// CAVOKATOR APP
// Website: https://github.com/Manuito83/Cavokator
// License GNU General Public License v3.0
// Manuel Ortega, 2018
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Cavokator
{
    internal class NotamTypeQ
    {
        

        public Match QMatch { get; set; } = Match.Empty;

        public string NotamId { get; set; } = String.Empty;

        

        public string EText { get; set; } = String.Empty;

        public DateTime StartTime { get; set; } = DateTime.MinValue;
        public DateTime EndTime { get; set; } = DateTime.MinValue;
        public bool CEstimated { get; set; }
        public bool CPermanent { get; set; }

        public string SpanTime { get; set; } = String.Empty;

        public string BottomLimit { get; set; } = String.Empty;
        public string TopLimit { get; set; } = String.Empty;
    }
}