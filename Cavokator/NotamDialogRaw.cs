using System;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace Cavokator 
{
    class NotamDialogRaw : Android.Support.V4.App.DialogFragment
    {
        private string mNotamID;
        private string mNotamText;

        private LinearLayout _notamRawBaseLayout;
        private TextView _notamRawId;
        private TextView _notamRawText;
        private Button _notamRawDismissDialogButton;

        public NotamDialogRaw(string notamId, string fullNotam)
        {
            mNotamID = notamId;
            mNotamText = fullNotam;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);

            // Inflate view
            var view = inflater.Inflate(Resource.Layout.notam_dialog_raw, container, false);

            // FindviewById and styling
            StyleViews(view);

            _notamRawDismissDialogButton.Click += OnDismissButtonClicked;

            // Slide in/out animation
            SlideAnimation(savedInstanceState);

            // Show Notam
            ShowNotam();

            return view;
        }

        private void StyleViews(View view)
        {
            // Find view IDs
            _notamRawBaseLayout = view.FindViewById<LinearLayout>(Resource.Id.notam_rawDialog_baseLinearLayout);
            _notamRawId = view.FindViewById<TextView>(Resource.Id.notam_rawDialog_notamId);
            _notamRawText = view.FindViewById<TextView>(Resource.Id.notam_rawDialog_notamText);
            _notamRawDismissDialogButton = view.FindViewById<Button>(Resource.Id.notam_rawDialog_closeButton);

            _notamRawBaseLayout.SetBackgroundColor(new ApplyTheme().GetColor(DesiredColor.MainBackground));
            _notamRawId.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MagentaText));
            _notamRawText.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));

            _notamRawDismissDialogButton.Text = Resources.GetString(Resource.String.OK);
        }

        private void OnDismissButtonClicked(object sender, EventArgs e)
        {
            Dismiss();
        }

        private void SlideAnimation(Bundle savedInstanceState)
        {
            // Sets the title bar to invisible
            Dialog.Window.RequestFeature(WindowFeatures.NoTitle);

            base.OnActivityCreated(savedInstanceState);

            // Sets the animation
            Dialog.Window.Attributes.WindowAnimations = Resource.Style.dialog_animation;
        }

        private void ShowNotam()
        {
            _notamRawId.Text = "NOTAM " + mNotamID;
            _notamRawText.Text = mNotamText;
        }
    }
}