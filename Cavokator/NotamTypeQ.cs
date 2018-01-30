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
        private string notamID = String.Empty;
        public string NotamID { get => notamID; set => notamID = value; }

        private Match qMatch = Match.Empty;
        public Match QMatch { get => qMatch; set => qMatch = value; }

        private DateTime startTime = DateTime.MinValue;
        public DateTime StartTime { get => startTime; set => startTime = value; }

        private DateTime endTime = DateTime.MinValue;
        public DateTime EndTime { get => endTime; set => endTime = value; }

        private bool cEstimated = false;
        public bool CEstimated { get => cEstimated; set => cEstimated = value; }

        private bool cPermanent = false;
        public bool CPermanent { get => cPermanent; set => cPermanent = value; }

        private string eText = String.Empty;
        public string EText { get => eText; set => eText = value; }
        
    }
}