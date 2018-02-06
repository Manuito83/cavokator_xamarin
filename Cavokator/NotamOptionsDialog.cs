//
// CAVOKATOR APP
// Website: https://github.com/Manuito83/Cavokator
// License GNU General Public License v3.0
// Manuel Ortega, 2018
//

using Android.OS;
using Android.Views;
using Android.Widget;
using FR.Ganfra.Materialspinner;
using System;
using Android.App;
using Android.Content;

namespace Cavokator
{
    internal class NotamOptionsDialog : Android.Support.V4.App.DialogFragment
    {

        public event EventHandler<NotamOptionsDialogEventArgs> SortBySpinnerChanged;

        // Configuration header
        private LinearLayout _notamMainbackground;
        private TextView _configurationText;
            
        // Sort by items
        private TextView _sortByTextView;
        private MaterialSpinner _sortBySpinner;
        private readonly int _spinnerSelection;

        // Dismiss button
        private Button _dismissDialogButton;

        public NotamOptionsDialog(string sortByCategory)
        {
            switch (sortByCategory)
            {
                case "category":
                    _spinnerSelection = 0;
                    break;
                case "date":
                    _spinnerSelection = 1;
                    break;
            }
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);

            // Inflate view
            var thisView = inflater.Inflate(Resource.Layout.notam_dialog_options, container, false);

            // Styling
            StyleViews(thisView);

            // Events
            _dismissDialogButton.Click += OnDismissButtonClicked;

            // SPINNER ADAPTER CONFIG
            string[] items = { Resources.GetString(Resource.String.NOTAM_categorySort) + "  ", Resources.GetString(Resource.String.NOTAM_dateSort) + "  "};
            var adapter = new ArrayAdapter<String>(Activity, Resource.Layout.notam_dialog_optionsSpinner, items);
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            _sortBySpinner.Adapter = adapter;
            _sortBySpinner.SetSelection(_spinnerSelection);
            _sortBySpinner.ItemSelected += delegate
            {
                // Call event raiser
                OnSpinnerChanged(_sortBySpinner.SelectedItemPosition);

                // Save ISharedPreference
                SetSortByPreferences(_sortBySpinner.SelectedItemPosition);
            };

            return thisView;
        }

        private void OnDismissButtonClicked(object sender, EventArgs e)
        {
            Dismiss();
        }

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            // Sets the title bar to invisible
            Dialog.Window.RequestFeature(WindowFeatures.NoTitle);

            base.OnActivityCreated(savedInstanceState);

            // Sets the animation
            Dialog.Window.Attributes.WindowAnimations = Resource.Style.dialog_animation;
        }

        private void StyleViews(View view)
        {
            // Find view IDs
            _notamMainbackground = view.FindViewById<LinearLayout>(Resource.Id.notam_options_linearlayoutBottom);
            _configurationText = view.FindViewById<TextView>(Resource.Id.notam_options_configuration_text);
            _sortByTextView = view.FindViewById<TextView>(Resource.Id.notam_options_sortBy_textView);
            _sortBySpinner = view.FindViewById<MaterialSpinner>(Resource.Id.notam_options_sortBy_spinner);
            _dismissDialogButton = view.FindViewById<Button>(Resource.Id.notam_option_closeButton);

            // Coloring
            _notamMainbackground.SetBackgroundColor(new ApplyTheme().GetColor(DesiredColor.MainBackground));
            _configurationText.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MagentaText));
            _sortByTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MainText));

            // Assign text fields
            _configurationText.Text = Resources.GetString(Resource.String.NOTAM_configuration);
            _sortByTextView.Text = Resources.GetString(Resource.String.NOTAM_sortBy);
        }

        private void SetSortByPreferences(int position)
        {
            string preference = String.Empty;

            switch (position)
            {
                case 0:
                    preference = "category";
                    break;
                case 1:
                    preference = "date";
                    break;
            }

            ISharedPreferences notamPreferencesprefs = Application.Context.GetSharedPreferences("NOTAM_Options", FileCreationMode.Private);
            notamPreferencesprefs.Edit().PutString("sortByPREF", preference).Apply();
        }

        // Event raiser
        protected virtual void OnSpinnerChanged(int position)
        {
            SortBySpinnerChanged?.Invoke(this, new NotamOptionsDialogEventArgs(position));
        }

    }


    internal class NotamOptionsDialogEventArgs
    {
        public string SortBy { get; }

        public NotamOptionsDialogEventArgs(int position)
        {
            switch (position)
            {
                case 0:
                    SortBy = "category";
                    break;
                case 1:
                    SortBy = "date";
                    break;
            }
        }
    }
}