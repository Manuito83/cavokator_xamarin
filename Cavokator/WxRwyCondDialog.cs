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
    class WxRwyCondDialog : DialogFragment
    {

        // Dialog fields
        private TextView _conditionTitle;
        private TextView _mainErrorTextView;
        private TextView _rwyCodeTextView;
        private TextView _rwyTextTextValue;
        
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
            _rwyTextTextValue = view.FindViewById<TextView>(Resource.Id.wx_rwycond_rwytext);
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
                // TODO: SHOW ERROR - CONFIGURE TEXT
            }
            else
            {
                _mainErrorTextView.Visibility = ViewStates.Gone;

                // Runway Code
                _rwyCodeTextView.Text = decodedCondition.RwyCode + ": ";

                if (!decodedCondition.RwyError)
                {
                    if (decodedCondition.RwyInt <= 36)
                    {
                        // Runway Value
                        _rwyTextTextValue.Text = Resources.GetString(Resource.String.Runway_Indicator) 
                                                 + " " + decodedCondition.RwyValue;
                    }
                    else if (decodedCondition.RwyInt == 88)
                    {
                        // Runway Value
                        _rwyTextTextValue.Text = Resources.GetString(Resource.String.Runway_AllRunways);
                    }
                    else if (decodedCondition.RwyInt == 99)
                    {
                        // Runway Value
                        _rwyTextTextValue.Text = Resources.GetString(Resource.String.Runway_ReportRepeated);
                    }


                }
                else
                {
                    // TODO: RWY ERROR
                }
                

            }


        }

    }
}