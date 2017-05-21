using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Text;
using Android.Text.Style;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Newtonsoft.Json;
using Plugin.Connectivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cavokator
{
    [Activity(Label = "Cavokator", MainLauncher = true, Icon = "@drawable/ic_appicon",
         Theme = "@android:style/Theme.Material", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]

    public class ActivityWxMain : Activity
    {

        // Main fields
        private LinearLayout _linearlayoutWxBottom;
        private EditText _airportEntryEditText;
        private Button _wxRequestButton;
        private Button _wxClearButton;
        private ImageButton _wxOptionsButton;
        private TextView _chooseIDtextview;


        // ProgressDialog to show while we fetch the wx information
        private ProgressDialog _wxProgressDialog;


        /// <summary>
        /// List of actual ICAO (as entered) airports that we are going to request
        /// </summary>
        private List<string> _icaoIdList = new List<string>();

        /// <summary>
        /// List of airports with a mix of ICAO and IATA, that we show to the user as it was requested
        /// </summary>
        private List<string> _myAirportsList = new List<string>();


        // Object to store List downloaded at OnCreate from a CAV file with IATA, ICAO and Airport Names
        private List<AirportCsvDefinition> _myAirportDefinitions = new List<AirportCsvDefinition>();


        // Instantiate WXInfo object
        private WxInfo _wxInfo = new WxInfo();


        // Dictionary of UTC fields IDS, used to update the time difference of METARS/TAFORS with a timer
        private readonly Dictionary<string, DateTime> _metarUtcFieldsIds = new Dictionary<string, DateTime>();
        private readonly Dictionary<string, DateTime> _taforUtcFieldsIds = new Dictionary<string, DateTime>();


        // Options
        private string _metarOrTafor;
        private int _hoursBefore;
        private bool _mostRecent;
        private bool _saveData;

        // Keep count of string length in EditText field, so that we know if it has decreased (deletion)
        private int _editTextIdLength;


        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Get ISharedPreferences
            GetPreferences();

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.wx_main);


            _linearlayoutWxBottom = FindViewById<LinearLayout>(Resource.Id.linearlayout_wx_bottom);
            _airportEntryEditText = FindViewById<EditText>(Resource.Id.airport_entry);
            _chooseIDtextview = FindViewById<TextView>(Resource.Id.choose_id_textview);
            _wxRequestButton = FindViewById<Button>(Resource.Id.wx_request_button);
            _wxClearButton = FindViewById<Button>(Resource.Id.wx_clear_button);
            _wxOptionsButton = FindViewById<ImageButton>(Resource.Id.wx_options_button);

            _wxRequestButton.Text = Resources.GetString(Resource.String.Send_button);
            _wxClearButton.Text = Resources.GetString(Resource.String.Clear_button);
            _chooseIDtextview.Text = Resources.GetString(Resource.String.Airport_ID_TextView);
            _airportEntryEditText.Hint = Resources.GetString(Resource.String.Icao_Or_Iata);


            // Subscribe events
            _airportEntryEditText.BeforeTextChanged += BeforeIdTextChanged;
            _airportEntryEditText.AfterTextChanged += OnIdTextChanged;
            _wxRequestButton.Click += OnRequestButtonClicked;




            // Get a list of ICAO/IATA/Airport's name from CSV at first execution
            // We get the whole list of 5000+ because it is faster to iterate compared to consulting CSV 
            // This operation might be time consuming and we do it at OnCreate in order to have
            // a better overall response from the app later on
            if (_myAirportDefinitions.Count == 0)
            {
                AirportConverter iataConverter = new AirportConverter();
                _myAirportDefinitions = iataConverter.GetCodeList();
            }



            // If persistence data option is selected, we get the last values from SharedPreferences
            // and then call ShowWeather again, so that the information is re-generated
            if (_saveData)
            {
                ISharedPreferences wxprefs = Application.Context.GetSharedPreferences("WX_OnPause",
                    FileCreationMode.Private);

                // Airport Text
                var storedAirportIdText = wxprefs.GetString("_airportEntryEditText.Text", String.Empty);

                // We will only get saved data if it exists at all, otherwise we could trigger
                // the event "aftertextchanged" for _airportEntryEditText and we would like to avoid that
                if (storedAirportIdText != string.Empty)
                {
                    _airportEntryEditText.Text = storedAirportIdText;
                }


                try
                {
                    // Airport IDs
                    var deserializeIds =
                        JsonConvert.DeserializeObject<List<string>>(wxprefs.GetString("airportIDs", string.Empty));
                    _wxInfo.AirportIDs = deserializeIds;

                    // My Airports
                    var myAirportsIDs =
                        JsonConvert.DeserializeObject<List<string>>(wxprefs.GetString("myAirportIDs", string.Empty));
                    _myAirportsList = myAirportsIDs;

                    // Airport Errors
                    var deserializeErrors =
                        JsonConvert.DeserializeObject<List<bool>>(wxprefs.GetString("airportErrors", string.Empty));
                    _wxInfo.AirportErrors = deserializeErrors;

                    // Airport Metars UTC
                    var deserializeMetarsUtc =
                        JsonConvert.DeserializeObject<List<List<DateTime>>>(wxprefs.GetString("airportMetarsUTC",
                            string.Empty));
                    _wxInfo.AirportMetarsUtc = deserializeMetarsUtc;

                    // Airport Metars
                    var deserializeMetars =
                        JsonConvert.DeserializeObject<List<List<string>>>(wxprefs.GetString("airportMetars",
                            string.Empty));
                    _wxInfo.AirportMetars = deserializeMetars;

                    // Airport Tafors UTC
                    var deserializeTaforsUtc =
                        JsonConvert.DeserializeObject<List<List<DateTime>>>(wxprefs.GetString("airportTaforsUTC",
                            string.Empty));
                    _wxInfo.AirportTaforsUtc = deserializeTaforsUtc;

                    // Airport Tafors
                    var deserializeTafors =
                        JsonConvert.DeserializeObject<List<List<string>>>(wxprefs.GetString("airportTafors",
                            string.Empty));
                    _wxInfo.AirportTafors = deserializeTafors;

                    ShowWeather();
                }
                catch (Exception)
                {
                    // Just do nothing, as values are possibly null (first initialization)
                }

            }

            
            
            // Sets up timer to update METAR UTC
            UtcTimerTick();


            // Close keyboard when click outside airport_entry EditText
            _linearlayoutWxBottom.Touch += delegate
            {
                var imm = (InputMethodManager) Application.Context.GetSystemService(InputMethodService);
                imm.HideSoftInputFromWindow(_airportEntryEditText.WindowToken, 0);

            };


            // Clear views
            _wxClearButton.Click += delegate
            {
                _metarUtcFieldsIds.Clear();
                _taforUtcFieldsIds.Clear();

                _wxInfo.AirportIDs = null;
                _myAirportsList = null;
                _wxInfo.AirportErrors = null;
                _wxInfo.AirportMetarsUtc = null;
                _wxInfo.AirportMetars = null;
                _wxInfo.AirportTaforsUtc = null;
                _wxInfo.AirportTafors = null;

                var linearlayoutWXmetarsTafors = FindViewById<LinearLayout>(Resource.Id.linearlayout_wx_metarstafors);

                // Remove all previous views from the linear layout
                linearlayoutWXmetarsTafors.RemoveAllViews();

                _airportEntryEditText.Text = "";
                _airportEntryEditText.SetTextColor(default(Color));
                _airportEntryEditText.SetBackgroundColor(Color.ParseColor("#aaaaaa"));
                _airportEntryEditText.SetTypeface(null, TypefaceStyle.Italic);

            };


            // OPTIONS DIALOG
            _wxOptionsButton.Click += delegate
            {
                // Pull up dialog
                var transaction = FragmentManager.BeginTransaction();
                var wxOptionsDialog = new WxOptionsDialog(_metarOrTafor, _hoursBefore, _mostRecent, _saveData);
                wxOptionsDialog.Show(transaction, "options_dialog");

                wxOptionsDialog.SpinnerChanged += OnMetarOrTaforChanged;
                wxOptionsDialog.SeekbarChanged += OnHoursBeforeChanged;
                wxOptionsDialog.SwitchChanged += OnSaveDataChanged;
            };
        }


        // Action when wx request button is clicked
        private async void OnRequestButtonClicked(object sender, EventArgs e)
        {
            // TODO: remove, only testing
            var lala = new WxColorCoder();

            _metarUtcFieldsIds.Clear();
            _taforUtcFieldsIds.Clear();

            _wxInfo.AirportIDs = null;
            _myAirportsList = null;
            _wxInfo.AirportErrors = null;
            _wxInfo.AirportMetarsUtc = null;
            _wxInfo.AirportMetars = null;
            _wxInfo.AirportTaforsUtc = null;
            _wxInfo.AirportTafors = null;

            // Clear focus of airport_entry
            var airportEntry = FindViewById<EditText>(Resource.Id.airport_entry);
            airportEntry.ClearFocus();



            // Show our ProgressDialog
            _wxProgressDialog = new ProgressDialog(this);
            _wxProgressDialog.SetMessage(Resources.GetString(Resource.String.Fetching));
            _wxProgressDialog.SetCancelable(true);
            _wxProgressDialog.SetProgressStyle(ProgressDialogStyle.Horizontal);
            _wxProgressDialog.Progress = 0;
            _wxProgressDialog.Max = 100;
            _wxProgressDialog.Show();

            // Sanitizes _icaoIdList before RequestWeather() makes use of it!
            await Task.Factory.StartNew(SanitizeAirportIds);


            // We won't start fetching weather unless we have a valid entry
            if (_myAirportsList != null && _myAirportsList.Count > 0)
            {
                // Launch weather request
                RequestWeather();
            }
            else
            {
                _wxProgressDialog.Dismiss();
            }
        }

        private void SanitizeAirportIds()
        {
            var airportEntry = FindViewById<EditText>(Resource.Id.airport_entry);

            // Split airport list entered
            // We perform the same operation to both lists, the user one and the ICAO one
            // to avoid copy (which would reference)
            _icaoIdList = airportEntry.Text.Split(' ', '\n', ',').ToList();
            _myAirportsList = airportEntry.Text.Split(' ', '\n', ',').ToList();

            // Check and delete any entries with less than 3 chars
            for (var i = _icaoIdList.Count - 1; i >= 0; i--)
            {
                if (_icaoIdList[i].Length < 3)
                {
                    _icaoIdList.RemoveAt(i);
                    _myAirportsList.RemoveAt(i);
                }
            }


            // If airport code length is 3, it might be an IATA airport
            // so we try to get its ICAO in order to get the WX information
            for (var i = 0; i < _icaoIdList.Count; i++)
            {
                if (_icaoIdList[i].Length == 3)
                {



                    // Try to find the IATA in the list
                    try
                    {
                        for (int j = 0; j < _myAirportDefinitions.Count; j++)
                        {
                            if (_myAirportDefinitions[j].iata == _icaoIdList[i].ToUpper())
                            {
                                _icaoIdList[i] = _myAirportDefinitions[j].icao;
                                break;
                            }
                            
                        }
                    }
                    catch 
                    {
                        _icaoIdList[i] = null;
                    }

                }
            }
            
        }


        // Here we update _editTextIdLength with the string length BEFORE the text has been changed
        // That way, in OnIdTextChanged we can figure out if we are deleting text
        private void BeforeIdTextChanged(object sender, TextChangedEventArgs e)
        {
            _editTextIdLength = _airportEntryEditText.Text.Length;
        }


        // Event handler for ID EditText text changed
        // First we change the box style
        // Secondly we limit the max word length to 4 chars
        private void OnIdTextChanged(object sender, AfterTextChangedEventArgs e)
        {

            // Style EdiText text when writting
            _airportEntryEditText.SetTextColor(Color.Black);
            _airportEntryEditText.SetBackgroundColor(Color.White);
            _airportEntryEditText.SetTypeface(null, TypefaceStyle.Normal);


            // Apply only if we are adding text
            // Otherwise, we could not delete (due to infinite loop)
            if (_airportEntryEditText.Text.Length > _editTextIdLength)
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


        private void UtcTimerTick()
        {
            var timerDelegate = new TimerCallback(OnTimedUtcEvent);
            var utcUpdateTimer = new Timer(timerDelegate, null, 0, 30000);
        }


        // Saves fields to SharedPreferences
        protected override void OnPause()
        {

            var wxDestroy = Application.Context.GetSharedPreferences("WX_OnPause", FileCreationMode.Private);

            // Save ICAO ID LIST
            _airportEntryEditText = FindViewById<EditText>(Resource.Id.airport_entry);
            wxDestroy.Edit().PutString("_airportEntryEditText.Text", _airportEntryEditText.Text).Apply();

            // Save AIRPORT IDs
            var airportIds = JsonConvert.SerializeObject(_wxInfo.AirportIDs);
            wxDestroy.Edit().PutString("airportIDs", airportIds).Apply();

            // Save REQUESTED AIRPORTS
            var requestedAirports = JsonConvert.SerializeObject(_myAirportsList);
            wxDestroy.Edit().PutString("myAirportIDs", requestedAirports).Apply();

            // Save AIRPORT Errors
            var airportErrors = JsonConvert.SerializeObject(_wxInfo.AirportErrors);
            wxDestroy.Edit().PutString("airportErrors", airportErrors).Apply();

            // Save AIRPORT Metars UTC
            var airportMetarsUtc = JsonConvert.SerializeObject(_wxInfo.AirportMetarsUtc);
            wxDestroy.Edit().PutString("airportMetarsUTC", airportMetarsUtc).Apply();

            // Save AIRPORT Metars
            var airportMetars = JsonConvert.SerializeObject(_wxInfo.AirportMetars);
            wxDestroy.Edit().PutString("airportMetars", airportMetars).Apply();

            // Save AIRPORT Tafors UTC
            var airportTaforsUtc = JsonConvert.SerializeObject(_wxInfo.AirportTaforsUtc);
            wxDestroy.Edit().PutString("airportTaforsUTC", airportTaforsUtc).Apply();

            // Save AIRPORT Tafors
            var airportTafors = JsonConvert.SerializeObject(_wxInfo.AirportTafors);
            wxDestroy.Edit().PutString("airportTafors", airportTafors).Apply();

            base.OnPause();
        }


        // Updates UtcLines
        protected override void OnResume()
        {
            UpdateMetarUtcLine();
            UpdateTaforUtcLine();

            base.OnResume();
        }


        /// <summary>
        /// Gets the weather, from List or airporst in _icaoIdList
        /// </summary>
        private void RequestWeather()
        {
            // Instanstiate WxGet object
            var requestedWx = new WxGet();

            // Subscription to event handlers
            requestedWx.WorkRunning += OnGetWxWorkStarted;
            requestedWx.ConnectionTimeOut += OnConnectionTimeOut;
            requestedWx.ConnectionError += OnConnectionError;
            requestedWx.PercentageCompleted += OnPercentageCompleted;

            
            var linearlayoutWXmetarsTafors = FindViewById<LinearLayout>(Resource.Id.linearlayout_wx_metarstafors);

            // Close keyboard when button pressed
            var im = (InputMethodManager)GetSystemService(InputMethodService);
            im.HideSoftInputFromWindow(CurrentFocus.WindowToken, 0);



            // Remove all previous views from the linear layout
            linearlayoutWXmetarsTafors.RemoveAllViews();

            if (CrossConnectivity.Current.IsConnected)
            {
                Toast.MakeText(this, Resource.String.Requesting_METAR_Information, ToastLength.Long).Show();
                    
                // Start thread outside UI
                Task.Factory.StartNew(() =>
                {
                    // Start work in GetWX object
                    _wxInfo = requestedWx.Fetch(_icaoIdList, _hoursBefore, _metarOrTafor, _mostRecent);

                    // Call function to show the weather
                    ShowWeather();
                });
            }
            else
            {
                Toast.MakeText(this, Resource.String.Internet_Error, ToastLength.Short).Show();
            }
        }


        // Shows weather either live or from stored SharedPreferences
        private void ShowWeather()
        {
            var linearlayoutWXmetarsTafors = FindViewById<LinearLayout>(Resource.Id.linearlayout_wx_metarstafors);

            // Get and show weather information
            for (var i = 0; i < _wxInfo.AirportIDs.Count; i++)
            {
                if (_wxInfo.AirportErrors[i])
                {
                    var errorAirportName = new TextView(this)
                    {
                        Text = _myAirportsList[i] + " " + Resources.GetString(Resource.String.Error_fetching_airport)
                    };

                    // Apply common style
                    errorAirportName = ApplyErrorLineStyle(errorAirportName);

                    RunOnUiThread(() =>
                    {
                        linearlayoutWXmetarsTafors.AddView(errorAirportName);
                    });
                }
                else
                {

                    // AIRPORT NAME LINES
                    var airportName = new TextView(this);

                    // Try to get the airport's name from existing _myAirportDefinition List
                    try
                    {
                        for (int j = 0; j < _myAirportDefinitions.Count; j++)
                        {
                            if (_myAirportDefinitions[j].icao == _wxInfo.AirportIDs[i].ToUpper())
                            {
                                airportName.Text = _myAirportsList[i] + " - " + _myAirportDefinitions[j].icao;
                                break;
                            }

                        }
                    }
                    catch
                    {
                        airportName.Text = _myAirportsList[i];
                    }


                    airportName = ApplyAirportIDLineStyle(airportName);

                    RunOnUiThread(() =>
                    {
                        linearlayoutWXmetarsTafors.AddView(airportName);
                    });



                    // METAR DATETIME LINE
                    if (_wxInfo.AirportMetarsUtc[i][0] != DateTime.MinValue)
                    {
                        var metarUtcLine = new TextView(this);

                        var timeComparison = DateTime.UtcNow - _wxInfo.AirportMetarsUtc[i][0];

                        // Convert to readable time comparison
                        metarUtcLine.Text = ParseToReadableUtc(timeComparison.Duration(), "metar");

                        // Apply common style
                        metarUtcLine = ApplyUTCLineStyle(metarUtcLine, timeComparison, "metar");

                        RunOnUiThread(() =>
                        {
                            linearlayoutWXmetarsTafors.AddView(metarUtcLine);
                        });

                        // Save dictionary of TextViews so that we can update the time difference later on
                        _metarUtcFieldsIds.Add(metarUtcLine.Id.ToString(), _wxInfo.AirportMetarsUtc[i][0]);
                    }



                    // METAR LINES
                    foreach (var m in _wxInfo.AirportMetars[i])
                    {
                        // If we don't request METARS, we don't want to add an empty line
                        if (m == null) continue;


                        // TODO: ON TEST
                        var colorCoder = new WxColorCoder();
                        var coloredMetar = colorCoder.ColorCodeMetar(m);
                        ////////////////

                        var metarLines = new TextView(this);
                        metarLines.TextFormatted = coloredMetar;


                        // Apply common style
                        metarLines = ApplyMetarLineStyle(metarLines);

                        RunOnUiThread(() =>
                        {
                            linearlayoutWXmetarsTafors.AddView(metarLines);
                        });
                    }
                    


                    // TAFOR DATETIME LINE
                    if (_wxInfo.AirportTaforsUtc[i][0] != DateTime.MinValue)
                    {
                        var taforUtcLine = new TextView(this);

                        var timeComparison = DateTime.UtcNow - _wxInfo.AirportTaforsUtc[i][0];

                        // Convert to readable time comparison
                        taforUtcLine.Text = ParseToReadableUtc(timeComparison.Duration(), "tafor");

                        // Apply common style
                        taforUtcLine = ApplyUTCLineStyle(taforUtcLine, timeComparison, "tafor");

                        RunOnUiThread(() =>
                        {
                            linearlayoutWXmetarsTafors.AddView(taforUtcLine);
                        });

                        // Save dictionary of TextViews so that we can update the time difference later on
                        _taforUtcFieldsIds.Add(taforUtcLine.Id.ToString(), _wxInfo.AirportTaforsUtc[i][0]);
                    }



                    // TAFOR LINES
                    foreach (string f in _wxInfo.AirportTafors[i])
                    {
                        // If we don't request TAFORS, we don't want to add an empty line
                        if (f == null) continue;
                        var taforLines = new TextView(this)
                        {
                            Text = f
                        };

                        // Apply common style
                        taforLines = ApplyTaforLineStyle(taforLines);

                        RunOnUiThread(() =>
                        {
                            linearlayoutWXmetarsTafors.AddView(taforLines);
                        });
                    }
                }
            }

        }


        private string ParseToReadableUtc(TimeSpan timeComparison, string type)
        {
            string readableUtc;
            var readableUtcStart = string.Empty;

            if (type == "metar")
            {
                readableUtcStart = Resources.GetString(Resource.String.Metar_Issued);
            }
            else if (type == "tafor")
            {
                readableUtcStart = Resources.GetString(Resource.String.Tafor_Issued);
            }

            var readableUtcEnd = Resources.GetString(Resource.String.Ago);
            var days = Resources.GetString(Resource.String.Days);
            var hours = Resources.GetString(Resource.String.Hours);
            var minutes = Resources.GetString(Resource.String.Minutes);
            var day = Resources.GetString(Resource.String.Day);
            var hour = Resources.GetString(Resource.String.Hour);
            var minute = Resources.GetString(Resource.String.Minute);

            if (timeComparison.Days > 1 && timeComparison.Hours > 1)
            {
                readableUtc =
                    $"{readableUtcStart} {timeComparison.Days} {days}, {timeComparison.Hours} {hours} {readableUtcEnd}";
            }
            else if (timeComparison.Days == 1 && timeComparison.Hours > 1)
            {
                readableUtc =
                    $"{readableUtcStart} {timeComparison.Days} {day}, {timeComparison.Hours} {hours} {readableUtcEnd}";
            }
            else if (timeComparison.Days > 1 && timeComparison.Hours == 1)
            {
                readableUtc =
                    $"{readableUtcStart} {timeComparison.Days} {days}, {timeComparison.Hours} {hour} {readableUtcEnd}";
            }
            else if (timeComparison.Days == 1 && timeComparison.Hours == 1)
            {
                readableUtc =
                    $"{readableUtcStart} {timeComparison.Days} {day}, {timeComparison.Hours} {hour} {readableUtcEnd}";
            }
            else if(timeComparison.Days < 1 && timeComparison.Hours > 1 && timeComparison.Minutes > 1)
            {
                readableUtc =
                    $"{readableUtcStart} {timeComparison.Hours} {hours}, {timeComparison.Minutes} {minutes} {readableUtcEnd}";
            }
            else if (timeComparison.Days < 1 && timeComparison.Hours == 1 && timeComparison.Minutes > 1)
            {
                readableUtc =
                    $"{readableUtcStart} {timeComparison.Hours} {hour}, {timeComparison.Minutes} {minutes} {readableUtcEnd}";
            }
            else if (timeComparison.Days < 1 && timeComparison.Hours > 1 && timeComparison.Minutes == 1)
            {
                readableUtc =
                    $"{readableUtcStart} {timeComparison.Hours} {hours}, {timeComparison.Minutes} {minute} {readableUtcEnd}";
            }
            else if (timeComparison.Days < 1 && timeComparison.Hours == 1 && timeComparison.Minutes == 1)
            {
                readableUtc =
                    $"{readableUtcStart} {timeComparison.Hours} {hour}, {timeComparison.Minutes} {minute} {readableUtcEnd}";
            }
            else if (timeComparison.Days < 1 && timeComparison.Hours < 1 && timeComparison.Minutes > 1)
            {
                readableUtc = $"{readableUtcStart} {timeComparison.Minutes} {minutes} {readableUtcEnd}";
            }
            else
            {
                readableUtc = $"{readableUtcStart} {timeComparison.Minutes} {minute} {readableUtcEnd}";
            }
            
            return readableUtc;
        }


        // Configuration for error lines
        private TextView ApplyErrorLineStyle(TextView errorAirportName)
        {
            errorAirportName.SetTextColor(Color.Red);
            errorAirportName.SetTextSize(ComplexUnitType.Dip, 16);
            LinearLayout.LayoutParams airportTextViewParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            airportTextViewParams.SetMargins(0, 40, 0, 0);
            errorAirportName.LayoutParameters = airportTextViewParams;
            return errorAirportName;
        }


        // Configuration for airport lines
        private TextView ApplyAirportIDLineStyle(TextView airportName)
        {
            airportName.SetTextColor(Color.Magenta);
            airportName.SetTextSize(ComplexUnitType.Dip, 16);
            LinearLayout.LayoutParams airportTextViewParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            airportTextViewParams.SetMargins(0, 50, 0, 0);
            airportName.LayoutParameters = airportTextViewParams;
            return airportName;
        }


        // Configuration for metar UTC lines
        private TextView ApplyUTCLineStyle(TextView utcLine, TimeSpan timeComparison, string type)
        {

            if (type == "metar")
            {
                if (timeComparison.Hours >= 6)
                {
                    utcLine.SetTextColor(Color.Red);
                }
                else if (timeComparison.Hours >= 2)
                {
                    utcLine.SetTextColor(Color.Yellow);
                }
                else
                {
                    utcLine.SetTextColor(Color.Green);
                }
            }
            else if (type == "tafor")
            {
                if (timeComparison.Hours >= 18)
                {
                    utcLine.SetTextColor(Color.Red);
                }
                else if (timeComparison.Hours >= 6)
                {
                    utcLine.SetTextColor(Color.Yellow);
                }
                else
                {
                    utcLine.SetTextColor(Color.Green);
                }
            }
                

            utcLine.SetTextSize(ComplexUnitType.Dip, 14);
            var wxTextViewParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            wxTextViewParams.SetMargins(15, 20, 0, 0);
            utcLine.LayoutParameters = wxTextViewParams;
            utcLine.Id = View.GenerateViewId();

            return utcLine;
        }


        // Configuration for metar lines
        private TextView ApplyMetarLineStyle(TextView metarLines)
        {
            metarLines.SetTextColor(Color.WhiteSmoke);
            metarLines.SetTextSize(ComplexUnitType.Dip, 14);
            var wxTextViewParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            wxTextViewParams.SetMargins(25, 5, 0, 0);
            metarLines.LayoutParameters = wxTextViewParams;
            return metarLines;
        }


        // Configuration for tafor lines
        private TextView ApplyTaforLineStyle(TextView taforLines)
        {
            taforLines.SetTextColor(Color.WhiteSmoke);
            taforLines.SetTextSize(ComplexUnitType.Dip, 14);
            LinearLayout.LayoutParams wxTextViewParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            wxTextViewParams.SetMargins(25, 5, 0, 0);
            taforLines.LayoutParameters = wxTextViewParams;
            return taforLines;
        }


        // Eventhandler to activate and deactivate the button while information is being fetched
        // The event triggers twice with different arguments 
        private void OnGetWxWorkStarted(object source, WxGetEventArgs e)
        {
            if (e.Running)
            {
                RunOnUiThread(() =>
                {
                    _wxRequestButton.Enabled = false;
                });
            }
            else
            {

                RunOnUiThread(() =>
                {
                    _wxRequestButton.Enabled = true;

                    if (e.Error)
                    {
                        Toast.MakeText(this, Resource.String.Server_Timeout, ToastLength.Short).Show();
                    }

                });
            }

        }


        // Eventhandler to show Toast
        private void OnConnectionTimeOut(object source, EventArgs e)
        {
            Toast.MakeText(this, Resource.String.Server_Timeout, ToastLength.Short).Show();
        }

        // Eventhandler to show Toast
        private void OnConnectionError(object source, EventArgs e)
        {
            Toast.MakeText(this, Resource.String.Connection_Error, ToastLength.Short).Show();
        }


        // Eventhandler to update _metarOrTafor value from Dialog
        private void OnMetarOrTaforChanged(object source, WXOptionsDialogEventArgs e)
        {
            _metarOrTafor = e.MetarOrTafor;
        }


        // Eventhandler to update _hoursBefore value from Dialog
        private void OnHoursBeforeChanged(object source, WXOptionsDialogEventArgs e)
        {
            // If we selected "LAST", we are going to look for 24 hours and
            // just return the last one
            if (e.HoursBefore == 0)
            {
                _hoursBefore = 24;
                _mostRecent = true;
            }
            else
            {
                _hoursBefore = e.HoursBefore;
                _mostRecent = false;
            }
        }


        // Eventhandler to update _saveData value from Dialog
        private void OnSaveDataChanged(object source, WXOptionsDialogEventArgs e)
        {
            _saveData = e.SaveData;
        }


        // Eventhandler to update UTC Times every minute
        private void OnTimedUtcEvent(object state)
        {
            UpdateMetarUtcLine();
            UpdateTaforUtcLine();
        }


        // Eventhandler to update ProgressDialog
        private async void OnPercentageCompleted(object source, WxGetEventArgs e)
        {
            RunOnUiThread(() =>
            {
                // Show the airport and percentage
                _wxProgressDialog.SetMessage(e.Airport);
                _wxProgressDialog.Progress = e.PercentageCompleted;
            });

            // If we reached a 100%, we will wait a bit to that users can see the whole bar
            if (e.PercentageCompleted == 100)
            {
                await Task.Delay(250);
                _wxProgressDialog.Dismiss();
            }
            
        }

        

        private void UpdateMetarUtcLine()
        {
            foreach (var pair in _metarUtcFieldsIds)
            {
                var utcTextView = FindViewById<TextView>(int.Parse(pair.Key));

                var utcNow = DateTime.UtcNow;
                var timeComparison = utcNow - pair.Value;

                var utcString = ParseToReadableUtc(timeComparison.Duration(), "metar");


                RunOnUiThread(() =>
                {
                    if (timeComparison.Hours > 6)
                    {
                        utcTextView.SetTextColor(Color.Red);
                    }
                    else if (timeComparison.Hours >= 2)
                    {
                        utcTextView.SetTextColor(Color.Yellow);
                    }
                    else
                    {
                        utcTextView.SetTextColor(Color.Green);
                    }

                    utcTextView.Text = utcString;
                });


            }
        }


        private void UpdateTaforUtcLine()
        {
            foreach (var pair in _taforUtcFieldsIds)
            {
                var utcTextView = FindViewById<TextView>(int.Parse(pair.Key));

                var utcNow = DateTime.UtcNow;
                var timeComparison = utcNow - pair.Value;

                var utcString = ParseToReadableUtc(timeComparison.Duration(), "tafor");


                RunOnUiThread(() =>
                {
                    if (timeComparison.Hours >= 18)
                    {
                        utcTextView.SetTextColor(Color.Red);
                    }
                    else if (timeComparison.Hours >= 6)
                    {
                        utcTextView.SetTextColor(Color.Yellow);
                    }
                    else
                    {
                        utcTextView.SetTextColor(Color.Green);
                    }

                    utcTextView.Text = utcString;
                });


            }
        }


        // Recovers or sets configuration from Shared Preferences
        private void GetPreferences()
        {
            ISharedPreferences wxprefs = 
                Application.Context.GetSharedPreferences("WX_Preferences", FileCreationMode.Private);


            // First initialization _metarOrTafor
            _metarOrTafor = wxprefs.GetString("metarOrTaforPREF", String.Empty);
            if (_metarOrTafor == String.Empty)
            {
                wxprefs.Edit().PutString("metarOrTaforPREF", "metar_and_tafor").Apply();
                _metarOrTafor = "metar_and_tafor";
            }
            else
            {
                _metarOrTafor = wxprefs.GetString("metarOrTaforPREF", String.Empty);
            }


            // First initialization _hoursBefore
            if (wxprefs.GetString("hoursBeforePREF", String.Empty) == String.Empty)
            {
                // First time, we look for 24 hours but show just the last
                _hoursBefore = 24;
                _mostRecent = true;
            }
            else
            {
                int hours = Int32.Parse(wxprefs.GetString("hoursBeforePREF", String.Empty));

                // We selected "LAST"
                if (hours == 0)
                {
                    // We look for 24 hours but show just the last
                    _hoursBefore = 24;
                    _mostRecent = true;
                }
                // We selected another value
                else
                {
                    _hoursBefore = hours;
                    _mostRecent = false;
                }
            }


            // First initialization _saveData
            if (wxprefs.GetString("saveDataPREF", String.Empty) == String.Empty)
            {
                _saveData = true;
            }
            else
            {
                string config = wxprefs.GetString("saveDataPREF", String.Empty);
                switch (config)
                {
                    case "true":
                        _saveData = true;
                        break;
                    case "false":
                        _saveData = false;
                        break;
                }
                
            }
        }

    }
}

