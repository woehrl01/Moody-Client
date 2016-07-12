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
        private EditText _address { get; set; }
        private Spinner _location { get; set; }
        private Button _accept { get; set; }
        private List<Loc> _locationList { get; set; }
        private string _serveraddress { get; set; }
        private SaveAndLoad _saveandload { get; set; }
        private Vibrator _vib { get; set; }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Login);

            _address = FindViewById<EditText>(Resource.Id.serveraddress);
            _location = FindViewById<Spinner>(Resource.Id.location);
            _accept = FindViewById<Button>(Resource.Id.accept);

            _vib = (Vibrator)GetSystemService(Context.VibratorService);

            _saveandload = new SaveAndLoad();
            try
            {
                string json = _saveandload.LoadText("cfg.json");
                Log.Info("Loading: ", json);
                Dictionary<string,string> cfg = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                _address.Text = cfg["ip"];
                SetSpinnerLocations(cfg["ip"], cfg["location"]);
                _accept.Enabled = true;
            }
            catch (Exception e)
            {
                Log.Info("Error while loading: ", e.Message);
            }

            _address.EditorAction += HandleEditorAction;

            _accept.Click += delegate
            {
				try
				{
					_vib.Vibrate(50);
					if (_address.Text != "")
					{
						if (_location.SelectedItem.ToString() != "")
						{
							Dictionary<string, string> newcfg = new Dictionary<string, string>();
							newcfg.Add("ip", _serveraddress);
							newcfg.Add("location", _location.SelectedItem.ToString());
							Log.Info("Saving cfg", _serveraddress);
							Log.Info("Saving cfg", _location.SelectedItem.ToString());
							_saveandload.SaveText("cfg.json", JsonConvert.SerializeObject(newcfg, Formatting.Indented));

							Loc currentloc = null;
							foreach (Loc l in _locationList)
							{
								if (l.Identiefier == _location.SelectedItemPosition + 1)
								{
									currentloc = l;
								}
							}

							var moodactivity = new Intent(this, typeof(Mood));
							moodactivity.PutExtra("LocationId", currentloc.Identiefier.ToString());
							moodactivity.PutExtra("Address", _serveraddress);
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
                RunOnUiThread(() =>
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
                            _location.Adapter = adapter;
                            _accept.Enabled = true;
                            if(defaultLocation != null)
                            {
                                foreach (Loc l in _locationList)
                                {
                                    if (l.Location.Equals(defaultLocation))
                                    {
                                        Log.Info("Loading (Location)", l.Location);
                                        _location.SetSelection(l.Identiefier - 1);
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

                _locationList = JsonConvert.DeserializeObject<List<Loc>>(content);
                List<string> locs = new List<string>();

                foreach (Loc l in _locationList)
                {
                    locs.Add(l.Location);
                }
                _serveraddress = address;
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
            SetSpinnerLocations(_address.Text,null);
            InputMethodManager inputManager = (InputMethodManager)GetSystemService(InputMethodService);
            inputManager.HideSoftInputFromWindow(CurrentFocus.WindowToken, HideSoftInputFlags.NotAlways);
        }
    }
}