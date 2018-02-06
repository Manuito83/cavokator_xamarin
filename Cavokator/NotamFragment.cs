//
// CAVOKATOR APP
// Website: https://github.com/Manuito83/Cavokator
// License GNU General Public License v3.0
// Manuel Ortega, 2018
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Views.InputMethods;
using Android.Text;
using Android.Graphics;
using Plugin.Connectivity;
using System.Threading.Tasks;
using Android.Support.V7.Widget;
using Android.Util;
using System.Threading;
using Android.Graphics.Drawables;
using Android.Support.V7.App;
using Android.Text.Method;
using Android.Text.Style;
using Newtonsoft.Json;
using AlertDialog = Android.App.AlertDialog;
using Android.Support.Design.Widget;

namespace Cavokator
{
    class NotamFragment : Android.Support.V4.App.Fragment
    {

        // TODO: implement as option
        private bool sortByCategory = true;
        
        // Floating action button
        private CoordinatorLayout _coordinatorLayout;
        private FloatingActionButton _fabScrollTop;
        
        // Main views
        private ScrollView _scrollViewContainer;
        private LinearLayout _linearlayoutBottom;
        private EditText _airportEntryEditText;
        private Button _notamRequestButton;
        private Button _notamClearButton;
        private TextView _chooseIDtextview;
        private LinearLayout _linearLayoutNotamLines;

        // ProgressDialog to show while we fetch the wx information
        private AlertDialog.Builder _notamFetchingAlertDialogBuilder;
        private AlertDialog _notamFetchingAlertDialog;

        private bool _connectionError;

        // View that will be used for FindViewById
        private View _thisView;

        // Views for UTC time
        private DateTime _mUtcRequestTime;
        private TextView _mUtcTextView;

        // Views for calendar color
        private List<ImageView> _mCalendarViews = new List<ImageView>();
        private List<DateTime> mStartDateTimes = new List<DateTime>();
        private List<DateTime> mEnDateTimes = new List<DateTime>();

        private List<NotamContainer> _mNotamContainerList = new List<NotamContainer>();

        // List of actual ICAO (as entered) airports that we are going to request
        private List<string> _mRequestedAirportsByIcao = new List<string>();

        // List of airports with a mix of ICAO and IATA, that we show to the user as it was requested
        private List<string> _mRequestedAirportsRawString = new List<string>();

        // Keep count of string length in EditText field, so that we know if it has decreased (deletion)
        private int _mEditTextIdLength;

        // Initialize object to store List downloaded at OnCreate from a CAV file with IATA, ICAO and Airport Names
        private List<AirportCsvDefinition> _mAirportDefinitions = AirportDefinitions._myAirportDefinitions;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            ((AppCompatActivity)Activity).SupportActionBar.Title = "NOTAM";

            HasOptionsMenu = true;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // In order to return the view for this Fragment
            _thisView = inflater.Inflate(Resource.Layout.notam_fragment, container, false);

            StyleViews();

            // Events
            _linearlayoutBottom.Touch += OnBackgroundTouch;
            _notamRequestButton.Click += OnRequestButtonClicked;
            _notamClearButton.Click += OnClearButtonClicked;
            _airportEntryEditText.BeforeTextChanged += BeforeIdTextChanged;
            _airportEntryEditText.AfterTextChanged += OnIdTextChanged;
            _scrollViewContainer.ScrollChange += OnScrollMoved;
            _fabScrollTop.Click += ScrollToTop;

            // TODO: do try/catch for weather as well?
            try
            {
                RecallSavedData();
            }
            catch
            {
                // Encountered new null fields
                _notamClearButton.CallOnClick();
            }
            
            // Add FAB and hide
            _coordinatorLayout.AddView(_fabScrollTop);
            _fabScrollTop.Hide();

            // Sets up timer to update NOTAM UTC
            TimeTick();
            
            return _thisView;
        }

        private void ScrollToTop(object sender, EventArgs e)
        {
            _scrollViewContainer.SmoothScrollTo(0, 0);
        }

        private void OnScrollMoved(object sender, View.ScrollChangeEventArgs e)
        {
            if (_scrollViewContainer.ScrollY > 1000)
            {
                CoordinatorLayout.LayoutParams lp = (CoordinatorLayout.LayoutParams)_fabScrollTop.LayoutParameters;
                lp.Gravity = (int)(GravityFlags.Bottom | GravityFlags.Right | GravityFlags.End);
                lp.SetMargins(0, 0, 16, 16);
                _fabScrollTop.LayoutParameters = lp;
                _fabScrollTop.Show();
            }
            else
            {
                _fabScrollTop.Hide();
            }
        }

        // Saves fields to SharedPreferences
        public override void OnPause()
        {
            SaveData();

            base.OnPause();
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
        if (_airportEntryEditText.Text.Length > _mEditTextIdLength)
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
            _mEditTextIdLength = _airportEntryEditText.Text.Length;
        }

        private void OnBackgroundTouch(object sender, View.TouchEventArgs e)
        {
            var imm = (InputMethodManager)Application.Context.GetSystemService(Context.InputMethodService);
            imm.HideSoftInputFromWindow(_airportEntryEditText.WindowToken, 0);
        }

        private void OnClearButtonClicked(object sender, EventArgs e)
        {
            _linearLayoutNotamLines.RemoveAllViews();

            _mNotamContainerList.Clear();
            _mUtcTextView = null;

            _mCalendarViews.Clear();
            mStartDateTimes.Clear();
            mEnDateTimes.Clear();

            _connectionError = false;

            _airportEntryEditText.Text = "";
            _airportEntryEditText.SetTextColor(default(Color));
            _airportEntryEditText.SetBackgroundColor(Color.ParseColor("#aaaaaa"));
            _airportEntryEditText.SetTypeface(null, TypefaceStyle.Italic);
        }

        private void OnRequestButtonClicked(object sender, EventArgs e)
        {
            // Close keyboard when button pressed
            var im = (InputMethodManager)Activity.GetSystemService(Context.InputMethodService);
            im.HideSoftInputFromWindow(Activity.CurrentFocus.WindowToken, 0);

            _connectionError = false;

            _airportEntryEditText.ClearFocus();

            _mNotamContainerList.Clear();
            _mUtcTextView = null;

            _mCalendarViews.Clear();
            mStartDateTimes.Clear();
            mEnDateTimes.Clear();

            // Remove all previous views from the linear layout
            _linearLayoutNotamLines.RemoveAllViews();

            // Update the time at which the request was performed
            _mUtcRequestTime = DateTime.UtcNow;
            
            if (CrossConnectivity.Current.IsConnected && _airportEntryEditText.Text != String.Empty)
            {
                _notamRequestButton.Enabled = false;
                
                // Show our AlertDialog
                _notamFetchingAlertDialogBuilder = new AlertDialog.Builder(Activity);
                _notamFetchingAlertDialogBuilder.SetTitle(Resources.GetString(Resource.String.Fetching));
                _notamFetchingAlertDialogBuilder.SetMessage("");
                _notamFetchingAlertDialog = _notamFetchingAlertDialogBuilder.Create();
                _notamFetchingAlertDialog.Show();

                // Start thread outside UI
                Task.Factory.StartNew(() =>
                {
                    // Populate "requestedAirports" lists
                    SanitizeRequestedNotams(_airportEntryEditText.Text);

                    // Populate list with notams for every airport requested
                    GetNotams();

                    // Did we connect succesfully? Then show Notams!
                    if (_connectionError == false)
                        ShowNotams();
                    else
                    {
                        _mNotamContainerList.Clear();
                        ShowConnectionError();
                    }

                    Activity.RunOnUiThread(() =>
                    {
                        _notamRequestButton.Enabled = true;
                    });
                });
            }
            else if (!CrossConnectivity.Current.IsConnected)
            {
                Toast.MakeText(Activity, Resource.String.Internet_Error, ToastLength.Short).Show();
            }
        }

