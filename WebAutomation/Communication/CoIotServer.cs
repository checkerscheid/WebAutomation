//###################################################################################
//#                                                                                 #
//#              (C) FreakaZone GmbH                                                #
//#              =======================                                            #
//#                                                                                 #
//###################################################################################
//#                                                                                 #
//# Author       : Christian Scheid                                                 #
//# Date         : 15.02.2025                                                       #
//#                                                                                 #
//# Revision     : $Rev:: 252                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: CoIotServer.cs 252 2025-12-23 12:07:55Z                  $ #
//#                                                                                 #
//###################################################################################
using CoAP;
using FreakaZone.Libraries.wpEventLog;
using FreakaZone.Libraries.wpIniFile;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using WebAutomation.Controller;

namespace WebAutomation.Communication {
	public class CoIot {
		private static bool running = false;
		private static Thread CoIotServer;
		private static int count = 0;
		private static int intervall = 5; // in seconds
		public static void Start() {
			Debug.Write(MethodInfo.GetCurrentMethod(), "CoIot Init");
			intervall = IniFile.GetInt("CoIot", "Intervall");
			if(intervall < 0) intervall = 30;
			if(intervall > 120) intervall = 120;
			if(intervall != 0) {
				CoIotServer = new Thread(new ThreadStart(Run));
				CoIotServer.Name = "CoIot Server";
				CoIotServer.Start();
			}
		}
		public static void Run() {
			Debug.Write(MethodInfo.GetCurrentMethod(), "CoIot Server Start");
			List<Shelly> devices = new List<Shelly>();
			devices = ShellyServer.GetCoIot();
			running = true;
			count = intervall - 1; // (fast) sofort starten
			while(running) {
				if(++count > intervall) {
					count = 0;
					foreach(Shelly d in devices) {
						if(d.IdPower > 0) {
							try {
								Request request = new Request(Method.GET);
								request.URI = new Uri($"coap://{d.Ip}:5683/cit/s");
								request.Send();
								// wait for response
								Response response = request.WaitForResponse();
								ShellyValues obj = JsonConvert.DeserializeObject<ShellyValues>(response.PayloadString);
								foreach(List<object> t in obj.G) {
									if(t[1].ToString() == "4101") {
										Datapoints.Get(d.IdPower)?.SetValue(t[2].ToString());
									}
									if(t[1].ToString() == "5101") {
										Datapoints.Get(d.IdBrightness)?.SetValue(t[2].ToString());
									}
								}
							} catch(Exception ex) {
								Debug.Write(MethodInfo.GetCurrentMethod(), $"Error reading CoIot from Shelly {d.Name} ({d.Ip}): {ex.Message}");
							}
						}
					}
				}
				
				Thread.Sleep(1000);
			}
			Debug.Write(MethodInfo.GetCurrentMethod(), "CoIot Server Stopped");
		}
		public static string GetDescription(string shellyIp) {
			Shelly s = ShellyServer.GetShellyWithCoIoT(shellyIp);
			if(s != null) {
				try {
					Request request = new Request(Method.GET);
					request.URI = new Uri($"coap://{s.Ip}:5683/cit/d");
					request.Send();
					// wait for response
					Response response = request.WaitForResponse();
					return response.PayloadString;

				} catch(Exception ex) {
					Debug.Write(MethodInfo.GetCurrentMethod(), $"Error reading CoIot description from Shelly {s.Name} ({s.Ip}): {ex.Message}");
					return string.Empty;
				}
			} else {
				Debug.Write(MethodInfo.GetCurrentMethod(), $"Error Shelly nicht gefunden {shellyIp}");
				return string.Empty;
			}
		}
		public static void Stop() {
			Debug.Write(MethodInfo.GetCurrentMethod(), "CoIot Server Stop");
			running = false;
			if(CoIotServer != null && CoIotServer.IsAlive) {
				CoIotServer.Join(1500);
				if(CoIotServer.IsAlive) {
					CoIotServer.Abort();
				}
			}
		}
		internal class ShellyValues {
			public IList<IList<object>> G { get; set; }
		}
	}
}
