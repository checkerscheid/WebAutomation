//###################################################################################
//#                                                                                 #
//#              (C) FreakaZone GmbH                                                #
//#              =======================                                            #
//#                                                                                 #
//###################################################################################
//#                                                                                 #
//# Author       : Christian Scheid                                                 #
//# Date         : 30.05.2025                                                       #
//#                                                                                 #
//# Revision     : $Rev:: 251                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: ShellyServer.cs 251 2025-12-23 12:06:40Z                 $ #
//#                                                                                 #
//###################################################################################
using FreakaZone.Libraries.wpEventLog;
using FreakaZone.Libraries.wpIniFile;
using FreakaZone.Libraries.wpSQL;
using FreakaZone.Libraries.wpSQL.Table;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using WebAutomation.Communication;

namespace WebAutomation.Controller {
	public static class ShellyServer {
		/// <summary></summary>
		private static Logger eventLog;

		private static List<Shelly> _shellies;
		public static List<Shelly> ForceMqttUpdateAvailable {
			get {
				return _shellies.FindAll(t => ShellyType.IsGen2(t.Type));
			}
		}

		private static List<string> Subscribtions = new List<string>();
		// set MQTT Online to 0 - Shelly set it back. In Seconds
		private static int _onlineTogglerSendIntervall = 30;
		public static int OnlineTogglerSendIntervall {
			set {
				_onlineTogglerSendIntervall = value;
				foreach(Shelly s in ForceMqttUpdateAvailable) {
					s.SetOnlineTogglerSendIntervall();
				}
			}
			get {
				return _onlineTogglerSendIntervall;
			}
		}
		private static int _onlineTogglerWait = 2;
		public static int OnlineTogglerWait {
			set {
				_onlineTogglerWait = value;
				foreach(Shelly s in ForceMqttUpdateAvailable) {
					s.SetOnlineTogglerWait();
				}
			}
			get {
				return _onlineTogglerWait;
			}
		}
		public static event EventHandler<valueChangedEventArgs> ValueChanged;
		public class valueChangedEventArgs: EventArgs {
			public int IdDatapoint { get; set; }
			public string Name { get; set; }
			public string Value { get; set; }
		}
		public static void Init() {
			Debug.Write(MethodInfo.GetCurrentMethod(), "Shelly Server Init");
			eventLog = new Logger(Logger.ESource.PlugInShelly);
			_shellies = new List<Shelly>();
			using(Database Sql = new Database("Select Shellys")) {
				List<TableShelly> lts = Sql.Select<TableShelly, TableRest>();
				foreach(TableShelly ts in lts) {
					try {
						Shelly s = new Shelly(ts);
						_shellies.Add(s);
						if(ShellyType.IsGen2(s.Type) && s.MqttActive) {
							AddSubscribtions(s.GetSubscribtions());
						}
					} catch(Exception ex) {
						eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
					}
				}
			}
			Program.MainProg.wpMQTTClient.shellyChanged += MQTTClient_shellyChanged;
			Debug.Write(MethodInfo.GetCurrentMethod(), "Shelly Server Inited");
		}
		public static void Start() {
			Task.Run(() => StartAsync()).Wait();
		}
		public async static Task StartAsync() {
			Debug.Write(MethodInfo.GetCurrentMethod(), "Shelly Server Start");
			OnlineTogglerSendIntervall = IniFile.GetInt("Shelly", "OnlineTogglerSendIntervall");
			OnlineTogglerWait = IniFile.GetInt("Shelly", "OnlineTogglerWait");
			foreach(Shelly s in _shellies) {
				s.GetStatus(true);
				s.GetHttpShelly(true);
				s.GetOutputStatus(true);
				s.GetMqttStatus();
				s.Start();
				await Task.Delay(100);
			}
			Debug.Write(MethodInfo.GetCurrentMethod(), "Shelly Server Started");
		}
		public static void Stop() {
			Debug.Write(MethodInfo.GetCurrentMethod(), "Shelly Server Stop");
			if(_shellies != null) {
				foreach(Shelly s in _shellies) {
					s.Stop();
				}
			}
			Debug.Write(MethodInfo.GetCurrentMethod(), "Shelly Server Stoped");
		}
		public static void RemoveShelly(int id) {
			using(Database Sql = new Database("Delete Shelly")) {
				if(_shellies != null && _shellies.Exists(t => t.Id == id)) {
					_shellies.Remove(_shellies.Find(t => t.Id == id));
					Sql.Delete<TableShelly>(id);
				}
			}
		}
		public static Shelly GetShellyFromWsId(string wsId) {
			if(_shellies != null && _shellies.Exists(t => t.WsId == wsId))
				return _shellies.Find(t => t.WsId == wsId);
			return null;
		}
		public static Shelly GetShellyWithCoIoT(string ip) {
			if (_shellies != null && _shellies.Exists(t => t.Ip == ip && t.CoIotActive))
				return _shellies.Find(t => t.Ip == ip && t.CoIotActive);
			return null;
		}
		public static string GetAllStatus() {
			foreach(Shelly s in _shellies) {
				s.GetStatus(true);
			}
			return "S_OK";
		}
		public static List<Shelly> GetCoIot() {
			return _shellies.FindAll(t => t.CoIotActive).FindAll(t => t.Active);
		}
		private static void SetValue(int idDp, string name, string value) {
			valueChangedEventArgs vcea = new valueChangedEventArgs();
			vcea.IdDatapoint = idDp;
			vcea.Name = name;
			vcea.Value = value;
			ValueChanged?.Invoke(idDp, vcea);
		}
		public static bool SetState(string mac, bool state) {
			bool returns = false;
			if(_shellies.Exists(t => t.Mac.ToLower() == mac)) {
				Shelly s = _shellies.Find(t => t.Mac.ToLower() == mac);
				SetValue(s.IdOnOff, s.Name, state ? "True" : "False");
				string DebugNewValue = $"Neuer Wert: Raum: {s.Name}, Status: {state}";
				if(Debug.debugShelly)
					eventLog.Write(MethodInfo.GetCurrentMethod(), DebugNewValue);
				Program.MainProg.lastchange = DebugNewValue;
				returns = true;
			} else {
				eventLog.Write(MethodInfo.GetCurrentMethod(), $"Shelly nicht gefunden: {mac}\r\n{_shellies}");
			}
			return returns;
		}
		public static bool SetWindow(string mac, bool window, string temp, string ldr) {
			bool returns = false;
			if(_shellies.Exists(t => t.Mac.ToLower() == mac)) {
				Shelly s = _shellies.Find(t => t.Mac.ToLower() == mac);
				SetValue(s.IdWindow, s.Name, window ? "True" : "False");
				SetValue(s.IdTemp, s.Name, temp.Replace(".", ","));
				SetValue(s.IdLdr, s.Name, ldr.Replace(".", ","));
				string DebugNewValue = String.Format("Raum: {0}", s.Name);
				DebugNewValue += String.Format("\r\n\tNeuer Wert: Status: {0}, ", window ? "True" : "False");
				DebugNewValue += String.Format("\r\n\tNeuer Wert: Temp: {0}", temp);
				DebugNewValue += String.Format("\r\n\tNeuer Wert: LDR: {0}, ", ldr);
				if(Debug.debugShelly)
					eventLog.Write(MethodInfo.GetCurrentMethod(), DebugNewValue);
				Program.MainProg.lastchange = DebugNewValue;
				returns = true;
			} else {
				eventLog.Write(MethodInfo.GetCurrentMethod(), $"Shelly nicht gefunden: {mac}");
			}
			return returns;
		}
		public static bool SetWindow(string mac, bool window) {
			bool returns = false;
			if(_shellies.Exists(t => t.Mac.ToLower() == mac)) {
				Shelly s = _shellies.Find(t => t.Mac.ToLower() == mac);
				SetValue(s.IdWindow, s.Name, window ? "True" : "False");
				string DebugNewValue = String.Format("Raum: {0}", s.Name);
				DebugNewValue += String.Format("\r\n\tNeuer Wert: Window: {0}, ", window ? "True" : "False");
				if(Debug.debugShelly)
					eventLog.Write(MethodInfo.GetCurrentMethod(), DebugNewValue);
				Program.MainProg.lastchange = DebugNewValue;
				returns = true;
			} else {
				eventLog.Write(MethodInfo.GetCurrentMethod(), $"Shelly nicht gefunden: {mac}");
			}
			return returns;
		}
		public static bool SetHumTemp(string mac, string hum, string temp) {
			bool returns = false;
			if(_shellies.Exists(t => t.Mac.ToLower() == mac)) {
				Shelly s = _shellies.Find(t => t.Mac.ToLower() == mac);
				SetValue(s.IdHum, s.Name, hum.Replace(".", ","));
				SetValue(s.IdTemp, s.Name, temp.Replace(".", ","));
				string DebugNewValue = String.Format("Raum: {0}", s.Name);
				DebugNewValue += String.Format("\r\n\tNeuer Wert: Hum: {0}, ", hum);
				DebugNewValue += String.Format("\r\n\tNeuer Wert: Temp: {0}", temp);
				if(Debug.debugShelly)
					eventLog.Write(MethodInfo.GetCurrentMethod(), DebugNewValue);
				Program.MainProg.lastchange = DebugNewValue;
				returns = true;
			} else {
				eventLog.Write(MethodInfo.GetCurrentMethod(), $"Shelly nicht gefunden: {mac}");
			}
			return returns;
		}
		public static bool SetTemp(string mac, string temp) {
			bool returns = false;
			if(_shellies.Exists(t => t.Mac.ToLower() == mac)) {
				Shelly s = _shellies.Find(t => t.Mac.ToLower() == mac);
				SetValue(s.IdTemp, s.Name, temp.Replace(".", ","));
				string DebugNewValue = String.Format("Raum: {0}", s.Name);
				DebugNewValue += String.Format("\r\n\tNeuer Wert: Temp: {0}", temp);
				if(Debug.debugShelly)
					eventLog.Write(MethodInfo.GetCurrentMethod(), DebugNewValue);
				Program.MainProg.lastchange = DebugNewValue;
				returns = true;
			} else {
				eventLog.Write(MethodInfo.GetCurrentMethod(), $"Shelly nicht gefunden: {mac}");
			}
			return returns;
		}
		public static bool SetHum(string mac, string hum) {
			bool returns = false;
			if(_shellies.Exists(t => t.Mac.ToLower() == mac)) {
				Shelly s = _shellies.Find(t => t.Mac.ToLower() == mac);
				SetValue(s.IdHum, s.Name, hum.Replace(".", ","));
				string DebugNewValue = String.Format("Raum: {0}", s.Name);
				DebugNewValue += String.Format("\r\n\tNeuer Wert: Hum: {0}, ", hum);
				if(Debug.debugShelly)
					eventLog.Write(MethodInfo.GetCurrentMethod(), DebugNewValue);
				Program.MainProg.lastchange = DebugNewValue;
				returns = true;
			} else {
				eventLog.Write(MethodInfo.GetCurrentMethod(), $"Shelly nicht gefunden: {mac}");
			}
			return returns;
		}
		public static bool SetLongPress(string mac) {
			bool returns = false;
			if(_shellies.Exists(t => t.Mac.ToLower() == mac)) {
				Shelly s = _shellies.Find(t => t.Mac.ToLower() == mac);
				s.SetLongPress();
				string DebugNewValue = String.Format("Raum: {0}", s.Name);
				if(Debug.debugShelly)
					eventLog.Write(MethodInfo.GetCurrentMethod(), DebugNewValue);
				Program.MainProg.lastchange = DebugNewValue;
				returns = true;
			} else {
				eventLog.Write(MethodInfo.GetCurrentMethod(), $"Shelly nicht gefunden: {mac}");
			}
			return returns;
		}
		public static string SendUrlCmd(string ip, string cmd) {
			IPAddress _ip;
			string target = $"http://{ip}/{cmd}";
			ret returns = new ret() { erg = ret.ERROR };
			if(IPAddress.TryParse(ip, out _ip)) {
				Shelly shelly = _shellies.Find(t => t.Ip.ToString() == ip);
				if(shelly != null) {
					if(shelly.Active) {
						HttpClientHandler handler = new HttpClientHandler();
						if(shelly.Un != "" && shelly.Pw != "") {
							handler.Credentials = new NetworkCredential(shelly.Un, shelly.Pw);
						}
						using(HttpClient webClient = new HttpClient(handler)) {
							Task.Run(async () => {
								try {
									HttpResponseMessage response = await webClient.GetAsync(target);
									response.EnsureSuccessStatusCode();
									returns.erg = ret.OK;
									returns.Json = await response.Content.ReadAsStringAsync();
									if(Debug.debugShelly)
										Debug.Write(MethodInfo.GetCurrentMethod(), $"Shelly sendUrlCmd after wait {_ip} - returns: {returns.message}");
								} catch(Exception ex) {
									returns.message = ex.Message;
									Debug.WriteError(MethodInfo.GetCurrentMethod(), ex, $"{target}: '{returns.message}'");
								}
							}).Wait();
						}
					} else {
						returns.message = "Shelly not active";
					}
				} else {
					returns.message = "Shelly not found";
				}
			} else {
				returns.message = "IP not valid";
			}
			return returns.ToString();
		}

		private static void MQTTClient_shellyChanged(object sender, MQTTClient.valueChangedEventArgs e) {
			int pos = e.topic.IndexOf("/");
			string name = e.topic.Substring(0, pos);
			string setting = e.topic.Substring(pos + 1);
			if(_shellies.Exists(t => t.MqttId == name)) {
				Shelly s = _shellies.Find(t => t.MqttId == name);
				if(setting == "info/Online")
					s.Online = e.value == "0" ? false : true;
			} else {
				Debug.Write(MethodInfo.GetCurrentMethod(), $"Shelly MQTT not active? '{e.topic}', '{e.value}'");
			}
		}
		public static void AddSubscribtions(List<string> topic) {
			Subscribtions.AddRange(topic);
		}
		public static List<string> GetSubscribtions() {
			return Subscribtions;
		}
	}
}