        // Populate "requestedAirports" lists
        private void SanitizeRequestedNotams(string requestedNotamsString)
        {
            // Split airport list entered
            // We perform the same operation to both lists, the user one and the ICAO one
            _mRequestedAirportsByIcao = requestedNotamsString.Split(' ', '\n', ',').ToList();
            _mRequestedAirportsRawString = requestedNotamsString.Split(' ', '\n', ',').ToList();

            // Check and delete any entries with less than 3 chars
            for (var i = _mRequestedAirportsByIcao.Count - 1; i >= 0; i--)
            {
                if (_mRequestedAirportsByIcao[i].Length < 3)
                {
                    _mRequestedAirportsByIcao.RemoveAt(i);
                    _mRequestedAirportsRawString.RemoveAt(i);
                }
            }

            // If airport code length is 3, it might be an IATA airport
            // so we try to get its ICAO in order to get the WX information
            for (var i = 0; i < _mRequestedAirportsByIcao.Count; i++)
            {
                if (_mRequestedAirportsByIcao[i].Length == 3)
                {
                    // Try to find the IATA in the list
                    try
                    {
                        for (int j = 0; j < _mAirportDefinitions.Count; j++)
                        {
                            if (_mAirportDefinitions[j].iata == _mRequestedAirportsByIcao[i].ToUpper())
                            {
                                _mRequestedAirportsByIcao[i] = _mAirportDefinitions[j].icao;
                                break;
                            }

                        }
                    }
                    catch
                    {
                        _mRequestedAirportsByIcao[i] = null;
                    }
                }
            }
        }

        // Populate list with notams for every airport requested
        private void GetNotams()
        {
            for (int i = 0; i < _mRequestedAirportsByIcao.Count; i++) 
            {
                string currentAirport = _mRequestedAirportsByIcao[i];

                NotamFetcher mNotams = new NotamFetcher(currentAirport);

                if (!mNotams.DecodedNotam.ConnectionError)
                {
                    _mNotamContainerList.Add(mNotams.DecodedNotam);
                    PercentageCompleted(i, _mRequestedAirportsByIcao.Count, currentAirport);
                }
                else
                {
                    _notamFetchingAlertDialog.Dismiss();
                    _connectionError = true;
                    break;
                }
            }
        }

        private void ShowNotams()
        {
            // Start working if there is something in the container
            if (_mNotamContainerList.Count > 0)
            {
                if (!_connectionError)
                {
                    AddRequestedTime();

                    // Iterate every airport populated by GetNotams()
                    for (int i = 0; i < _mNotamContainerList.Count; i++)
                    {
                        AddAirportName(i);

                        if (_mNotamContainerList[i].NotamRaw.Count == 0)
                        {
                            AddErrorCard();
                            break;
                        }

                        if (sortByCategory)
                        {
                            LocalAddNotamsByCategory(i);
                        }
                        else
                        {
                            LocalAddNotamsByDate(i);
                        }
                    }
                }
                else
                {
                    ShowConnectionError();
                }
            }

            void LocalAddNotamsByCategory(int i)
            {

                try
                {
                    bool anyQNotam = false;
                    bool anyDNotam = false;
                    bool anyRawNotam = false;

                    bool anyCategoryL = false;
                    bool anyCategoryM = false;
                    bool anyCategoryF = false;
                    bool anyCategoryA = false;
                    bool anyCategoryS = false;
                    bool anyCategoryP = false;
                    bool anyCategoryC = false;
                    bool anyCategoryI = false;
                    bool anyCategoryG = false;
                    bool anyCategoryN = false;
                    bool anyCategoryR = false;
                    bool anyCategoryW = false;
                    bool anyCategoryO = false;
                    bool anyCategoryNotReported = false;

                    List<int> positionL = new List<int>();
                    List<int> positionM = new List<int>();
                    List<int> positionF = new List<int>();
                    List<int> positionA = new List<int>();
                    List<int> positionS = new List<int>();
                    List<int> positionP = new List<int>();
                    List<int> positionC = new List<int>();
                    List<int> positionI = new List<int>();
                    List<int> positionG = new List<int>();
                    List<int> positionN = new List<int>();
                    List<int> positionR = new List<int>();
                    List<int> positionW = new List<int>();
                    List<int> positionO = new List<int>();
                    List<int> positionUnknown = new List<int>();
                    List<int> positionRaw = new List<int>();

                    for (int j = 0; j < _mNotamContainerList[i].NotamRaw.Count; j++)
                    {
                        // NOTAM Q
                        if (_mNotamContainerList[i].NotamQ[j])
                        {
                            anyQNotam = true;

                            if (_mNotamContainerList[i].CodeSecondThird[j].Substring(0, 1) == "L")
                            {
                                anyCategoryL = true;
                                positionL.Add(j);
                            }
                            else if (_mNotamContainerList[i].CodeSecondThird[j].Substring(0, 1) == "M")
                            {
                                anyCategoryM = true;
                                positionM.Add(j);
                            }
                            else if (_mNotamContainerList[i].CodeSecondThird[j].Substring(0, 1) == "F")
                            {
                                anyCategoryF = true;
                                positionF.Add(j);
                            }
                            else if (_mNotamContainerList[i].CodeSecondThird[j].Substring(0, 1) == "A")
                            {
                                anyCategoryA = true;
                                positionA.Add(j);
                            }
                            else if (_mNotamContainerList[i].CodeSecondThird[j].Substring(0, 1) == "S")
                            {
                                anyCategoryS = true;
                                positionS.Add(j);
                            }
                            else if (_mNotamContainerList[i].CodeSecondThird[j].Substring(0, 1) == "P")
                            {
                                anyCategoryP = true;
                                positionP.Add(j);
                            }
                            else if (_mNotamContainerList[i].CodeSecondThird[j].Substring(0, 1) == "C")
                            {
                                anyCategoryC = true;
                                positionC.Add(j);
                            }
                            else if (_mNotamContainerList[i].CodeSecondThird[j].Substring(0, 1) == "I")
                            {
                                anyCategoryI = true;
                                positionI.Add(j);
                            }
                            else if (_mNotamContainerList[i].CodeSecondThird[j].Substring(0, 1) == "G")
                            {
                                anyCategoryG = true;
                                positionG.Add(j);
                            }
                            else if (_mNotamContainerList[i].CodeSecondThird[j].Substring(0, 1) == "N")
                            {
                                anyCategoryN = true;
                                positionN.Add(j);
                            }
                            else if (_mNotamContainerList[i].CodeSecondThird[j].Substring(0, 1) == "R")
                            {
                                anyCategoryR = true;
                                positionR.Add(j);
                            }
                            else if (_mNotamContainerList[i].CodeSecondThird[j].Substring(0, 1) == "W")
                            {
                                anyCategoryW = true;
                                positionW.Add(j);
                            }
                            else if (_mNotamContainerList[i].CodeSecondThird[j].Substring(0, 1) == "O")
                            {
                                anyCategoryO = true;
                                positionO.Add(j);
                            }
                            else
                            {
                                anyCategoryNotReported = true;
                                positionUnknown.Add(j);
                            }
                        }
                        else if (_mNotamContainerList[i].NotamD[j])
                        {
                            // Placeholder for USA D NOTAMS
                        }
                        else
                        {
                            // Raw Notams
                            anyRawNotam = true;
                            positionRaw.Add(j);
                        }
                    }

                    if (anyQNotam)
                    {
                        if (anyCategoryL)
                        {
                            TextView categoryTitleTextView = new TextView(Activity);
                            categoryTitleTextView.Text = "Lightning facilities";

                            Activity.RunOnUiThread(() => { _linearLayoutNotamLines.AddView(categoryTitleTextView); });

                            foreach (var p in positionL)
                            {
                                AddNotamQCard(i, p);
                            }
                        }

                        if (anyCategoryM)
                        {
                            GradientDrawable categoryTitleBackground = new GradientDrawable();
                            categoryTitleBackground.SetCornerRadius(8);
                            categoryTitleBackground.SetColor(new ApplyTheme().GetColor(DesiredColor.CardViews));
                            categoryTitleBackground.SetStroke(3, Color.Black);

                            TextView categoryTitleTextView = new TextView(Activity);
                            categoryTitleTextView.Text = "Movement and landing area";
                            categoryTitleTextView.Background = categoryTitleBackground;
                            categoryTitleTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.CyanText));
                            categoryTitleTextView.SetPadding(20, 5, 20, 5);
                            var categoryTitleTextViewParams =
                                new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                            categoryTitleTextViewParams.SetMargins(80, 20, 80, 10);
                            categoryTitleTextViewParams.Gravity = GravityFlags.Center;
                            categoryTitleTextView.LayoutParameters = categoryTitleTextViewParams;

                            Activity.RunOnUiThread(() => { _linearLayoutNotamLines.AddView(categoryTitleTextView); });

                            foreach (var p in positionM)
                            {
                                AddNotamQCard(i, p);
                            }
                        }

                        if (anyCategoryF)
                        {
                            GradientDrawable categoryTitleBackground = new GradientDrawable();
                            categoryTitleBackground.SetCornerRadius(8);
                            categoryTitleBackground.SetColor(new ApplyTheme().GetColor(DesiredColor.CardViews));
                            categoryTitleBackground.SetStroke(3, Color.Black);

                            TextView categoryTitleTextView = new TextView(Activity);
                            categoryTitleTextView.Text = "Facilities and services";
                            categoryTitleTextView.Background = categoryTitleBackground;
                            categoryTitleTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.CyanText));
                            categoryTitleTextView.SetPadding(20, 5, 20, 5);
                            var categoryTitleTextViewParams =
                                new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                            categoryTitleTextViewParams.SetMargins(80, 20, 80, 10);
                            categoryTitleTextViewParams.Gravity = GravityFlags.Center;
                            categoryTitleTextView.LayoutParameters = categoryTitleTextViewParams;

                            Activity.RunOnUiThread(() => { _linearLayoutNotamLines.AddView(categoryTitleTextView); });

                            foreach (var p in positionF)
                            {
                                AddNotamQCard(i, p);
                            }
                        }

