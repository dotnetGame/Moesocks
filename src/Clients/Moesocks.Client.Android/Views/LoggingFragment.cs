using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Text;

namespace Moesocks.Client.Views
{
    public class LoggingFragment : Fragment
    {
        private TextView _loggintTextView;
        private readonly SpannableStringBuilder _loggingSpans;

        public LoggingFragment()
        {
            _loggingSpans = new SpannableStringBuilder("hahaha");
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.loggingFragment, container, false);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            _loggintTextView = view.FindViewById<TextView>(Resource.Id.loggingText);
            _loggintTextView.SetText(_loggingSpans, TextView.BufferType.Spannable);
        }

        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            base.OnCreateOptionsMenu(menu, inflater);
        }
    }
}