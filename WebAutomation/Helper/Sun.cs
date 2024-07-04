using Newtonsoft.Json;
using System;
using System.Net;
using System.Threading.Tasks;

namespace WebAutomation.Helper {
	public class Sun {
		private int SunShineId;
		private int SunRiseId;
		private int SunSetId;
		private DateTime sunrise;
		public DateTime Sunrise { get { return sunrise; } }
		private DateTime sunset;
		public DateTime Sunset { get { return sunset; } }
		private System.Timers.Timer setNewSunriseSunsetTimer;
		private System.Timers.Timer SunriseTimer;
		private System.Timers.Timer SunsetTimer;
		public Sun() {
			int testSunIsShining, testSunRising, testSunsetting;
			if(Int32.TryParse(Ini.get("Projekt", "SunIsShining"), out testSunIsShining)) {
				SunShineId = testSunIsShining;
			}
			if(Int32.TryParse(Ini.get("Projekt", "SunRise"), out testSunRising)) {
				SunRiseId = testSunRising;
			}
			if(Int32.TryParse(Ini.get("Projekt", "SunSet"), out testSunsetting)) {
				SunSetId = testSunsetting;
			}
			StartSunriseSunsetTimer();
		}
		private void StartSunriseSunsetTimer() {
			getSunsetSunrise();

			setNewSunriseSunsetTimer = new System.Timers.Timer();
			setNewSunriseSunsetTimer.Elapsed += SunriseSunsetTimer_Elapsed;
			setNewSunriseSunsetTimer.AutoReset = false;

			SunriseTimer = new System.Timers.Timer();
			SunriseTimer.Elapsed += SunriseTimer_Elapsed;
			SunriseTimer.AutoReset = false;

			SunsetTimer = new System.Timers.Timer();
			SunsetTimer.Elapsed += SunsetTimer_Elapsed;
			SunsetTimer.AutoReset = false;

			TimeSpan firstStart = new TimeSpan();
			DateTime Now = DateTime.Now;
			if(Now.Hour < 1) {
				firstStart = new DateTime(Now.Year, Now.Month, Now.Day, 1, 0, 0) - Now;
			} else {
				DateTime tomorrow = Now.AddDays(1);
				firstStart = new DateTime(tomorrow.Year, tomorrow.Month, tomorrow.Day, 1, 0, 0) - Now;
			}
			setNewSunriseSunsetTimer.Interval = firstStart.TotalMilliseconds;
			setNewSunriseSunsetTimer.Enabled = true;
			wpDebug.Write("setNewSunriseSunsetTimer gestartet - wird ausgelöst in {0}", firstStart);
		}

		private void SunriseSunsetTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
			DateTime Now = DateTime.Now;
			DateTime tomorrow = Now.AddDays(1);
			TimeSpan nextStart = new DateTime(tomorrow.Year, tomorrow.Month, tomorrow.Day, 1, 0, 0) - Now;
			setNewSunriseSunsetTimer.Interval = nextStart.TotalMilliseconds;
			setNewSunriseSunsetTimer.Enabled = true;
			wpDebug.Write("SunriseSunset Timer gestartet - wird ausgelöst in {0}", nextStart);
			await GetSunsetSunrise();
			// clean Database once a night
			await Task.Run(() => {
				using(SQL s = new SQL("HistoryCleaner")) {
					s.HistoryCleaner();
				}
			});
		}
		private void SunriseTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
			Datapoints.Get(SunShineId).writeValue("True");
		}
		private void SunsetTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
			Datapoints.Get(SunShineId).writeValue("False");
		}

		private void getSunsetSunrise() {
			Thread SunsetSunrise = new Thread(new ThreadStart(handleGetSunsetSunrise));
			SunsetSunrise.Name = "getSunsetSunrise";
			SunsetSunrise.Start();
		}
		private void handleGetSunsetSunrise() {
			try {
				WebClient webClient = new WebClient();
				string url = String.Format("http://api.openweathermap.org/data/2.5/weather?id={0}&APPID=99efbd2754161093642df0e72e881c87&units=metric&lang=de", Ini.get("Projekt", "OpenWeatherCode"));
				webClient.DownloadStringCompleted += (e, args) => {
					if(args.Error == null) {
						OpenWeather.weather SunsetSunrise = JsonConvert.DeserializeObject<OpenWeather.weather>(args.Result);
						sunrise = UnixTimeStampToDateTime(SunsetSunrise.sys.sunrise);
						sunset = UnixTimeStampToDateTime(SunsetSunrise.sys.sunset);
						Datapoints.Get(SunRiseId).writeValue(sunrise.ToString(SQL.DateTimeFormat));
						Datapoints.Get(SunSetId).writeValue(sunset.ToString(SQL.DateTimeFormat));
						//PDebug.Write(result);
						wpDebug.Write("Found Sunrise: {0:HH:mm:ss}, Found Sunset: {1:HH:mm:ss}", sunrise, sunset);
						DateTime Now = DateTime.Now;
						if(Now < sunrise) {
							Datapoints.Get(SunShineId).writeValue("False");
							Datapoints.Get(SunShineId).setValue("False");
						} else if(Now >= sunrise && Now < sunset) {
							Datapoints.Get(SunShineId).writeValue("True");
							Datapoints.Get(SunShineId).setValue("True");
						} else if(Now > sunset) {
							Datapoints.Get(SunShineId).writeValue("False");
							Datapoints.Get(SunShineId).setValue("False");
						}
						TimeSpan toSunrise = sunrise - Now;
						TimeSpan toSunset = sunset - Now;
						if(toSunrise.Ticks > 0) {
							SunriseTimer.Interval = toSunrise.TotalMilliseconds;
							SunriseTimer.Enabled = true;
							wpDebug.Write("toSunrise Timer gestartet - wird ausgelöst in {0}", toSunrise);
						} else {
							wpDebug.Write("toSunrise war heute schon");
						}
						if(toSunset.Ticks > 0) {
							SunsetTimer.Interval = toSunset.TotalMilliseconds;
							SunsetTimer.Enabled = true;
							wpDebug.Write("toSunset Timer gestartet - wird ausgelöst in {0}", toSunset);
						} else {
							wpDebug.Write("toSunset war heute schon");
						}
					} else {
						wpDebug.WriteError(args.Error);
					}
				};
				webClient.DownloadStringAsync(new Uri(url));
			} catch(Exception ex) {
				wpDebug.WriteError(ex);
			}
		}
		public DateTime UnixTimeStampToDateTime(int unixTimeStamp) {
			// Unix timestamp is seconds past epoch
			DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			dtDateTime = dtDateTime.AddSeconds(Convert.ToDouble(unixTimeStamp)).ToLocalTime();
			return dtDateTime;
		}
	}
}
namespace OpenWeather {
	public class weather {
		public sys sys { get; set; }
	}
	public class sys {
		public int type { get; set; }
		public int id { get; set; }
		public string country { get; set; }
		public int sunrise { get; set; }
		public int sunset { get; set; }
	}
}
