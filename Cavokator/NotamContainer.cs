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
    class NotamContainer
    {
        public bool ConnectionError;

        public List<bool> NotamQ = new List<bool>();
        public List<bool> NotamD = new List<bool>();

        public List<string> NotamID = new List<string>();
        public List<string> NotamFreeText = new List<string>();
        public List<string> NotamRaw = new List<string>();
    }
}