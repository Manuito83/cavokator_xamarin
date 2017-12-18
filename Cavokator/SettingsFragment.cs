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
using Android.Support.V7.Widget;

namespace Cavokator
{
    public class SettingsFragment : Android.Support.V4.App.Fragment
    {

        // Main fields
        private LinearLayout _backgroundLayout;
        // Title
        private TextView _mainTitle;
        // Theme
        private TextView _textTheme;
        private SwitchCompat _themeSwitch;
        private TextView _themeSwitchText;

        // Settings Preferences
        private string _currentTheme;

        // View that will be used for FindViewById
        private View thisView;


        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }


        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // In order to return the view for this Fragment
            thisView = inflater.Inflate(Resource.Layout.settings_fragment, container, false);

            // Assign fields and style
            ApplyStyle();

            // What are the current settings (or initialize in first run)?
            GetCurrentPreferences();

            // Adjust switches, sliders...
            ApplyCurrentPreferences();

            // Theme swith controls
            _themeSwitch.CheckedChange += delegate
            {
                ISharedPreferences mSettingsPrefs = Application.Context.GetSharedPreferences("Settings_Preferences", FileCreationMode.Private);

                if (!_themeSwitch.Checked)
                {
                    _currentTheme = "light";
                    mSettingsPrefs.Edit().PutString("themePREF", "light").Apply();
                    _themeSwitchText.Text = Resources.GetString(Resource.String.settings_themeSwitchLight);
                    new ApplyTheme().SetTheme("LIGHT");
                    ApplyStyle();
                }
                else
                {
                    _currentTheme = "dark";
                    mSettingsPrefs.Edit().PutString("themePREF", "dark").Apply();
                    _themeSwitchText.Text = Resources.GetString(Resource.String.settings_themeSwitchDark);
                    new ApplyTheme().SetTheme("DARK");
                    ApplyStyle();
                }
            };

            return thisView;
        }


        private void ApplyStyle()
        {
            // FindViewById
            _backgroundLayout = thisView.FindViewById<LinearLayout>(Resource.Id.settings_backgroundLayout);
            _mainTitle = thisView.FindViewById<TextView>(Resource.Id.settings_mainTitle);
            _textTheme = thisView.FindViewById<TextView>(Resource.Id.settings_textTheme);
            _themeSwitch = thisView.FindViewById<SwitchCompat>(Resource.Id.settings_themeSwitch);
            _themeSwitchText = thisView.FindViewById<TextView>(Resource.Id.settings_themeSwitchText);

            // Styling
            _backgroundLayout.SetBackgroundColor(new ApplyTheme().GetColor(DesiredColor.MainBackground));
            _textTheme.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            _themeSwitchText.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));

            // Strings
            _mainTitle.Text = Resources.GetString(Resource.String.settings_settingsTitle);
            _textTheme.Text = Resources.GetString(Resource.String.settings_textTheme);
        }

        private void GetCurrentPreferences()
        {
            ISharedPreferences mSettingsPrefs = Application.Context.GetSharedPreferences("Settings_Preferences", FileCreationMode.Private);
            
            // First initialization Theme
            _currentTheme = mSettingsPrefs.GetString("themePREF", String.Empty);
            if (_currentTheme == String.Empty)
            {
                mSettingsPrefs.Edit().PutString("themePREF", "light").Apply();
                _currentTheme = "light";
            }
        }

        private void ApplyCurrentPreferences()
        {
            if (_currentTheme == "light")
            {
                _themeSwitch.Checked = false;
                _themeSwitchText.Text = Resources.GetString(Resource.String.settings_themeSwitchLight);
            }
            else
            {
                _themeSwitch.Checked = true;
                _themeSwitchText.Text = Resources.GetString(Resource.String.settings_themeSwitchDark);
            }

        }
    }
}