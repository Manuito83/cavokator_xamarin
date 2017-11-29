using Android.App;
using Android.OS;
using Android.Support.V7.App;

namespace Cavokator
{
    [Activity(Label = "Cavokator", MainLauncher = true, Icon = "@drawable/ic_appicon",
         Theme = "@style/Theme.AppCompat.Light", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]

    public class ActivityWxMain : AppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.drawer_layout);



            //var newFragment = new WxMetarFragment();
            //var ft = FragmentManager.BeginTransaction();
            //ft.Add(Resource.Id.weather_fragment_container, newFragment);
            //ft.Commit();
        }
    }

}