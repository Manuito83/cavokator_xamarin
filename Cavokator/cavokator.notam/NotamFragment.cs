//
// CAVOKATOR APP
// Website: https://github.com/Manuito83/Cavokator
// License GNU General Public License v3.0
// Manuel Ortega, 2018
//

using System;
using System.Collections.Generic;
using System.IO;
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
using Android;
using Android.Graphics.Drawables;
using Android.Provider;
using Android.Support.V7.App;
using Android.Text.Method;
using Android.Text.Style;
using Newtonsoft.Json;
using AlertDialog = Android.App.AlertDialog;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Support.V4.Widget;
using Java.Security;
using Permission = Android.Content.PM.Permission;

namespace Cavokator
{
    class NotamFragment : Android.Support.V4.App.Fragment, ActivityCompat.IOnRequestPermissionsResultCallback
    {

        // Options from options menu
        private string mSortByCategory;
        private bool showSubcategories = true;

        // Floating action button
        private CoordinatorLayout _coordinatorLayout;
        private FloatingActionButton _fabScrollTop;

        // Main views
        private NestedScrollView _scrollViewContainer;
        private LinearLayout _linearlayoutBottom;
        private EditText _airportEntryEditText;
        private Button _notamRequestButton;
        private Button _notamClearButton;
        private ImageButton _notamOptionsButton;
        private TextView _chooseIDtextview;
        private LinearLayout _linearLayoutNotamRequestedTime;

        // Notam view to share
        private View _myViewToShare;
        private string myNotamIdToShare;
        private string myNotamRawToShare;

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

        // RecyclerView, LayoutManager and Adapter
        RecyclerView mRecyclerView;
        LinearLayoutManager mLayoutManager;
        NotamFieldsAdapter mAdapter;

        // List of <object> to be used in RecyclerView
        private List<object> myRecyclerNotamList = new List<object>();

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
            _notamOptionsButton.Click += OnOptionsButtonClicked;
            _airportEntryEditText.BeforeTextChanged += BeforeIdTextChanged;
            _airportEntryEditText.AfterTextChanged += OnIdTextChanged;
            _fabScrollTop.Click += ScrollToTop;
            
            // Get our RecyclerView layout
            mRecyclerView = _thisView.FindViewById<RecyclerView>(Resource.Id.notamRecyclerView);

            // Plug in the linear layout manager
            mLayoutManager = new LinearLayoutManager(Activity);
            mRecyclerView.SetLayoutManager(mLayoutManager);

            
            
            try
            {
                RecallSavedData();
            }
            catch
            {
                // Encountered new null fields
                _notamClearButton.CallOnClick();
            }


            // Add ScrollChangeListener to our NestedScrollView and subscribe to scroll event
            var mNestedScrollViewListener = new NestedScrollViewListener();
            _scrollViewContainer.SetOnScrollChangeListener(mNestedScrollViewListener);
            mNestedScrollViewListener.OnScrollEvent += OnScrollMoved;

            // Add FAB and hide for later on
            _coordinatorLayout.AddView(_fabScrollTop);
            _fabScrollTop.Hide();

            // Sets up timer to update NOTAM UTC
            TimeTick();

            return _thisView;
        }

        private void ScrollToTop(object sender, EventArgs e)
        {
            _scrollViewContainer.ScrollTo(0, 0);
        }

