
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Support.V4.Content;
using System;

namespace Cavokator
{

    public enum DesiredColor
    {
        MainBackground,
        MainText, 
        RedTextWarning,
        YellowTextWarning,
        GreenTextWarning,
        CyanTextWarning,
        MagentaTextWarning,
        TextHint        
    }


    class ApplyTheme
    {
        // Current theme selection
        private string _currentTheme;

        // COLORS ##LIGHT##
        private Color _color_mainBackground_LIGHT = Color.ParseColor("#e0e0e0");
        private Color _color_mainText_LIGHT = Color.ParseColor("#000000");
        private Color _color_redTextWarning_LIGHT = Color.ParseColor("#d60000");
        private Color _color_yellowTextWarning_LIGHT = Color.ParseColor("#ff6f00");
        private Color _color_greenTextWarning_LIGHT = Color.ParseColor("#1d781d");
        private Color _color_cyanTextWarning_LIGHT = Color.ParseColor("#039be5");
        private Color _color_magentaTextWarning_LIGHT = Color.ParseColor("#aa00ff");
        private Color _color_textHint_LIGHT = Color.ParseColor("#8C8C8C");

        // COLORS ##DARK##
        private Color _color_mainBackground_DARK = Color.ParseColor("#303030");
        private Color _color_mainText_DARK = Color.ParseColor("#ffffff");
        private Color _color_redTextWarning_DARK = Color.ParseColor("#ff1744");
        private Color _color_yellowTextWarning_DARK = Color.ParseColor("#ffea00");
        private Color _color_greenTextWarning_DARK = Color.ParseColor("#64dd17");
        private Color _color_cyanTextWarning_DARK = Color.ParseColor("#00e5ff");
        private Color _color_magentaTextWarning_DARK = Color.ParseColor("#e040fb");
        private Color _color_textHint_DARK = Color.ParseColor("#e0e0e0");


        public Color GetColor (DesiredColor mDesiredColor)
        {

            GetCurrentTheme();

            Color myColor = Color.Pink;

            switch (mDesiredColor)
            {
                case DesiredColor.MainBackground:
                    if (_currentTheme == "LIGHT")
                        myColor = _color_mainBackground_LIGHT;
                    else if (_currentTheme == "DARK")
                        myColor = _color_mainBackground_DARK;
                    break;
                case DesiredColor.MainText:
                    if (_currentTheme == "LIGHT")
                        myColor = _color_mainText_LIGHT;
                    else if (_currentTheme == "DARK")
                        myColor = _color_mainText_DARK;
                    break;
                case DesiredColor.RedTextWarning:
                    if (_currentTheme == "LIGHT")
                        myColor = _color_redTextWarning_LIGHT;
                    else if (_currentTheme == "DARK")
                        myColor = _color_redTextWarning_DARK;
                    break;
                case DesiredColor.YellowTextWarning:
                    if (_currentTheme == "LIGHT")
                        myColor = _color_yellowTextWarning_LIGHT;
                    else if (_currentTheme == "DARK")
                        myColor = _color_yellowTextWarning_DARK;
                    break;
                case DesiredColor.GreenTextWarning:
                    if (_currentTheme == "LIGHT")
                        myColor = _color_greenTextWarning_LIGHT;
                    else if (_currentTheme == "DARK")
                        myColor = _color_greenTextWarning_DARK;
                    break;
                case DesiredColor.CyanTextWarning:
                    if (_currentTheme == "LIGHT")
                        myColor = _color_cyanTextWarning_LIGHT;
                    else if (_currentTheme == "DARK")
                        myColor = _color_cyanTextWarning_DARK;
                    break;
                case DesiredColor.MagentaTextWarning:
                    if (_currentTheme == "LIGHT")
                        myColor = _color_magentaTextWarning_LIGHT;
                    else if (_currentTheme == "DARK")
                        myColor = _color_magentaTextWarning_DARK;
                    break;
                case DesiredColor.TextHint:
                    if (_currentTheme == "LIGHT")
                        myColor = _color_textHint_LIGHT;
                    else if (_currentTheme == "DARK")
                        myColor = _color_textHint_DARK;
                    break;
            }

            // In case of error
            return myColor;

        }

        private void GetCurrentTheme()
        {
            ISharedPreferences mThemePrefs = Application.Context.GetSharedPreferences("App_Preferences", FileCreationMode.Private);

            // First initialization Theme
            _currentTheme = mThemePrefs.GetString("themePREF", String.Empty);
            if (_currentTheme == String.Empty)
            {
                mThemePrefs.Edit().PutString("themePREF", "LIGHT").Apply();
                _currentTheme = "LIGHT";
            }
        }

        public void SetTheme (string themeName)
        {
            ISharedPreferences mThemePrefs = Application.Context.GetSharedPreferences("App_Preferences", FileCreationMode.Private);

            if (themeName == "LIGHT")
            {
                mThemePrefs.Edit().PutString("themePREF", "LIGHT").Apply();
                _currentTheme = "LIGHT";
            }
            else
            {
                mThemePrefs.Edit().PutString("themePREF", "DARK").Apply();
                _currentTheme = "DARK";
            }

        }

    }
}