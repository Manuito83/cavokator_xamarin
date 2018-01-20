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
using Android.Text;
using Android.Graphics;

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
        private LinearLayout _linearLayoutNotamLines;

        // View that will be used for FindViewById
        private View thisView;

        private NotamContainer myNotamContainer = new NotamContainer();

        // Keep count of string length in EditText field, so that we know if it has decreased (deletion)
        private int editTextIdLength;

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
            _notamClearButton.Click += OnClearButtonClicked;
            _airportEntryEditText.BeforeTextChanged += BeforeIdTextChanged;
            _airportEntryEditText.AfterTextChanged += OnIdTextChanged;

            return thisView;
        }

        /// <summary>
        /// First we change the box style, then we limit length to 4 chars
        /// </summary>
        private void OnIdTextChanged(object sender, AfterTextChangedEventArgs e)
        {
            // Style EdiText text when writting
            _airportEntryEditText.SetTextColor(Color.Black);
            _airportEntryEditText.SetBackgroundColor(Color.White);
            _airportEntryEditText.SetTypeface(null, TypefaceStyle.Normal);


            // Apply only if we are adding text
            // Otherwise, we could not delete (due to infinite loop)
            if (_airportEntryEditText.Text.Length > editTextIdLength)
            {
                // If our text is already 4 positions long
                if (_airportEntryEditText.Text.Length > 3)
                {
                    // Take a look at the last 4 chars entered
                    string lastFourChars = _airportEntryEditText.Text.Substring(_airportEntryEditText.Text.Length - 4, 4);

                    // If there is at least a space, then do nothing
                    bool maxLengthReached = true;
                    foreach (char c in lastFourChars)
                    {
                        if (c == ' ')
                        {
                            maxLengthReached = false;
                        }
                    }

                    // If there is no space, then we apply a space
                    if (maxLengthReached)
                    {
                        // We need to unsubscribe and subscribe again to the event
                        // Otherwise we would get an infinite loop
                        _airportEntryEditText.AfterTextChanged -= OnIdTextChanged;

                        _airportEntryEditText.Append(" ");

                        _airportEntryEditText.AfterTextChanged += OnIdTextChanged;

                    }

                }
            }
        }

        private void BeforeIdTextChanged(object sender, TextChangedEventArgs e)
        {
            editTextIdLength = _airportEntryEditText.Text.Length;
        }

        private void OnBackgroundTouch(object sender, View.TouchEventArgs e)
        {
            var imm = (InputMethodManager)Application.Context.GetSystemService(Context.InputMethodService);
            imm.HideSoftInputFromWindow(_airportEntryEditText.WindowToken, 0);
        }

        private void OnClearButtonClicked(object sender, EventArgs e)
        {
            _linearLayoutNotamLines.RemoveAllViews();
        }

        private void OnRequestButtonClicked(object sender, EventArgs e)
        {
            // Close keyboard when button pressed
            var im = (InputMethodManager)Activity.GetSystemService(Context.InputMethodService);
            im.HideSoftInputFromWindow(Activity.CurrentFocus.WindowToken, 0);

            _airportEntryEditText.ClearFocus();

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
                
                TextView notamLine = new TextView(Activity);

                notamLine.Text = myNotamContainer.NotamRaw[i];

                Activity.RunOnUiThread(() =>
                {
                    // TODO: Add
                    _linearLayoutNotamLines.AddView(notamLine);
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

            _linearLayoutNotamLines = thisView.FindViewById<LinearLayout>(Resource.Id.notam_linearlayout_lines);
        }

     
    }
}