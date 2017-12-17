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

namespace Cavokator
{
    public class SettingsFragment : Android.Support.V4.App.Fragment
    {

        // Main fields
        private RelativeLayout _mainRelativeLayout;
        // Title
        private TextView _mainTitle;
        // Theme
        private TextView _textTheme;
        private Switch _themeSwitch;
        private TextView _themeSwitchText;

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

            return thisView;
        }


        private void ApplyStyle()
        {
            // FindViewById
            _mainRelativeLayout = thisView.FindViewById<RelativeLayout>(Resource.Id.settings_mainRelativeLayout);
            _mainTitle = thisView.FindViewById<TextView>(Resource.Id.settings_mainTitle);
            _textTheme = thisView.FindViewById<TextView>(Resource.Id.settings_textTheme);
            _themeSwitch = thisView.FindViewById<Switch>(Resource.Id.settings_themeSwitch);
            _themeSwitchText = thisView.FindViewById<TextView>(Resource.Id.settings_themeSwitchText);

            // Styling
            _mainRelativeLayout.SetBackgroundColor(new ApplyTheme().GetColor(DesiredColor.MainBackground));
            _textTheme.SetBackgroundColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            _themeSwitchText.SetBackgroundColor(new ApplyTheme().GetColor(DesiredColor.MainText));

            // Strings
            // TODO: REPLACE AND ADD
            // _introTextView.Text = Resources.GetString(Resource.String.condition_Intro);

        }



    }
}