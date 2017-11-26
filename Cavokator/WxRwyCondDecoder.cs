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
        private string _rwyConditionText;
        
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


        /// <summary>
        /// Input runway condition string of any of this types: 
        /// (Type 1 for RXXL/123456)
        /// (Type 2 for RXX/123456)
        /// (Type 3 for 88123456)
        /// (Type 4 for R/SNOCLO)
        /// (Type 5 for RXXL/CLRD//)
        /// (Type 6 for RXX/CLRD//)
        /// </summary>
        /// <param name="input_condition"></param>
        /// <returns></returns>
        public WxRwyCondition DecodeCondition(string input_condition)
        {

            _rwyConditionText = input_condition;

            // Find out what it is about
            DiscernType();


            if (_MainError)
            {
                _wxRunwayCondition.MainError = true;
            }
            else
            {
                // TODO: IMPLEMENT ALL!
                switch (_ConditionType)
                {

                    // (Type 1 for RXXL/123456)
                    case 1:
                        
                        // RUNWAY CODE
                        try
                        {
                            int.TryParse(_rwyConditionText.Substring(1, 2), out int intRunway);
                            if (intRunway <= 36)
                            {
                                _wxRunwayCondition.RwyCode = _rwyConditionText.Substring(0, 4);
                                _wxRunwayCondition.RwyValue = _rwyConditionText.Substring(1, 3);
                                _wxRunwayCondition.RwyInt = intRunway;
                            }
                            else
                            {
                                _wxRunwayCondition.RwyCode = _rwyConditionText.Substring(0, 4);
                                _wxRunwayCondition.RwyError = true;
                            }
                        }
                        catch
                        {
                            _wxRunwayCondition.RwyCode = _rwyConditionText.Substring(0, 4);
                            _wxRunwayCondition.RwyError = true;
                        }

                        // DEPOSIT TYPE
                        try
                        {
                            if (_rwyConditionText.Substring(5, 1) == "/")
                            {
                                _wxRunwayCondition.DepositCode = "/";
                            }
                            else
                            {
                                _wxRunwayCondition.DepositCode = _rwyConditionText.Substring(5, 1);

                                if (!int.TryParse(_rwyConditionText.Substring(5, 1), out int intDeposits))
                                {
                                    _wxRunwayCondition.DepositError = true;
                                }
                            }
                        }
                        catch
                        {
                            _wxRunwayCondition.DepositCode = _rwyConditionText.Substring(5, 1);
                            _wxRunwayCondition.DepositError = true;
                        }

                        // EXTENT TYPE
                        try
                        {
                            if (_rwyConditionText.Substring(6, 1) == "/")
                            {
                                _wxRunwayCondition.ExtentCode = "/";
                            }
                            else
                            {
                                if (int.TryParse(_rwyConditionText.Substring(6, 1), out int intExtent))
                                {

                                    _wxRunwayCondition.ExtentCode = _rwyConditionText.Substring(6, 1);

                                    if (!(intExtent == 1 || intExtent == 2 || intExtent == 5 || intExtent == 9))
                                    {
                                        _wxRunwayCondition.ExtentError = true;
                                    }
                                }
                                else
                                {
                                    _wxRunwayCondition.ExtentError = true;
                                }
                            }
                        }
                        catch
                        {
                            _wxRunwayCondition.ExtentCode = _rwyConditionText.Substring(6, 1);
                            _wxRunwayCondition.ExtentError = true;
                        }
                        


                        // CONTAMINATION DEPTH
                        try
                        {
                            if (_rwyConditionText.Substring(7, 2) == "/")
                            {
                                _wxRunwayCondition.DepthCode = "/";
                            }
                            else
                            {
                                if (int.TryParse(_rwyConditionText.Substring(7, 2), out int intDepth))
                                {

                                    _wxRunwayCondition.DepthCode = _rwyConditionText.Substring(7, 2);

                                    if (intDepth == 91)
                                    {
                                        _wxRunwayCondition.ExtentError = true;
                                    }
                                }
                                else
                                {
                                    _wxRunwayCondition.ExtentError = true;
                                }
                            }
                        }
                        catch
                        {
                            _wxRunwayCondition.DepthCode = _rwyConditionText.Substring(7, 2);
                            _wxRunwayCondition.DepthError = true;
                        }





                        break;



                    // (Type 2 for RXX/123456)
                    case 2:
                        
                        // RUNWAY CODE
                        try
                        {
                            int.TryParse(_rwyConditionText.Substring(1, 2), out int intRunway);
                            if (intRunway <= 36 || intRunway == 88 || intRunway == 99)
                            {
                                _wxRunwayCondition.RwyCode = _rwyConditionText.Substring(0, 3);
                                _wxRunwayCondition.RwyValue = _rwyConditionText.Substring(1, 2);
                                _wxRunwayCondition.RwyInt = intRunway;
                            }
                            else
                            {
                                _wxRunwayCondition.RwyCode = _rwyConditionText.Substring(0, 3);
                                _wxRunwayCondition.RwyError = true;
                            }
                        }
                        catch
                        {
                            _wxRunwayCondition.RwyCode = _rwyConditionText.Substring(0, 3);
                            _wxRunwayCondition.RwyError = true;
                        }

                        
                        // DEPOSIT TYPE
                        try
                        {
                            if (_rwyConditionText.Substring(4, 1) == "/")
                            {
                                _wxRunwayCondition.DepositCode = "/";
                            }
                            else
                            {
                                _wxRunwayCondition.DepositCode = _rwyConditionText.Substring(4, 1);

                                if (!int.TryParse(_rwyConditionText.Substring(4, 1), out int intDeposits))
                                {
                                    _wxRunwayCondition.DepositError = true;
                                }
                            }
                        }
                        catch
                        {
                            _wxRunwayCondition.DepositCode = _rwyConditionText.Substring(4, 1);
                            _wxRunwayCondition.DepositError = true;
                        }

                        
                        // EXTENT TYPE
                        try
                        {
                            if (_rwyConditionText.Substring(5, 1) == "/")
                            {
                                _wxRunwayCondition.ExtentCode = "/";
                            }
                            else
                            {
                                if (int.TryParse(_rwyConditionText.Substring(5, 1), out int intExtent))
                                {

                                    _wxRunwayCondition.ExtentCode = _rwyConditionText.Substring(5, 1);

                                    if (!(intExtent == 1 || intExtent == 2 || intExtent == 5 || intExtent == 9))
                                    {
                                        _wxRunwayCondition.ExtentError = true;
                                    }
                                }
                                else
                                {
                                    _wxRunwayCondition.ExtentError = true;
                                }
                            }
                        }
                        catch
                        {
                            _wxRunwayCondition.ExtentCode = _rwyConditionText.Substring(5, 1);
                            _wxRunwayCondition.ExtentError = true;
                        }



                        // CONTAMINATION DEPTH
                        try
                        {
                            if (_rwyConditionText.Substring(6, 2) == "//")
                            {
                                _wxRunwayCondition.DepthCode = "//";
                            }
                            else
                            {
                                if (int.TryParse(_rwyConditionText.Substring(6, 2), out int intDepth))
                                {

                                    _wxRunwayCondition.DepthCode = _rwyConditionText.Substring(6, 2);

                                    if (intDepth == 91)
                                    {
                                        _wxRunwayCondition.ExtentError = true;
                                    }
                                }
                                else
                                {
                                    _wxRunwayCondition.ExtentError = true;
                                }
                            }
                        }
                        catch
                        {
                            _wxRunwayCondition.DepthCode = _rwyConditionText.Substring(6, 2);
                            _wxRunwayCondition.DepthError = true;
                        }


                        break;

                }
            }


            return _wxRunwayCondition;
        }



        private void DiscernType()
        {
            // Check if the string length is correct first
            if (_rwyConditionText.Length > 11 && _rwyConditionText.Length < 8)
            {
                _MainError = true;
            }

            if (!_MainError)
            {
                try
                {
                    // TYPE 1: R12L/123456
                    if ((_rwyConditionText.Substring(0, 1) == "R") &&
                        (Regex.IsMatch(_rwyConditionText.Substring(1, 1), @"\d")) &&
                        (Regex.IsMatch(_rwyConditionText.Substring(2, 1), @"\d")) &&
                        (Regex.IsMatch(_rwyConditionText.Substring(3, 1), @"[L|R|C]")) &&
                        (_rwyConditionText.Substring(4, 1) == "/") &&
                        (Regex.IsMatch(_rwyConditionText.Substring(5, 6), @"(([0-9]|\/){6})")) &&
                        (_rwyConditionText.Length == 11))
                    {
                        _ConditionType = 1;
                    }
                    // TYPE 2: R12/123456
                    else if ((_rwyConditionText.Substring(0, 1) == "R") &&
                        (Regex.IsMatch(_rwyConditionText.Substring(1, 2), @"\d")) &&
                        (_rwyConditionText.Substring(3, 1) == "/") &&
                        (Regex.IsMatch(_rwyConditionText.Substring(4, 6), @"(([0-9]|\/){6})")) &&
                        (_rwyConditionText.Length == 10))
                    {
                        _ConditionType = 2;
                    }
                    // TYPE 3: 88123456
                    else if (Regex.IsMatch(_rwyConditionText, @"(\b)+(([0-9]|\/){8})+(\b)"))
                    {
                        _ConditionType = 3;
                    }
                    // TYPE 4: R/SNOCLO
                    else if (Regex.IsMatch(_rwyConditionText, @"(\b)+(R\/SNOCLO)+(\b)"))
                    {
                        _ConditionType = 4;
                    }
                    // TYPE 5: R14L/CLRD//
                    else if ((_rwyConditionText.Substring(0, 1) == "R") &&
                        (Regex.IsMatch(_rwyConditionText.Substring(1, 1), @"\d")) &&
                        (Regex.IsMatch(_rwyConditionText.Substring(2, 1), @"\d")) &&
                        (Regex.IsMatch(_rwyConditionText.Substring(3, 1), @"[L|R|C]")) &&
                        (_rwyConditionText.Substring(4, 1) == "/") &&
                        (Regex.IsMatch(_rwyConditionText.Substring(5, 6), @"(CLRD)+(\/\/)")) &&
                        (_rwyConditionText.Length == 11))
                    {
                        _ConditionType = 5;
                    }
                    // TYPE 6: R14/CLRD//
                    else if ((_rwyConditionText.Substring(0, 1) == "R") &&
                        (Regex.IsMatch(_rwyConditionText.Substring(1, 2), @"\d")) &&
                        (_rwyConditionText.Substring(3, 1) == "/") &&
                        (Regex.IsMatch(_rwyConditionText.Substring(4, 6), @"(CLRD)+(\/\/)")) &&
                        (_rwyConditionText.Length == 10))
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



    }
}