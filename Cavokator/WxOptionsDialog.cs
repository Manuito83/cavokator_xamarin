using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using FR.Ganfra.Materialspinner;

namespace Cavokator
{
    class WxOptionsDialog : Android.Support.V4.App.DialogFragment
    {

        public event EventHandler<WXOptionsDialogEventArgs> SpinnerChanged;
        public event EventHandler<WXOptionsDialogEventArgs> SeekbarChanged;
        public event EventHandler<WXOptionsDialogEventArgs> SwitchChanged;
        public event EventHandler<WXOptionsDialogEventArgs> ColorWeatherChanged;
        public event EventHandler<WXOptionsDialogEventArgs> DivideTaforChanged;


        // Configuration header
        private LinearLayout _wx_mainbackground;
        private TextView _configurationText;

        // Type of weather group
        private MaterialSpinner _metarOrTaforSpinner;
        private TextView _metarOrTaforText;

        // Metar delay group
        private TextView _metarHoursText;
        private SeekBar _metarHoursSeekBar;
        private TextView _metarHoursSeekBarText;
        
        // Save data group
        private TextView _saveDataText;
        private Switch _saveDataSwitch;

        // Color Weather
        private TextView _colorWeatherText;
        private Switch _colorWeatherSwitch;

        // Divide tafor group
        private TextView _divideTaforText;
        private Switch _divideTaforSwitch;


        // Dismiss button
        private Button _dismissBialogButton;
        
        // Configuration
        private int _maxSpinnerHours = 12;

        private int _spinnerSelection;
        private int _hoursBefore;
        private bool _mostRecent;
        private bool _saveData;
        private bool _doColorWeather;
        private bool _doDivideTafor;

        // Convert strings to order in spinner
        public WxOptionsDialog(string metar_or_tafor, int hoursBefore, bool mostRecent, bool saveData,
                                bool colorWeather, bool divideTafor)
        {
            switch (metar_or_tafor)
            {
                case "metar_and_tafor":
                    _spinnerSelection = 0;
                    break;
                case "only_metar":
                    _spinnerSelection = 1;
                    break;
                case "only_tafor":
                    _spinnerSelection = 2;
                    break;
            }
            
            this._hoursBefore = hoursBefore;

            this._mostRecent = mostRecent;

            this._saveData = saveData;

            this._doColorWeather = colorWeather;

            this._doDivideTafor = divideTafor;
        }


        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);

            // Inflate view
            var view = inflater.Inflate(Resource.Layout.wx_options_dialog, container, false);

            // Styling
            StyleViews(view);


            // SPINNER ADAPTER CONFIG
            string[] ITEMS = { "METAR + TAFOR ", "METAR", "TAFOR" };
            var adapter = new ArrayAdapter<String>(Activity, Resource.Layout.wx_options_spinner, ITEMS);
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            _metarOrTaforSpinner.Adapter = adapter;

            _metarOrTaforSpinner.SetSelection(_spinnerSelection);

            _metarOrTaforSpinner.ItemSelected += delegate
            {
                // Call event raiser
                OnSpinnerChanged(_metarOrTaforSpinner.SelectedItemPosition);

                // Save ISharedPreference
                SetWeatherOrTaforPreferences(_metarOrTaforSpinner.SelectedItemPosition);
            };



            // SEEKBAR CONFIG

            // Set max hours
            _metarHoursSeekBar.Max = _maxSpinnerHours;

            // Set actual value to the one passed by main activity
            if (_mostRecent)
            {
                _metarHoursSeekBar.Progress = 0;
            }
            else
            {
                _metarHoursSeekBar.Progress = _hoursBefore;
            }


