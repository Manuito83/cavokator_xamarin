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
    class ConditionDecoder
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
        private ConditionContainer _wxRunwayCondition = new ConditionContainer();


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
        public ConditionContainer DecodeCondition(string input_condition)
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

                // To avoid excess clutter, we will analyse the RUNWAY for each type (as the numbers of characters change). 
                // But then, for the other fields, we will just add a correction to identify the right number.

                int position0 = 0;
                int position1 = 1;
                int position2 = 2;
                int position3 = 3;
                int position4 = 4;
                int position5 = 5;
                int position6 = 6;
                int position7 = 7;
                int position8 = 8;

                switch (_ConditionType)
                {
                    
                    // (Type 1 for RXXL/123456)
                    case 1:

                        position0 += 3;
                        position1 += 3;
                        position2 += 3;
                        position3 += 3;
                        position4 += 3;
                        position5 += 3;
                        position6 += 3;
                        position7 += 3;
                        position8 += 3;

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

                        break;


                    // (Type 2 for RXX/123456)
                    case 2:

                        position0 += 2;
                        position1 += 2;
                        position2 += 2;
                        position3 += 2;
                        position4 += 2;
                        position5 += 2;
                        position6 += 2;
                        position7 += 2;
                        position8 += 2;

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

                        break;


                    // (Type 3 for XX123456)
                    case 3:

                        // Do not change positions

                        try
                        {
                            int.TryParse(_rwyConditionText.Substring(0, 2), out int intRunway);

                            _wxRunwayCondition.RwyCode = _rwyConditionText.Substring(0, 2);
                            _wxRunwayCondition.RwyValue = _rwyConditionText.Substring(0, 2);
                            _wxRunwayCondition.RwyInt = intRunway;

                            if (!(intRunway <= 36 || intRunway == 88 || intRunway == 99))
                            {
                                _wxRunwayCondition.RwyCode = _rwyConditionText.Substring(0, 2);
                                _wxRunwayCondition.RwyError = true;
                            }
                        }
                        catch
                        {
                            _wxRunwayCondition.RwyCode = _rwyConditionText.Substring(0, 2);
                            _wxRunwayCondition.RwyError = true;
                        }
                        
                        break;


                }

                
                // We only need to calculate deposit, extent, depth and friction for conditions 1, 2 or 3
                if (_ConditionType < 4)

                {

                    // DEPOSIT TYPE
                    try
                    {
                        if (_rwyConditionText.Substring(position2, 1) == "/")
                        {
                            _wxRunwayCondition.DepositCode = "/";
                        }
                        else
                        {
                            _wxRunwayCondition.DepositCode = _rwyConditionText.Substring(position2, 1);

                            if (!int.TryParse(_rwyConditionText.Substring(position2, 1), out int intDeposits))
                            {
                                _wxRunwayCondition.DepositError = true;
                            }
                        }
                    }
                    catch
                    {
                        _wxRunwayCondition.DepositCode = _rwyConditionText.Substring(position2, 1);
                        _wxRunwayCondition.DepositError = true;
                    }


                    // EXTENT TYPE
                    try
                    {
                        if (_rwyConditionText.Substring(position3, 1) == "/")
                        {
                            _wxRunwayCondition.ExtentCode = "/";
                        }
                        else
                        {
                            if (int.TryParse(_rwyConditionText.Substring(position3, 1), out int intExtent))
                            {

                                _wxRunwayCondition.ExtentCode = _rwyConditionText.Substring(position3, 1);

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
                        _wxRunwayCondition.ExtentCode = _rwyConditionText.Substring(position3, 1);
                        _wxRunwayCondition.ExtentError = true;
                    }



                    // DEPTH
                    try
                    {
                        if (_rwyConditionText.Substring(position4, 2) == "//")
                        {
                            _wxRunwayCondition.DepthCode = "//";
                        }
                        else
                        {
                            if (int.TryParse(_rwyConditionText.Substring(position4, 2), out int intDepth))
                            {

                                _wxRunwayCondition.DepthCode = _rwyConditionText.Substring(position4, 2);
                                _wxRunwayCondition.DepthValue = intDepth;

                                if (intDepth == 91)
                                {
                                    _wxRunwayCondition.DepthError = true;
                                }
                            }
                            else
                            {
                                _wxRunwayCondition.DepthError = true;
                            }
                        }
                    }
                    catch
                    {
                        _wxRunwayCondition.DepthCode = _rwyConditionText.Substring(position4, 2);
                        _wxRunwayCondition.DepthError = true;
                    }



                    // FRICTION
                    try
                    {
                        if (_rwyConditionText.Substring(position6, 2) == "//")
                        {
                            _wxRunwayCondition.FrictionCode = "//";
                        }
                        else
                        {
                            if (int.TryParse(_rwyConditionText.Substring(position6, 2), out int intFriction))
                            {

                                _wxRunwayCondition.FrictionCode = _rwyConditionText.Substring(position6, 2);
                                _wxRunwayCondition.FrictionValue = intFriction;

                                if (intFriction >= 96 && intFriction <= 98)
                                {
                                    _wxRunwayCondition.FrictionError = true;
                                }
                            }
                            else
                            {
                                _wxRunwayCondition.FrictionError = true;
                            }
                        }
                    }
                    catch
                    {
                        _wxRunwayCondition.FrictionCode = _rwyConditionText.Substring(position6, 2);
                        _wxRunwayCondition.FrictionError = true;
                    }
                }
                // Conditions type 4, 5 or 6
                else
                {
                    // R/SNOCLO
                    if (_ConditionType == 4)
                    {
                        _wxRunwayCondition.SNOCLO = true;
                    }
                    // RXXL/CLRD//
                    else if (_ConditionType == 5)
                    {
                        _wxRunwayCondition.CLRD = true;

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

                    }
                    // RXX/CLRD//
                    else if (_ConditionType == 6)
                    {
                        _wxRunwayCondition.CLRD = true;

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
                    }

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