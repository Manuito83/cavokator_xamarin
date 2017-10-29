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
        private string _condition_title;
        
        // Dismiss button
        private Button _dismissDialogButton;

        public WxRwyCondDialog(string clicked_condition)
        {
            this._condition_title = clicked_condition;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);
            

            // Inflate view
            var view = inflater.Inflate(Resource.Layout.wx_rwycond_dialog, container, false);

            SetStyle(DialogFragmentStyle.NoTitle, 0);

            // Find view IDs
            _conditionTitle = view.FindViewById<TextView>(Resource.Id.wx_rwycond_title);
            _dismissDialogButton = view.FindViewById<Button>(Resource.Id.wx_rwycond_closeButton);

            // Assign title from actual condition clicked
            _conditionTitle.Text = _condition_title;

            // CLOSE BUTTON (dismiss dialog)
            _dismissDialogButton.Click += delegate
            {
                this.Dismiss();
            };


            // PASS INFORMATION FOR DECODING
            DecodeCondition();
            
            return view;
        }


        private void DecodeCondition()
        {
            var conditionLines = new 

            var decoder = new WxRwyCondDecoder(_condition_title);
        }

    }
}