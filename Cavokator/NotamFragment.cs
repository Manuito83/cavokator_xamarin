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
using Plugin.Connectivity;
using System.Threading.Tasks;

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

        // List of actual ICAO (as entered) airports that we are going to request
        private List<string> requestedAirportsByIcao = new List<string>();

        // List of airports with a mix of ICAO and IATA, that we show to the user as it was requested
        private List<string> requestedAirportsRawString = new List<string>();

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

            // Remove all previous views from the linear layout
            _linearLayoutNotamLines.RemoveAllViews();

            if (CrossConnectivity.Current.IsConnected)
            {
                // Start thread outside UI
                Task.Factory.StartNew(() =>
                {
                    // Populate "requestedAirports" lists
                    SanitizeRequestedNotams(_airportEntryEditText.Text);

                    myNotamContainer = RequestNotams();

                    ShowNotams(myNotamContainer);
                });
            }
            else
            {
                Toast.MakeText(Activity, Resource.String.Internet_Error, ToastLength.Short).Show();
            }
        }

        private void SanitizeRequestedNotams(string requestedNotamsString)
        {
            // Split airport list entered
            // We perform the same operation to both lists, the user one and the ICAO one
            requestedAirportsByIcao = requestedNotamsString.Split(' ', '\n', ',').ToList();
            requestedAirportsRawString = requestedNotamsString.Split(' ', '\n', ',').ToList();

            // Check and delete any entries with less than 3 chars
            for (var i = requestedAirportsByIcao.Count - 1; i >= 0; i--)
            {
                if (requestedAirportsByIcao[i].Length < 3)
                {
                    requestedAirportsByIcao.RemoveAt(i);
                    requestedAirportsRawString.RemoveAt(i);
                }
            }

            // If airport code length is 3, it might be an IATA airport
            // so we try to get its ICAO in order to get the WX information
            for (var i = 0; i < requestedAirportsByIcao.Count; i++)
            {
                if (requestedAirportsByIcao[i].Length == 3)
                {
                    // Try to find the IATA in the list
                    try
                    {
                        for (int j = 0; j < AirportDefinitions._myAirportDefinitions.Count; j++)
                        {
                            if (AirportDefinitions._myAirportDefinitions[j].iata == requestedAirportsByIcao[i].ToUpper())
                            {
                                requestedAirportsByIcao[i] = AirportDefinitions._myAirportDefinitions[j].icao;
                                break;
                            }

                        }
                    }
                    catch
                    {
                        requestedAirportsByIcao[i] = null;
                    }
                }
            }
        }

        private NotamContainer RequestNotams()
        {
            string myAirportRequest = requestedAirportsByIcao[0];
            
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
            _linearlayoutBottom.SetBackgroundColor(new ApplyTheme().GetColor(DesiredColor.MainBackground));
            
            _chooseIDtextview = thisView.FindViewById<TextView>(Resource.Id.notam_choose_id_textview);
            _chooseIDtextview.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));

            _airportEntryEditText = thisView.FindViewById<EditText>(Resource.Id.notam_airport_entry);
            _notamRequestButton = thisView.FindViewById<Button>(Resource.Id.notam_request_button);
            _notamClearButton = thisView.FindViewById<Button>(Resource.Id.notam_clear_button);
            _linearLayoutNotamLines = thisView.FindViewById<LinearLayout>(Resource.Id.notam_linearlayout_lines);

            _notamRequestButton.Text = Resources.GetString(Resource.String.Send_button);
            _notamClearButton.Text = Resources.GetString(Resource.String.Clear_button);
            _chooseIDtextview.Text = Resources.GetString(Resource.String.Airport_ID_TextView);
            _airportEntryEditText.Hint = Resources.GetString(Resource.String.Icao_Or_Iata);

            

            
            
        }

     
    }
}