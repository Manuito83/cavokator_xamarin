using Android.App;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.Widget;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;

namespace Cavokator
{
    [Activity(Label = "Cavokator", MainLauncher = true, Icon = "@drawable/ic_appicon",
     ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]

    public class ActivityWxMain : AppCompatActivity
    {

        DrawerLayout drawerLayout;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.drawer_layout);

            drawerLayout = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);

            // Initialize Toolbar
            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.SetHomeButtonEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_menu);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            // Attach item selected handler to navigation view
            var navigationView = FindViewById<NavigationView>(Resource.Id.my_navigation_view);
            navigationView.NavigationItemSelected += NavigationView_NavigationItemSelected;





            var ft = SupportFragmentManager.BeginTransaction(); 
            ft.AddToBackStack(null);
            ft.Add(Resource.Id.flContent, new WxMetarFragment());
            ft.Commit();
                        

        }



        public override void OnBackPressed()
        {
            if (drawerLayout.IsDrawerOpen((int)GravityFlags.Start))
            {
                drawerLayout.CloseDrawers();
            }
            else
            {
                base.OnBackPressed();
            }
        }



        // Assess button pressed
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            // Hamburger button (open drawer)
            if (item.ItemId == Android.Resource.Id.Home)
                drawerLayout.OpenDrawer((int)GravityFlags.Start);
                return true;
        }


        void NavigationView_NavigationItemSelected(object sender, NavigationView.NavigationItemSelectedEventArgs e)
        {
            switch (e.MenuItem.ItemId)
            {
                //case (Resource.Id.nav_home):
                //    // React on 'nav_home' selection
                //    break;
                //case (Resource.Id.nav_messages):
                //    //
                //    break;
                //case (Resource.Id.nav_friends):
                //    // React on 'Friends' selection
                //    break;
            }
            // Close drawer
            drawerLayout.CloseDrawers();
        }

    }

}