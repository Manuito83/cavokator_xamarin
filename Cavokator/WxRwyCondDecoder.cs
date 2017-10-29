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
using System.Text.RegularExpressions;

namespace Cavokator
{
    class WxRwyCondDecoder
    {
        // String used for main decoding
        private string _InputCondition;

        // Deconding variables that will be passed
        private bool _MainError;

        // Type 1 for RXXL/123456
        // Type 2 for RXX/123456
        // Type 3 for 88123456
        // Type 4 for R/SNOCLO
        // Type 5 for RXXL/CLRD//
        // Type 6 for RXX/CLRD//
        private int _ConditionType;


        // We will use a WxRwyCondition container
        private WxRwyCondition _wxRunwayCondition = new WxRwyCondition();


        public WxRwyCondDecoder(string input_code)
        {
            // Assign private variable
            _InputCondition = input_code;

            // Start decoding process
            DiscernType();

            DecodeCondition();
        }
        

        private void DiscernType ()
        {
            // Check if the string length is correct first
            if (_InputCondition.Length > 11 && _InputCondition.Length < 8)
            {
                _MainError = true;
            }

            if (!_MainError)
            {
                try
                {
                    // TYPE 1: R12L/123456
                    if ((_InputCondition.Substring(0, 1) == "R") && 
                        (Regex.IsMatch(_InputCondition.Substring(1, 1), @"\d")) &&
                        (Regex.IsMatch(_InputCondition.Substring(2, 1), @"\d")) &&
                        (Regex.IsMatch(_InputCondition.Substring(3, 1), @"[L|R|C]")) &&
                        (_InputCondition.Substring(4, 1) == "/") &&
                        (Regex.IsMatch(_InputCondition.Substring(5, 6), @"(([0-9]|\/){6})")) &&
                        (_InputCondition.Length == 11))
                    {
                        _ConditionType = 1;
                    }
                    // TYPE 2: R12/123456
                    else if ((_InputCondition.Substring(0, 1) == "R") &&
                        (Regex.IsMatch(_InputCondition.Substring(1, 2), @"\d")) &&
                        (_InputCondition.Substring(3, 1) == "/") &&
                        (Regex.IsMatch(_InputCondition.Substring(4, 6), @"(([0-9]|\/){6})")) &&
                        (_InputCondition.Length == 10))
                    {
                        _ConditionType = 2;
                    }
                    // TYPE 3: 88123456
                    else if (Regex.IsMatch(_InputCondition, @"(\b)+(([0-9]|\/){8})+(\b)"))
                    {
                        _ConditionType = 3;
                    }
                    // TYPE 4: R/SNOCLO
                    else if (Regex.IsMatch(_InputCondition, @"(\b)+(R\/SNOCLO)+(\b)"))
                    {
                        _ConditionType = 4;
                    }
                    // TYPE 5: R14L/CLRD//
                    else if ((_InputCondition.Substring(0, 1) == "R") &&
                        (Regex.IsMatch(_InputCondition.Substring(1, 1), @"\d")) &&
                        (Regex.IsMatch(_InputCondition.Substring(2, 1), @"\d")) &&
                        (Regex.IsMatch(_InputCondition.Substring(3, 1), @"[L|R|C]")) &&
                        (_InputCondition.Substring(4, 1) == "/") &&
                        (Regex.IsMatch(_InputCondition.Substring(5, 6), @"(CLRD)+(\/\/)")) &&
                        (_InputCondition.Length == 11))
                    {
                        _ConditionType = 5;
                    }
                    // TYPE 6: R14/CLRD//
                    else if ((_InputCondition.Substring(0, 1) == "R") &&
                        (Regex.IsMatch(_InputCondition.Substring(1, 2), @"\d")) &&
                        (_InputCondition.Substring(3, 1) == "/") &&
                        (Regex.IsMatch(_InputCondition.Substring(4, 6), @"(CLRD)+(\/\/)")) &&
                        (_InputCondition.Length == 10))
                    {
                        _ConditionType = 6;
                    }
                    else
                    {
                        _MainError = true;
                    }

                }
                catch
                {
                    _MainError = true;
                }
            }
        }


        public WxRwyCondition DecodeCondition()
        {
            switch (_ConditionType)
            {
                case 1:

                    int intRunway;
                    int.TryParse(_InputCondition.Substring(1, 2), out intRunway);

                    if (intRunway <= 36)
                    {
                        _wxRunwayCondition.RwyCode = _InputCondition.Substring(0, 4);
                        _wxRunwayCondition.RwyText = Resource.String.Runway_Indicator + _InputCondition.Substring(1, 3);

                        // TODO
                        Console.WriteLine(_wxRunwayCondition.RwyCode + ": " + _wxRunwayCondition.RwyText);

                    }
                    else
                    {
                        _wxRunwayCondition.RwyError = true;
                    }
                    
                    
                    break;
            }


            return _wxRunwayCondition;
        }

    
    }
}