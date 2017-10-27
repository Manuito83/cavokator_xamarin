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

        private bool _MainError;

        public WxRwyCondDecoder(string input_code)
        {
            // Assign private variable
            _RwyCondition = input_code;

            // Start decoding process
            DiscernType(_RwyCondition);
        }


        void DiscernType (string input_text)
        {
            // Check if the string length is correct first
            if (input_text.Length > 11 && input_text.Length < 8)
            {
                _MainError = true;
            }

            if (!_MainError)
            {
                try
                {
                    // NEW TYPE
                    if ((input_text.Substring(0 , 1) == "R") && 
                        ((input_text.Substring(20, 1) == "/") || (input_text.Substring(4, 1) == "/")))
                    {
                        Console.WriteLine("YES: " + input_text);
                    }
                }
                catch
                {
                    _MainError = true;
                }
                





            }


        }


    }
}