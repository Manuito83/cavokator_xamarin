using Android.OS;
using Android.Views;
using Android.Widget;

namespace Cavokator
{
    class ConditionDialog : Android.Support.V4.App.DialogFragment
    {

        // Dialog fields
        private LinearLayout _conditionBackground;
        private TextView _conditionTitle;
        private TextView _mainErrorTextView;
        private TextView _rwyCodeTextView;
        private TextView _rwyTextTextView;
        private TextView _rwyDepositCodeTextview;
        private TextView _rwyDepositTextTextview;
        private TextView _rwyExtentCodeTextview;
        private TextView _rwyExtentTextTextview;
        private TextView _rwyDepthCodeTextview;
        private TextView _rwyDepthTextTextview;
        private TextView _rwyFrictionCodeTextview;
        private TextView _rwyFrictionTextTextview;


        // Dismiss button
        private Button _dismissDialogButton;

        // Condition that was submitted
        private string _entered_condition;
        
        
        

        public ConditionDialog(string condition_input)
        {
            this._entered_condition = condition_input;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);
            

            // Inflate view
            var view = inflater.Inflate(Resource.Layout.wx_rwycond_dialog, container, false);

            // Find view IDs
            _conditionBackground = view.FindViewById<LinearLayout>(Resource.Id.wx_rwycond_titleLinearLayout);
            _conditionTitle = view.FindViewById<TextView>(Resource.Id.wx_rwycond_title);
            _mainErrorTextView = view.FindViewById<TextView>(Resource.Id.wx_rwycond_main_error);
            _rwyCodeTextView = view.FindViewById<TextView>(Resource.Id.wx_rwycond_rwycode);
            _rwyTextTextView = view.FindViewById<TextView>(Resource.Id.wx_rwycond_rwytext);
            _rwyDepositCodeTextview = view.FindViewById<TextView>(Resource.Id.wx_rwycond_depositsCode);
            _rwyDepositTextTextview = view.FindViewById<TextView>(Resource.Id.wx_rwycond_depositsText);
            _rwyExtentCodeTextview = view.FindViewById<TextView>(Resource.Id.wx_rwycond_extentCode);
            _rwyExtentTextTextview = view.FindViewById<TextView>(Resource.Id.wx_rwycond_extentText);
            _rwyDepthCodeTextview = view.FindViewById<TextView>(Resource.Id.wx_rwycond_depthCode);
            _rwyDepthTextTextview = view.FindViewById<TextView>(Resource.Id.wx_rwycond_depthText);
            _rwyFrictionCodeTextview = view.FindViewById<TextView>(Resource.Id.wx_rwycond_frictionCode);
            _rwyFrictionTextTextview = view.FindViewById<TextView>(Resource.Id.wx_rwycond_frictionText);

