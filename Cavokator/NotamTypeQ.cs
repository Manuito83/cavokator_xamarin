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

        private string eText = String.Empty;
        public string EText { get => eText; set => eText = value; }



    }
}