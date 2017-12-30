using System;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace Cavokator
{
    class ChangelogDialog : Android.Support.V4.App.DialogFragment
    {

        // Dialog fields
        private LinearLayout _changelog_mainBackground;
        private TextView _changelog_titleText;
        private TextView _changelog_whatIsNew;
        private TextView _changelog_bullet1;
        private TextView _changelog_bullet2;
        private TextView _changelog_bullet3;
        private TextView _changelog_bullet4;
        private TextView _changelog_item1Text;
        private TextView _changelog_item2Text;
        private TextView _changelog_item3Text;
        private TextView _changelog_item4Text;
        private TextView _changelog_warningText;
        private TextView _changelog_warningLongText;
        private Button _changelog_closeButton;

        // View that will be used for FindViewById
        private View thisView;


        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);

            // Inflate view
            thisView = inflater.Inflate(Resource.Layout.changelog_dialog, container, false);

            // Styling
            StyleViews();


            _changelog_closeButton.Click += delegate
            {
                this.Dismiss();
            };

            // Slide in/out animation
            SlideAnimation(savedInstanceState);

            return thisView;
        }

        private void SlideAnimation(Bundle savedInstanceState)
        {
            // Sets the title bar to invisible
            Dialog.Window.RequestFeature(WindowFeatures.NoTitle);

            base.OnActivityCreated(savedInstanceState);

            // Sets the animation
            Dialog.Window.Attributes.WindowAnimations = Resource.Style.dialog_animation;
        }

        private void StyleViews()
        {
            // Find by ID
            _changelog_mainBackground = thisView.FindViewById<LinearLayout>(Resource.Id.changelog_mainBackground);
            _changelog_titleText = thisView.FindViewById<TextView>(Resource.Id.changelog_titleText);
            _changelog_whatIsNew = thisView.FindViewById<TextView>(Resource.Id.changelog_whatIsNew);
            _changelog_bullet1 = thisView.FindViewById<TextView>(Resource.Id.changelog_bullet1);
            _changelog_bullet2 = thisView.FindViewById<TextView>(Resource.Id.changelog_bullet2);
            _changelog_bullet3 = thisView.FindViewById<TextView>(Resource.Id.changelog_bullet3);
            _changelog_bullet4 = thisView.FindViewById<TextView>(Resource.Id.changelog_bullet4);
            _changelog_item1Text = thisView.FindViewById<TextView>(Resource.Id.changelog_item1Text);
            _changelog_item2Text = thisView.FindViewById<TextView>(Resource.Id.changelog_item2Text);
            _changelog_item3Text = thisView.FindViewById<TextView>(Resource.Id.changelog_item3Text);
            _changelog_item4Text = thisView.FindViewById<TextView>(Resource.Id.changelog_item4Text);
            _changelog_warningText = thisView.FindViewById<TextView>(Resource.Id.changelog_warningText);
            _changelog_warningLongText = thisView.FindViewById<TextView>(Resource.Id.changelog_warningLongText);
            _changelog_closeButton = thisView.FindViewById<Button>(Resource.Id.changelog_closeButton);

            // Coloring
            _changelog_mainBackground.SetBackgroundColor(new ApplyTheme().GetColor(DesiredColor.MainBackground));
            _changelog_titleText.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MagentaText));
            _changelog_whatIsNew.SetTextColor(new ApplyTheme().GetColor(DesiredColor.RedTextWarning));
            _changelog_bullet1.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            _changelog_bullet2.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            _changelog_bullet3.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            _changelog_bullet4.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            _changelog_item1Text.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            _changelog_item2Text.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            _changelog_item3Text.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            _changelog_item4Text.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            _changelog_warningText.SetTextColor(new ApplyTheme().GetColor(DesiredColor.RedTextWarning));
            _changelog_warningLongText.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));

            // Strings
            _changelog_titleText.Text = Resources.GetString(Resource.String.changelog_titleText);
            _changelog_whatIsNew.Text = Resources.GetString(Resource.String.changelog_whatIsNew);
            _changelog_item1Text.Text = Resources.GetString(Resource.String.changelog_item1Text);
            _changelog_item2Text.Text = Resources.GetString(Resource.String.changelog_item2Text);
            _changelog_item3Text.Text = Resources.GetString(Resource.String.changelog_item3Text);
            _changelog_item4Text.Text = Resources.GetString(Resource.String.changelog_item4Text);
            _changelog_warningText.Text = Resources.GetString(Resource.String.changelog_warningText);
            _changelog_warningLongText.Text = Resources.GetString(Resource.String.changelog_warningLongText);
            _changelog_closeButton.Text = Resources.GetString(Resource.String.changelog_closeButton);

        }
    }
}