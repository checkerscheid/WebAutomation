//###################################################################################
//#                                                                                 #
//#              (C) FreakaZone GmbH                                                #
//#              =======================                                            #
//#                                                                                 #
//###################################################################################
//#                                                                                 #
//# Author       : Christian Scheid                                                 #
//# Date         : 12.01.2024                                                       #
//#                                                                                 #
//# Revision     : $Rev:: 190                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: Sun.cs 190 2025-02-18 19:50:45Z                          $ #
//#                                                                                 #
//###################################################################################
using FreakaZone.Libraries.wpEventLog;
using FreakaZone.Libraries.wpIniFile;
using FreakaZone.Libraries.wpSQL;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace WebAutomation.Helper {
	public class Sun {
		private int SunShineId;
		private int SunRiseId;
		private int SunSetId;
		private int SummerId;
		private DateTime sunrise;
		public DateTime Sunrise { get { return sunrise; } }
		private DateTime sunset;
		public DateTime Sunset { get { return sunset; } }
		private bool summer;
		public bool Summer { get { return summer; } set { summer = value; } }
		private System.Timers.Timer setNewSunriseSunsetTimer;
		private System.Timers.Timer SunriseTimer;
		private System.Timers.Timer SunsetTimer;
		public Sun() {
			Debug.Write(MethodInfo.GetCurrentMethod(), "Sun init");
			int testSunIsShining, testSunRising, testSunsetting, testSummer;
			if(Int32.TryParse(IniFile.get("Projekt", "SunIsShining"), out testSunIsShining)) {
				SunShineId = testSunIsShining;
			}
			if(Int32.TryParse(IniFile.get("Projekt", "SunRise"), out testSunRising)) {
				SunRiseId = testSunRising;
			}
			if(Int32.TryParse(IniFile.get("Projekt", "SunSet"), out testSunsetting)) {
				SunSetId = testSunsetting;
			}
			if(Int32.TryParse(IniFile.get("Projekt", "Summer"), out testSummer)) {
				SummerId = testSummer;
			}
			_ = StartSunriseSunsetTimer();
			Debug.Write(MethodInfo.GetCurrentMethod(), "Sun gestartet");
		}
		public String SetSummer(bool summer) {
			this.summer = summer;
			using(Database Sql = new Database("Sun")) {
				Sql.wpQuery("UPDATE [cfg] SET [value] = '" + (summer ? "1" : "0") + "' WHERE [key] = 'summer'");
			}
			Datapoints.Get(SummerId).writeValue(summer ? "1" : "0");
			return new ret() { erg = ret.OK, message = $"Summer Set to {(summer ? "True" : "False")}" }.ToString();
		}
		public void InitSummer() {
			using(Database Sql = new Database("Sun")) {
				summer = Sql.wpQuery("SELECT TOP 1 [value] FROM [cfg] WHERE [key] = 'summer'")[0][0] == "1";
			}
			Datapoints.Get(SummerId).writeValue(summer ? "1" : "0");
		}
		private async Task StartSunriseSunsetTimer() {

			setNewSunriseSunsetTimer = new System.Timers.Timer();
			setNewSunriseSunsetTimer.Elapsed += SunriseSunsetTimer_Elapsed;
			setNewSunriseSunsetTimer.AutoReset = false;

			SunriseTimer = new System.Timers.Timer();
			SunriseTimer.Elapsed += SunriseTimer_Elapsed;
			SunriseTimer.AutoReset = false;

			SunsetTimer = new System.Timers.Timer();
			SunsetTimer.Elapsed += SunsetTimer_Elapsed;
			SunsetTimer.AutoReset = false;

			await GetSunsetSunrise();

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
			Debug.Write(MethodInfo.GetCurrentMethod(), "setNewSunriseSunsetTimer gestartet - wird ausgelöst in {0}", firstStart);
		}

		private async void SunriseSunsetTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
			DateTime Now = DateTime.Now;
			DateTime tomorrow = Now.AddDays(1);
			TimeSpan nextStart = new DateTime(tomorrow.Year, tomorrow.Month, tomorrow.Day, 1, 0, 0) - Now;
			setNewSunriseSunsetTimer.Interval = nextStart.TotalMilliseconds;
			setNewSunriseSunsetTimer.Enabled = true;
			Debug.Write(MethodInfo.GetCurrentMethod(), "SunriseSunset Timer gestartet - wird ausgelöst in {0}", nextStart);
			await GetSunsetSunrise();
			// clean Database once a night
			await Task.Run(() => {
				using(Database Sql = new Database("HistoryCleaner")) {
					Sql.HistoryCleaner();
				}
			});
		}
		private void SunriseTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
			Datapoints.Get(SunShineId).writeValue("1");
			InitSummer();
		}
		private void SunsetTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
			Datapoints.Get(SunShineId).writeValue("0");
			InitSummer();
		}
		private async Task GetSunsetSunrise() {
			try {
				InitSummer();
				WebClient webClient = new WebClient();
				string url = String.Format("http://api.openweathermap.org/data/2.5/weather?id={0}&APPID=99efbd2754161093642df0e72e881c87&units=metric&lang=de", IniFile.get("Projekt", "OpenWeatherCode"));
				webClient.DownloadStringCompleted += (e, args) => {
					if(args.Error == null) {
						try {
							OpenWeather.weather SunsetSunrise = JsonConvert.DeserializeObject<OpenWeather.weather>(args.Result);
							sunrise = UnixTimeStampToDateTime(SunsetSunrise.sys.sunrise);
							sunset = UnixTimeStampToDateTime(SunsetSunrise.sys.sunset);
							Datapoints.Get(SunRiseId).writeValue(sunrise.ToString(Database.DateTimeFormat));
							Datapoints.Get(SunSetId).writeValue(sunset.ToString(Database.DateTimeFormat));
							//PDebug.Write(result);
							Debug.Write(MethodInfo.GetCurrentMethod(), "Found Sunrise: {0:HH:mm:ss}, Found Sunset: {1:HH:mm:ss}", sunrise, sunset);
							DateTime Now = DateTime.Now;
							if(Now < sunrise) {
								Datapoints.Get(SunShineId).writeValue("0");
							} else if(Now >= sunrise && Now < sunset) {
								Datapoints.Get(SunShineId).writeValue("1");
							} else if(Now > sunset) {
								Datapoints.Get(SunShineId).writeValue("0");
							}
							TimeSpan toSunrise = sunrise - Now;
							TimeSpan toSunset = sunset - Now;
							if(toSunrise.Ticks > 0) {
								SunriseTimer.Interval = toSunrise.TotalMilliseconds;
								SunriseTimer.Enabled = true;
								Debug.Write(MethodInfo.GetCurrentMethod(), "toSunrise Timer gestartet - wird ausgelöst in {0}", toSunrise);
							} else {
								Debug.Write(MethodInfo.GetCurrentMethod(), "toSunrise war heute schon");
							}
							if(toSunset.Ticks > 0) {
								SunsetTimer.Interval = toSunset.TotalMilliseconds;
								SunsetTimer.Enabled = true;
								Debug.Write(MethodInfo.GetCurrentMethod(), "toSunset Timer gestartet - wird ausgelöst in {0}", toSunset);
							} else {
								Debug.Write(MethodInfo.GetCurrentMethod(), "toSunset war heute schon");
							}
						} catch(Exception ex) {
							Debug.WriteError(MethodBase.GetCurrentMethod(), ex);
						}
					} else {
						Debug.WriteError(MethodInfo.GetCurrentMethod(), args.Error);
					}
				};
				await Task.Run(() => {
					webClient.DownloadStringAsync(new Uri(url));
				});
			} catch(Exception ex) {
				Debug.WriteError(MethodInfo.GetCurrentMethod(), ex);
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