                        if (anyCategoryA)
                        {
                            GradientDrawable categoryTitleBackground = new GradientDrawable();
                            categoryTitleBackground.SetCornerRadius(8);
                            categoryTitleBackground.SetColor(new ApplyTheme().GetColor(DesiredColor.CardViews));
                            categoryTitleBackground.SetStroke(3, Color.Black);

                            TextView categoryTitleTextView = new TextView(Activity);
                            categoryTitleTextView.Text = "Airspace organization";
                            categoryTitleTextView.Background = categoryTitleBackground;
                            categoryTitleTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.CyanText));
                            categoryTitleTextView.SetPadding(20, 5, 20, 5);
                            var categoryTitleTextViewParams =
                                new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                            categoryTitleTextViewParams.SetMargins(80, 20, 80, 10);
                            categoryTitleTextViewParams.Gravity = GravityFlags.Center;
                            categoryTitleTextView.LayoutParameters = categoryTitleTextViewParams;

                            Activity.RunOnUiThread(() => { _linearLayoutNotamLines.AddView(categoryTitleTextView); });

                            foreach (var p in positionA)
                            {
                                AddNotamQCard(i, p);
                            }
                        }

                        if (anyCategoryS)
                        {
                            GradientDrawable categoryTitleBackground = new GradientDrawable();
                            categoryTitleBackground.SetCornerRadius(8);
                            categoryTitleBackground.SetColor(new ApplyTheme().GetColor(DesiredColor.CardViews));
                            categoryTitleBackground.SetStroke(3, Color.Black);

                            TextView categoryTitleTextView = new TextView(Activity);
                            categoryTitleTextView.Text = "Air traffic and VOLMET";
                            categoryTitleTextView.Background = categoryTitleBackground;
                            categoryTitleTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.CyanText));
                            categoryTitleTextView.SetPadding(20, 5, 20, 5);
                            var categoryTitleTextViewParams =
                                new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                            categoryTitleTextViewParams.SetMargins(80, 20, 80, 10);
                            categoryTitleTextViewParams.Gravity = GravityFlags.Center;
                            categoryTitleTextView.LayoutParameters = categoryTitleTextViewParams;

                            Activity.RunOnUiThread(() => { _linearLayoutNotamLines.AddView(categoryTitleTextView); });

                            foreach (var p in positionS)
                            {
                                AddNotamQCard(i, p);
                            }
                        }

                        if (anyCategoryP)
                        {
                            GradientDrawable categoryTitleBackground = new GradientDrawable();
                            categoryTitleBackground.SetCornerRadius(8);
                            categoryTitleBackground.SetColor(new ApplyTheme().GetColor(DesiredColor.CardViews));
                            categoryTitleBackground.SetStroke(3, Color.Black);

                            TextView categoryTitleTextView = new TextView(Activity);
                            categoryTitleTextView.Text = "Air traffic procedures";
                            categoryTitleTextView.Background = categoryTitleBackground;
                            categoryTitleTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.CyanText));
                            categoryTitleTextView.SetPadding(20, 5, 20, 5);
                            var categoryTitleTextViewParams =
                                new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                            categoryTitleTextViewParams.SetMargins(80, 20, 80, 10);
                            categoryTitleTextViewParams.Gravity = GravityFlags.Center;
                            categoryTitleTextView.LayoutParameters = categoryTitleTextViewParams;

                            Activity.RunOnUiThread(() => { _linearLayoutNotamLines.AddView(categoryTitleTextView); });

                            foreach (var p in positionP)
                            {
                                AddNotamQCard(i, p);
                            }
                        }

                        if (anyCategoryC)
                        {
                            GradientDrawable categoryTitleBackground = new GradientDrawable();
                            categoryTitleBackground.SetCornerRadius(8);
                            categoryTitleBackground.SetColor(new ApplyTheme().GetColor(DesiredColor.CardViews));
                            categoryTitleBackground.SetStroke(3, Color.Black);

                            TextView categoryTitleTextView = new TextView(Activity);
                            categoryTitleTextView.Text = "Communications and surveillance";
                            categoryTitleTextView.Background = categoryTitleBackground;
                            categoryTitleTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.CyanText));
                            categoryTitleTextView.SetPadding(20, 5, 20, 5);
                            var categoryTitleTextViewParams =
                                new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                            categoryTitleTextViewParams.SetMargins(80, 20, 80, 10);
                            categoryTitleTextViewParams.Gravity = GravityFlags.Center;
                            categoryTitleTextView.LayoutParameters = categoryTitleTextViewParams;

                            Activity.RunOnUiThread(() => { _linearLayoutNotamLines.AddView(categoryTitleTextView); });

                            foreach (var p in positionC)
                            {
                                AddNotamQCard(i, p);
                            }
                        }

                        if (anyCategoryI)
                        {
                            GradientDrawable categoryTitleBackground = new GradientDrawable();
                            categoryTitleBackground.SetCornerRadius(8);
                            categoryTitleBackground.SetColor(new ApplyTheme().GetColor(DesiredColor.CardViews));
                            categoryTitleBackground.SetStroke(3, Color.Black);

                            TextView categoryTitleTextView = new TextView(Activity);
                            categoryTitleTextView.Text = "Instrument landing system";
                            categoryTitleTextView.Background = categoryTitleBackground;
                            categoryTitleTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.CyanText));
                            categoryTitleTextView.SetPadding(20, 5, 20, 5);
                            var categoryTitleTextViewParams =
                                new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                            categoryTitleTextViewParams.SetMargins(80, 20, 80, 10);
                            categoryTitleTextViewParams.Gravity = GravityFlags.Center;
                            categoryTitleTextView.LayoutParameters = categoryTitleTextViewParams;

                            Activity.RunOnUiThread(() => { _linearLayoutNotamLines.AddView(categoryTitleTextView); });

                            foreach (var p in positionI)
                            {
                                AddNotamQCard(i, p);
                            }
                        }

                        if (anyCategoryG)
                        {
                            GradientDrawable categoryTitleBackground = new GradientDrawable();
                            categoryTitleBackground.SetCornerRadius(8);
                            categoryTitleBackground.SetColor(new ApplyTheme().GetColor(DesiredColor.CardViews));
                            categoryTitleBackground.SetStroke(3, Color.Black);

                            TextView categoryTitleTextView = new TextView(Activity);
                            categoryTitleTextView.Text = "GNSS services";
                            categoryTitleTextView.Background = categoryTitleBackground;
                            categoryTitleTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.CyanText));
                            categoryTitleTextView.SetPadding(20, 5, 20, 5);
                            var categoryTitleTextViewParams =
                                new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                            categoryTitleTextViewParams.SetMargins(80, 20, 80, 10);
                            categoryTitleTextViewParams.Gravity = GravityFlags.Center;
                            categoryTitleTextView.LayoutParameters = categoryTitleTextViewParams;

                            Activity.RunOnUiThread(() => { _linearLayoutNotamLines.AddView(categoryTitleTextView); });

                            foreach (var p in positionG)
                            {
                                AddNotamQCard(i, p);
                            }
                        }

                        if (anyCategoryN)
                        {
                            GradientDrawable categoryTitleBackground = new GradientDrawable();
                            categoryTitleBackground.SetCornerRadius(8);
                            categoryTitleBackground.SetColor(new ApplyTheme().GetColor(DesiredColor.CardViews));
                            categoryTitleBackground.SetStroke(3, Color.Black);

                            TextView categoryTitleTextView = new TextView(Activity);
                            categoryTitleTextView.Text = "Terminal and en-route navaids";
                            categoryTitleTextView.Background = categoryTitleBackground;
                            categoryTitleTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.CyanText));
                            categoryTitleTextView.SetPadding(20, 5, 20, 5);
                            var categoryTitleTextViewParams =
                                new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                            categoryTitleTextViewParams.SetMargins(80, 20, 80, 10);
                            categoryTitleTextViewParams.Gravity = GravityFlags.Center;
                            categoryTitleTextView.LayoutParameters = categoryTitleTextViewParams;

                            Activity.RunOnUiThread(() => { _linearLayoutNotamLines.AddView(categoryTitleTextView); });

                            foreach (var p in positionN)
                            {
                                AddNotamQCard(i, p);
                            }
                        }

                        if (anyCategoryR)
                        {
                            GradientDrawable categoryTitleBackground = new GradientDrawable();
                            categoryTitleBackground.SetCornerRadius(8);
                            categoryTitleBackground.SetColor(new ApplyTheme().GetColor(DesiredColor.CardViews));
                            categoryTitleBackground.SetStroke(3, Color.Black);

                            TextView categoryTitleTextView = new TextView(Activity);
                            categoryTitleTextView.Text = "Airspace restrictions";
                            categoryTitleTextView.Background = categoryTitleBackground;
                            categoryTitleTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.CyanText));
                            categoryTitleTextView.SetPadding(20, 5, 20, 5);
                            var categoryTitleTextViewParams =
                                new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                            categoryTitleTextViewParams.SetMargins(80, 20, 80, 10);
                            categoryTitleTextViewParams.Gravity = GravityFlags.Center;
                            categoryTitleTextView.LayoutParameters = categoryTitleTextViewParams;

                            Activity.RunOnUiThread(() => { _linearLayoutNotamLines.AddView(categoryTitleTextView); });

                            foreach (var p in positionR)
                            {
                                AddNotamQCard(i, p);
                            }
                        }

                        if (anyCategoryW)
                        {
                            GradientDrawable categoryTitleBackground = new GradientDrawable();
                            categoryTitleBackground.SetCornerRadius(8);
                            categoryTitleBackground.SetColor(new ApplyTheme().GetColor(DesiredColor.CardViews));
                            categoryTitleBackground.SetStroke(3, Color.Black);

                            TextView categoryTitleTextView = new TextView(Activity);
                            categoryTitleTextView.Text = "Warnings";
                            categoryTitleTextView.Background = categoryTitleBackground;
                            categoryTitleTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.CyanText));
                            categoryTitleTextView.SetPadding(20, 5, 20, 5);
                            var categoryTitleTextViewParams =
                                new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                            categoryTitleTextViewParams.SetMargins(80, 20, 80, 10);
                            categoryTitleTextViewParams.Gravity = GravityFlags.Center;
                            categoryTitleTextView.LayoutParameters = categoryTitleTextViewParams;

                            Activity.RunOnUiThread(() => { _linearLayoutNotamLines.AddView(categoryTitleTextView); });

                            foreach (var p in positionW)
                            {
                                AddNotamQCard(i, p);
                            }
                        }

                        if (anyCategoryO)
                        {
                            GradientDrawable categoryTitleBackground = new GradientDrawable();
                            categoryTitleBackground.SetCornerRadius(8);
                            categoryTitleBackground.SetColor(new ApplyTheme().GetColor(DesiredColor.CardViews));
                            categoryTitleBackground.SetStroke(3, Color.Black);

                            TextView categoryTitleTextView = new TextView(Activity);
                            categoryTitleTextView.Text = "Other information";
                            categoryTitleTextView.Background = categoryTitleBackground;
                            categoryTitleTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.CyanText));
                            categoryTitleTextView.SetPadding(20, 5, 20, 5);
                            var categoryTitleTextViewParams =
                                new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                            categoryTitleTextViewParams.SetMargins(80, 20, 80, 10);
                            categoryTitleTextViewParams.Gravity = GravityFlags.Center;
                            categoryTitleTextView.LayoutParameters = categoryTitleTextViewParams;

                            Activity.RunOnUiThread(() => { _linearLayoutNotamLines.AddView(categoryTitleTextView); });

                            foreach (var p in positionO)
                            {
                                AddNotamQCard(i, p);
                            }
                        }

                        if (anyCategoryNotReported)
                        {
                            GradientDrawable categoryTitleBackground = new GradientDrawable();
                            categoryTitleBackground.SetCornerRadius(8);
                            categoryTitleBackground.SetColor(new ApplyTheme().GetColor(DesiredColor.CardViews));
                            categoryTitleBackground.SetStroke(3, Color.Black);

                            TextView categoryTitleTextView = new TextView(Activity);
                            categoryTitleTextView.Text = "(category not reported)";
                            categoryTitleTextView.Background = categoryTitleBackground;
                            categoryTitleTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.CyanText));
                            categoryTitleTextView.SetPadding(20, 5, 20, 5);
                            var categoryTitleTextViewParams =
                                new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                            categoryTitleTextViewParams.SetMargins(80, 20, 80, 10);
                            categoryTitleTextViewParams.Gravity = GravityFlags.Center;
                            categoryTitleTextView.LayoutParameters = categoryTitleTextViewParams;

                            Activity.RunOnUiThread(() => { _linearLayoutNotamLines.AddView(categoryTitleTextView); });

                            foreach (var p in positionUnknown)
                            {
                                AddNotamQCard(i, p);
                            }
                        }

                        if (anyRawNotam)
                        {
                            GradientDrawable categoryTitleBackground = new GradientDrawable();
                            categoryTitleBackground.SetCornerRadius(8);
                            categoryTitleBackground.SetColor(new ApplyTheme().GetColor(DesiredColor.CardViews));
                            categoryTitleBackground.SetStroke(3, Color.Black);

                            TextView categoryTitleTextView = new TextView(Activity);
                            categoryTitleTextView.Text = "(raw NOTAM)";
                            categoryTitleTextView.Background = categoryTitleBackground;
                            categoryTitleTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.CyanText));
                            categoryTitleTextView.SetPadding(20, 5, 20, 5);
                            var categoryTitleTextViewParams =
                                new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                            categoryTitleTextViewParams.SetMargins(80, 20, 80, 10);
                            categoryTitleTextViewParams.Gravity = GravityFlags.Center;
                            categoryTitleTextView.LayoutParameters = categoryTitleTextViewParams;

                            Activity.RunOnUiThread(() => { _linearLayoutNotamLines.AddView(categoryTitleTextView); });

                            foreach (var p in positionRaw)
                            {
                                AddRawNotamsCard(i, p);
                            }
                        }

                    }
                    else if (anyDNotam)
                    {
                        // Placeholder for USA D NOTAMS
                    }
                    else
                    {
                        // Raw Notam
                        GradientDrawable categoryTitleBackground = new GradientDrawable();
                        categoryTitleBackground.SetCornerRadius(8);
                        categoryTitleBackground.SetColor(new ApplyTheme().GetColor(DesiredColor.CardViews));
                        categoryTitleBackground.SetStroke(3, Color.Black);

                        TextView categoryTitleTextView = new TextView(Activity);
                        categoryTitleTextView.Text = "(raw NOTAM)";
                        categoryTitleTextView.Background = categoryTitleBackground;
                        categoryTitleTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.CyanText));
                        categoryTitleTextView.SetPadding(20, 5, 20, 5);
                        var categoryTitleTextViewParams =
                            new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                        categoryTitleTextViewParams.SetMargins(80, 20, 80, 10);
                        categoryTitleTextViewParams.Gravity = GravityFlags.Center;
                        categoryTitleTextView.LayoutParameters = categoryTitleTextViewParams;

                        Activity.RunOnUiThread(() => { _linearLayoutNotamLines.AddView(categoryTitleTextView); });
                        
                        foreach (var p in positionRaw)
                        {
                            AddRawNotamsCard(i, p);
                        }
                    }
                }
                catch
                {
                    // If error showing, show Raw
                    for (int j = 0; j < _mNotamContainerList[i].NotamRaw.Count; j++)
                        AddRawNotamsCard(i, j);
                }
            }

            void LocalAddNotamsByDate(int i)
            {
                for (int j = 0; j < _mNotamContainerList[i].NotamRaw.Count; j++)
                {
                    // It's Q
                    if (_mNotamContainerList[i].NotamQ[j])
                    {
                        try
                        {
                            AddNotamQCard(i, j);
                        }
                        catch
                        {
                            // If error showing Q, show Raw
                            AddRawNotamsCard(i, j);
                        }
                    }
                    // It's D
                    else if (_mNotamContainerList[i].NotamD[j])
                    {
                        // Placeholder for USA D NOTAMS
                    }
                    // It's raw
                    else
                    {
                        AddRawNotamsCard(i, j);
                    }
                }
            }
        }

        private void ShowConnectionError()
        {
            TextView errorTextView = new TextView(Activity);
            errorTextView.Text = Resources.GetString(Resource.String.NOTAM_connectionError);
            errorTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.RedTextWarning));
            errorTextView.SetTextSize(ComplexUnitType.Dip, 14);
            errorTextView.Gravity = GravityFlags.Center;
            var errorTextViewParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            errorTextViewParams.SetMargins(0, 50, 0, 0);
            errorTextView.LayoutParameters = errorTextViewParams;

            // Adding view
            Activity.RunOnUiThread(() =>
            {
                _linearLayoutNotamLines.AddView(errorTextView);
            });
        }

        private void AddRequestedTime()
        {
            string utcStringBeginning = "* " + Resources.GetString(Resource.String.NOTAM_requested);
            string justNow = Resources.GetString(Resource.String.time_just_now) + " *";

            string utcString = $"{utcStringBeginning} {justNow}";

            _mUtcTextView = new TextView(Activity);
            _mUtcTextView.Text = utcString;
            _mUtcTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.GreenText));
            _mUtcTextView.SetTextSize(ComplexUnitType.Dip, 14);
            _mUtcTextView.Gravity = GravityFlags.Center;
            var utcTextViewParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            utcTextViewParams.SetMargins(0, 50, 0, 0);
            _mUtcTextView.LayoutParameters = utcTextViewParams;

            // Adding view
            Activity.RunOnUiThread(() =>
            {
                _linearLayoutNotamLines.AddView(_mUtcTextView);
            });
        }

        private void AddAirportName(int i)
        {
            TextView airportName = new TextView(Activity);
            
            // Try to get the airport's name from existing _myAirportDefinition List
            bool foundAirportIcao = false;
            try
            {
                for (var j = 0; j < _mAirportDefinitions.Count; j++)
                    if (_mAirportDefinitions[j].icao == _mRequestedAirportsByIcao[i].ToUpper())
                    {
                        airportName.Text = _mRequestedAirportsRawString[i].ToUpper() + " - " + _mAirportDefinitions[j].description;
                        foundAirportIcao = true;
                        break;
                    }
            }
            finally
            {
                if (!foundAirportIcao) airportName.Text = _mRequestedAirportsRawString[i].ToUpper();
            }

            // Styling
            airportName.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MagentaText));
            airportName.SetTextSize(ComplexUnitType.Dip, 16);
            LinearLayout.LayoutParams airportTextViewParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            airportTextViewParams.SetMargins(0, 50, 0, 20);
            airportName.LayoutParameters = airportTextViewParams;

            // Adding view
            Activity.RunOnUiThread(() =>
            {
                _linearLayoutNotamLines.AddView(airportName);
            });

        }

        private void AddErrorCard()
        {
            CardView notamCard = new CardView(Activity);
            TextView notamLine = new TextView(Activity);

            notamLine.Text = Resources.GetString(Resource.String.Notam_not_found);

            // Styling text
            notamLine.SetTextColor(new ApplyTheme().GetColor(DesiredColor.RedTextWarning));
            notamLine.SetTextSize(ComplexUnitType.Dip, 12);
            notamLine.SetPadding(30, 30, 15, 30);

            // Styling cards
            notamCard.SetBackgroundColor(new ApplyTheme().GetColor(DesiredColor.LightYellowBackground));
            notamCard.Elevation = 5.0f;
            var cardViewParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            cardViewParams.SetMargins(10, 10, 10, 10);
            notamCard.LayoutParameters = cardViewParams;

            // Adding view
            Activity.RunOnUiThread(() =>
            {
                notamCard.AddView(notamLine);
                _linearLayoutNotamLines.AddView(notamCard);
            });
        }

        private void AddNotamQCard(int i, int j)
        {
            // Style card and RelativeLayout
            CardView notamCard = LocalStyleCard();

            // Styling MainLayout
            LinearLayout notamLayoutContainer = LocalStyleContainer();

            // Styling notamId
            RelativeLayout topLayout = LocalStyleTopLayout();
            
            // Styling notamFreeText
            LinearLayout notamFreeTextLayout = LocalStyleFreeText();

            RelativeLayout toFromRelativeLayout = LocalTimeFromTo(_mNotamContainerList[i].StartTime[j],
                                                   _mNotamContainerList[i].EndTime[j]);

            RelativeLayout spanRelativeLayout = LocalTimeSpan();

            RelativeLayout bottomToTopRelativeLayout = LocalBottomToTop();

            // Adding view
            Activity.RunOnUiThread(() =>
            {
                notamCard.AddView(notamLayoutContainer);
                notamLayoutContainer.AddView(topLayout);
                notamLayoutContainer.AddView(notamFreeTextLayout);
                notamLayoutContainer.AddView(toFromRelativeLayout);
                notamLayoutContainer.AddView(spanRelativeLayout);
                notamLayoutContainer.AddView(bottomToTopRelativeLayout);
                _linearLayoutNotamLines.AddView(notamCard);
            });

            // ** Local functions for styling ** //
            
            CardView LocalStyleCard()
            {
                CardView cardView = new CardView(Activity);
                cardView.SetBackgroundColor(new ApplyTheme().GetColor(DesiredColor.CardViews));
                cardView.Elevation = 5.0f;
                var cardViewParams =
                    new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                cardViewParams.SetMargins(10, 10, 10, 20);
                cardView.LayoutParameters = cardViewParams;

                return cardView;
            }

            LinearLayout LocalStyleContainer()
            {
                LinearLayout myContainerLayout = new LinearLayout(Activity);
                myContainerLayout.Orientation = Orientation.Vertical;
                FrameLayout.LayoutParams myContainerLayoutParams =
                    new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
                myContainerLayoutParams.SetMargins(0, 0, 0, 30);
                myContainerLayout.LayoutParameters = myContainerLayoutParams;

                return myContainerLayout;
            }

            RelativeLayout LocalStyleTopLayout()
            {
                RelativeLayout myTopLayout = new RelativeLayout(Activity);

                TextView notamIdTextView = new TextView(Activity);
                notamIdTextView.Id = 1;

                ClickableSpan myClickableSpan = new ClickableSpan(_mNotamContainerList[i].NotamId[j]);

                myClickableSpan.ClickedMyClickableSpan += delegate
                {
                    Activity.RunOnUiThread(() =>
                    {
                        // Pull up dialog
                        var transaction = FragmentManager.BeginTransaction();
                        var notamRawDialog = new NotamDialogRaw(_mNotamContainerList[i].NotamId[j], _mNotamContainerList[i].NotamRaw[j]);
                        notamRawDialog.Show(transaction, "notamRawDialog");
                    });
                };

                SpannableString idSpan = new SpannableString(_mNotamContainerList[i].NotamId[j]);
                idSpan.SetSpan(myClickableSpan, 0, idSpan.Length(), 0);
                idSpan.SetSpan(new UnderlineSpan(), 0, idSpan.Length(), 0);
                idSpan.SetSpan(new ForegroundColorSpan(new ApplyTheme().GetColor(DesiredColor.CyanText)), 0, idSpan.Length(), 0);

                notamIdTextView.TextFormatted = idSpan;
                notamIdTextView.MovementMethod = new LinkMovementMethod();

                notamIdTextView.SetTextSize(ComplexUnitType.Dip, 13);
                notamIdTextView.SetPadding(30, 30, 15, 0);

                myTopLayout.AddView(notamIdTextView);
                
                if (_mNotamContainerList[i].Latitude[j] != 9999)
                {
                    ImageView myWorldMap = new ImageView(Activity);

                    myWorldMap.Id = 2;

                    myWorldMap.SetImageResource(Resource.Drawable.ic_world_map);

                    RelativeLayout.LayoutParams worldMapIconParams =
                        new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, (ViewGroup.LayoutParams.WrapContent));
                    worldMapIconParams.AddRule(LayoutRules.AlignParentRight);
                    worldMapIconParams.Height = Resources.GetDimensionPixelSize(Resource.Dimension.dimen_entry_in_dp_35);
                    worldMapIconParams.Width = Resources.GetDimensionPixelSize(Resource.Dimension.dimen_entry_in_dp_35);
                    worldMapIconParams.SetMargins(0, 10, 15, 25);
                    myWorldMap.LayoutParameters = worldMapIconParams;

                    myTopLayout.AddView(myWorldMap);

                    myWorldMap.Click += delegate
                    {
                        var transaction = FragmentManager.BeginTransaction();
                        var notamRawMap = new NotamDialogMap(_mNotamContainerList[i].NotamId[j],
                            _mNotamContainerList[i].Latitude[j],
                            _mNotamContainerList[i].Longitude[j],
                            _mNotamContainerList[i].Radius[j]);
                        notamRawMap.Show(transaction, "notamRawDialog");
                    };
                }

                return myTopLayout;
            }            
            
            LinearLayout LocalStyleFreeText()
            {
                LinearLayout freeTextLayout = new LinearLayout(Activity);
                freeTextLayout.Orientation = Orientation.Vertical;

                LinearLayout.LayoutParams freeTextLayoutParams = 
                    new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
                freeTextLayout.LayoutParameters = freeTextLayoutParams;

                TextView notamFreeText = new TextView(Activity);
                notamFreeText.Text = _mNotamContainerList[i].NotamFreeText[j];
                notamFreeText.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
                notamFreeText.SetTextSize(ComplexUnitType.Dip, 12);
                notamFreeText.SetPadding(30, 30, 15, 10);

                freeTextLayout.AddView(notamFreeText);

                return freeTextLayout;
            }

            RelativeLayout LocalTimeFromTo(DateTime myTimeStart, DateTime myTimeEnd)
            {
                RelativeLayout myBaseTimeLayout = new RelativeLayout(Activity);
                LinearLayout.LayoutParams myBaseTimeLayoutParams = 
                    new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, (ViewGroup.LayoutParams.WrapContent));
                myBaseTimeLayoutParams.SetMargins(30, 20, 0, 0);
                myBaseTimeLayout.LayoutParameters = myBaseTimeLayoutParams;
                
                ImageView calendarIcon = new ImageView(Activity);
                calendarIcon.Id = 1;
                calendarIcon.SetImageResource(Resource.Drawable.ic_calendar_multiple_black_48dp);
                RelativeLayout.LayoutParams calendarIconParams =
                    new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, (ViewGroup.LayoutParams.WrapContent));
                calendarIconParams.Height = Resources.GetDimensionPixelSize(Resource.Dimension.dimen_entry_in_dp_20);
                calendarIconParams.Width = Resources.GetDimensionPixelSize(Resource.Dimension.dimen_entry_in_dp_20);
                calendarIconParams.SetMargins(0, 0, 20, 0);
                calendarIconParams.AddRule(LayoutRules.CenterVertical);
                calendarIcon.LayoutParameters = calendarIconParams;
                DateTime timeNow = DateTime.UtcNow;
                if (timeNow > myTimeStart && timeNow < myTimeEnd)
                {
                    calendarIcon.SetImageResource(Resource.Drawable.ic_calendar_multiple_red_48dp);
                }
                else
                {
                    calendarIcon.SetImageResource(Resource.Drawable.ic_calendar_multiple_black_48dp);
                }
                // Save view in order to update the calendar color later
                _mCalendarViews.Add(calendarIcon);
                mStartDateTimes.Add(myTimeStart);
                mEnDateTimes.Add(myTimeEnd);

                TextView myStartTimeEditText = new TextView(Activity);
                myStartTimeEditText.Id = 2;
                myStartTimeEditText.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
                myStartTimeEditText.SetTextSize(ComplexUnitType.Dip, 11);
                RelativeLayout.LayoutParams myStartTimeEditTextParams = 
                    new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                myStartTimeEditTextParams.SetMargins(0, 0, 0, 0);
                myStartTimeEditTextParams.AddRule(LayoutRules.RightOf, calendarIcon.Id);
                myStartTimeEditTextParams.AddRule(LayoutRules.CenterVertical);
                myStartTimeEditText.LayoutParameters = myStartTimeEditTextParams;
                myStartTimeEditText.Text = myTimeStart.ToString("dd") + "-" +
                                           myTimeStart.ToString("MMM") + "-" +
                                           myTimeStart.ToString("yy") + " " +
                                           myTimeStart.ToString("HH") + ":" +
                                           myTimeStart.ToString("mm");

                ImageView arrowIcon = new ImageView(Activity);
                arrowIcon.Id = 3;
                arrowIcon.SetImageResource(Resource.Drawable.ic_menu_right_black_48dp);
                RelativeLayout.LayoutParams arrowIconParams =
                    new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, (ViewGroup.LayoutParams.WrapContent));
                arrowIconParams.Height = Resources.GetDimensionPixelSize(Resource.Dimension.dimen_entry_in_dp_20);
                arrowIconParams.Width = Resources.GetDimensionPixelSize(Resource.Dimension.dimen_entry_in_dp_20);
                arrowIconParams.SetMargins(8, 0, 8, 0);
                arrowIconParams.AddRule(LayoutRules.RightOf, myStartTimeEditText.Id);
                arrowIconParams.AddRule(LayoutRules.CenterVertical);
                arrowIcon.LayoutParameters = arrowIconParams;

                TextView myEndTimeEditText = new TextView(Activity);
                myEndTimeEditText.Id = 4;
                myEndTimeEditText.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
                myEndTimeEditText.SetTextSize(ComplexUnitType.Dip, 11);
                RelativeLayout.LayoutParams myEndTimeEditTextParams =
                    new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                myEndTimeEditTextParams.SetMargins(0, 0, 0, 0);
                myEndTimeEditTextParams.AddRule(LayoutRules.RightOf, arrowIcon.Id);
                myEndTimeEditTextParams.AddRule(LayoutRules.CenterVertical);
                myEndTimeEditText.LayoutParameters = myEndTimeEditTextParams;
                string myEndTimeString = String.Empty;
                if (!_mNotamContainerList[i].CEstimated[j] && !_mNotamContainerList[i].CPermanent[j])
                {
                    myEndTimeString = myTimeEnd.ToString("dd") + "-" +
                           myTimeEnd.ToString("MMM") + "-" +
                           myTimeEnd.ToString("yy") + " " +
                           myTimeEnd.ToString("HH") + ":" +
                           myTimeEnd.ToString("mm");
                }
                else if (_mNotamContainerList[i].CEstimated[j])
                {
                    myEndTimeString = myTimeEnd.ToString("dd") + "-" +
                           myTimeEnd.ToString("MMM") + "-" +
                           myTimeEnd.ToString("yy") + " " +
                           myTimeEnd.ToString("HH") + ":" +
                           myTimeEnd.ToString("mm") + " (ESTIMATED)";
                }
                else if (_mNotamContainerList[i].CPermanent[j])
                {
                    myEndTimeString = "PERMANENT";
                }
                myEndTimeEditText.Text = myEndTimeString;

                myBaseTimeLayout.AddView(calendarIcon);
                myBaseTimeLayout.AddView(myStartTimeEditText);
                myBaseTimeLayout.AddView(arrowIcon);
                myBaseTimeLayout.AddView(myEndTimeEditText);

                return myBaseTimeLayout;
            }

            RelativeLayout LocalTimeSpan()
            {
                RelativeLayout myBaseSpanLayout = new RelativeLayout(Activity);

                if (_mNotamContainerList[i].Span[j] != String.Empty)
                {
                    LinearLayout.LayoutParams myBaseSpanLayoutParams = 
                        new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, (ViewGroup.LayoutParams.WrapContent));
                    myBaseSpanLayoutParams.SetMargins(30, 20, 0, 0);
                    myBaseSpanLayout.LayoutParameters = myBaseSpanLayoutParams;

                    ImageView spanClockIcon = new ImageView(Activity);
                    spanClockIcon.SetImageResource(Resource.Drawable.ic_clock_black_48dp);
                    spanClockIcon.Id = 1;
                    RelativeLayout.LayoutParams spanClockIconParams =
                        new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, (ViewGroup.LayoutParams.WrapContent));
                    spanClockIconParams.Height = Resources.GetDimensionPixelSize(Resource.Dimension.dimen_entry_in_dp_20);
                    spanClockIconParams.Width = Resources.GetDimensionPixelSize(Resource.Dimension.dimen_entry_in_dp_20);
                    spanClockIconParams.SetMargins(0, 0, 20, 0);
                    spanClockIconParams.AddRule(LayoutRules.CenterVertical);
                    spanClockIcon.LayoutParameters = spanClockIconParams;

                    TextView spanText = new TextView(Activity);
                    spanText.Id = 2;
                    spanText.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
                    spanText.SetTextSize(ComplexUnitType.Dip, 11);
                    RelativeLayout.LayoutParams spanTextParams =
                        new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                    spanTextParams.SetMargins(0, 0, 0, 0);
                    spanTextParams.AddRule(LayoutRules.RightOf,spanClockIcon.Id);
                    spanTextParams.AddRule(LayoutRules.CenterVertical);
                    spanText.LayoutParameters = spanTextParams;
                    spanText.Text = _mNotamContainerList[i].Span[j];

                    myBaseSpanLayout.AddView(spanClockIcon);
                    myBaseSpanLayout.AddView(spanText);
                }

                return myBaseSpanLayout;
            }

            RelativeLayout LocalBottomToTop()
            {
                RelativeLayout myBottomTopLayout = new RelativeLayout(Activity);

                if (_mNotamContainerList[i].BottomLimit[j] != String.Empty ||
                    _mNotamContainerList[i].TopLimit[j] != String.Empty)
                {
                    
                    LinearLayout.LayoutParams myBottomTopLayoutParams =
                        new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, (ViewGroup.LayoutParams.WrapContent));
                    myBottomTopLayoutParams.SetMargins(30, 20, 0, 0);
                    myBottomTopLayout.LayoutParameters = myBottomTopLayoutParams;

                    ImageView bottomIcon = new ImageView(Activity);
                    bottomIcon.Id = 1;
                    bottomIcon.SetImageResource(Resource.Drawable.ic_format_vertical_align_bottom_black_48dp);
                    RelativeLayout.LayoutParams bottomIconParams =
                        new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, (ViewGroup.LayoutParams.WrapContent));
                    bottomIconParams.Height = Resources.GetDimensionPixelSize(Resource.Dimension.dimen_entry_in_dp_20);
                    bottomIconParams.Width = Resources.GetDimensionPixelSize(Resource.Dimension.dimen_entry_in_dp_20);
                    bottomIconParams.SetMargins(0, 0, 0, 0);
                    bottomIconParams.AddRule(LayoutRules.CenterVertical);
                    bottomIcon.LayoutParameters = bottomIconParams;

                    TextView myBottomTextView = new TextView(Activity);
                    myBottomTextView.Id = 2;
                    myBottomTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
                    myBottomTextView.SetTextSize(ComplexUnitType.Dip, 11);
                    RelativeLayout.LayoutParams myBottomTextViewParams =
                        new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                    myBottomTextViewParams.SetMargins(10, 0, 0, 0);
                    myBottomTextViewParams.AddRule(LayoutRules.RightOf, bottomIcon.Id);
                    myBottomTextViewParams.AddRule(LayoutRules.CenterVertical);
                    myBottomTextView.LayoutParameters = myBottomTextViewParams;
                    myBottomTextView.Text = _mNotamContainerList[i].BottomLimit[j];

                    ImageView topIcon = new ImageView(Activity);
                    topIcon.Id = 3;
                    topIcon.SetImageResource(Resource.Drawable.ic_format_vertical_align_top_black_48dp);
                    RelativeLayout.LayoutParams topIconParams =
                        new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, (ViewGroup.LayoutParams.WrapContent));
                    topIconParams.Height = Resources.GetDimensionPixelSize(Resource.Dimension.dimen_entry_in_dp_20);
                    topIconParams.Width = Resources.GetDimensionPixelSize(Resource.Dimension.dimen_entry_in_dp_20);
                    topIconParams.SetMargins(40, 0, 0, 0);
                    topIconParams.AddRule(LayoutRules.RightOf, myBottomTextView.Id);
                    topIconParams.AddRule(LayoutRules.CenterVertical);
                    topIcon.LayoutParameters = topIconParams;

                    TextView myTopTextView = new TextView(Activity);
                    myTopTextView.Id = 4;
                    myTopTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
                    myTopTextView.SetTextSize(ComplexUnitType.Dip, 11);
                    RelativeLayout.LayoutParams myTopTextViewParams =
                        new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                    myTopTextViewParams.SetMargins(10, 0, 0, 0);
                    myTopTextViewParams.AddRule(LayoutRules.RightOf, topIcon.Id);
                    myTopTextViewParams.AddRule(LayoutRules.CenterVertical);
                    myTopTextView.LayoutParameters = myTopTextViewParams;
                    myTopTextView.Text = _mNotamContainerList[i].TopLimit[j];


                    myBottomTopLayout.AddView(bottomIcon);
                    myBottomTopLayout.AddView(myBottomTextView);
                    myBottomTopLayout.AddView(topIcon);
                    myBottomTopLayout.AddView(myTopTextView);

                    return myBottomTopLayout;
                }

                return myBottomTopLayout;
            }
        }

        private void AddRawNotamsCard(int i, int j)
        {
            CardView notamCard = new CardView(Activity);
            TextView notamLine = new TextView(Activity);

            notamLine.Text = _mNotamContainerList[i].NotamRaw[j];

            // Styling text
            notamLine.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            notamLine.SetTextSize(ComplexUnitType.Dip, 12);
            notamLine.SetPadding(30, 30, 15, 0);
                        
            // Styling cards
            notamCard.SetBackgroundColor(new ApplyTheme().GetColor(DesiredColor.CardViews));
            notamCard.Elevation = 5.0f;
            var cardViewParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            cardViewParams.SetMargins(10, 10, 10, 10);
            notamCard.LayoutParameters = cardViewParams;

            // Adding view
            Activity.RunOnUiThread(() =>
            {
                notamCard.AddView(notamLine);
                _linearLayoutNotamLines.AddView(notamCard);
            });
        }

        private void StyleViews()
        {
            _coordinatorLayout = _thisView.FindViewById<CoordinatorLayout>(Resource.Id.cl);
            _fabScrollTop = new FloatingActionButton(Activity);
            _fabScrollTop.SetImageResource(Resource.Drawable.ic_arrow_up_bold_white_48dp);
            _scrollViewContainer = _thisView.FindViewById<ScrollView>(Resource.Id.notam_fragment_container);

            _linearlayoutBottom = _thisView.FindViewById<LinearLayout>(Resource.Id.notam_linearlayout_bottom);
            _linearlayoutBottom.SetBackgroundColor(new ApplyTheme().GetColor(DesiredColor.MainBackground));
            
            _chooseIDtextview = _thisView.FindViewById<TextView>(Resource.Id.notam_choose_id_textview);
            _chooseIDtextview.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));

            _airportEntryEditText = _thisView.FindViewById<EditText>(Resource.Id.notam_airport_entry);
            _notamRequestButton = _thisView.FindViewById<Button>(Resource.Id.notam_request_button);
            _notamClearButton = _thisView.FindViewById<Button>(Resource.Id.notam_clear_button);
            _linearLayoutNotamLines = _thisView.FindViewById<LinearLayout>(Resource.Id.notam_linearlayout_lines);

            _notamRequestButton.Text = Resources.GetString(Resource.String.Send_button);
            _notamClearButton.Text = Resources.GetString(Resource.String.Clear_button);
            _chooseIDtextview.Text = Resources.GetString(Resource.String.NOTAM_ID_TextView);
            _airportEntryEditText.Hint = Resources.GetString(Resource.String.Icao_Or_Iata);
        }

        private void SaveData()
        {
            var notamDestroy = Application.Context.GetSharedPreferences("NOTAM_OnPause", FileCreationMode.Private);

            // Save ICAO ID LIST
            notamDestroy.Edit().PutString("_airportEntryEditText", _airportEntryEditText.Text).Apply();

            // Save AIRPORT IDs
            var notamContainer = JsonConvert.SerializeObject(_mNotamContainerList);
            notamDestroy.Edit().PutString("notamContainer", notamContainer).Apply();

            var requestedUtc = JsonConvert.SerializeObject(_mUtcRequestTime);
            notamDestroy.Edit().PutString("requestedUtc", requestedUtc).Apply();

            var airportsByIcao = JsonConvert.SerializeObject(_mRequestedAirportsByIcao);
            notamDestroy.Edit().PutString("airportsICAO", airportsByIcao).Apply();

            var airportsRaw = JsonConvert.SerializeObject(_mRequestedAirportsRawString);
            notamDestroy.Edit().PutString("airportsRaw", airportsRaw).Apply();
        }

        private void RecallSavedData()
        {
            ISharedPreferences notamPrefs = Application.Context.GetSharedPreferences("NOTAM_OnPause", FileCreationMode.Private);

            // Airport Text
            var airportEntryEditText = notamPrefs.GetString("_airportEntryEditText", String.Empty);

            // We will only get saved data if it exists at all, otherwise we could trigger
            // the event "aftertextchanged" for _airportEntryEditText and we would like to avoid that
            if (airportEntryEditText != string.Empty)
            {
                _airportEntryEditText.Text = airportEntryEditText.ToUpper();
            }

            // Make sure there are values != null, in order to avoid assigning null!
            var deserializeNotamContainer = JsonConvert.DeserializeObject<List<NotamContainer>>(notamPrefs.GetString("notamContainer", string.Empty));
            if (deserializeNotamContainer != null)
            {
                _mNotamContainerList = deserializeNotamContainer;

                var deserializeRequestedUtc = JsonConvert.DeserializeObject<DateTime>(notamPrefs.GetString("requestedUtc", string.Empty));
                _mUtcRequestTime = deserializeRequestedUtc;

                var deserializeAirportsByIcao = JsonConvert.DeserializeObject<List<String>>(notamPrefs.GetString("airportsICAO", string.Empty));
                _mRequestedAirportsByIcao = deserializeAirportsByIcao;

                var deserializeAirportsRawString = JsonConvert.DeserializeObject<List<String>>(notamPrefs.GetString("airportsRaw", string.Empty));
                _mRequestedAirportsRawString = deserializeAirportsRawString;

                ShowNotams();
            }
        }

        private void TimeTick()
        {
            // Update requested UTC time
            var timerDelegate = new TimerCallback(UpdateRequestedTime);
            var utcUpdateTimer = new Timer(timerDelegate, null, 0, 30000);

            var timerDelegateCalendar = new TimerCallback(UpdateCalendarColors);
            var utcUpdateTimerCalendar = new Timer(timerDelegateCalendar, null, 0, 60000);
        }

        // Update calendar colors on timer tick
        private void UpdateCalendarColors(object state)
        {
            if (_thisView.IsAttachedToWindow && _mCalendarViews.Count > 0)
            {
                try
                {
                    Activity.RunOnUiThread(() =>
                    {
                        DateTime timeNow = DateTime.UtcNow;
                        for (int i = 0; i < _mCalendarViews.Count; i++)
                        {
                            if (timeNow > mStartDateTimes[i] && timeNow < mEnDateTimes[i])
                            {
                                _mCalendarViews[i].SetImageResource(Resource.Drawable.ic_calendar_multiple_red_48dp);
                            }
                            else
                            {
                                _mCalendarViews[i].SetImageResource(Resource.Drawable.ic_calendar_multiple_black_48dp);
                            }
                        }
                    });
                }
                catch
                {
                    // Calendar colors won't update 
                }
            }
        }

        // Update requested UTC time on timer tick
        private void UpdateRequestedTime(object state)
        {
            // Make sure were are finding the TextView
            if (_thisView.IsAttachedToWindow && _mUtcTextView != null)
            {
                var utcNow = DateTime.UtcNow;
                var timeComparison = utcNow - _mUtcRequestTime;

                string utcStringBeginning = "* " + Resources.GetString(Resource.String.NOTAM_requested);
                string utcStringEnd = Resources.GetString(Resource.String.Ago) + " *";
                string justNow = Resources.GetString(Resource.String.time_just_now) + " *";
                string days = Resources.GetString(Resource.String.Days);
                string hours = Resources.GetString(Resource.String.Hours);
                string minutes = Resources.GetString(Resource.String.Minutes);
                string day = Resources.GetString(Resource.String.Day);
                string hour = Resources.GetString(Resource.String.Hour);
                string minute = Resources.GetString(Resource.String.Minute);

                string utcString = String.Empty;

                if (timeComparison.Days > 1 && timeComparison.Hours > 1)
                    utcString =
                        $"{utcStringBeginning} {timeComparison.Days} {days}, {timeComparison.Hours} {hours} {utcStringEnd}";
                else if (timeComparison.Days == 1 && timeComparison.Hours > 1)
                    utcString =
                        $"{utcStringBeginning} {timeComparison.Days} {day}, {timeComparison.Hours} {hours} {utcStringEnd}";
                else if (timeComparison.Days > 1 && timeComparison.Hours == 1)
                    utcString =
                        $"{utcStringBeginning} {timeComparison.Days} {days}, {timeComparison.Hours} {hour} {utcStringEnd}";
                else if (timeComparison.Days == 1 && timeComparison.Hours == 1)
                    utcString =
                        $"{utcStringBeginning} {timeComparison.Days} {day}, {timeComparison.Hours} {hour} {utcStringEnd}";
                else if (timeComparison.Days < 1 && timeComparison.Hours > 1 && timeComparison.Minutes > 1)
                    utcString =
                        $"{utcStringBeginning} {timeComparison.Hours} {hours}, {timeComparison.Minutes} {minutes} {utcStringEnd}";
                else if (timeComparison.Days < 1 && timeComparison.Hours == 1 && timeComparison.Minutes > 1)
                    utcString =
                        $"{utcStringBeginning} {timeComparison.Hours} {hour}, {timeComparison.Minutes} {minutes} {utcStringEnd}";
                else if (timeComparison.Days < 1 && timeComparison.Hours > 1 && timeComparison.Minutes == 1)
                    utcString =
                        $"{utcStringBeginning} {timeComparison.Hours} {hours}, {timeComparison.Minutes} {minute} {utcStringEnd}";
                else if (timeComparison.Days < 1 && timeComparison.Hours == 1 && timeComparison.Minutes == 1)
                    utcString =
                        $"{utcStringBeginning} {timeComparison.Hours} {hour}, {timeComparison.Minutes} {minute} {utcStringEnd}";
                else if (timeComparison.Days < 1 && timeComparison.Hours < 1 && timeComparison.Minutes > 1)
                    utcString = $"{utcStringBeginning} {timeComparison.Minutes} {minutes} {utcStringEnd}";
                else
                    utcString = $"{utcStringBeginning} {justNow}";

                // Adding view
                Activity.RunOnUiThread(() =>
                {
                    _mUtcTextView.Text = utcString;

                    // Styling
                    if (timeComparison.Hours >= 6)
                        _mUtcTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.RedTextWarning));
                    else if (timeComparison.Hours >= 2)
                        _mUtcTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.YellowText));
                    else
                        _mUtcTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.GreenText));
                });
            }
        }

        private async Task PercentageCompleted(int currentCount, int totalCount, string currentAirport)
        {
            int percentage = (currentCount +1 ) * 100 / totalCount;

            if (percentage <= 100)
            {
                Activity.RunOnUiThread(() =>
                {
                    // Show the airport and percentage
                    _notamFetchingAlertDialog.SetMessage(currentAirport.ToUpper() + " - (" + percentage + "%)");
                });
            }

            // If we reached a 100%, we will wait a bit to that users can see the whole bar
            if (percentage == 100)
            {
                await Task.Delay(750);
                _notamFetchingAlertDialog.Dismiss();
            }
        }
    }
}