            _conditionBackground.SetBackgroundColor(new ApplyTheme().GetColor(DesiredColor.MainBackground));
            _conditionTitle.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MagentaTextWarning));
            _mainErrorTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.RedTextWarning));
            _rwyCodeTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MagentaTextWarning));
            _rwyTextTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            _rwyDepositCodeTextview.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MagentaTextWarning));
            _rwyDepositTextTextview.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            _rwyExtentCodeTextview.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MagentaTextWarning));
            _rwyExtentTextTextview.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            _rwyDepthCodeTextview.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MagentaTextWarning));
            _rwyDepthTextTextview.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            _rwyFrictionCodeTextview.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MagentaTextWarning));
            _rwyFrictionTextTextview.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));


            _dismissDialogButton = view.FindViewById<Button>(Resource.Id.wx_rwycond_closeButton);

            // Assign title from actual condition clicked
            _conditionTitle.Text = _entered_condition;

            // CLOSE BUTTON (dismiss dialog)
            _dismissDialogButton.Click += delegate
            {
                this.Dismiss();
            };


            // PASS INFORMATION FOR DECODING
            ShowCondition();
            
            return view;
        }


        private void ShowCondition()
        {
            var decoder = new ConditionDecoder();
            var decodedCondition = decoder.DecodeCondition(_entered_condition);

            // SHOW MAIN ERROR
            if (decodedCondition.MainError)
            {
                _mainErrorTextView.Text = Resources.GetString(Resource.String.Main_Error);

                // Show error and hide rest of TextViews below with runway information
                _rwyCodeTextView.Visibility = ViewStates.Gone;
                _rwyTextTextView.Visibility = ViewStates.Gone;
                _rwyDepositCodeTextview.Visibility = ViewStates.Gone;
                _rwyDepositTextTextview.Visibility = ViewStates.Gone;
                _rwyExtentCodeTextview.Visibility = ViewStates.Gone;
                _rwyExtentTextTextview.Visibility = ViewStates.Gone;
                _rwyDepthCodeTextview.Visibility = ViewStates.Gone;
                _rwyDepthTextTextview.Visibility = ViewStates.Gone;
                _rwyFrictionCodeTextview.Visibility = ViewStates.Gone;
                _rwyFrictionTextTextview.Visibility = ViewStates.Gone;
            }
            // SHOW CONDITION FOR TYPES 1,2 AND 3
            else if (!(decodedCondition.CLRD || decodedCondition.SNOCLO))
            {
                // Make sure Main Error does not appear
                _mainErrorTextView.Visibility = ViewStates.Gone;

                // ** RUNWAY CODE **
                _rwyCodeTextView.Text = decodedCondition.RwyCode + ": ";

                if (!decodedCondition.RwyError)
                {
                    if (decodedCondition.RwyInt <= 36)
                    {
                        // Runway Value
                        _rwyTextTextView.Text = Resources.GetString(Resource.String.Runway_Indicator) 
                                                 + " " + decodedCondition.RwyValue;
                    }
                    else if (decodedCondition.RwyInt == 88)
                    {
                        // Runway Value
                        _rwyTextTextView.Text = Resources.GetString(Resource.String.Runway_AllRunways);
                    }
                    else if (decodedCondition.RwyInt == 99)
                    {
                        // Runway Value
                        _rwyTextTextView.Text = Resources.GetString(Resource.String.Runway_ReportRepeated);
                    }
                }
                else
                {
                    _rwyTextTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.RedTextWarning));
                    _rwyTextTextView.Text = Resources.GetString(Resource.String.Runway_Error);
                }


                // ** DEPOSIT CODE **
                _rwyDepositCodeTextview.Text = decodedCondition.DepositCode + ": ";

                if (!decodedCondition.DepositError)
                {
                    switch (decodedCondition.DepositCode)
                    {
                        case "/":
                            _rwyDepositTextTextview.Text = Resources.GetString(Resource.String.DepositNO);
                            break;

                        case "0":
                            _rwyDepositTextTextview.Text = Resources.GetString(Resource.String.Deposit0);
                            break;

                        case "1":
                            _rwyDepositTextTextview.Text = Resources.GetString(Resource.String.Deposit1);
                            break;

                        case "2":
                            _rwyDepositTextTextview.Text = Resources.GetString(Resource.String.Deposit2);
                            break;

                        case "3":
                            _rwyDepositTextTextview.Text = Resources.GetString(Resource.String.Deposit3);
                            break;

                        case "4":
                            _rwyDepositTextTextview.Text = Resources.GetString(Resource.String.Deposit4);
                            break;

                        case "5":
                            _rwyDepositTextTextview.Text = Resources.GetString(Resource.String.Deposit5);
                            break;

                        case "6":
                            _rwyDepositTextTextview.Text = Resources.GetString(Resource.String.Deposit6);
                            break;

                        case "7":
                            _rwyDepositTextTextview.Text = Resources.GetString(Resource.String.Deposit7);
                            break;

                        case "8":
                            _rwyDepositTextTextview.Text = Resources.GetString(Resource.String.Deposit8);
                            break;

                        case "9":
                            _rwyDepositTextTextview.Text = Resources.GetString(Resource.String.Deposit9);
                            break;

                    }
                }
                else
                {
                    // Probably never gonna happen, as we got all numbers covered
                    _rwyDepositTextTextview.SetTextColor(new ApplyTheme().GetColor(DesiredColor.RedTextWarning));
                    _rwyDepositTextTextview.Text = Resources.GetString(Resource.String.Deposit_Error);
                }



                // ** EXTENT CODE **
                _rwyExtentCodeTextview.Text = decodedCondition.ExtentCode + ": ";

                if (!decodedCondition.ExtentError)
                {
                    switch (decodedCondition.ExtentCode)
                    {
                        case "/":
                            _rwyExtentTextTextview.Text = Resources.GetString(Resource.String.ExtentNO);
                            break;

                        case "1":
                            _rwyExtentTextTextview.Text = Resources.GetString(Resource.String.Extent1);
                            break;

                        case "2":
                            _rwyExtentTextTextview.Text = Resources.GetString(Resource.String.Extent2);
                            break;

                        case "5":
                            _rwyExtentTextTextview.Text = Resources.GetString(Resource.String.Extent5);
                            break;

                        case "9":
                            _rwyExtentTextTextview.Text = Resources.GetString(Resource.String.Extent9);
                            break;
                    }
                }
                else
                {
                    _rwyExtentTextTextview.SetTextColor(new ApplyTheme().GetColor(DesiredColor.RedTextWarning));
                    _rwyExtentTextTextview.Text = Resources.GetString(Resource.String.Extent_Error);
                }



                // ** DEPTH CODE **
                _rwyDepthCodeTextview.Text = decodedCondition.DepthCode + ": ";

                if (!decodedCondition.DepthError)
                {
                    if (decodedCondition.DepthCode == "//")
                    {
                        _rwyDepthTextTextview.Text = Resources.GetString(Resource.String.DepthNO);
                    }
                    else if (decodedCondition.DepthValue == 0)
                    {
                        _rwyDepthTextTextview.Text = Resources.GetString(Resource.String.Depth00);
                    }
                    else if (decodedCondition.DepthValue >= 1 && decodedCondition.DepthValue <= 90)
                    {
                        _rwyDepthTextTextview.Text = (Resources.GetString(Resource.String.Depth) +
                                                     " " + decodedCondition.DepthValue + " mm");
                    }
                    else if (decodedCondition.DepthValue >= 92 && decodedCondition.DepthValue <= 97)
                    {
                        _rwyDepthTextTextview.Text = (Resources.GetString(Resource.String.Depth) +
                                                     " " + decodedCondition.DepthValue + " cm");
                    }
                    else if (decodedCondition.DepthValue == 98)
                    {
                        _rwyDepthTextTextview.Text = (Resources.GetString(Resource.String.Depth) +
                                                     " " + decodedCondition.DepthValue + " cm" +
                                                     " " + Resources.GetString(Resource.String.DepthMORE));
                    }
                    else if (decodedCondition.DepthValue == 99)
                    {
                        _rwyDepthTextTextview.Text = Resources.GetString(Resource.String.Depth99);
                    }
                }
                else
                {
                    _rwyDepthTextTextview.SetTextColor(new ApplyTheme().GetColor(DesiredColor.RedTextWarning));
                    _rwyDepthTextTextview.Text = Resources.GetString(Resource.String.Depth_Error);
                }



                // ** FRICTION CODE **
                _rwyFrictionCodeTextview.Text = decodedCondition.FrictionCode + ": ";

                if (!decodedCondition.FrictionError)
                {
                    if (decodedCondition.FrictionCode == "//")
                    {
                        _rwyFrictionTextTextview.Text = Resources.GetString(Resource.String.FrictionNO);
                    }
                    else if (decodedCondition.FrictionValue >= 1 && decodedCondition.FrictionValue <= 90)
                    {
                        _rwyFrictionTextTextview.Text = (Resources.GetString(Resource.String.FrictionCoefficient) +
                                                     " ." + decodedCondition.FrictionValue);
                    }
                    else if (decodedCondition.FrictionValue == 91)
                    {
                        _rwyFrictionTextTextview.Text = Resources.GetString(Resource.String.FrictionBA91);
                    }
                    else if (decodedCondition.FrictionValue == 92)
                    {
                        _rwyFrictionTextTextview.Text = Resources.GetString(Resource.String.FrictionBA92);
                    }
                    else if (decodedCondition.FrictionValue == 93)
                    {
                        _rwyFrictionTextTextview.Text = Resources.GetString(Resource.String.FrictionBA93);
                    }
                    else if (decodedCondition.FrictionValue == 94)
                    {
                        _rwyFrictionTextTextview.Text = Resources.GetString(Resource.String.FrictionBA94);
                    }
                    else if (decodedCondition.FrictionValue == 95)
                    {
                        _rwyFrictionTextTextview.Text = Resources.GetString(Resource.String.FrictionBA95);
                    }
                    else if (decodedCondition.FrictionValue == 99)
                    {
                        _rwyFrictionTextTextview.Text = Resources.GetString(Resource.String.Friction99);
                    }
                }
                else
                {
                    _rwyFrictionTextTextview.SetTextColor(new ApplyTheme().GetColor(DesiredColor.RedTextWarning));
                    _rwyFrictionTextTextview.Text = Resources.GetString(Resource.String.Friction_Error);
                }

                
            }
            // SHOW CONDITION FOR SNOCLO
            else if (decodedCondition.SNOCLO)
            {
                _rwyDepositCodeTextview.Visibility = ViewStates.Gone;
                _rwyDepositTextTextview.Visibility = ViewStates.Gone;
                _rwyExtentCodeTextview.Visibility = ViewStates.Gone;
                _rwyExtentTextTextview.Visibility = ViewStates.Gone;
                _rwyDepthCodeTextview.Visibility = ViewStates.Gone;
                _rwyDepthTextTextview.Visibility = ViewStates.Gone;
                _rwyFrictionCodeTextview.Visibility = ViewStates.Gone;
                _rwyFrictionTextTextview.Visibility = ViewStates.Gone;

                // Make sure Main Error does not appear
                _mainErrorTextView.Visibility = ViewStates.Gone;

                _rwyCodeTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.YellowTextWarning));
                _rwyCodeTextView.Text = "R/SNOCLO: ";

                _rwyTextTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.RedTextWarning));
                _rwyTextTextView.Text = Resources.GetString(Resource.String.SNOCLO);

            }
            // SHOW CONDITION FOR CLRD (BOTH RXX/CLRD// AND RXXL/CLRD//
            else if (decodedCondition.CLRD)
            {
                _rwyExtentCodeTextview.Visibility = ViewStates.Gone;
                _rwyExtentTextTextview.Visibility = ViewStates.Gone;
                _rwyDepthCodeTextview.Visibility = ViewStates.Gone;
                _rwyDepthTextTextview.Visibility = ViewStates.Gone;
                _rwyFrictionCodeTextview.Visibility = ViewStates.Gone;
                _rwyFrictionTextTextview.Visibility = ViewStates.Gone;

                // Make sure Main Error does not appear
                _mainErrorTextView.Visibility = ViewStates.Gone;


                // ** RUNWAY CODE **
                _rwyCodeTextView.Text = decodedCondition.RwyCode + ": ";
                if (!decodedCondition.RwyError)
                {
                    if (decodedCondition.RwyInt <= 36)
                    {
                        // Runway Value
                        _rwyTextTextView.Text = Resources.GetString(Resource.String.Runway_Indicator)
                                                 + " " + decodedCondition.RwyValue;
                    }
                    else if (decodedCondition.RwyInt == 88)
                    {
                        // Runway Value
                        _rwyTextTextView.Text = Resources.GetString(Resource.String.Runway_AllRunways);
                    }
                    else if (decodedCondition.RwyInt == 99)
                    {
                        // Runway Value
                        _rwyTextTextView.Text = Resources.GetString(Resource.String.Runway_ReportRepeated);
                    }
                }
                else
                {
                    _rwyTextTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.RedTextWarning));
                    _rwyTextTextView.Text = Resources.GetString(Resource.String.Runway_Error);
                }

                // CLRD CODE
                _rwyDepositCodeTextview.Text = "CLRD: ";
                _rwyDepositTextTextview.Text = Resources.GetString(Resource.String.CLRD);

            }


        }

    }
}