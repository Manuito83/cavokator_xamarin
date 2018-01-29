//
// CAVOKATOR APP
// Website: https://github.com/Manuito83/Cavokator
// License GNU General Public License v3.0
// Manuel Ortega, 2018
//

using System;
using System.Collections.Generic;
using System.Linq;
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

        private bool connectionError;

        // View that will be used for FindViewById
        private View thisView;

        // Views for UTC time
        private DateTime mUtcRequestTime;
        private TextView mUtcTextView;

        private List<NotamContainer> mNotamContainerList = new List<NotamContainer>();

        // List of actual ICAO (as entered) airports that we are going to request
        private List<string> mRequestedAirportsByIcao = new List<string>();

        // List of airports with a mix of ICAO and IATA, that we show to the user as it was requested
        private List<string> mRequestedAirportsRawString = new List<string>();

        // Keep count of string length in EditText field, so that we know if it has decreased (deletion)
        private int mEditTextIdLength;

        // Initialize object to store List downloaded at OnCreate from a CAV file with IATA, ICAO and Airport Names
        private List<AirportCsvDefinition> mAirportDefinitions = AirportDefinitions._myAirportDefinitions;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            ((AppCompatActivity)Activity).SupportActionBar.Title = "NOTAM";

            HasOptionsMenu = true;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // In order to return the view for this Fragment
            thisView = inflater.Inflate(Resource.Layout.notam_fragment, container, false);

            StyleViews();

            // TODO: change after implementation
            //RecallSavedData();

            // Events
            _linearlayoutBottom.Touch += OnBackgroundTouch;
            _notamRequestButton.Click += OnRequestButtonClicked;
            _notamClearButton.Click += OnClearButtonClicked;
            _airportEntryEditText.BeforeTextChanged += BeforeIdTextChanged;
            _airportEntryEditText.AfterTextChanged += OnIdTextChanged;
            _scrollViewContainer.ScrollChange += OnScrollMoved;
            _fabScrollTop.Click += ScrollToTop;

            // Add FAB and hide
            _coordinatorLayout.AddView(_fabScrollTop);
            _fabScrollTop.Hide();

            // Sets up timer to update NOTAM UTC
            TimeTick();
            
            return thisView;
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
        if (_airportEntryEditText.Text.Length > mEditTextIdLength)
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
            mEditTextIdLength = _airportEntryEditText.Text.Length;
        }

        private void OnBackgroundTouch(object sender, View.TouchEventArgs e)
        {
            var imm = (InputMethodManager)Application.Context.GetSystemService(Context.InputMethodService);
            imm.HideSoftInputFromWindow(_airportEntryEditText.WindowToken, 0);
        }

        private void OnClearButtonClicked(object sender, EventArgs e)
        {
            _linearLayoutNotamLines.RemoveAllViews();

            mNotamContainerList.Clear();
            mUtcTextView = null;

            connectionError = false;

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

            connectionError = false;

            _airportEntryEditText.ClearFocus();

            mNotamContainerList.Clear();

            // Remove all previous views from the linear layout
            _linearLayoutNotamLines.RemoveAllViews();

            // Update the time at which the request was performed
            mUtcRequestTime = DateTime.UtcNow;
            
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
                    if (connectionError == false)
                        ShowNotams();
                    else
                    {
                        mNotamContainerList.Clear();
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

        /// Populate "requestedAirports" lists
        private void SanitizeRequestedNotams(string requestedNotamsString)
        {
            // Split airport list entered
            // We perform the same operation to both lists, the user one and the ICAO one
            mRequestedAirportsByIcao = requestedNotamsString.Split(' ', '\n', ',').ToList();
            mRequestedAirportsRawString = requestedNotamsString.Split(' ', '\n', ',').ToList();

            // Check and delete any entries with less than 3 chars
            for (var i = mRequestedAirportsByIcao.Count - 1; i >= 0; i--)
            {
                if (mRequestedAirportsByIcao[i].Length < 3)
                {
                    mRequestedAirportsByIcao.RemoveAt(i);
                    mRequestedAirportsRawString.RemoveAt(i);
                }
            }

            // If airport code length is 3, it might be an IATA airport
            // so we try to get its ICAO in order to get the WX information
            for (var i = 0; i < mRequestedAirportsByIcao.Count; i++)
            {
                if (mRequestedAirportsByIcao[i].Length == 3)
                {
                    // Try to find the IATA in the list
                    try
                    {
                        for (int j = 0; j < mAirportDefinitions.Count; j++)
                        {
                            if (mAirportDefinitions[j].iata == mRequestedAirportsByIcao[i].ToUpper())
                            {
                                mRequestedAirportsByIcao[i] = mAirportDefinitions[j].icao;
                                break;
                            }

                        }
                    }
                    catch
                    {
                        mRequestedAirportsByIcao[i] = null;
                    }
                }
            }
        }

        /// Populate list with notams for every airport requested
        private void GetNotams()
        {
            for (int i = 0; i < mRequestedAirportsByIcao.Count; i++) 
            {
                string currentAirport = mRequestedAirportsByIcao[i];

                NotamFetcher mNotams = new NotamFetcher(currentAirport);

                if (!mNotams.DecodedNotam.ConnectionError)
                {
                    mNotamContainerList.Add(mNotams.DecodedNotam);
                    PercentageCompleted(i, mRequestedAirportsByIcao.Count, currentAirport);
                }
                else
                {
                    _notamFetchingAlertDialog.Dismiss();
                    connectionError = true;
                    break;
                }
            }
        }

        private void ShowNotams()
        {
            // Start working if there is something in the container
            if (mNotamContainerList.Count > 0)
            {
                if (!connectionError)
                {
                    AddRequestedTime();

                    // Iterate every airport populated by GetNotams()
                    for (int i = 0; i < mNotamContainerList.Count; i++)
                    {
                        AddAirportName(i);

                        if (mNotamContainerList[i].NotamRaw.Count == 0)
                        {
                            AddErrorCard();
                            break;
                        }

                        for (int j = 0; j < mNotamContainerList[i].NotamRaw.Count; j++)
                        { 
                            // It's Q
                            if (mNotamContainerList[i].NotamQ[j])
                            {
                                AddNotamQCards(i, j);
                            }
                            // It's D
                            else if (mNotamContainerList[i].NotamD[j])
                            {
                                //
                            }
                            // It's raw
                            else
                            {
                                AddRawNotamsCards(i, j);
                            }
                        }
                    }
                }
                else
                {
                    ShowConnectionError();
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

            mUtcTextView = new TextView(Activity);
            mUtcTextView.Text = utcString;
            mUtcTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.GreenText));
            mUtcTextView.SetTextSize(ComplexUnitType.Dip, 14);
            mUtcTextView.Gravity = GravityFlags.Center;
            var utcTextViewParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            utcTextViewParams.SetMargins(0, 50, 0, 0);
            mUtcTextView.LayoutParameters = utcTextViewParams;

            // Adding view
            Activity.RunOnUiThread(() =>
            {
                _linearLayoutNotamLines.AddView(mUtcTextView);
            });
        }

        private void AddAirportName(int i)
        {
            TextView airportName = new TextView(Activity);
            
            // Try to get the airport's name from existing _myAirportDefinition List
            bool foundAirportICAO = false;
            try
            {
                for (var j = 0; j < mAirportDefinitions.Count; j++)
                    if (mAirportDefinitions[j].icao == mRequestedAirportsByIcao[i].ToUpper())
                    {
                        airportName.Text = mRequestedAirportsRawString[i].ToUpper() + " - " + mAirportDefinitions[j].description;
                        foundAirportICAO = true;
                        break;
                    }
            }
            finally
            {
                if (!foundAirportICAO) airportName.Text = mRequestedAirportsRawString[i].ToUpper();
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

        private void AddNotamQCards(int i, int j)
        {
            // Style card and RelativeLayout
            var notamCard = LocalStyleCard(out var qCardsLayout);
            
            // Styling notamId
            var notamId = LocalStyleNotamId();

            // Styling notamFreeText
            var notamFreeText = LocalStyleFreeText(notamId);

            ImageView worldIcon = new ImageView(Activity);
            if (mNotamContainerList[i].Latitude[j] != 999)
            {
                worldIcon = LocalStyleMapIcon(worldIcon);
            }
            
            // Adding view
            Activity.RunOnUiThread(() =>
            {
                qCardsLayout.AddView(notamId);
                qCardsLayout.AddView(notamFreeText);
                qCardsLayout.AddView(worldIcon);
                notamCard.AddView(qCardsLayout);
                _linearLayoutNotamLines.AddView(notamCard);
            });

            // ** Local functions for styling ** //
            
            // Styling CardViews
            CardView LocalStyleCard(out RelativeLayout relativeLayout)
            {
                // --- Styling cards ---
                CardView cardView = new CardView(Activity);
                cardView.SetBackgroundColor(new ApplyTheme().GetColor(DesiredColor.CardViews));
                cardView.Elevation = 5.0f;
                var cardViewParams =
                    new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
                cardViewParams.SetMargins(10, 10, 10, 20);
                cardView.LayoutParameters = cardViewParams;

                // --- RelativeLayout ---
                relativeLayout = new RelativeLayout(Activity);
                return cardView;
            }

            // Styling NOTAM ID
            TextView LocalStyleNotamId()
            {
                TextView notamIdTextView = new TextView(Activity);
                notamIdTextView.Id = 1;

                ClickableSpan myClickableSpan = new ClickableSpan(mNotamContainerList[i].NotamID[j]);

                myClickableSpan.ClickedMyClickableSpan += delegate
                {
                    Activity.RunOnUiThread(() =>
                    {
                        // Pull up dialog
                        var transaction = FragmentManager.BeginTransaction();
                        var notamRawDialog = new NotamDialogRaw(mNotamContainerList[i].NotamID[j], mNotamContainerList[i].NotamRaw[j]);
                        notamRawDialog.Show(transaction, "notamRawDialog");
                    });
                };

                SpannableString idSpan = new SpannableString(mNotamContainerList[i].NotamID[j]);
                idSpan.SetSpan(myClickableSpan, 0, idSpan.Length(), 0);
                idSpan.SetSpan(new UnderlineSpan(), 0, idSpan.Length(), 0);
                idSpan.SetSpan(new ForegroundColorSpan(new ApplyTheme().GetColor(DesiredColor.CyanText)), 0, idSpan.Length(), 0);

                notamIdTextView.TextFormatted = idSpan;
                notamIdTextView.MovementMethod = new LinkMovementMethod();

                notamIdTextView.SetTextSize(ComplexUnitType.Dip, 12);
                notamIdTextView.SetPadding(30, 30, 15, 0);
                return notamIdTextView;
            }

            // Styling free NOTAM text
            TextView LocalStyleFreeText(TextView notamId1)
            {
                TextView notamFreeText1 = new TextView(Activity);
                notamFreeText1.Id = 2;
                notamFreeText1.Text = mNotamContainerList[i].NotamFreeText[j];
                notamFreeText1.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
                notamFreeText1.SetTextSize(ComplexUnitType.Dip, 12);
                notamFreeText1.SetPadding(30, 30, 15, 10);
                RelativeLayout.LayoutParams notamFreeTextParams =
                    new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                notamFreeTextParams.AddRule(LayoutRules.Below, notamId1.Id);
                notamFreeText1.LayoutParameters = notamFreeTextParams;
                return notamFreeText1;
            }

            // Styling map icon
            ImageView LocalStyleMapIcon(ImageView myWorldMap)
            {
                myWorldMap.Id = 3;

                myWorldMap.SetImageResource(Resource.Drawable.ic_world_map);

                RelativeLayout.LayoutParams worldMapIconParams = 
                    new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, (ViewGroup.LayoutParams.WrapContent));
                worldMapIconParams.AddRule(LayoutRules.AlignParentRight);
                worldMapIconParams.Height = Resources.GetDimensionPixelSize(Resource.Dimension.dimen_entry_in_dp_35);
                worldMapIconParams.Width = Resources.GetDimensionPixelSize(Resource.Dimension.dimen_entry_in_dp_35);
                worldMapIconParams.SetMargins(0, 10, 15, 25);
                myWorldMap.LayoutParameters = worldMapIconParams;

                myWorldMap.Click += delegate
                {
                    var transaction = FragmentManager.BeginTransaction();
                    var notamRawMap = new NotamDialogMap(mNotamContainerList[i].NotamID[j],
                        mNotamContainerList[i].Latitude[j],
                        mNotamContainerList[i].Longitude[j],
                        mNotamContainerList[i].Radius[j]);
                    notamRawMap.Show(transaction, "notamRawDialog");
                };

                return myWorldMap;
            }
        }

        private void AddRawNotamsCards(int i, int j)
        {
            CardView notamCard = new CardView(Activity);
            TextView notamLine = new TextView(Activity);

            notamLine.Text = mNotamContainerList[i].NotamRaw[j];

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
            _coordinatorLayout = thisView.FindViewById<CoordinatorLayout>(Resource.Id.cl);
            _fabScrollTop = new FloatingActionButton(Activity);
            _fabScrollTop.SetImageResource(Resource.Drawable.ic_arrow_up_bold_white_48dp);
            _scrollViewContainer = thisView.FindViewById<ScrollView>(Resource.Id.notam_fragment_container);

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
            _chooseIDtextview.Text = Resources.GetString(Resource.String.NOTAM_ID_TextView);
            _airportEntryEditText.Hint = Resources.GetString(Resource.String.Icao_Or_Iata);
        }

        private void SaveData()
        {
            var notamDestroy = Application.Context.GetSharedPreferences("NOTAM_OnPause", FileCreationMode.Private);

            // Save ICAO ID LIST
            notamDestroy.Edit().PutString("_airportEntryEditText", _airportEntryEditText.Text).Apply();

            // Save AIRPORT IDs
            var notamContainer = JsonConvert.SerializeObject(mNotamContainerList);
            notamDestroy.Edit().PutString("notamContainer", notamContainer).Apply();

            var requestedUtc = JsonConvert.SerializeObject(mUtcRequestTime);
            notamDestroy.Edit().PutString("requestedUtc", requestedUtc).Apply();

            var airportsByIcao = JsonConvert.SerializeObject(mRequestedAirportsByIcao);
            notamDestroy.Edit().PutString("airportsICAO", airportsByIcao).Apply();

            var airportsRaw = JsonConvert.SerializeObject(mRequestedAirportsRawString);
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
                _airportEntryEditText.Text = airportEntryEditText;
            }

            // Make sure there are values != null, in order to avoid assigning null!
            var deserializeNotamContainer = JsonConvert.DeserializeObject<List<NotamContainer>>(notamPrefs.GetString("notamContainer", string.Empty));
            if (deserializeNotamContainer != null)
            {
                mNotamContainerList = deserializeNotamContainer;

                var deserializeRequestedUtc = JsonConvert.DeserializeObject<DateTime>(notamPrefs.GetString("requestedUtc", string.Empty));
                mUtcRequestTime = deserializeRequestedUtc;

                var deserializeAirportsByIcao = JsonConvert.DeserializeObject<List<String>>(notamPrefs.GetString("airportsICAO", string.Empty));
                mRequestedAirportsByIcao = deserializeAirportsByIcao;

                var deserializeAirportsRawString = JsonConvert.DeserializeObject<List<String>>(notamPrefs.GetString("airportsRaw", string.Empty));
                mRequestedAirportsRawString = deserializeAirportsRawString;

                ShowNotams();
            }
        }

        private void TimeTick()
        {
            // Update requested UTC time
            var timerDelegate = new TimerCallback(UpdateRequestedTime);
            var utcUpdateTimer = new Timer(timerDelegate, null, 0, 30000);
        }


        /// Update requested UTC time
        private void UpdateRequestedTime(object state)
        {
            // Make sure were are finding the TextView
            if (thisView.IsAttachedToWindow && mUtcTextView != null)
            {
                var utcNow = DateTime.UtcNow;
                var timeComparison = utcNow - mUtcRequestTime;

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
                    mUtcTextView.Text = utcString;

                    // Styling
                    if (timeComparison.Hours >= 6)
                        mUtcTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.RedTextWarning));
                    else if (timeComparison.Hours >= 2)
                        mUtcTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.YellowText));
                    else
                        mUtcTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.GreenText));
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