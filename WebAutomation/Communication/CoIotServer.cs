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
//# Revision     : $Rev:: 197                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: TableShelly.cs 197 2025-03-30 13:07:37Z                  $ #
//#                                                                                 #
//###################################################################################
using CoAP;
using FreakaZone.Libraries.wpEventLog;
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
		public static void Start() {
			Debug.Write(MethodInfo.GetCurrentMethod(), "CoIot Init");
			CoIotServer = new Thread(new ThreadStart(Run));
			CoIotServer.Name = "CoIot Server";
			CoIotServer.Start();
		}
		public static void Run() {
			Debug.Write(MethodInfo.GetCurrentMethod(), "CoIot Server Start");
			List<Shelly> devices = new List<Shelly>();
			devices = ShellyServer.GetCoIot();
			running = true;
			while(running) {
				if(++count > 5) {
					count = 0;
					foreach(Shelly d in devices) {
						if(d.IdPower > 0) {
							try {
								CoapClient client = new CoapClient();
								Request request = new Request(Method.GET);
								request.URI = new Uri($"coap://{d.Ip}:5683/cit/s");
								request.Send();

								// wait for response
								Response response = request.WaitForResponse();
								ShellyValues obj = JsonConvert.DeserializeObject<ShellyValues>(response.PayloadString);
								foreach(List<object> t in obj.G) {
									if(t[1].ToString() == "4101") {
										Datapoints.Get(d.IdPower).SetValue(t[2].ToString());
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
