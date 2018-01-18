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
using Android.Views.InputMethods;

namespace Cavokator
{
    class NotamFragment : Android.Support.V4.App.Fragment
    {
        // Main fields
        private LinearLayout _linearlayoutBottom;
        private EditText _airportEntryEditText;
        private Button _notamRequestButton;
        private Button _notamClearButton;
        private TextView _chooseIDtextview;

        // View that will be used for FindViewById
        private View thisView;

        private NotamContainer myNotamContainer = new NotamContainer();

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            HasOptionsMenu = true;
        }


        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // In order to return the view for this Fragment
            thisView = inflater.Inflate(Resource.Layout.notam_fragment, container, false);

            StyleViews();
            
            // Events
            _linearlayoutBottom.Touch += OnBackgroundTouch;
            _notamRequestButton.Click += OnRequestButtonClicked;

            return thisView;
        }


        private void OnBackgroundTouch(object sender, View.TouchEventArgs e)
        {
            var imm = (InputMethodManager)Application.Context.GetSystemService(Context.InputMethodService);
            imm.HideSoftInputFromWindow(_airportEntryEditText.WindowToken, 0);
        }


        private void OnRequestButtonClicked(object sender, EventArgs e)
        {
            // Close keyboard when button pressed
            var im = (InputMethodManager)Activity.GetSystemService(Context.InputMethodService);
            im.HideSoftInputFromWindow(Activity.CurrentFocus.WindowToken, 0);

            myNotamContainer = RequestNotams();

            ShowNotams(myNotamContainer);
        }

        private NotamContainer RequestNotams()
        {
            // TODO: Send a list here
            string myAirportRequest = _airportEntryEditText.Text;
            NotamFetcher mNotams = new NotamFetcher(myAirportRequest);

            return mNotams.DecodedNotam;
        }

        private void ShowNotams(NotamContainer myNotamContainer)
        {
            for (int i = 0; i < myNotamContainer.NotamRaw.Count; i++)
            {
                var notamLine = new TextView(Activity);

                notamLine.Text = myNotamContainer.NotamRaw[i];

                Activity.RunOnUiThread(() =>
                {
                    // TODO: Add
                    linearlayoutNotams.AddView(notamLine);
                });

            }
        }

        private void StyleViews()
        {
            _linearlayoutBottom = thisView.FindViewById<LinearLayout>(Resource.Id.notam_linearlayout_bottom);
            _airportEntryEditText = thisView.FindViewById<EditText>(Resource.Id.notam_airport_entry);
            _chooseIDtextview = thisView.FindViewById<TextView>(Resource.Id.notam_choose_id_textview);
            _notamRequestButton = thisView.FindViewById<Button>(Resource.Id.notam_request_button);
            _notamClearButton = thisView.FindViewById<Button>(Resource.Id.notam_clear_button);
            
            _notamRequestButton.Text = Resources.GetString(Resource.String.Send_button);
            _notamClearButton.Text = Resources.GetString(Resource.String.Clear_button);
            _chooseIDtextview.Text = Resources.GetString(Resource.String.Airport_ID_TextView);
            _airportEntryEditText.Hint = Resources.GetString(Resource.String.Icao_Or_Iata);
        }

     
    }
}