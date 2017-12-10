
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Support.V4.Content;


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
        MagentaTextWarning
    }


    class ApplyTheme
    {
        // Current theme selection
        private string _currentTheme = "LIGHT";

        // COLORS ##LIGHT##
        private Color _color_mainBackground_LIGHT = Color.ParseColor("#e0e0e0");
        private Color _color_mainText_LIGHT = Color.ParseColor("#000000");
        private Color _color_redTextWarning_LIGHT = Color.ParseColor("#d60000");
        private Color _color_yellowTextWarning_LIGHT = Color.ParseColor("#ff6f00");
        private Color _color_greenTextWarning_LIGHT = Color.ParseColor("#1d781d");
        private Color _color_cyanTextWarning_LIGHT = Color.ParseColor("#039be5");
        private Color _color_magentaTextWarning_LIGHT = Color.ParseColor("#aa00ff");



        public Color GetColor (DesiredColor mDesiredColor)
        {

            Color myColor = Color.Pink;

            switch (mDesiredColor)
            {
                case DesiredColor.MainBackground:
                    if (_currentTheme == "LIGHT")
                        myColor = _color_mainBackground_LIGHT;
                    break;

                case DesiredColor.MainText:
                    if (_currentTheme == "LIGHT")
                        myColor = _color_mainText_LIGHT;
                    break;
                case DesiredColor.RedTextWarning:
                    if (_currentTheme == "LIGHT")
                        myColor = _color_redTextWarning_LIGHT;
                    break;
                case DesiredColor.YellowTextWarning:
                    if (_currentTheme == "LIGHT")
                        myColor = _color_yellowTextWarning_LIGHT;
                    break;
                case DesiredColor.GreenTextWarning:
                    if (_currentTheme == "LIGHT")
                        myColor = _color_greenTextWarning_LIGHT;
                    break;
                case DesiredColor.CyanTextWarning:
                    if (_currentTheme == "LIGHT")
                        myColor = _color_cyanTextWarning_LIGHT;
                    break;
                case DesiredColor.MagentaTextWarning:
                    if (_currentTheme == "LIGHT")
                        myColor = _color_magentaTextWarning_LIGHT;
                    break;
            }

            // In case of error
            return myColor;

        }


        public string SetTheme (string themeName)
        {
            // IMPLEMENT
            return null;
        }


    }
}