using System;
using System.Collections.Specialized;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using Android.Widget;

namespace Moody
{
    [Activity(Label = "Main", ScreenOrientation = ScreenOrientation.Portrait)]
    public class Mood : Android.App.Activity
    {
        private string id { get; set; }
        private string address { get; set; }
        private Vibrator vib { get; set; }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);
            ActionBar.SetTitle(Resource.String.ApplicationName);

            ImageButton b1 = FindViewById<ImageButton>(Resource.Id.bone);
            ImageButton b2 = FindViewById<ImageButton>(Resource.Id.btwo);
            ImageButton b3 = FindViewById<ImageButton>(Resource.Id.bthree);
            ImageButton b4 = FindViewById<ImageButton>(Resource.Id.bfour);

            vib = (Vibrator)GetSystemService(Context.VibratorService);

            b1.Click += delegate
            {
                SendMood(1);
            };

            b2.Click += delegate
            {
                SendMood(2);
            };

            b3.Click += delegate
            {
                SendMood(3);
            };

            b4.Click += delegate
            {
                SendMood(4);
            };

            id = Intent.GetStringExtra("LocationId") ?? "-1";
            address = Intent.GetStringExtra("Address") ?? "-1";
        }

        public void SendMood(int mood)
        {
            vib.Vibrate(100);
            AlertDialog.Builder alert = new AlertDialog.Builder(this);
            alert.SetTitle("Send Mood: " + GetMoodDescriptionById(mood) + "?");
            alert.SetPositiveButton("Yes", (senderAlert, args) => 
            {
                if (!address.Equals("-1"))
                {
                    string url = "http://" + address + "/api/entry/";
                    var progressDialog = ProgressDialog.Show(this, "", "Sending mood...", true);
                    progressDialog.SetProgressStyle(ProgressDialogStyle.Spinner);

                    new Thread(new ThreadStart(async () =>
                    {
                        bool succes = await SendAsync(url, mood, Int32.Parse(id));
                        RunOnUiThread(() =>
                        {
                            progressDialog.Dismiss();
                            if (!succes)
                            {
                                Toast.MakeText(this, "Couldn´t connect to server!", ToastLength.Short).Show();
                            }
                            else
                            {
                                Toast.MakeText(this, "Mood sent.", ToastLength.Short).Show();
                            }
                        });     
                    })).Start();
                }
                else
                {
                    Toast.MakeText(this, "Invalid server-address!", ToastLength.Short).Show();
                }
            });

            alert.SetNegativeButton("No", (senderAlert, args) => {
                                                                     Toast.MakeText(this, "Cancelled!", ToastLength.Short).Show();
            });

            Dialog dialog = alert.Create();
            dialog.Show();
        }

        public Task<bool> SendAsync(string url, int mood, int location)
        {
            return Task.Run(() => SendToServer(url, mood, location));
        }

        public class MoodyWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri uri)
            {
                WebRequest w = base.GetWebRequest(uri);
                w.Timeout = 10 * 1000;
                return w;
            }
        }

        public bool SendToServer(string url, int mood, int location)
        {
            try
            {

                using (WebClient client = new MoodyWebClient())
                {
                    var reqparm = new NameValueCollection();
                    reqparm.Add("mood", mood.ToString());
                    reqparm.Add("location", location.ToString());
                    byte[] responsebytes = client.UploadValues(url, "POST", reqparm);
                    string responsebody = System.Text.Encoding.UTF8.GetString(responsebytes);
                }
                return true;
            }
            catch (Exception e)
            {
                Log.Info("Error", e.Message);
                return false;
            }
        }

        public string GetMoodDescriptionById(int mood)
        {
            switch (mood)
            {
                case 1:
                    return "Very good";
                case 2:
                    return "Good";
                case 3:
                    return "Bad";
                case 4:
                    return "Very bad";
                default:
                    return "-1";
            }
        }
    }
}