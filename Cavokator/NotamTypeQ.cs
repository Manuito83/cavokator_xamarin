﻿using System;
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
        public string NotamID { get; set; }
        public Match qMatch  { get; set; }
    }
}