        private void OnScrollMoved(object sender, NestedScrollViewListenerEventArgs e)
        {
            if (e.PositionY > 1000)
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

        // Empty RecyclerView to allow garbage collection
        public override void OnDestroy()
        {
            base.OnDestroy();


            // Make sure RecyclerVier and Adapter are cleared
            // in order to avoid memory leaks
            if (mAdapter != null)
            {
                myRecyclerNotamList.Clear();
                mAdapter.NotifyDataSetChanged();
            }

            mRecyclerView.SetAdapter(null);
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
            _linearLayoutNotamRequestedTime.RemoveAllViews();

            if (mAdapter != null)
            {
                myRecyclerNotamList.Clear();
                mAdapter.NotifyDataSetChanged();
            }

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

        private void OnOptionsButtonClicked(object sender, EventArgs e)
        {
            // Pull up dialog
            var transaction = FragmentManager.BeginTransaction();
            var notamOptionsDialog = new NotamOptionsDialog(mSortByCategory);
            notamOptionsDialog.Show(transaction, "options_dialog");

            notamOptionsDialog.SortBySpinnerChanged += OnSortSpinnedChanged;
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

            // Remove all previous views
            _linearLayoutNotamRequestedTime.RemoveAllViews();

            if (mAdapter != null)
            {
                myRecyclerNotamList.Clear();
                mAdapter.NotifyDataSetChanged();
            }

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
                    {
                        ShowNotams();
                    }
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
                ShowConnectionError();
            }
        }

        private void SanitizeRequestedNotams(string requestedNotamsString)
        {
            // Populate "requestedAirports" lists

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

        private async void GetNotams()
        {
            // Populate list with notams for every airport requested

            for (int i = 0; i < _mRequestedAirportsByIcao.Count; i++)
            {
                string currentAirport = _mRequestedAirportsByIcao[i];

                NotamFetcher mNotams = new NotamFetcher(currentAirport);

                if (!mNotams.DecodedNotam.ConnectionError)
                {
                    _mNotamContainerList.Add(mNotams.DecodedNotam);
                    await PercentageCompleted(i, _mRequestedAirportsByIcao.Count, currentAirport);
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
            // Plug in my adapter
            mAdapter = new NotamFieldsAdapter(myRecyclerNotamList);
            Activity.RunOnUiThread(() => { mRecyclerView.SetAdapter(mAdapter); });

            mRecyclerView.NestedScrollingEnabled = false;

            AddRequestedTime();

            FillRecyclerList();
        }

        private void FillRecyclerList()
        {

            // FILL AIRPORT NAMES
            string myAirport = string.Empty;

            // Iterate every airport populated by GetNotams()
            for (int i = 0; i < _mNotamContainerList.Count; i++)
            {
                // Try to get the airport's name from existing _myAirportDefinition List
                bool foundAirportIcao = false;
                try
                {
                    for (var j = 0; j < _mAirportDefinitions.Count; j++)
                        if (_mAirportDefinitions[j].icao == _mRequestedAirportsByIcao[i].ToUpper())
                        {
                            myAirport = _mRequestedAirportsRawString[i].ToUpper() + " - " + _mAirportDefinitions[j].description;
                            foundAirportIcao = true;
                            break;
                        }
                }
                finally
                {
                    if (!foundAirportIcao) myAirport = _mRequestedAirportsRawString[i].ToUpper();
                }


                // FILL AIRPORT RECYCLER LIST
                MyAirportRecycler myAirportRecycler = new MyAirportRecycler();
                myAirportRecycler.Name = myAirport;
                myRecyclerNotamList.Add(myAirportRecycler);


                // FILL ERROR RECYCLER LIST
                if (_mNotamContainerList[i].NotamRaw.Count == 0)
                {
                    MyErrorRecycler errorRecycler = new MyErrorRecycler();
                    errorRecycler.ErrorString = Resources.GetString(Resource.String.Notam_not_found);
                    myRecyclerNotamList.Add(errorRecycler);
                    continue;
                }


                // FILL NOTAMS
                var anyQNotam = false;
                var anyDNotam = false;
                var anyRawNotam = false;

                var anyCategoryL = false;
                var anyCategoryM = false;
                var anyCategoryF = false;
                var anyCategoryA = false;
                var anyCategoryS = false;
                var anyCategoryP = false;
                var anyCategoryC = false;
                var anyCategoryI = false;
                var anyCategoryG = false;
                var anyCategoryN = false;
                var anyCategoryR = false;
                var anyCategoryW = false;
                var anyCategoryO = false;
                var anyCategoryNotReported = false;

                var positionL = new List<int>();
                var positionM = new List<int>();
                var positionF = new List<int>();
                var positionA = new List<int>();
                var positionS = new List<int>();
                var positionP = new List<int>();
                var positionC = new List<int>();
                var positionI = new List<int>();
                var positionG = new List<int>();
                var positionN = new List<int>();
                var positionR = new List<int>();
                var positionW = new List<int>();
                var positionO = new List<int>();
                var positionUnknown = new List<int>();
                var positionRaw = new List<int>();

                for (int j = 0; j < _mNotamContainerList[i].NotamRaw.Count; j++)
                { 
                    if (mSortByCategory == "category")
                    {
                        LocalAnalyzeCategories(j);
                    }
                    else
                    {
                        if (_mNotamContainerList[i].NotamQ[j])
                        {
                            LocalFillOnlyNotams(j);
                        }
                        else if (_mNotamContainerList[i].NotamD[j])
                        {
                            // Placeholder for D Notam
                        }
                        else
                        {
                            LocalFillRawNotams(j);
                        }
                    }
                }

                // Fill recycler list
                LocalFillCategoriesAndNotams();


                // ** LOCAL FUNCTIONS **
                void LocalAnalyzeCategories(int j)
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

                void LocalFillCategoriesAndNotams()
                {
                    if (anyQNotam)
                    {
                        if (anyCategoryL)
                        {
                            MyCategoryRecycler myCategoryRecycler = new MyCategoryRecycler();
                            myCategoryRecycler.CategoryString = "Lightning facilities";

                            myRecyclerNotamList.Add(myCategoryRecycler);

                            foreach (var p in positionL)
                            {
                                LocalFillOnlyNotams(p);
                            }
                        }

                        if (anyCategoryM)
                        {
                            MyCategoryRecycler myCategoryRecycler = new MyCategoryRecycler();
                            myCategoryRecycler.CategoryString = "Movement and landing area";

                            myRecyclerNotamList.Add(myCategoryRecycler);

                            foreach (var p in positionM)
                            {
                                LocalFillOnlyNotams(p);
                            }
                        }

                        if (anyCategoryF)
                        {
                            MyCategoryRecycler myCategoryRecycler = new MyCategoryRecycler();
                            myCategoryRecycler.CategoryString = "Facilities and services";

                            myRecyclerNotamList.Add(myCategoryRecycler);

                            foreach (var p in positionF)
                            {
                                LocalFillOnlyNotams(p);
                            }
                        }

                        if (anyCategoryA)
                        {
                            MyCategoryRecycler myCategoryRecycler = new MyCategoryRecycler();
                            myCategoryRecycler.CategoryString = "Airspace organization";

                            myRecyclerNotamList.Add(myCategoryRecycler);

                            foreach (var p in positionA)
                            {
                                LocalFillOnlyNotams(p);
                            }
                        }

                        if (anyCategoryS)
                        {
                            MyCategoryRecycler myCategoryRecycler = new MyCategoryRecycler();
                            myCategoryRecycler.CategoryString = "Air traffic and VOLMET";

                            myRecyclerNotamList.Add(myCategoryRecycler);

                            foreach (var p in positionS)
                            {
                                LocalFillOnlyNotams(p);
                            }
                        }

                        if (anyCategoryP)
                        {
                            MyCategoryRecycler myCategoryRecycler = new MyCategoryRecycler();
                            myCategoryRecycler.CategoryString = "Air traffic procedures";

                            myRecyclerNotamList.Add(myCategoryRecycler);

                            foreach (var p in positionP)
                            {
                                LocalFillOnlyNotams(p);
                            }
                        }

                        if (anyCategoryC)
                        {
                            MyCategoryRecycler myCategoryRecycler = new MyCategoryRecycler();
                            myCategoryRecycler.CategoryString = "Communications and surveillance";

                            myRecyclerNotamList.Add(myCategoryRecycler);

                            foreach (var p in positionC)
                            {
                                LocalFillOnlyNotams(p);
                            }
                        }

                        if (anyCategoryI)
                        {
                            MyCategoryRecycler myCategoryRecycler = new MyCategoryRecycler();
                            myCategoryRecycler.CategoryString = "Instrument landing system";

                            myRecyclerNotamList.Add(myCategoryRecycler);

                            foreach (var p in positionI)
                            {
                                LocalFillOnlyNotams(p);
                            }
                        }

                        if (anyCategoryG)
                        {
                            MyCategoryRecycler myCategoryRecycler = new MyCategoryRecycler();
                            myCategoryRecycler.CategoryString = "GNSS services";

                            myRecyclerNotamList.Add(myCategoryRecycler);

                            foreach (var p in positionG)
                            {
                                LocalFillOnlyNotams(p);
                            }
                        }

                        if (anyCategoryN)
                        {
                            MyCategoryRecycler myCategoryRecycler = new MyCategoryRecycler();
                            myCategoryRecycler.CategoryString = "Terminal and en-route navaids";

                            myRecyclerNotamList.Add(myCategoryRecycler);

                            foreach (var p in positionN)
                            {
                                LocalFillOnlyNotams(p);
                            }
                        }

                        if (anyCategoryR)
                        {
                            MyCategoryRecycler myCategoryRecycler = new MyCategoryRecycler();
                            myCategoryRecycler.CategoryString = "Airspace restrictions";

                            myRecyclerNotamList.Add(myCategoryRecycler);

                            foreach (var p in positionR)
                            {
                                LocalFillOnlyNotams(p);
                            }
                        }

                        if (anyCategoryW)
                        {
                            MyCategoryRecycler myCategoryRecycler = new MyCategoryRecycler();
                            myCategoryRecycler.CategoryString = "Warnings";

                            myRecyclerNotamList.Add(myCategoryRecycler);

                            foreach (var p in positionW)
                            {
                                LocalFillOnlyNotams(p);
                            }
                        }

                        if (anyCategoryO)
                        {
                            MyCategoryRecycler myCategoryRecycler = new MyCategoryRecycler();
                            myCategoryRecycler.CategoryString = "Other information";

                            myRecyclerNotamList.Add(myCategoryRecycler);

                            foreach (var p in positionO)
                            {
                                LocalFillOnlyNotams(p);
                            }
                        }

                        if (anyCategoryNotReported)
                        {
                            MyCategoryRecycler myCategoryRecycler = new MyCategoryRecycler();
                            myCategoryRecycler.CategoryString = "(category not reported)";

                            myRecyclerNotamList.Add(myCategoryRecycler);

                            foreach (var p in positionUnknown)
                            {
                                LocalFillOnlyNotams(p);
                            }
                        }

                        if (anyRawNotam)
                        {
                            MyCategoryRecycler myCategoryRecycler = new MyCategoryRecycler();
                            myCategoryRecycler.CategoryString = "(raw NOTAM)";

                            myRecyclerNotamList.Add(myCategoryRecycler);

                            foreach (var p in positionRaw)
                            {
                                LocalFillRawNotams(p);
                            }
                        }
                    }
                    else if (anyDNotam)
                    {
                        // Placeholder for USA D NOTAMS
                    }
                    else
                    {
                        if (mSortByCategory == "category")
                        {
                            MyCategoryRecycler myCategoryRecycler = new MyCategoryRecycler();
                            myCategoryRecycler.CategoryString = "(raw NOTAM)";

                            myRecyclerNotamList.Add(myCategoryRecycler);
                        }

                        foreach (var p in positionRaw)
                        {
                            LocalFillRawNotams(p);
                        }
                    }

                }

                void LocalFillOnlyNotams(int j)
                {
                    int a = i;
                    int b = j;

                    MyNotamCardRecycler myNotamCardRecycler = new MyNotamCardRecycler();

                    // FILL ID
                    ClickableSpan myClickableSpan = new ClickableSpan(_mNotamContainerList[a].NotamId[b]);
                    SpannableString idSpan = new SpannableString(_mNotamContainerList[a].NotamId[b]);
                    idSpan.SetSpan(myClickableSpan, 0, idSpan.Length(), 0);
                    idSpan.SetSpan(new UnderlineSpan(), 0, idSpan.Length(), 0);
                    idSpan.SetSpan(new ForegroundColorSpan(new ApplyTheme().GetColor(DesiredColor.CyanText)), 0, idSpan.Length(), 0);

                    myClickableSpan.ClickedMyClickableSpan += delegate
                    {
                        Activity.RunOnUiThread(() =>
                        {
                            // Pull up dialog
                            var transaction = FragmentManager.BeginTransaction();
                            var notamRawDialog = new NotamDialogRaw(_mNotamContainerList[a].NotamId[b], _mNotamContainerList[a].NotamRaw[b]);
                            notamRawDialog.Show(transaction, "notamRawDialog");
                        });
                    };

                    myNotamCardRecycler.NotamId = idSpan;

                    // FILL FREE TEXT
                    myNotamCardRecycler.NotamFreeText = _mNotamContainerList[a].NotamFreeText[b];
                    
                    // ADD NOTAMRECYCLER TO RECYCLER LIST
                    myRecyclerNotamList.Add(myNotamCardRecycler);
                }

                void LocalFillRawNotams(int j)
                {
                    MyNotamCardRecycler myNotamCardRecycler = new MyNotamCardRecycler();

                    // FILL FREE TEXT
                    myNotamCardRecycler.NotamFreeText = _mNotamContainerList[i].NotamRaw[j];

                    // Disable layouts to get rid of margins
                    myNotamCardRecycler.DisableTopLayout = true;
                    
                    // ADD NOTAMRECYCLER TO RECYCLER LIST
                    myRecyclerNotamList.Add(myNotamCardRecycler);
                }
            }
        }

        private void AddRequestedTime()
        {
            GetTimeDifferenceString (out var timeComparison, out var utcString);

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
                _mUtcTextView.Text = utcString;

                // Styling
                if (timeComparison.Hours >= 6)
                    _mUtcTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.RedTextWarning));
                else if (timeComparison.Hours >= 2)
                    _mUtcTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.YellowText));
                else
                    _mUtcTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.GreenText));

                _linearLayoutNotamRequestedTime.AddView(_mUtcTextView);
            });
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
                _linearLayoutNotamRequestedTime.AddView(errorTextView);
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
            airportTextViewParams.SetMargins(0, 80, 0, 20);
            airportName.LayoutParameters = airportTextViewParams;

            // Adding view
            Activity.RunOnUiThread(() =>
            {
                //_linearLayoutNotamLines.AddView(airportName);
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
                //_linearLayoutNotamLines.AddView(notamCard);
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

            RelativeLayout subcategoriesLayout = LocalStyleSubcategoriesLayout();

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
                notamLayoutContainer.AddView(subcategoriesLayout);
                notamLayoutContainer.AddView(notamFreeTextLayout);
                notamLayoutContainer.AddView(toFromRelativeLayout);
                notamLayoutContainer.AddView(spanRelativeLayout);
                notamLayoutContainer.AddView(bottomToTopRelativeLayout);
                //_linearLayoutNotamLines.AddView(notamCard);
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
                // Clickable ID
                RelativeLayout myTopLayout = new RelativeLayout(Activity);
                LinearLayout.LayoutParams myTopLayoutParams =
                    new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, (ViewGroup.LayoutParams.WrapContent));
                myTopLayoutParams.SetMargins(30, 20, 20, 0);
                myTopLayout.LayoutParameters = myTopLayoutParams;

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
                notamIdTextView.SetPadding(0, 0, 0, 0);

                myTopLayout.AddView(notamIdTextView);


                // Share and Coordinates holder
                LinearLayout shareAndCoordinatesLayout = new LinearLayout(Activity);
                RelativeLayout.LayoutParams shareAndCoordinatesLayoutParams =
                    new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, (ViewGroup.LayoutParams.WrapContent));
                shareAndCoordinatesLayoutParams.AddRule(LayoutRules.AlignParentRight);
                shareAndCoordinatesLayout.LayoutParameters = shareAndCoordinatesLayoutParams;

                // Share Icon
                ImageView myShareIcon = new ImageView(Activity);
                myShareIcon.SetImageResource(Resource.Drawable.ic_share_variant_black_48dp);
                LinearLayout.LayoutParams myShareIconParams =
                    new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, (ViewGroup.LayoutParams.WrapContent));
                myShareIconParams.Height = Resources.GetDimensionPixelSize(Resource.Dimension.dimen_entry_in_dp_20);
                myShareIconParams.Width = Resources.GetDimensionPixelSize(Resource.Dimension.dimen_entry_in_dp_20);
                myShareIconParams.SetMargins(0, 0, 0, 0);
                myShareIconParams.Gravity = GravityFlags.CenterVertical;
                myShareIcon.LayoutParameters = myShareIconParams;

                shareAndCoordinatesLayout.AddView(myShareIcon);

                myShareIcon.Click += delegate
                {
                    _myViewToShare = notamLayoutContainer;
                    myNotamIdToShare = _mRequestedAirportsByIcao[i].ToUpper();
                    myNotamRawToShare = _mNotamContainerList[i].NotamRaw[j];

                    ShareSpecificNotam(false);
                };

                // Coordinates
                if (_mNotamContainerList[i].Latitude[j] != 9999)
                {
                    ImageView myWorldMap = new ImageView(Activity);
                    LinearLayout.LayoutParams worldMapIconParams =
                        new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, (ViewGroup.LayoutParams.WrapContent));
                    worldMapIconParams.Height = Resources.GetDimensionPixelSize(Resource.Dimension.dimen_entry_in_dp_35);
                    worldMapIconParams.Width = Resources.GetDimensionPixelSize(Resource.Dimension.dimen_entry_in_dp_35);
                    worldMapIconParams.SetMargins(20, 0, 0, 0);
                    worldMapIconParams.Gravity = GravityFlags.CenterVertical;
                    myWorldMap.LayoutParameters = worldMapIconParams; myWorldMap.SetImageResource(Resource.Drawable.ic_world_map);

                    shareAndCoordinatesLayout.AddView(myWorldMap);

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

                myTopLayout.AddView(shareAndCoordinatesLayout);

                return myTopLayout;
            }

            RelativeLayout LocalStyleSubcategoriesLayout()
            {
                RelativeLayout mySubcaterogiesLayout = new RelativeLayout(Activity);

                if (showSubcategories)
                {
                    string myMainCategoryString = ReturnMainCategory(_mNotamContainerList[i].CodeSecondThird[j]);
                    string mySecondaryCategoryString = ReturnSecondaryCategory(_mNotamContainerList[i].CodeFourthFifth[j]);

                    if (myMainCategoryString != string.Empty)
                    {
                        RelativeLayout categoryTopLayout = new RelativeLayout(Activity);
                        categoryTopLayout.Id = 1;

                        TextView mainCategoryTextView = new TextView(Activity);
                        mainCategoryTextView.Text = myMainCategoryString;
                        mainCategoryTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.CyanText));
                        mainCategoryTextView.SetTextSize(ComplexUnitType.Dip, 10);
                        RelativeLayout.LayoutParams mainCategoryTextViewParams =
                            new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                        mainCategoryTextViewParams.SetMargins(30, 0, 0, 0);
                        mainCategoryTextView.LayoutParameters = mainCategoryTextViewParams;

                        categoryTopLayout.AddView(mainCategoryTextView);
                        mySubcaterogiesLayout.AddView(categoryTopLayout);

                        if (mySecondaryCategoryString != string.Empty)
                        {
                            RelativeLayout categoryBottomLayout = new RelativeLayout(Activity);
                            RelativeLayout.LayoutParams categoryBottomLayoutParams =
                                new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                            categoryBottomLayoutParams.AddRule(LayoutRules.Below, categoryTopLayout.Id);
                            categoryBottomLayout.LayoutParameters = categoryBottomLayoutParams;

                            ImageView arrowIcon = new ImageView(Activity);
                            arrowIcon.Id = 1;
                            arrowIcon.SetImageResource(Resource.Drawable.ic_menu_right_black_48dp);
                            RelativeLayout.LayoutParams arrowIconParams =
                                new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                            arrowIconParams.Height = Resources.GetDimensionPixelSize(Resource.Dimension.dimen_entry_in_dp_15);
                            arrowIconParams.Width = Resources.GetDimensionPixelSize(Resource.Dimension.dimen_entry_in_dp_15);
                            arrowIconParams.SetMargins(30, 0, 0, 0);
                            arrowIconParams.AddRule(LayoutRules.CenterVertical);
                            arrowIcon.LayoutParameters = arrowIconParams;

                            TextView secondaryCategoryTextView = new TextView(Activity);
                            secondaryCategoryTextView.Text = mySecondaryCategoryString;
                            secondaryCategoryTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.CyanText));
                            secondaryCategoryTextView.SetTextSize(ComplexUnitType.Dip, 10);
                            RelativeLayout.LayoutParams secondaryCategoryTextViewParams =
                                new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                            secondaryCategoryTextViewParams.SetMargins(0, 0, 0, 0);
                            secondaryCategoryTextViewParams.AddRule(LayoutRules.RightOf, arrowIcon.Id);
                            secondaryCategoryTextViewParams.AddRule(LayoutRules.CenterVertical);
                            secondaryCategoryTextView.LayoutParameters = secondaryCategoryTextViewParams;

                            categoryBottomLayout.AddView(arrowIcon);
                            categoryBottomLayout.AddView(secondaryCategoryTextView);

                            mySubcaterogiesLayout.AddView(categoryBottomLayout);
                        }

                    }

                }

                return mySubcaterogiesLayout;
            }

            LinearLayout LocalStyleFreeText()
            {
                LinearLayout freeTextLayout = new LinearLayout(Activity);
                freeTextLayout.Orientation = Orientation.Vertical;

                LinearLayout.LayoutParams freeTextLayoutParams =
                    new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
                freeTextLayoutParams.SetMargins(30, 30, 30, 20);
                freeTextLayout.LayoutParameters = freeTextLayoutParams;

                TextView notamFreeText = new TextView(Activity);
                notamFreeText.Text = _mNotamContainerList[i].NotamFreeText[j];
                notamFreeText.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
                notamFreeText.SetTextSize(ComplexUnitType.Dip, 12);

                freeTextLayout.AddView(notamFreeText);

                return freeTextLayout;
            }

            RelativeLayout LocalTimeFromTo(DateTime myTimeStart, DateTime myTimeEnd)
            {
                RelativeLayout myBaseTimeLayout = new RelativeLayout(Activity);
                LinearLayout.LayoutParams myBaseTimeLayoutParams =
                    new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, (ViewGroup.LayoutParams.WrapContent));
                myBaseTimeLayoutParams.SetMargins(30, 10, 30, 10);
                myBaseTimeLayout.LayoutParameters = myBaseTimeLayoutParams;

                ImageView calendarIcon = new ImageView(Activity);
                calendarIcon.Id = 1;
                calendarIcon.SetImageResource(Resource.Drawable.ic_calendar_multiple_black_48dp);
                RelativeLayout.LayoutParams calendarIconParams =
                    new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, (ViewGroup.LayoutParams.WrapContent));
                calendarIconParams.Height = Resources.GetDimensionPixelSize(Resource.Dimension.dimen_entry_in_dp_20);
                calendarIconParams.Width = Resources.GetDimensionPixelSize(Resource.Dimension.dimen_entry_in_dp_20);
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
                myStartTimeEditTextParams.SetMargins(10, 0, 0, 0);
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
                    myBaseSpanLayoutParams.SetMargins(30, 10, 0, 10);
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
                    spanTextParams.AddRule(LayoutRules.RightOf, spanClockIcon.Id);
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
                    myBottomTopLayoutParams.SetMargins(30, 10, 0, 10);
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
                //_linearLayoutNotamLines.AddView(notamCard);
            });
        }

        private void ShareSpecificNotam(bool shareRaw)
        {
            View myNotamView = _myViewToShare;
            string airportId = myNotamIdToShare;
            string notamRaw = myNotamRawToShare;


            string[] permissionsStorage =
            {
                Manifest.Permission.WriteExternalStorage,
                Manifest.Permission.ReadExternalStorage
            };

            if (!shareRaw)
            {
                try
                {
                    bool permissionsGranted = LocalGetWritePermission();

                    if (permissionsGranted)
                    {
                        LocalShareBitmapToApps();
                    }
                    else
                    {
                        // Save the view 
                        _myViewToShare = myNotamView;

                        // Request permissions
                        RequestPermissions(permissionsStorage, 0);
                    }
                }
                catch
                {
                    LocalShareRawNotam();
                }
            }
            else
            {
                LocalShareRawNotam();
            }

            void LocalShareRawNotam()
            {
                // TODO: Implement
            }

            Boolean LocalGetWritePermission()
            {
                const string permission = Manifest.Permission.WriteExternalStorage;
                if (ContextCompat.CheckSelfPermission(Activity, permission) == (int)Permission.Granted)
                {
                    return true;
                }

                return false;
            }

            void LocalShareBitmapToApps()
            {
                Intent intent = new Intent(Intent.ActionSend);

                intent.SetType("*/*");

                intent.PutExtra(Intent.ExtraText, "CAVOKATOR APP, NOTAM from airport " + airportId + ", requested @ " + _mUtcRequestTime.ToString("dd-MMM-yyyy HH:mm") + "UTC");
                intent.PutExtra(Intent.ExtraStream, LocalGetImageUri(Activity, LocalGetBitmapFromView(myNotamView)));

                StartActivity(Intent.CreateChooser(intent, "NOTAM"));
            }

            Android.Net.Uri LocalGetImageUri(Context inContext, Bitmap inImage)
            {
                MemoryStream bytes = new MemoryStream();
                inImage.Compress(Bitmap.CompressFormat.Jpeg, 100, bytes);

                String path = MediaStore.Images.Media.InsertImage(inContext.ContentResolver, inImage, "NOTAM", null);
                return Android.Net.Uri.Parse(path);
            }

            Bitmap LocalGetBitmapFromView(View view)
            {
                Bitmap returnedBitmap = Bitmap.CreateBitmap(view.Width, view.Height, Bitmap.Config.Argb8888);
                Canvas canvas = new Canvas(returnedBitmap);
                Drawable bgDrawable = view.Background;

                if (bgDrawable != null)
                    bgDrawable.Draw(canvas);
                else
                    canvas.DrawColor(Color.White);

                view.Draw(canvas);
                return returnedBitmap;
            }

        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            switch (requestCode)
            {
                case 0:
                    {
                        if (grantResults[0] == Permission.Granted)
                        {
                            // Call again function to share
                            ShareSpecificNotam(false);
                        }
                        else
                        {
                            //Explain to the user why we need to read the contacts
                            Snackbar.Make(_scrollViewContainer, "Permission needed to share full NOTAM as image!", Snackbar.LengthShort).Show();

                            // Call again function to share
                            ShareSpecificNotam(true);
                        }

                        break;
                    }
            }
        }

        private void StyleViews()
        {
            _coordinatorLayout = _thisView.FindViewById<CoordinatorLayout>(Resource.Id.cl);
            _fabScrollTop = new FloatingActionButton(Activity);
            _fabScrollTop.SetImageResource(Resource.Drawable.ic_arrow_up_bold_white_48dp);
            _scrollViewContainer = _thisView.FindViewById<NestedScrollView>(Resource.Id.notam_fragment_container);

            _linearlayoutBottom = _thisView.FindViewById<LinearLayout>(Resource.Id.notam_linearlayout_bottom);
            _linearlayoutBottom.SetBackgroundColor(new ApplyTheme().GetColor(DesiredColor.MainBackground));

            _chooseIDtextview = _thisView.FindViewById<TextView>(Resource.Id.notam_choose_id_textview);
            _chooseIDtextview.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));

            _airportEntryEditText = _thisView.FindViewById<EditText>(Resource.Id.notam_airport_entry);
            _notamRequestButton = _thisView.FindViewById<Button>(Resource.Id.notam_request_button);
            _notamClearButton = _thisView.FindViewById<Button>(Resource.Id.notam_clear_button);
            _notamOptionsButton = _thisView.FindViewById<ImageButton>(Resource.Id.notam_options_button);
            _linearLayoutNotamRequestedTime = _thisView.FindViewById<LinearLayout>(Resource.Id.notam_RequestedTime);

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
            // Get options from options dialog
            ISharedPreferences notamOptionsPreferences = Application.Context.GetSharedPreferences("NOTAM_Options", FileCreationMode.Private);

            // First initialization _metarOrTafor
            mSortByCategory = notamOptionsPreferences.GetString("sortByPREF", String.Empty);
            if (mSortByCategory == String.Empty)
            {
                notamOptionsPreferences.Edit().PutString("sortByPREF", "category").Apply();
                mSortByCategory = "category";
            }
            else
            {
                mSortByCategory = notamOptionsPreferences.GetString("sortByPREF", String.Empty);
            }

            // Get fragment's saved state
            ISharedPreferences notamFragmentPreferences = Application.Context.GetSharedPreferences("NOTAM_OnPause", FileCreationMode.Private);

            // Airport Text
            var airportEntryEditText = notamFragmentPreferences.GetString("_airportEntryEditText", String.Empty);

            // We will only get saved data if it exists at all, otherwise we could trigger
            // the event "aftertextchanged" for _airportEntryEditText and we would like to avoid that
            if (airportEntryEditText != string.Empty)
            {
                _airportEntryEditText.Text = airportEntryEditText.ToUpper();
            }

            // Make sure there are values != null, in order to avoid assigning null!
            var deserializeNotamContainer = JsonConvert.DeserializeObject<List<NotamContainer>>(notamFragmentPreferences.GetString("notamContainer", string.Empty));
            if (deserializeNotamContainer != null)
            {
                _mNotamContainerList = deserializeNotamContainer;

                var deserializeRequestedUtc = JsonConvert.DeserializeObject<DateTime>(notamFragmentPreferences.GetString("requestedUtc", string.Empty));
                _mUtcRequestTime = deserializeRequestedUtc;

                var deserializeAirportsByIcao = JsonConvert.DeserializeObject<List<String>>(notamFragmentPreferences.GetString("airportsICAO", string.Empty));
                _mRequestedAirportsByIcao = deserializeAirportsByIcao;

                var deserializeAirportsRawString = JsonConvert.DeserializeObject<List<String>>(notamFragmentPreferences.GetString("airportsRaw", string.Empty));
                _mRequestedAirportsRawString = deserializeAirportsRawString;

                if (_mNotamContainerList.Count > 0)
                    ShowNotams();
            }
        }

        private void TimeTick()
        {
            // Update requested UTC time
            var timerDelegate = new TimerCallback(UpdateRequestedTimeOnTick);
            var utcUpdateTimer = new Timer(timerDelegate, null, 0, 30000);

            var timerDelegateCalendar = new TimerCallback(UpdateCalendarColorsOnTick);
            var utcUpdateTimerCalendar = new Timer(timerDelegateCalendar, null, 0, 60000);
        }

        private void UpdateCalendarColorsOnTick(object state)
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

        private void UpdateRequestedTimeOnTick(object state)
        {
            // Make sure were are finding the TextView
            if (_thisView.IsAttachedToWindow && _mUtcTextView != null)
            {
                GetTimeDifferenceString(out var timeComparison, out var utcString);

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

        private void GetTimeDifferenceString(out TimeSpan timeComparison, out string utcString)
        {
            var utcNow = DateTime.UtcNow;
            timeComparison = utcNow - _mUtcRequestTime;
            string utcStringBeginning = "* " + Resources.GetString(Resource.String.NOTAM_requested);
            string utcStringEnd = Resources.GetString(Resource.String.Ago) + " *";
            string justNow = Resources.GetString(Resource.String.time_just_now) + " *";
            string days = Resources.GetString(Resource.String.Days);
            string hours = Resources.GetString(Resource.String.Hours);
            string minutes = Resources.GetString(Resource.String.Minutes);
            string day = Resources.GetString(Resource.String.Day);
            string hour = Resources.GetString(Resource.String.Hour);
            string minute = Resources.GetString(Resource.String.Minute);

            utcString = String.Empty;
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


        }

        private async Task PercentageCompleted(int currentCount, int totalCount, string currentAirport)
        {
            int percentage = (currentCount + 1) * 100 / totalCount;

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

        private void OnSortSpinnedChanged(object sender, NotamOptionsDialogEventArgs e)
        {
            if (e.SortBy == "category")
            {
                mSortByCategory = "category";
            }
            else
            {
                mSortByCategory = "date";
            }

            // Remove all views and show again with new methodology
            _linearLayoutNotamRequestedTime.RemoveAllViews();

            if (mAdapter != null)
            {
                myRecyclerNotamList.Clear();
                mAdapter.NotifyDataSetChanged();
            }
            
            ShowNotams();
        }

        private string ReturnMainCategory(string secondAndThirdLetters)
        {

            Dictionary<string, string> mainCategoriesDictionary = new Dictionary<string, string>
            {
                // Lighting Facilities
                { "LA", "APPR lighting system" },
                { "LB", "Aerodrome beacon" },
                { "LC", "RWY center line lights" },
                { "LD", "Landing direction indicator lights" },
                { "LE", "Runway edge lights" },
                { "LF", "Sequenced flashing lights" },
                { "LG", "Pilot-controlled lighting" },
                { "LH", "High intensity RWY lights" },
                { "LI", "RWY end identifier lights" },
                { "LJ", "RWY alignment indicator lights" },
                { "LK", "CAT II lighting components" },
                { "LL", "Low intensity runway lights" },
                { "LM", "Medium intensity runway lights" },
                { "LP", "PAPI" },
                { "LR", "All landing area lighting facilities" },
                { "LS", "Stopway lights" },
                { "LT", "Threshold lights" },
                { "LU", "Helicopter approach path indicator" },
                { "LV", "VASIS" },
                { "LW", "Heliport lighting" },
                { "LX", "Taxiway center line lights" },
                { "LY", "Taxiway edge lights" },
                { "LZ", "Runway touchdown zone lights" },

                // Movement and landing area
                { "MA", "Movement area" },
                { "MB", "Bearing strength" },
                { "MC", "Clearway" },
                { "MD", "Declared distances" },
                { "MG", "Taxiing guidance system" },
                { "MH", "Runway arresting gear" },
                { "MK", "Parking area" },
                { "MM", "Daylight markings" },
                { "MN", "Apron" },
                { "MO", "Stop bar" },
                { "MP", "Aircraft stands" },
                { "MR", "Runway" },
                { "MS", "Stopway" },
                { "MT", "Threshold" },
                { "MU", "Runway turning bay" },
                { "MW", "Strip" },
                { "MX", "Taxiway" },
                { "MY", "Rapid exit taxiway" },

                // Facilities and services
                { "FA", "Aerodrome" },
                { "FB", "Friction measuring devide" },
                { "FC", "Ceiling measurement equipment" },
                { "FD", "Docking system" },
                { "FE", "Oxygen" },
                { "FF", "Firefighting and rescue" },
                { "FG", "Ground movement control" },
                { "FH", "Helicopter alighting area/platform" },
                { "FI", "Aircraft de-icing" },
                { "FJ", "Oils" },
                { "FL", "Landing direction indicator" },
                { "FM", "Meteorological service" },
                { "FO", "Fog dispersal system" },
                { "FP", "Heliport" },
                { "FS", "Snow removal equipment" },
                { "FT", "Transmissometer" },
                { "FU", "Fuel availability" },
                { "FW", "Wind direction indicator" },
                { "FZ", "Customs" },

                // Airspace organization
                { "AA", "Minimum altitude" },
                { "AC", "Control zone" },
                { "AD", "Air defense identification zone" },
                { "AE", "Control area" },
                { "AF", "Flight information region" },
                { "AG", "General facility" },
                { "AH", "Upper control area" },
                { "AL", "Minimum usable flight level" },
                { "AN", "Area navigation route" },
                { "AO", "Oceanic control area" },
                { "AP", "Reporting point" },
                { "AR", "ATS route" },
                { "AT", "Terminal control area" },
                { "AU", "Upper flight information region" },
                { "AV", "Upper advisory area" },
                { "AX", "Intersection" },
                { "AZ", "Aerodrome traffic zone" },

                // Air traffic and VOLMET
                { "SA", "ATIS" },
                { "SB", "ATS reporting office" },
                { "SC", "Area control center" },
                { "SE", "Flight information service" },
                { "SF", "Airport flight information service" },
                { "SL", "Flow control centre" },
                { "SO", "Oceanic area control centre" },
                { "SP", "Approach control service" },
                { "SS", "Flight service station" },
                { "ST", "Aerodrome control tower" },
                { "SU", "Upper area control centre" },
                { "SV", "VOLMET broadcast" },
                { "SY", "Upper advisory service" },

                // Air traffic procedures
                { "PA", "Standard instrument arrival" },
                { "PB", "Standard VFR arrival" },
                { "PC", "Contingency procedures" },
                { "PD", "Standard instrument departure" },
                { "PE", "Stardard VFR departure" },
                { "PF", "Flow control procedure" },
                { "PH", "Holding procedure" },
                { "PI", "Instrument approach procedure" },
                { "PK", "VFR approach procedure" },
                { "PL", "Obstacle clearance limit'" },
                { "PM", "Aerodrome operating minima'" },
                { "PN", "Noise operating restrictions'" },
                { "PO", "Obstacle clearance altitude" },
                { "PP", "Obstacle clearance height" },
                { "PR", "Radio failure procedure" },
                { "PT", "Transition altitude" },
                { "PU", "Missed approach procedure" },
                { "PX", "Minimum holding altitude" },
                { "PZ", "ADIZ procedure" },

                // Communications and surveillance facilities
                { "CA", "Air/ground facility" },
                { "CB", "Automatic dependent surv. broadcast" },
                { "CC", "automatic dependent surv. contract" },
                { "CD", "CPDLC" },
                { "CE", "En-route surveillance radar" },
                { "CG", "Ground controlled approach system" },
                { "CL", "SELCAL" },
                { "CM", "Surface movement radar" },
                { "CP", "Precision approach radar (PAR)" },
                { "CR", "Surveillance radar element of PAR" },
                { "CS", "Secondary surveillance radar" },
                { "CT", "Terminal area surveillance radar" },

                // Instrument and microwave landing system
                { "IC", "ILS" },
                { "ID", "DME associated with ILS" },
                { "IG", "Glide path (ILS)" },
                { "II", "Inner marker (ILS)" },
                { "IL", "Localizer (ILS)" },
                { "IM", "Middle marker (ILS)" },
                { "IN", "Localizer (non-ILS)" },
                { "IO", "Outer marker (ILS)" },
                { "IS", "ILS CAT I" },
                { "IT", "ILS CAT II" },
                { "IU", "ILS CAT III" },
                { "IW", "Microwave landing system" },
                { "IX", "Locator, outer (ILS)" },
                { "IY", "Locator, middle (ILS)" },

                // GNSS services
                { "GA", "GNSS airfield-specidic operations" },
                { "GW", "GNSS area-wide operations" },

                // Terminal and en-route navigation facilities
                { "NA", "All radio navigation facilities" },
                { "NB", "NDB" },
                { "NC", "DECCA" },
                { "ND", "DME" },
                { "NF", "Fan marker" },
                { "NL", "Locator" },
                { "NM", "VOR/DME" },
                { "NN", "TACAN" },
                { "NO", "OMEGA" },
                { "NT", "VORTAC" },
                { "NV", "VOR" },
                { "NX", "Direction finding station" },

                // Airspace restrictions
                { "RA", "Airspace reservation" },
                { "RD", "Danger area" },
                { "RM", "Military operating area" },
                { "RO", "Overflying" },
                { "RP", "Prohibited area" },
                { "RR", "Restricted area" },
                { "RT", "Temporary restricted area" },

                // Warnings
                { "WA", "Air display" },
                { "WB", "Aerobatics" },
                { "WC", "Captive balloon or kite" },
                { "WD", "Demolition of explosives" },
                { "WE", "Exercises" },
                { "WF", "Air refueling" },
                { "WG", "Glider flying" },
                { "WH", "Blasting" },
                { "WJ", "Banner/target towing" },
                { "WL", "Ascent of free balloon" },
                { "WM", "Missile, gun or rocket firing" },
                { "WP", "Parachute jumping exercise" },
                { "WR", "Radioactive/toxic materials" },
                { "WS", "Burning or blowing gas" },
                { "WT", "Mass movement of aircraft" },
                { "WU", "Unmanned aircraft" },
                { "WV", "Formation flight" },
                { "WW", "Significant volcanic activity" },
                { "WY", "Aerial survey" },
                { "WZ", "Model flying" },

                // Other information
                { "OA", "Aeronautical information service" },
                { "OB", "Obstacle" },
                { "OE", "Aircraft entry requirements" },
                { "OL", "Obstacle lights on" },
                { "OR", "Rescue coordination centre" },
            };

            foreach (KeyValuePair<string, string> entry in mainCategoriesDictionary)
            {
                if (entry.Key == secondAndThirdLetters)
                {
                    return entry.Value;
                }
            }

            return string.Empty;
        }

        private string ReturnSecondaryCategory(string fourthAndFifthLetters)
        {

            Dictionary<string, string> secondaryCategoriesDictionary = new Dictionary<string, string>
            {

                // Availability
                { "AC", "Withdrawn from maintenance" },
                { "AD", "Available for daylight operations" },
                { "AF", "Flight checked and found reliable" },
                { "AG", "Operating but awaiting flight check" },
                { "AH", "Hours service change" },
                { "AK", "Resumed normal operation" },
                { "AL", "Operative subject to previous limitations" },
                { "AM", "Military operations only" },
                { "AN", "Available for night operations" },
                { "AO", "Operational" },
                { "AP", "Available, prior permission required" },
                { "AR", "Available on request" },
                { "AS", "Unserviceable" },
                { "AU", "Not available" },
                { "AW", "Completely withdrawn" },
                { "AX", "Previous shutdown cancelled" },

                // Availability
                { "CA", "Activated" },
                { "CC", "Completed" },
                { "CD", "Deactivated" },
                { "CE", "Erected" },
                { "CF", "Operating frequency changed" },
                { "CG", "Downgraded" },
                { "CH", "Changed" },
                { "CI", "Ident/callsign changed" },
                { "CL", "Realigned" },
                { "CM", "Displaced" },
                { "CN", "Cancelled" },
                { "CO", "Operating" },
                { "CP", "Operating on reduced power" },
                { "CR", "Temporarily replaced" },
                { "CS", "Installed" },
                { "CT", "On test, do not use" },

                // Hazard conditions
                { "HA", "Braking action" },
                { "HB", "Friction coefficient" },
                { "HC", "Covered by compacted snow" },
                { "HD", "Covered by dry snow" },
                { "HE", "Covered by water" },
                { "HF", "Totally free of snow and ice" },
                { "HG", "Grass cutting in progrss" },
                { "HH", "Hazard" },
                { "HI", "Covered by ice" },
                { "HJ", "Launch planned" },
                { "HK", "Bird migration" },
                { "HL", "Snow clearance completed" },
                { "HM", "Marked" },
                { "HN", "Covered by wet snow/slush" },
                { "HO", "Obscured by snow" },
                { "HP", "Snow clearance in progress" },
                { "HQ", "Operation cancelled" },
                { "HR", "Standing water" },
                { "HS", "Sanding in progress" },
                { "HT", "Approach according to signal area" },
                { "HU", "Launch in progress" },
                { "HV", "Work completed" },
                { "HW", "Work in progress" },
                { "HX", "Concentration of birds" },
                { "HY", "Snow banks exist" },
                { "HZ", "Covered by frozen ruts/ridges" },

                // Limitations
                { "LA", "Operating on auxiliary power" },
                { "LB", "Reserved for aircraft based therein" },
                { "LC", "Closed" },
                { "LD", "Unsafe" },
                { "LE", "Operating without auxiliary power" },
                { "LF", "Interference" },
                { "LG", "Opeating without identification" },
                { "LH", "Unserviceable for heavy aircraft" },
                { "LI", "Closed to IFR operations" },
                { "LK", "Operating as a fixed light" },
                { "LL", "Unsafe for certain length/width" },
                { "LN", "Closed to night operations" },
                { "LP", "Prohibited" },
                { "LR", "Aircraft restricted to runways/taxiways" },
                { "LS", "Subject to interruption" },
                { "LT", "Limited" },
                { "LV", "Closed to VFR operations" },
                { "LW", "Will take place" },
                { "LX", "Operating but caution advised" },

            };

            foreach (KeyValuePair<string, string> entry in secondaryCategoriesDictionary)
            {
                if (entry.Key == fourthAndFifthLetters)
                {
                    return entry.Value;
                }
            }

            return string.Empty;
        }

    }
}