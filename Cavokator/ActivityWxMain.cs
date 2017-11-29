using Android.App;
using Android.OS;

namespace Cavokator
{
    [Activity(Label = "Cavokator", MainLauncher = true, Icon = "@drawable/ic_appicon",
         Theme = "@android:style/Theme.Material", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]

    public class ActivityWxMain : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.wx_weather_main);



            //var newFragment = new WxMetarFragment();
            //var ft = FragmentManager.BeginTransaction();
            //ft.Add(Resource.Id.flContent, newFragment);
            //ft.Commit();
        }
    }

}