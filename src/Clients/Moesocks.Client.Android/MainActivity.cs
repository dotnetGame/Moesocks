using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Moesocks.Client.Views;
using Android.Net;
using Moesocks.Client.Services;

namespace Moesocks.Client
{
    [Activity(Label = "Moesocks", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            ActionBar.NavigationMode = ActionBarNavigationMode.Tabs;
            
            AddFragment<LoggingFragment>(Resource.String.Logging);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.mainMenu, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch(item.ItemId)
            {
                case Resource.Id.action_start:
                    PrepareVpnService();
                    break;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void PrepareVpnService()
        {
            var intent = VpnService.Prepare(this);
            if (intent == null)
                StartActivityForResult(intent, 0);
            else
                OnActivityResult(0, Result.Ok, null);
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            if(resultCode == Result.Ok)
            {
                var intent = new Intent(this, Java.Lang.Class.FromType(typeof(AppVpnService)));
                StartService(intent);
            }
        }

        private void AddFragment<T>(int textResId) where T : Fragment
        {
            var frag = new Lazy<T>();
            var tab = ActionBar.NewTab()
                .SetText(textResId);
            tab.TabSelected += (s, e) => e.FragmentTransaction.Add(Resource.Id.fragmentContainer, frag.Value);
            ActionBar.AddTab(tab);
        }
    }
}

