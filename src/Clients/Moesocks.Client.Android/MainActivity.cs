using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Moesocks.Client.Views;

namespace Moesocks.Client
{
    [Activity(Label = "Moesocks.Client", MainLauncher = true, Icon = "@drawable/icon")]
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

        private void AddFragment<T>(int textResId) where T : Fragment
        {
            var frag = new Lazy<Fragment>();
            var tab = ActionBar.NewTab()
                .SetText(textResId);
            tab.TabSelected += (s, e) => e.FragmentTransaction.Add(Resource.Id.fragmentContainer, frag.Value);
            ActionBar.AddTab(tab);
        }
    }
}

