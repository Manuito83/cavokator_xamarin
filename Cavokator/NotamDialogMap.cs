using Android.App;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Java.Lang;
using SupportFragment = Android.Support.V4.App.Fragment;



namespace Cavokator
{
    class NotamDialogMap : Android.Support.V4.App.DialogFragment, IOnMapReadyCallback
    {
        
        SupportMapFragment mapFragment = new SupportMapFragment();

        private GoogleMap mMap;

        private View thisView;

        private float latitude;
        private float longitude;
        private int radius;

        public NotamDialogMap(float latitude, float longitude, int radius)
        {
            this.latitude = latitude;
            this.longitude = longitude;
            this.radius = radius;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);
            mapFragment = new SupportMapFragment();
            if (thisView != null)
            {
                ViewGroup parent = (ViewGroup)thisView.Parent;
                if (parent != null)
                    parent.RemoveView(thisView);
            }

            try
            {
                thisView = inflater.Inflate(Resource.Layout.notam_dialog_map, container, false);
            }
            catch (InflateException e)
            {
                /* map is already there, just return view as it is */
            }

            // Inflate view
            //if (savedInstanceState == null)
            //    thisView = inflater.Inflate(Resource.Layout.notam_dialog_map, container, false);

            //SupportMapFragment mapFragment = (SupportMapFragment)FragmentManager.FindFragmentById(Resource.Id.mapFragment);
            //mapFragment.GetMapAsync(this);

            return thisView;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            Android.Support.V4.App.FragmentManager fm = ChildFragmentManager;
            mapFragment = (SupportMapFragment)fm.FindFragmentByTag("mapFragment");
            if (mapFragment == null)
            {
                mapFragment = new SupportMapFragment();
                Android.Support.V4.App.FragmentTransaction ft = fm.BeginTransaction();
                ft.Add(Resource.Id.notam_mapDialog_mapContainer, mapFragment, "mapFragment");
                ft.Commit();
                fm.ExecutePendingTransactions();
            }

            mapFragment.GetMapAsync(this);
        }

        public void OnMapReady(GoogleMap map)
        {
            mMap = map;

            mMap.MoveCamera(CameraUpdateFactory.NewLatLngZoom(new LatLng(latitude, longitude), 7));

            var mapOptions = new MarkerOptions();
            //mapOptions.SetIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.ic_share_variant_white_24dp)).Anchor(0.0f, 1.0f);
            mapOptions.SetTitle("NOTAM");
            mapOptions.SetPosition(new LatLng(latitude, longitude));
            mMap.AddMarker(mapOptions);

            CircleOptions circleOptions = new CircleOptions();
            circleOptions.InvokeCenter(new LatLng(latitude, longitude));
            circleOptions.InvokeRadius(radius * 1852);
            circleOptions.InvokeStrokeColor(new ApplyTheme().GetColor(DesiredColor.RedTextWarning));
            mMap.AddCircle(circleOptions);

        }

    }
}