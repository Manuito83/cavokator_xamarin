using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Text;
using Android.Text.Method;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Newtonsoft.Json;
using Plugin.Connectivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

            var newFragment = new WxMetarFragment();
            var ft = FragmentManager.BeginTransaction();
            ft.Add(Resource.Id.weather_fragment_container, newFragment);
            ft.Commit();
        }
    }

}