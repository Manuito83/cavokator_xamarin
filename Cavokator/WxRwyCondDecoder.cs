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
    class WxRwyCondDecoder
    {
        
        private string _RwyCondition;


        public WxRwyCondDecoder(string input_code)
        {
            _RwyCondition = input_code;
        }
    }
}