using System;
using Android.App;
using Android.Content;
using Android.Widget;
using Android.OS;
using Android.Views.InputMethods;
using Android.Content.PM;
using System.Net;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using Android.Util;

namespace Moody
{
    [Activity(Label = "Moody", MainLauncher = true, Icon = "@drawable/icon", ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : Activity
    {
        public EditText address { get; set; }
        public Spinner location { get; set; }
        public Button accept { get; set; }
        public List<Loc> locationList { get; set; }
        public string serveraddress { get; set; }
        public SaveAndLoad saveandload { get; set; }
        public Vibrator vib { get; set; }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Login);

            address = FindViewById<EditText>(Resource.Id.serveraddress);
            location = FindViewById<Spinner>(Resource.Id.location);
            accept = FindViewById<Button>(Resource.Id.accept);

            vib = (Vibrator)this.GetSystemService(Context.VibratorService);

            saveandload = new SaveAndLoad();
            try
            {
                string json = saveandload.LoadText("cfg.json");
                Log.Info("Loading: ", json);
                Dictionary<string,string> cfg = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                string loc = cfg["location"];
                address.Text = cfg["ip"];
                SetSpinnerLocations(cfg["ip"], cfg["location"]);
                accept.Enabled = true;
            }
            catch (Exception e)
            {
                Log.Info("Error while loading: ", e.Message);
            }

            address.EditorAction += HandleEditorAction;

            accept.Click += delegate
            {
				try
				{
					vib.Vibrate(50);
					if (address.Text != "")
					{
						if (location.SelectedItem.ToString() != "")
						{
							Dictionary<String, String> newcfg = new Dictionary<string, string>();
							newcfg.Add("ip", serveraddress);
							newcfg.Add("location", location.SelectedItem.ToString());
							Log.Info("Saving cfg", serveraddress);
							Log.Info("Saving cfg", location.SelectedItem.ToString());
							saveandload.SaveText("cfg.json", JsonConvert.SerializeObject(newcfg, Formatting.Indented));

							Loc currentloc = null;
							foreach (Loc l in locationList)
							{
								if (l.Identiefier == location.SelectedItemPosition + 1)
								{
									currentloc = l;
								}
							}

							var moodactivity = new Intent(this, typeof(Mood));
							moodactivity.PutExtra("LocationId", currentloc.Identiefier.ToString());
							moodactivity.PutExtra("Address", serveraddress);
							StartActivity(moodactivity);
						}
						else
						{
							Toast.MakeText(this, "You have to select a location!", ToastLength.Long).Show();
						}
					}
					else
					{
						Toast.MakeText(this, "Server-Address must not be empty!", ToastLength.Long).Show();
					}
				}
				catch (Exception)
                {
                    Toast.MakeText(this, "No locations available!", ToastLength.Long).Show();
                }
            };
        }

        public void SetSpinnerLocations (string address, string defaultLocation)
        {
            var progressDialog = ProgressDialog.Show(this, "", "Getting locations...", true);
            progressDialog.SetProgressStyle(ProgressDialogStyle.Spinner);
            new Thread(new ThreadStart(async () =>
            {
                bool succes;
                ArrayAdapter adapter = null; 
                try
                {
                    var locs = await LoadLocationAsync(address);
                    locs.ForEach(delegate (string loc) { Log.Info("Loc", loc); });
                    adapter = new ArrayAdapter<string>(this, Resource.Layout.SpinnerItem, locs);
                    succes = true;
                }
                catch (Exception e)
                {
                    Log.Info("Error", e.Message);
                    succes = false;
                }
                this.RunOnUiThread(() =>
                {
                    progressDialog.Dismiss();
                    if (!succes)
                    {
                        Toast.MakeText(this, "Couldn´t connect to server!", ToastLength.Short).Show();
                    }
                    else
                    {
                        try
                        {
                            location.Adapter = adapter;
                            accept.Enabled = true;
                            if(defaultLocation != null)
                            {
                                foreach (Loc l in locationList)
                                {
                                    if (l.Location.Equals(defaultLocation))
                                    {
                                        Log.Info("Loading (Location)", l.Location);
                                        location.SetSelection(l.Identiefier - 1);
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {
                            Toast.MakeText(this, "Couldn´t connect to server!", ToastLength.Short).Show();
                        }
                    }
                });
            })).Start();
        }

        public Task<List<string>> LoadLocationAsync(string address)
        {
            return Task.Run(() => DownloadLocations(address));
        }
        
        public List<string> DownloadLocations(string address)
        {
            try
            {
                WebRequest request = WebRequest.Create("http://" + address + "/api/locations");
                request.Method = "GET";
                request.Timeout = 10000;
                WebResponse response = request.GetResponse();
                Stream stream = response.GetResponseStream();
                StreamReader reader = new StreamReader(stream);
                string content = reader.ReadToEnd();
                reader.Close();
                response.Close();

                locationList = JsonConvert.DeserializeObject<List<Loc>>(content);
                List<string> locs = new List<string>();

                foreach (Loc l in locationList)
                {
                    locs.Add(l.Location);
                }
                serveraddress = address;
                return locs;
            }
            catch (Exception e)
            {
                Log.Info("Error", e.Message);
                return null;
            }
        }

        private void HandleEditorAction (object sender, TextView.EditorActionEventArgs e)
        {
            SetSpinnerLocations(address.Text,null);
            InputMethodManager inputManager = (InputMethodManager)this.GetSystemService(Context.InputMethodService);
            inputManager.HideSoftInputFromWindow(this.CurrentFocus.WindowToken, HideSoftInputFlags.NotAlways);
        }
    }

    [Activity(Label = "Main", ScreenOrientation = ScreenOrientation.Portrait)]
    public class Mood : Android.App.Activity
    {
        public string id { get; set; }
        public string address { get; set; }
        public Vibrator vib { get; set; }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);
			ActionBar.SetTitle(Resource.String.ApplicationName);

            ImageButton b1 = FindViewById<ImageButton>(Resource.Id.bone);
            ImageButton b2 = FindViewById<ImageButton>(Resource.Id.btwo);
            ImageButton b3 = FindViewById<ImageButton>(Resource.Id.bthree);
            ImageButton b4 = FindViewById<ImageButton>(Resource.Id.bfour);

            vib = (Vibrator)this.GetSystemService(Context.VibratorService);

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
            alert.SetPositiveButton("Yes", (senderAlert, args) => {
                if (!address.Equals("-1"))
                {
                    string url = "http://" + address + "/api/entry/" + mood + "&" + id;
                    var progressDialog = ProgressDialog.Show(this, "", "Sending mood...", true);
                    progressDialog.SetProgressStyle(ProgressDialogStyle.Spinner);

                    new Thread(new ThreadStart(async () =>
                    {
                        bool succes = await SendAsync(url);
                        this.RunOnUiThread(() =>
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

        public Task<bool> SendAsync(string url)
        {
            return Task.Run(() => SendToServer(url));
        }

        public bool SendToServer(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Timeout = 10000;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                response.Close();
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

    public class Loc
    {
        public Loc(int id, string location)
        {
            this.Identiefier = id;
            this.Location = location;
        }

        public string Location
        {
            get; set;
        }

        public int Identiefier
        {
            get; set;
        }
    }

    public class SaveAndLoad{
        public void SaveText (string filename, string text) {
            var documentsPath = System.Environment.GetFolderPath (System.Environment.SpecialFolder.Personal);
            var filePath = Path.Combine (documentsPath, filename);
            System.IO.File.WriteAllText (filePath, text);
            Log.Info("Save", "Succesful");
        }
        public string LoadText (string filename){
            var documentsPath = System.Environment.GetFolderPath (System.Environment.SpecialFolder.Personal);
            var filePath = Path.Combine (documentsPath, filename);  
            return System.IO.File.ReadAllText (filePath);
        }
    }
}