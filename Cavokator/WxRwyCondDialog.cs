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
using Android.Graphics;

namespace Cavokator
{
    class WxRwyCondDialog : DialogFragment
    {

        // Dialog fields
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
        
        
        

        public WxRwyCondDialog(string condition_input)
        {
            this._entered_condition = condition_input;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);
            

            // Inflate view
            var view = inflater.Inflate(Resource.Layout.wx_rwycond_dialog, container, false);

            SetStyle(DialogFragmentStyle.NoTitle, 0);

            // Find view IDs
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
            var decoder = new WxRwyCondDecoder();
            var decodedCondition = decoder.DecodeCondition(_entered_condition);

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
            else
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
                    _rwyTextTextView.SetTextColor(Color.ParseColor("#ff0000"));
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
                    _rwyDepositTextTextview.SetTextColor(Color.ParseColor("#ff0000"));
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
                    _rwyExtentTextTextview.SetTextColor(Color.ParseColor("#ff0000"));
                    _rwyExtentTextTextview.Text = Resources.GetString(Resource.String.Extent_Error);
                }


                // TODO NEXT
                // ** DEPTH CODE **
                _rwyExtentCodeTextview.Text = decodedCondition.DepthCode + ": ";

                if (!decodedCondition.DepthError)
                {
                    if (decodedCondition.DepthCode == "//")
                    {
                        _rwyDepthTextTextview.Text = Resources.GetString(Resource.String.DepthNO);
                    }
                    elseif (decodedCondition.DepthCode == 0)
                }
                else
                {
                    _rwyDepthTextTextview.SetTextColor(Color.ParseColor("#ff0000"));
                    _rwyDepthTextTextview.Text = Resources.GetString(Resource.String.Depth_Error);
                }



            }


        }

    }
}