            // Set initial value for seekbar text
            _metarHoursSeekBarText.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));

            if (_metarHoursSeekBar.Progress == 0)
            {
                _metarHoursSeekBarText.Text = GetString(Resource.String.Option_JustGetLast);
            }
            else if (_metarHoursSeekBar.Progress == 1)
            {
                _metarHoursSeekBarText.Text = _metarHoursSeekBar.Progress.ToString()
                    + " " + GetString(Resource.String.Option_Hour);
            }
            else
            {
                _metarHoursSeekBarText.Text = _metarHoursSeekBar.Progress.ToString()
                    + " " + GetString(Resource.String.Option_Hours);
            }

            _metarHoursSeekBar.ProgressChanged += delegate
            {
                // We want to write "Last" instead of 0
                if (_metarHoursSeekBar.Progress == 0)
                {
                    _metarHoursSeekBarText.Text = GetString(Resource.String.Option_JustGetLast);
                }
                else if (_metarHoursSeekBar.Progress == 1)
                {
                    _metarHoursSeekBarText.Text = _metarHoursSeekBar.Progress.ToString()
                        + " " + GetString(Resource.String.Option_Hour);
                }
                else
                {
                    _metarHoursSeekBarText.Text = _metarHoursSeekBar.Progress.ToString()
                        + " " + GetString(Resource.String.Option_Hours);
                }


                // Call event raiser
                OnSeekbarChanged(_metarHoursSeekBar.Progress);

                // Save ISharedPreferences
                SetHoursBeforePreferences(_metarHoursSeekBar.Progress);

            };



            // SWITCH CONFIG

            if (_saveData)
            {
                _saveDataSwitch.Checked = true;
            }
            else
            {
                _saveDataSwitch.Checked = false;
            }

            _saveDataSwitch.CheckedChange += delegate
            {

                // Call event raiser with parameters
                if (_saveDataSwitch.Checked)
                {
                    OnSwitchChanged(true);

                    SetSaveDataPreferences(true);
                }
                else
                {
                    OnSwitchChanged(false);

                    SetSaveDataPreferences(false);
                }


            };



            // COLOR WEATHER CONFIG

            if (_doColorWeather)
            {
                _colorWeatherSwitch.Checked = true;
            }
            else
            {
                _colorWeatherSwitch.Checked = false;
            }

            _colorWeatherSwitch.CheckedChange += delegate
            {
                // Call event raiser with parameters
                if (_colorWeatherSwitch.Checked)
                {
                    OnColorWeatherChanged(true);

                    SetColorWeatherPreferences(true);
                }
                else
                {
                    OnColorWeatherChanged(false);

                    SetColorWeatherPreferences(false);
                }
            };



            // DIVIDE TAFOR CONFIG

            if (_doDivideTafor)
            {
                _divideTaforSwitch.Checked = true;
            }
            else
            {
                _divideTaforSwitch.Checked = false;
            }

            _divideTaforSwitch.CheckedChange += delegate
            {
                // Call event raiser with parameters
                if (_divideTaforSwitch.Checked)
                {
                    OnDivideTaforChanged(true);

                    SetDivideTaforPreferences(true);
                }
                else
                {
                    OnDivideTaforChanged(false);

                    SetDivideTaforPreferences(false);
                }
            };





            // CLOSE BUTTON (dismiss dialog)
            _dismissBialogButton.Click += delegate
            {
                this.Dismiss();
            };


            return view;
        }

        private void StyleViews(View view)
        {
            // Find view IDs
            _wx_mainbackground = view.FindViewById<LinearLayout>(Resource.Id.wx_options_linearlayoutBottom);
            _metarOrTaforSpinner = view.FindViewById<MaterialSpinner>(Resource.Id.wx_options_metarORtafor_spinner);
            _configurationText = view.FindViewById<TextView>(Resource.Id.wx_options_configuration_text);
            _metarOrTaforText = view.FindViewById<TextView>(Resource.Id.wx_options_metarORtafor_text);
            _metarHoursText = view.FindViewById<TextView>(Resource.Id.wx_options_metarHours);
            _metarHoursSeekBar = view.FindViewById<SeekBar>(Resource.Id.wx_options_metarHours_seekbar);
            _metarHoursSeekBarText = view.FindViewById<TextView>(Resource.Id.wx_option_metarHours_seekbarText);
            _dismissBialogButton = view.FindViewById<Button>(Resource.Id.wx_option_closeButton);
            _saveDataText = view.FindViewById<TextView>(Resource.Id.wx_options_saveDataText);
            _saveDataSwitch = view.FindViewById<Switch>(Resource.Id.wx_options_saveDataSwitch);
            _colorWeatherText = view.FindViewById<TextView>(Resource.Id.wx_options_colorWeatherText);
            _colorWeatherSwitch = view.FindViewById<Switch>(Resource.Id.wx_options_colorWeatherSwitch);
            _divideTaforText = view.FindViewById<TextView>(Resource.Id.wx_options_divideTaforText);
            _divideTaforSwitch = view.FindViewById<Switch>(Resource.Id.wx_options_divideTaforSwitch);


            // TODO: SPINNER COLORING!!!!!

            // Coloring
            _wx_mainbackground.SetBackgroundColor(new ApplyTheme().GetColor(DesiredColor.MainBackground));
            _configurationText.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MagentaText));
            _metarOrTaforText.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            _metarHoursText.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            _metarHoursSeekBarText.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            _saveDataText.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            _colorWeatherText.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            _divideTaforText.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            _divideTaforSwitch.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));


            // Assign text fields
            _configurationText.Text = Resources.GetString(Resource.String.Option_ConfigurationText);
            _metarOrTaforText.Text = Resources.GetString(Resource.String.Option_ChooseMetarOrTaforText);
            _metarHoursText.Text = Resources.GetString(Resource.String.Option_MetarHoursText);
            _saveDataText.Text = Resources.GetString(Resource.String.Option_SaveDataText);
            _colorWeatherText.Text = Resources.GetString(Resource.String.Option_ColorWeatherText);
            _divideTaforText.Text = Resources.GetString(Resource.String.Option_DivideTaforText);
        }

        private void SetSaveDataPreferences(bool saveData)
        {
            ISharedPreferences wxprefs = Application.Context.GetSharedPreferences("WX_Preferences", FileCreationMode.Private);

            if (saveData)
            {
                wxprefs.Edit().PutString("saveDataPREF", "true").Apply();
            }
            else
            {
                wxprefs.Edit().PutString("saveDataPREF", "false").Apply();
            }
            
        }


        private void SetHoursBeforePreferences(int progress)
        {
            ISharedPreferences wxprefs = Application.Context.GetSharedPreferences("WX_Preferences", FileCreationMode.Private);
            wxprefs.Edit().PutString("hoursBeforePREF", progress.ToString()).Apply();
        }


        private void SetWeatherOrTaforPreferences(int position)
        {

            string preference = String.Empty;

            switch (position)
            {
                case 0:
                    preference = "metar_and_tafor";
                    break;
                case 1:
                    preference = "only_metar";
                    break;
                case 2:
                    preference = "only_tafor";
                    break;
            }

            ISharedPreferences wxprefs = Application.Context.GetSharedPreferences("WX_Preferences", FileCreationMode.Private);
            wxprefs.Edit().PutString("metarOrTaforPREF", preference).Apply();
        }


        private void SetColorWeatherPreferences(bool colorWeather)
        {
            ISharedPreferences wxprefs = Application.Context.GetSharedPreferences("WX_Preferences", FileCreationMode.Private);

            if (colorWeather)
            {
                wxprefs.Edit().PutString("colorWeatherPREF", "true").Apply();
            }
            else
            {
                wxprefs.Edit().PutString("colorWeatherPREF", "false").Apply();
            }

        }


        private void SetDivideTaforPreferences(bool divideTafor)
        {
            ISharedPreferences wxprefs = Application.Context.GetSharedPreferences("WX_Preferences", FileCreationMode.Private);

            if (divideTafor)
            {
                wxprefs.Edit().PutString("divideTaforPREF", "true").Apply();
            }
            else
            {
                wxprefs.Edit().PutString("divideTaforPREF", "false").Apply();
            }

        }


        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            // Sets the title bar to invisible
            Dialog.Window.RequestFeature(WindowFeatures.NoTitle);

            base.OnActivityCreated(savedInstanceState);

            // Sets the animation
            Dialog.Window.Attributes.WindowAnimations = Resource.Style.dialog_animation;

        }


        // Event raiser
        protected virtual void OnSpinnerChanged(int position)
        {
            SpinnerChanged?.Invoke(this, new WXOptionsDialogEventArgs(position));
        }


        // Event raiser
        protected virtual void OnSeekbarChanged(int position)
        {
            SeekbarChanged?.Invoke(this, new WXOptionsDialogEventArgs() { HoursBefore = position } );
        }


        // Event raiser
        protected virtual void OnSwitchChanged(bool toggled)
        {
            SwitchChanged?.Invoke(this, new WXOptionsDialogEventArgs() { SaveData = toggled });
        }


        // Event raiser
        protected virtual void OnColorWeatherChanged(bool toggled)
        {
            ColorWeatherChanged?.Invoke(this, new WXOptionsDialogEventArgs() { ColorWeather = toggled });
        }


        // Event raiser
        protected virtual void OnDivideTaforChanged(bool toggled)
        {
            DivideTaforChanged?.Invoke(this, new WXOptionsDialogEventArgs() { DivideTafor = toggled });
        }

    }


    public class WXOptionsDialogEventArgs : EventArgs
    {

        public string MetarOrTafor { get; private set; }
        public int HoursBefore { get; set; }
        public bool SaveData { get; set; }
        public bool ColorWeather { get; set; }
        public bool DivideTafor { get; set; }

        public WXOptionsDialogEventArgs() { }

        public WXOptionsDialogEventArgs(int position)
        {
            switch (position)
            {
                case 0:
                    MetarOrTafor = "metar_and_tafor";
                    break;
                case 1:
                    MetarOrTafor = "only_metar";
                    break;
                case 2:
                    MetarOrTafor = "only_tafor";
                    break;
            }
        }

    }


}