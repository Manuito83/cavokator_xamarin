//
// CAVOKATOR APP
// Website: https://github.com/Manuito83/Cavokator
// License GNU General Public License v3.0
// Manuel Ortega, 2018
//

using Android.Content;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;


namespace Cavokator
{
    class AboutFragment : Android.Support.V4.App.Fragment
    {

        // Main fields
        private LinearLayout _about_backgroundLayout;
        private TextView _about_textAbout;
        private TextView _about_textContact;
        private TextView _about_textContact2;
        private TextView _about_textContribute;
        private TextView _about_textContribute2;
        private TextView _about_textChangelog;
        private TextView _about_textChangelog2;
        private TextView _about_textWarning;
        private TextView _about_textWarningLong;

        // View that will be used for FindViewById
        private View thisView;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            ((AppCompatActivity)Activity).SupportActionBar.Title = "About";

            HasOptionsMenu = true;
        }

        
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // In order to return the view for this Fragment
            thisView = inflater.Inflate(Resource.Layout.about_fragment, container, false);

            ApplyStyle();

            
            // Open email app if link clicked
            _about_textContact2.Click += delegate
            {
                try
                {
                    var email = new Intent(Intent.ActionSend);
                    email.PutExtra(Intent.ExtraEmail, new string[] { "info@cavokator.com"});
                    email.SetType("message/rfc822");
                    StartActivity(email);
                }
                catch { }
            };

            // Open browser app if link clicked
            _about_textContribute2.Click += delegate
            {
                try
                {
                    var uri = Android.Net.Uri.Parse("http://www.cavokator.com");
                    var intent = new Intent(Intent.ActionView, uri);
                    StartActivity(intent);
                }
                catch { }
            };

            // Show changelog dialog if clicked
            _about_textChangelog2.Click += delegate
            {
                // If there is a changelog prepared
                if (ActivityWxMain.versionWithChangelog)
                {
                    // Pull up dialog
                    var transaction = FragmentManager.BeginTransaction();
                    var changelogDialog = new ChangelogDialog();
                    changelogDialog.Show(transaction, "changelog_dialog");
                }
                // Else, show error
                else
                {
                    Toast.MakeText(Activity, Resource.String.about_noChangelog, ToastLength.Short).Show();
                }
            };


            return thisView;
        }

        private void ApplyStyle()
        {
            // FindViewById
            _about_backgroundLayout = thisView.FindViewById<LinearLayout>(Resource.Id.about_backgroundLayout);
            _about_textAbout = thisView.FindViewById<TextView>(Resource.Id.about_textAbout);
            _about_textContact = thisView.FindViewById<TextView>(Resource.Id.about_textContact);
            _about_textContact2 = thisView.FindViewById<TextView>(Resource.Id.about_textContact2);
            _about_textContribute = thisView.FindViewById<TextView>(Resource.Id.about_textContribute);
            _about_textContribute2 = thisView.FindViewById<TextView>(Resource.Id.about_textContribute2);
            _about_textChangelog = thisView.FindViewById<TextView>(Resource.Id.about_textChangelog);
            _about_textChangelog2 = thisView.FindViewById<TextView>(Resource.Id.about_textChangelog2);
            _about_textWarning = thisView.FindViewById<TextView>(Resource.Id.about_textWarning);
            _about_textWarningLong = thisView.FindViewById<TextView>(Resource.Id.about_textWarningLong);

            // Styling
            _about_backgroundLayout.SetBackgroundColor(new ApplyTheme().GetColor(DesiredColor.MainBackground));
            _about_textAbout.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            _about_textContact.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            _about_textContact2.SetTextColor(new ApplyTheme().GetColor(DesiredColor.CyanText));
            _about_textContribute.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            _about_textContribute2.SetTextColor(new ApplyTheme().GetColor(DesiredColor.CyanText));
            _about_textChangelog.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            _about_textChangelog2.SetTextColor(new ApplyTheme().GetColor(DesiredColor.CyanText));
            _about_textWarning.SetTextColor(new ApplyTheme().GetColor(DesiredColor.RedTextWarning));
            _about_textWarningLong.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));

            // Strings
            _about_textAbout.Text = Resources.GetString(Resource.String.about_textAbout);
            _about_textContact.Text = Resources.GetString(Resource.String.about_textContact);
            _about_textContact2.Text = Resources.GetString(Resource.String.about_textContact2);
            _about_textContribute.Text = Resources.GetString(Resource.String.about_textContribute);
            _about_textContribute2.Text = Resources.GetString(Resource.String.about_textContribute2);
            _about_textChangelog.Text = Resources.GetString(Resource.String.about_textChangelog);
            _about_textChangelog2.Text = Resources.GetString(Resource.String.about_textChangelog2);
            _about_textWarning.Text = Resources.GetString(Resource.String.about_textWarning);
            _about_textWarningLong.Text = Resources.GetString(Resource.String.about_textWarningLong);
        }
    }
}