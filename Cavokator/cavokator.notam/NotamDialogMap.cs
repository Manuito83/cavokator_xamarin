//
// CAVOKATOR APP
// Website: https://github.com/Manuito83/Cavokator
// License GNU General Public License v3.0
// Manuel Ortega, 2018
//

using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace Cavokator
{
    class NotamDialogMap : Android.Support.V4.App.DialogFragment, IOnMapReadyCallback
    {

        private GoogleMap mMap;

        private View thisView;

        private LinearLayout _notamContainerBottom;
        private LinearLayout _buttonContainerBottom;
        private TextView _notamIdTextView;
        private Button _dismissButton;

        private string notamId;
        private float latitude;
        private float longitude;
        private int radius;

        public NotamDialogMap(string notamId, float latitude, float longitude, int radius)
        {
            this.notamId = notamId;
            this.latitude = latitude;
            this.longitude = longitude;
            this.radius = radius;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);

            thisView = inflater.Inflate(Resource.Layout.notam_dialog_map, container, false);

            StyleViews();

            return thisView;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            Android.Support.V4.App.FragmentManager fm = ChildFragmentManager;
            SupportMapFragment mapFragment = (SupportMapFragment)fm.FindFragmentByTag("mapFragment");
            if (mapFragment == null)
            {
                mapFragment = new SupportMapFragment();
                Android.Support.V4.App.FragmentTransaction ft = fm.BeginTransaction();
                ft.Add(Resource.Id.notam_mapDialog_mapContainer, mapFragment, "mapFragment");
                ft.Commit();
                fm.ExecutePendingTransactions();
            }

            mapFragment.GetMapAsync(this);

            _buttonContainerBottom.AddView(_dismissButton);
        }

        public void OnMapReady(GoogleMap map)
        {
            mMap = map;

            mMap.MoveCamera(CameraUpdateFactory.NewLatLngZoom(new LatLng(latitude, longitude), 7));

            var mapOptions = new MarkerOptions();
            mapOptions.SetTitle("NOTAM");
            mapOptions.SetPosition(new LatLng(latitude, longitude));
            mMap.AddMarker(mapOptions);

            CircleOptions circleOptions = new CircleOptions();
            circleOptions.InvokeCenter(new LatLng(latitude, longitude));
            circleOptions.InvokeRadius(radius * 1852);
            circleOptions.InvokeStrokeColor(new ApplyTheme().GetColor(DesiredColor.RedTextWarning));
            mMap.AddCircle(circleOptions);
        }

        private void StyleViews()
        {
            _notamContainerBottom = thisView.FindViewById<LinearLayout>(Resource.Id.notam_mapDialog_mapContainer);
            _notamContainerBottom.SetBackgroundColor(new ApplyTheme().GetColor(DesiredColor.MainBackground));

            _buttonContainerBottom = thisView.FindViewById<LinearLayout>(Resource.Id.notam_mapDialog_buttonContainer);
            _buttonContainerBottom.SetBackgroundColor(new ApplyTheme().GetColor(DesiredColor.MainBackground));

            _notamIdTextView = thisView.FindViewById<TextView>(Resource.Id.notam_mapDialog_notamId);
            _notamIdTextView.Text = "NOTAM " + notamId;
            _notamIdTextView.SetTextColor(new ApplyTheme().GetColor(DesiredColor.MagentaText));

            _dismissButton = new Button(Activity);
            _dismissButton.Text = Resources.GetString(Resource.String.OK);
            _dismissButton.Click += delegate { Dismiss(); };
        }
    }
}