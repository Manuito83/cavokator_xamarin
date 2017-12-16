using Android.App;
using Android.OS;
using Android.Content;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using System;

namespace Cavokator
{
    public class ConditionFragment : Android.Support.V4.App.Fragment
    {

        // Main fields
        private RelativeLayout _relativeLayoutMain;
        private TextView _introTextView;
        private EditText _conditionEntryEditText;
        private Button _conditionRequestButton;
        // TODO: ADD HERE CLEAR BUTTON!
        private TextView _exampleTextView;
        private TextView _example1TextView;
        private TextView _example2TextView;
        private TextView _example3TextView;
        private TextView _example4TextView;


        // View that will be used for FindViewById
        private View thisView;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // In order to return the view for this Fragment
            thisView = inflater.Inflate(Resource.Layout.condition_fragment, container, false);

            // Assign fields and style
            ApplyStyle();

            // Subscribe events
            _conditionRequestButton.Click += OnRequestButtonClicked;



            // Close keyboard when click outside airport_entry EditText
            _relativeLayoutMain.Touch += delegate
            {
                var imm = (InputMethodManager)Application.Context.GetSystemService(Context.InputMethodService);
                imm.HideSoftInputFromWindow(_conditionEntryEditText.WindowToken, 0);
            };



            return thisView;
        }


        // Action when wx request button is clicked
        private void OnRequestButtonClicked(object sender, EventArgs e)
        {
            
            // Clear focus of _condition_entry
            var conditiontEntry = thisView.FindViewById<EditText>(Resource.Id.condition_entry);
            conditiontEntry.ClearFocus();


            Activity.RunOnUiThread(() =>
            {
                // Pull up dialog
                var transaction = FragmentManager.BeginTransaction();
                var wxRwyCondDialog = new ConditionDialog(conditiontEntry.Text);
                wxRwyCondDialog.Show(transaction, "condition_dialog");
            });

        }


        private void ApplyStyle()
        {
            _relativeLayoutMain = thisView.FindViewById<RelativeLayout>(Resource.Id.condition_background);
            _introTextView = thisView.FindViewById<TextView>(Resource.Id.condition_intro);
            _conditionEntryEditText = thisView.FindViewById<EditText>(Resource.Id.condition_entry);
            _conditionRequestButton = thisView.FindViewById<Button>(Resource.Id.condition_decodeButton);
            _exampleTextView = thisView.FindViewById<TextView>(Resource.Id.condition_example);
            _example1TextView = thisView.FindViewById<TextView>(Resource.Id.condition_example1);
            _example2TextView = thisView.FindViewById<TextView>(Resource.Id.condition_example2);
            _example3TextView = thisView.FindViewById<TextView>(Resource.Id.condition_example3);
            _example4TextView = thisView.FindViewById<TextView>(Resource.Id.condition_example4);

            // Styling
            _relativeLayoutMain.SetBackgroundColor(new ApplyTheme().GetColor(DesiredColor.MainBackground));
            _introTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            _conditionEntryEditText.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));
            _exampleTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.TextHint));
            _example1TextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.TextHint));
            _example2TextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.TextHint));
            _example3TextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.TextHint));
            _example4TextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.TextHint));

            _introTextView.Text = Resources.GetString(Resource.String.condition_Intro);
            _conditionRequestButton.Text = Resources.GetString(Resource.String.condition_Decode);
            _exampleTextView.Text = Resources.GetString(Resource.String.condition_Example);
        }


    }
}