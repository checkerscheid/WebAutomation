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
//# Revision     : $Rev:: 245                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: D1MiniServer.cs 245 2025-06-28 15:07:22Z                 $ #
//#                                                                                 #
//###################################################################################
using FreakaZone.Libraries.wpCommen;
using FreakaZone.Libraries.wpEventLog;
using FreakaZone.Libraries.wpIniFile;
using FreakaZone.Libraries.wpSQL;
using FreakaZone.Libraries.wpSQL.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WebAutomation.Communication;

namespace WebAutomation.Controller {
	public static class D1MiniServer {
		/// <summary></summary>
		private static Logger eventLog;

		private static List<D1Mini> _d1Minis;
		private static List<string> Subscribtions = new List<string>();

		private static UdpClient udpClient;
		private static int udpPort = 51346;
		private static bool searchActive = false;

		// set MQTT Online to 0 - D1Mini set it back. In Seconds
		private static int _onlineTogglerSendIntervall = 30;
		public static int OnlineTogglerSendIntervall {
			set {
				_onlineTogglerSendIntervall = value;
				foreach(D1Mini d1md in _d1Minis) {
					d1md.SetOnlineTogglerSendIntervall();
					Task.Delay(100).Wait();
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
				foreach(D1Mini d1md in _d1Minis) {
					d1md.SetOnlineTogglerWait();
				}
			}
			get {
				return _onlineTogglerWait;
			}
		}
		public static event EventHandler<valueChangedEventArgs> valueChanged;
		public class valueChangedEventArgs: EventArgs {
			public int idDatapoint { get; set; }
			public string name { get; set; }
			public string value { get; set; }
		}
		public static void Init() {
			Debug.Write(MethodInfo.GetCurrentMethod(), "D1Mini Server Init");
			eventLog = new Logger(Logger.ESource.PlugInD1Mini);
			_d1Minis = new List<D1Mini>();
			using(Database Sql = new Database("Select D1Minis")) {
				try {
					List<TableD1Mini> ltd1m = Sql.Select<TableD1Mini, TableRest>();
					foreach(TableD1Mini td1m in ltd1m) {
						D1Mini d1m = new D1Mini(td1m);
						_d1Minis.Add(d1m);
						AddSubscribtions(d1m.GetSubscribtions());
						//d1md.sendCmd("ForceMqttUpdate");
					}
				} catch(Exception ex) {
					Debug.WriteError(MethodInfo.GetCurrentMethod(), ex);
				}
			}
			Program.MainProg.wpMQTTClient.d1MiniChanged += MQTTClient_d1MiniChanged;
			Debug.Write(MethodInfo.GetCurrentMethod(), "D1Mini Server Inited");
		}
		public static void Start() {
			Task.Run(() => StartAsync()).Wait();
		}
		public async static Task StartAsync() {
			Debug.Write(MethodInfo.GetCurrentMethod(), "D1Mini Server Start");
			OnlineTogglerSendIntervall = IniFile.GetInt("D1Mini", "OnlineTogglerSendIntervall");
			OnlineTogglerWait = IniFile.GetInt("D1Mini", "OnlineTogglerWait");
			foreach(D1Mini d1md in _d1Minis) {
				d1md.Start();
				await Task.Delay(100);
			}
			Debug.Write(MethodInfo.GetCurrentMethod(), "D1Mini Server Started");
		}
		public static void Stop() {
			Debug.Write(MethodInfo.GetCurrentMethod(), "D1Mini Server Stop");
			StopSearch();
			if(_d1Minis != null) {
				foreach(D1Mini d1md in _d1Minis) {
					d1md.Stop();
				}
			}
			Debug.Write(MethodInfo.GetCurrentMethod(), "D1Mini Server Stoped");
		}
		public static void ForceRenewValue() {
			foreach(D1Mini d1md in _d1Minis) {
				d1md.SendCmd(new D1Mini.CmdList(D1Mini.CmdList.ForceRenewValue));
			}
		}
		public static bool ForceMqttUpdate() {
			bool returns = false;
			foreach(D1Mini d1md in _d1Minis) {
				if(!d1md.SendCmd(new D1Mini.CmdList(D1Mini.CmdList.ForceMqttUpdate)))
					returns = false;
			}
			return returns;
		}
		public static string GetServerSettings() {
			return "{" +
				$"\"OnlineTogglerSendIntervall\":\"{OnlineTogglerSendIntervall}\"," +
				$"\"OnlineTogglerWait\":\"{OnlineTogglerWait}\"" +
				"}";
		}
		public static string SetServerSetting(string setting, string value) {
			int _v = 0;
			int OnlineTogglerSendIntervallMin = 0;
			int OnlineTogglerSendIntervallMax = 120;
			int OnlineTogglerWaitMin = 1;
			int OnlineTogglerWaitMax = 10;

			if(!Int32.TryParse(value, out _v)) {
				return $"{{\"erg\":\"S_ERROR\",\"msg\":\"'value' is no integer ({value})\"}}";
			}
			switch(setting) {
				case "OnlineTogglerSendIntervall":
					if(_v < OnlineTogglerSendIntervallMin) {
						Debug.Write(MethodInfo.GetCurrentMethod(), $"ERROR OnlineTogglerSendIntervall, min value is {OnlineTogglerSendIntervallMin} ({_v})");
						return $"{{\"erg\":\"S_ERROR\",\"msg\":\"min value is {OnlineTogglerSendIntervallMin} ({_v})\"}}";
					}
					if(_v > OnlineTogglerSendIntervallMax) {
						Debug.Write(MethodInfo.GetCurrentMethod(), $"ERROR OnlineTogglerSendIntervall, max value is {OnlineTogglerSendIntervallMax} ({_v})");
						return $"{{\"erg\":\"S_ERROR\",\"msg\":\"max value is {OnlineTogglerSendIntervallMax} ({_v})\"}}";
					}
					OnlineTogglerSendIntervall = _v;
					Debug.Write(MethodInfo.GetCurrentMethod(), $"OnlineTogglerSendIntervall, new Value: {_v}");
					return $"{{\"erg\":\"S_OK\"}}";
				case "OnlineTogglerWait":
					if(_v < OnlineTogglerWaitMin) {
						Debug.Write(MethodInfo.GetCurrentMethod(), $"ERROR OnlineTogglerWait, min value is {OnlineTogglerWaitMin} ({_v})");
						return $"{{\"erg\":\"S_ERROR\",\"msg\":\"min value is {OnlineTogglerWaitMin} ({_v})\"}}";
					}
					if(_v > OnlineTogglerWaitMax) {
						Debug.Write(MethodInfo.GetCurrentMethod(), $"ERROR OnlineTogglerWait, max value is {OnlineTogglerWaitMax} ({_v})");
						return $"{{\"erg\":\"S_ERROR\",\"msg\":\"max value is {OnlineTogglerWaitMax} ({_v})\"}}";
					}
					OnlineTogglerWait = _v;
					Debug.Write(MethodInfo.GetCurrentMethod(), $"OnlineTogglerWait, new Value: {_v}");
					return $"{{\"erg\":\"S_OK\"}}";
				default:
					return $"{{\"erg\":\"S_ERROR\",\"msg\":\"unknown command\"}}";
			}
		}
		public static void AddD1Mini(int idd1mini) {
			using(Database Sql = new Database("Select new D1Mini")) {
				TableD1Mini td1m = Sql.Select<TableD1Mini>(idd1mini);
				D1Mini d1md = new D1Mini(td1m);
				if(!_d1Minis.Exists(t => t.Id == idd1mini))
					_d1Minis.Add(d1md);
				AddSubscribtions(d1md.GetSubscribtions());
			}
			Program.MainProg.wpMQTTClient.registerNewD1MiniDatapoints();
		}
		public static void RemoveD1Mini(int idd1mini) {
			using(Database Sql = new Database("Delete D1Mini")) {
				if(_d1Minis.Exists(t => t.Id == idd1mini))
					_d1Minis.Remove(_d1Minis.Find(t => t.Id == idd1mini));
				Sql.Delete<TableD1Mini>(idd1mini);
			}
		}
		private static void MQTTClient_d1MiniChanged(object sender, MQTTClient.valueChangedEventArgs e) {
			int pos = e.topic.IndexOf("/");
			string name = e.topic.Substring(0, pos);
			string setting = e.topic.Substring(pos + 1);
			if(_d1Minis.Exists(t => t.Name == name)) {
				D1Mini d1md = _d1Minis.Find(t => t.Name == name);
				if(setting == "info/DeviceName")
					d1md.readDeviceName = e.value;
				if(setting == "info/DeviceDescription")
					d1md.readDeviceDescription = e.value;
				if(setting == "info/Version")
					d1md.readVersion = e.value;
				if(setting == "info/WiFi/Ip")
					d1md.readIp = e.value;
				if(setting == "info/WiFi/Mac")
					d1md.readMac = e.value;
				if(setting == "info/WiFi/SSID")
					d1md.readSsid = e.value;
				if(setting == "UpdateMode")
					d1md.readUpdateMode = e.value;
				if(setting == "info/Online")
					d1md.Online = e.value == "0" ? false : true;
			}
		}
		public static void RenewActiveState() {
			using(Database db = new Database("Get All D1Mini's")) {
				List<TableD1Mini> table = db.Select<TableD1Mini>();
				foreach(TableD1Mini d1mini in table) {
					_d1Minis.Find(t => t.Id == d1mini.id_d1mini).Active = d1mini.active;
				}
			}
		}
		public static void AddSubscribtions(List<string> topic) {
			try {
				Subscribtions.AddRange(topic);
			} catch(Exception ex) {
				Debug.WriteError(MethodInfo.GetCurrentMethod(), ex);
			}
		}
		public static List<string> GetSubscribtions() {
			return Subscribtions;
		}
		public static string GetJson() {
			if(Debug.debugD1Mini)
				Debug.Write(MethodInfo.GetCurrentMethod(), "D1Mini getJson Settings");
			string returns = "{";
			foreach(D1Mini d1md in _d1Minis) {
				returns += $"\"{d1md.Name}\":{{";
				returns += $"\"DeviceName\":\"{d1md.readDeviceName}\",";
				returns += $"\"DeviceDescription\":\"{d1md.readDeviceDescription}\",";
				returns += $"\"Version\":\"{d1md.readVersion}\",";
				returns += $"\"Ip\":\"{d1md.readIp}\",";
				returns += $"\"Mac\":\"{d1md.readMac}\",";
				returns += $"\"Ssid\":\"{d1md.readSsid}\",";
				returns += $"\"UpdateMode\":\"{d1md.readUpdateMode}\"";
				returns += "},";
			}
			return returns.Remove(returns.Length - 1) + "}";
		}
		public static string GetJsonStatus(string ip) {
			return GetJsonStatus(ip, false);
		}
		public static string GetJsonStatus(string ip, bool saveStatus) {
			IPAddress _ip;
			string returns = new ret { erg = ret.ERROR }.ToString();
			if(IPAddress.TryParse(ip, out _ip)) {
				D1Mini d1md = _d1Minis.Find(t => t.IpAddress.ToString() == ip);
				if(d1md == null)
					return new ret { erg = ret.ERROR, message = "D1Mini not found" }.ToString();
				if(d1md.Active) {
					if(Debug.debugD1Mini)
						Debug.Write(MethodInfo.GetCurrentMethod(), $"D1Mini getJson Status {_ip}");

					string url = $"http://{_ip}/status";
					try {
						WebClient webClient = new WebClient();
						returns = webClient.DownloadString(new Uri(url));
						if(saveStatus)
							SaveJsonStatus(_ip, returns);
					} catch(Exception ex) {
						Debug.WriteError(MethodInfo.GetCurrentMethod(), ex, $"{_ip}: '{returns}'");
					}
				}
			} else {
				Debug.Write(MethodInfo.GetCurrentMethod(), $"D1Mini getJson Status IpError: '{ip}'");
			}
			return returns;
		}
		private static void SaveJsonStatus(IPAddress ip, string status) {
			using(Database Sql = new Database("save status")) {
				List<TableD1Mini> ltd1m = Sql.Select<TableD1Mini>($"[{nameof(TableD1Mini.ip)}] = '{ip}'");
				//string[][] erg = Sql.Query($"SELECT [id_d1mini] FROM [d1mini] WHERE [ip] = '{ip}'");
				if(ltd1m.Count != 1) {
					Debug.Write(MethodBase.GetCurrentMethod(), $"Warnung: D1Mini mit {ip}: {ltd1m.Count}");
				} else {
					Sql.Query(@$"MERGE INTO [d1minicfg] AS [TARGET]
USING (
VALUES (
	{ltd1m[0].id_d1mini}, '{ip}', '{status}', GETDATE()
)
) AS [SOURCE] (
	[id_d1mini], [ip], [status], [datetime]
) ON
[TARGET].[id_d1mini] = [SOURCE].[id_d1mini]
WHEN MATCHED AND [TARGET].[status] != [SOURCE].[status] THEN
UPDATE SET
	[TARGET].[ip] = [SOURCE].[ip],
	[TARGET].[status2] = [TARGET].[status],
	[TARGET].[datetime2] = [TARGET].[datetime],
	[TARGET].[status] = [SOURCE].[status],
	[TARGET].[datetime] = [SOURCE].[datetime]
WHEN NOT MATCHED THEN
INSERT (
	[id_d1mini], [ip], [status], [datetime]
)
VALUES (
	[SOURCE].[id_d1mini], [SOURCE].[ip], [SOURCE].[status], [SOURCE].[datetime]
);");
				}
			}
		}
		public static string GetJsonNeoPixel(string ip) {
			IPAddress _ip;
			string returns = "S_ERROR";
			if(IPAddress.TryParse(ip, out _ip)) {
				D1Mini d1md = _d1Minis.Find(t => t.IpAddress.ToString() == ip);
				if(d1md.Active) {
					if(Debug.debugD1Mini)
						Debug.Write(MethodInfo.GetCurrentMethod(), $"D1Mini getNeoPixel Status {_ip}");

					string url = $"http://{_ip}/getNeoPixel";
					try {
						WebClient webClient = new WebClient();
						returns = webClient.DownloadString(new Uri(url));
					} catch(Exception ex) {
						Debug.WriteError(MethodInfo.GetCurrentMethod(), ex, $"{_ip}: '{returns}' ({url})");
					}
				}
			} else {
				Debug.Write(MethodInfo.GetCurrentMethod(), $"D1Mini getJson NeoPixel Status IpError: '{ip}'");
			}
			return returns;
		}
		public static D1Mini Get(int id) {
			if(!_d1Minis.Exists(t => t.Id == id))
				return null;
			return _d1Minis.Find(t => t.Id == id);
		}
		public static D1Mini Get(string name) {
			if(!_d1Minis.Exists(t => t.Name == name))
				return null;
			return _d1Minis.Find(t => t.Name == name);
		}
		public static string SendUrlCmd(string ip, string cmd) {
			IPAddress _ip;
			string returns = "{\"erg\":\"S_ERROR\"}";
			if(IPAddress.TryParse(ip, out _ip)) {
				D1Mini d1md = _d1Minis.Find(t => t.IpAddress.ToString() == ip);
				if(d1md.Active) {
					string url = $"http://{_ip}/{cmd}";
					try {
						WebClient webClient = new WebClient();
						Task.Run(() => returns = webClient.DownloadString(new Uri(url))).Wait();
						if(Debug.debugD1Mini)
							Debug.Write(MethodInfo.GetCurrentMethod(), $"D1Mini sendUrlCmd after wait {_ip} - {url} - returns: {returns}");
					} catch(Exception ex) {
						Debug.WriteError(MethodInfo.GetCurrentMethod(), ex, $"{_ip}: '{returns}'");
					}
				}
			}
			return returns;
		}
		public static string StartSearch() {
			Debug.Write(MethodInfo.GetCurrentMethod(), "Start FreakaZone search");
			searchActive = true;
			udpClient = new UdpClient();
			udpClient.Client.SendTimeout = 1000;
			udpClient.Client.ReceiveTimeout = 1000;
			udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, udpPort));
			Dictionary<string, D1MiniBroadcast> foundNewD1Mini = new Dictionary<string, D1MiniBroadcast>();

			var from = new IPEndPoint(0, 0);
			var task = Task.Run(() => {
				while(searchActive) {
					try {
						var recvBuffer = udpClient.Receive(ref from);
						string recieved = Encoding.UTF8.GetString(recvBuffer);
						if(Common.IsValidJson(recieved)) {
							D1MiniBroadcast D1MiniRecieved = JsonConvert.DeserializeObject<D1MiniBroadcast>(recieved);
							if(D1MiniRecieved != null && D1MiniRecieved.Iam != null &&
								!_d1Minis.Exists(t => t.Name == D1MiniRecieved.Iam.FreakaZoneClient)) {
								foundNewD1Mini.Add(D1MiniRecieved.Iam.FreakaZoneClient, D1MiniRecieved);

								Debug.Write(MethodInfo.GetCurrentMethod(), $"Found new D1Mini: {D1MiniRecieved.Iam.FreakaZoneClient}");
							}
						}
						Debug.Write(MethodInfo.GetCurrentMethod(), Encoding.UTF8.GetString(recvBuffer));
					} catch(SocketException ex) {
						if(ex.SocketErrorCode == SocketError.TimedOut) {
							searchActive = false;
							Debug.Write(MethodInfo.GetCurrentMethod(), "Search finished, cause: Timeout");
						} else {
							searchActive = false;
							Debug.WriteError(MethodInfo.GetCurrentMethod(), ex);
						}
					} catch(Exception ex) {
						searchActive = false;
						Debug.WriteError(MethodInfo.GetCurrentMethod(), ex);
					}
				}
				StopSearch();
			});
			SendSearch();
			task.Wait();
			string returns = JsonConvert.SerializeObject(foundNewD1Mini);
			return returns;
		}
		public static string StartSearch(Guid id) {
			Debug.Write(MethodInfo.GetCurrentMethod(), "Start WS - FreakaZone search");
			searchActive = true;
			udpClient = new UdpClient();
			udpClient.Client.SendTimeout = 2000;
			udpClient.Client.ReceiveTimeout = 2000;
			udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, udpPort));
			//Dictionary<string, D1MiniBroadcast> foundNewD1Mini = new Dictionary<string, D1MiniBroadcast>();

			var from = new IPEndPoint(0, 0);
			var task = Task.Run(() => {
				while(searchActive) {
					try {
						var recvBuffer = udpClient.Receive(ref from);
						string recieved = Encoding.UTF8.GetString(recvBuffer);
						if(Common.IsValidJson(recieved)) {
							D1MiniBroadcast D1MiniRecieved = JsonConvert.DeserializeObject<D1MiniBroadcast>(recieved);
							if(D1MiniRecieved != null && D1MiniRecieved.Iam != null) {
								Program.MainProg.wpWebSockets.sendText(new WebSockets.wpTcpClient(id), "SearchD1Mini",
									"{\"exists\":" + (_d1Minis.Exists(t => t.Name == D1MiniRecieved.Iam.FreakaZoneClient) ? "true" : "false") + "," +
									$"\"recieved\":{recieved}" + "}");
								//foundNewD1Mini.Add(D1MiniRecieved.Iam.FreakaZoneClient, D1MiniRecieved);

								Debug.Write(MethodInfo.GetCurrentMethod(), $"Found new D1Mini: {D1MiniRecieved.Iam.FreakaZoneClient}");
							}
						}
						Debug.Write(MethodInfo.GetCurrentMethod(), Encoding.UTF8.GetString(recvBuffer));
					} catch(SocketException ex) {
						if(ex.SocketErrorCode == SocketError.TimedOut) {
							searchActive = false;
							Program.MainProg.wpWebSockets.sendText(new WebSockets.wpTcpClient(id), "SearchD1MiniFinished", "\"S_OK\"");
							Debug.Write(MethodInfo.GetCurrentMethod(), "Search finished, cause: Timeout");
						} else {
							searchActive = false;
							Debug.WriteError(MethodInfo.GetCurrentMethod(), ex);
						}
					} catch(Exception ex) {
						searchActive = false;
						Debug.WriteError(MethodInfo.GetCurrentMethod(), ex);
					}
				}
				StopSearch();
			});
			SendSearch();
			task.Wait();
			string returns = "S_OK";
			return returns;
		}
		private static void SendSearch() {
			var data = Encoding.UTF8.GetBytes("FreakaZone Member?");
			udpClient.Send(data, data.Length, "255.255.255.255", udpPort);
		}
		private static void StopSearch() {
			try {
				Debug.Write(MethodInfo.GetCurrentMethod(), "Stop FreakaZone search");
				searchActive = false;
				if(udpClient != null) {
					udpClient.Close();
					udpClient.Dispose();
				}
			} catch(Exception ex) {
				Debug.WriteError(MethodInfo.GetCurrentMethod(), ex);
			}
		}
		private static void SetValue(int idDp, string name, string value) {
			valueChangedEventArgs vcea = new valueChangedEventArgs();
			vcea.idDatapoint = idDp;
			vcea.name = name;
			vcea.value = value;
			if(valueChanged != null)
				valueChanged.Invoke(idDp, vcea);
		}
		public static bool SetBM(string mac, bool state) {
			bool returns = false;
			if(_d1Minis.Exists(t => t.readMac == mac)) {
				D1Mini d1m = _d1Minis.Find(t => t.readMac == mac);
				if(d1m.Id_onoff > 0) {
					SetValue(d1m.Id_onoff, d1m.Name, state ? "True" : "False");
					string DebugNewValue = String.Format("Neuer Wert: D1Mini: {0}, BM: {1}", d1m.Name, state);
					if(Debug.debugD1Mini)
						eventLog.Write(MethodInfo.GetCurrentMethod(), DebugNewValue);
					Program.MainProg.lastchange = DebugNewValue;
					returns = true;
				} else {
					if(Debug.debugD1Mini)
						eventLog.Write(MethodInfo.GetCurrentMethod(), $"D1Mini: {d1m.Name}, idOnOff nicht gesetzt");
				}
			} else {
				eventLog.Write(MethodInfo.GetCurrentMethod(), $"D1Mini nicht gefunden: {mac}");
			}
			return returns;
		}
		public static bool SetTemp(string mac, string temp) {
			bool returns = false;
			if(_d1Minis.Exists(t => t.readMac == mac)) {
				D1Mini d1m = _d1Minis.Find(t => t.readMac == mac);
				if(d1m.Id_temp > 0) {
					SetValue(d1m.Id_temp, d1m.Name, temp.Replace(".", ","));
					string DebugNewValue = String.Format("D1Mini: {0}", d1m.Name);
					DebugNewValue += String.Format("\r\n\tNeuer Wert: Temp: {0}", temp);
					if(Debug.debugD1Mini)
						eventLog.Write(MethodInfo.GetCurrentMethod(), DebugNewValue);
					Program.MainProg.lastchange = DebugNewValue;
					returns = true;
				} else {
					if(Debug.debugD1Mini)
						eventLog.Write(MethodInfo.GetCurrentMethod(), $"D1Mini: {d1m.Name}, idTemp nicht gesetzt");
				}
			} else {
				eventLog.Write(MethodInfo.GetCurrentMethod(), $"D1Mini nicht gefunden: {mac}");
			}
			return returns;
		}
		public static bool SetHum(string mac, string hum) {
			bool returns = false;
			if(_d1Minis.Exists(t => t.readMac == mac)) {
				D1Mini d1m = _d1Minis.Find(t => t.readMac == mac);
				if(d1m.Id_hum > 0) {
					SetValue(d1m.Id_hum, d1m.Name, hum.Replace(".", ","));
					string DebugNewValue = String.Format("D1Mini: {0}", d1m.Name);
					DebugNewValue += String.Format("\r\n\tNeuer Wert: Hum: {0}, ", hum);
					if(Debug.debugD1Mini)
						eventLog.Write(MethodInfo.GetCurrentMethod(), DebugNewValue);
					Program.MainProg.lastchange = DebugNewValue;
					returns = true;
				} else {
					if(Debug.debugD1Mini)
						eventLog.Write(MethodInfo.GetCurrentMethod(), $"D1Mini: {d1m.Name}, idHum nicht gesetzt");
				}
			} else {
				eventLog.Write(MethodInfo.GetCurrentMethod(), $"D1Mini nicht gefunden: {mac}");
			}
			return returns;
		}
		public static bool SetLdr(string mac, string ldr) {
			bool returns = false;
			if(_d1Minis.Exists(t => t.readMac == mac)) {
				D1Mini d1m = _d1Minis.Find(t => t.readMac == mac);
				if(d1m.Id_ldr > 0) {
					SetValue(d1m.Id_ldr, d1m.Name, ldr.Replace(".", ","));
					string DebugNewValue = String.Format("D1Mini: {0}", d1m.Name);
					DebugNewValue += String.Format("\r\n\tNeuer Wert: LDR: {0}, ", ldr);
					if(Debug.debugD1Mini)
						eventLog.Write(MethodInfo.GetCurrentMethod(), DebugNewValue);
					Program.MainProg.lastchange = DebugNewValue;
					returns = true;
				} else {
					if(Debug.debugD1Mini)
						eventLog.Write(MethodInfo.GetCurrentMethod(), $"D1Mini: {d1m.Name}, idLdr nicht gesetzt");
				}
			} else {
				eventLog.Write(MethodInfo.GetCurrentMethod(), $"D1Mini nicht gefunden: {mac}");
			}
			return returns;
		}
		public static bool SetLight(string mac, string light) {
			bool returns = false;
			if(_d1Minis.Exists(t => t.readMac == mac)) {
				D1Mini d1m = _d1Minis.Find(t => t.readMac == mac);
				if(d1m.Id_light > 0) {
					SetValue(d1m.Id_light, d1m.Name, light.Replace(".", ","));
					string DebugNewValue = String.Format("D1Mini: {0}", d1m.Name);
					DebugNewValue += String.Format("\r\n\tNeuer Wert: Light: {0}, ", light);
					if(Debug.debugD1Mini)
						eventLog.Write(MethodInfo.GetCurrentMethod(), DebugNewValue);
					Program.MainProg.lastchange = DebugNewValue;
					returns = true;
				} else {
					if(Debug.debugD1Mini)
						eventLog.Write(MethodInfo.GetCurrentMethod(), $"D1Mini: {d1m.Name}, idLight nicht gesetzt");
				}
			} else {
				eventLog.Write(MethodInfo.GetCurrentMethod(), $"D1Mini nicht gefunden: {mac}");
			}
			return returns;
		}
		public static bool SetRelais(string mac, bool state) {
			bool returns = false;
			if(_d1Minis.Exists(t => t.readMac == mac)) {
				D1Mini d1m = _d1Minis.Find(t => t.readMac == mac);
				if(d1m.Id_relais > 0) {
					SetValue(d1m.Id_relais, d1m.Name, state ? "True" : "False");
					string DebugNewValue = String.Format("Neuer Wert: D1Mini: {0}, Relais: {1}", d1m.Name, state);
					if(Debug.debugD1Mini)
						eventLog.Write(MethodInfo.GetCurrentMethod(), DebugNewValue);
					Program.MainProg.lastchange = DebugNewValue;
					returns = true;
				} else {
					if(Debug.debugD1Mini)
						eventLog.Write(MethodInfo.GetCurrentMethod(), $"D1Mini: {d1m.Name}, idRelais nicht gesetzt");
				}
			} else {
				eventLog.Write(MethodInfo.GetCurrentMethod(), $"D1Mini nicht gefunden: {mac}");
			}
			return returns;
		}
		public static bool SetRain(string mac, string rain) {
			bool returns = false;
			if(_d1Minis.Exists(t => t.readMac == mac)) {
				D1Mini d1m = _d1Minis.Find(t => t.readMac == mac);
				if(d1m.Id_rain > 0) {
					SetValue(d1m.Id_rain, d1m.Name, rain.Replace(".", ","));
					string DebugNewValue = String.Format("D1Mini: {0}", d1m.Name);
					DebugNewValue += String.Format("\r\n\tNeuer Wert: Rain: {0}, ", rain);
					if(Debug.debugD1Mini)
						eventLog.Write(MethodInfo.GetCurrentMethod(), DebugNewValue);
					Program.MainProg.lastchange = DebugNewValue;
					returns = true;
				} else {
					if(Debug.debugD1Mini)
						eventLog.Write(MethodInfo.GetCurrentMethod(), $"D1Mini: {d1m.Name}, idRain nicht gesetzt");
				}
			} else {
				eventLog.Write(MethodInfo.GetCurrentMethod(), $"D1Mini nicht gefunden: {mac}");
			}
			return returns;
		}
		public static bool SetMoisture(string mac, string moisture) {
			bool returns = false;
			if(_d1Minis.Exists(t => t.readMac == mac)) {
				D1Mini d1m = _d1Minis.Find(t => t.readMac == mac);
				if(d1m.Id_moisture > 0) {
					SetValue(d1m.Id_moisture, d1m.Name, moisture.Replace(".", ","));
					string DebugNewValue = String.Format("D1Mini: {0}", d1m.Name);
					DebugNewValue += String.Format("\r\n\tNeuer Wert: Moisture: {0}, ", moisture);
					if(Debug.debugD1Mini)
						eventLog.Write(MethodInfo.GetCurrentMethod(), DebugNewValue);
					Program.MainProg.lastchange = DebugNewValue;
					returns = true;
				} else {
					if(Debug.debugD1Mini)
						eventLog.Write(MethodInfo.GetCurrentMethod(), $"D1Mini: {d1m.Name}, idOnOff nicht gesetzt");
				}
			} else {
				eventLog.Write(MethodInfo.GetCurrentMethod(), $"D1Mini nicht gefunden: {mac}");
			}
			return returns;
		}
		public static bool SetVolume(string mac, string volume) {
			bool returns = false;
			if(_d1Minis.Exists(t => t.readMac == mac)) {
				D1Mini d1m = _d1Minis.Find(t => t.readMac == mac);
				if(d1m.Id_vol > 0) {
					SetValue(d1m.Id_vol, d1m.Name, volume.Replace(".", ","));
					string DebugNewValue = String.Format("D1Mini: {0}", d1m.Name);
					DebugNewValue += String.Format("\r\n\tNeuer Wert: Volume: {0}, ", volume);
					if(Debug.debugD1Mini)
						eventLog.Write(MethodInfo.GetCurrentMethod(), DebugNewValue);
					Program.MainProg.lastchange = DebugNewValue;
					returns = true;
				} else {
					if(Debug.debugD1Mini)
						eventLog.Write(MethodInfo.GetCurrentMethod(), $"D1Mini: {d1m.Name}, idVol nicht gesetzt");
				}
			} else {
				eventLog.Write(MethodInfo.GetCurrentMethod(), $"D1Mini nicht gefunden: {mac}");
			}
			return returns;
		}
		public static bool SetWindow(string mac, bool state) {
			bool returns = false;
			if(_d1Minis.Exists(t => t.readMac == mac)) {
				D1Mini d1m = _d1Minis.Find(t => t.readMac == mac);
				if(d1m.Id_window > 0) {
					SetValue(d1m.Id_window, d1m.Name, state ? "True" : "False");
					string DebugNewValue = String.Format("Neuer Wert: D1Mini: {0}, Window: {1}", d1m.Name, state);
					if(Debug.debugD1Mini)
						eventLog.Write(MethodInfo.GetCurrentMethod(), DebugNewValue);
					Program.MainProg.lastchange = DebugNewValue;
					returns = true;
				} else {
					if(Debug.debugD1Mini)
						eventLog.Write(MethodInfo.GetCurrentMethod(), $"D1Mini: {d1m.Name}, idWindow nicht gesetzt");
				}
			} else {
				eventLog.Write(MethodInfo.GetCurrentMethod(), $"D1Mini nicht gefunden: {mac}");
			}
			return returns;
		}
		public static bool SetAnalogOut(string mac, string analogout) {
			bool returns = false;
			if(_d1Minis.Exists(t => t.readMac == mac)) {
				D1Mini d1m = _d1Minis.Find(t => t.readMac == mac);
				if(d1m.Id_analogout > 0) {
					SetValue(d1m.Id_analogout, d1m.Name, analogout);
					string DebugNewValue = String.Format("D1Mini: {0}", d1m.Name);
					DebugNewValue += String.Format("\r\n\tNeuer Wert: AnalogOut: {0}, ", analogout);
					if(Debug.debugD1Mini)
						eventLog.Write(MethodInfo.GetCurrentMethod(), DebugNewValue);
					Program.MainProg.lastchange = DebugNewValue;
					returns = true;
				} else {
					if(Debug.debugD1Mini)
						eventLog.Write(MethodInfo.GetCurrentMethod(), $"D1Mini: {d1m.Name}, idAnalog nicht gesetzt");
				}
			} else {
				eventLog.Write(MethodInfo.GetCurrentMethod(), $"D1Mini nicht gefunden: {mac}");
			}
			return returns;
		}
		public static string SetRFID(string RFID) {
			string returns = "";
			string[][] erg;
			string id = "NULL";
			try {
				using(Database Sql = new Database("get User RFID")) {
					erg = Sql.Query(@$"SELECT TOP(1) [u].[name], [u].[lastname], [r].[id_rfid], [r].[description]
					FROM [user] [u]
					INNER JOIN [rfid] [r] ON [u].[id_user] = [r].[id_user]
					WHERE [r].[chipid] = '{RFID}'");
				}
				if(erg.Length > 0) {
					Debug.Write(MethodBase.GetCurrentMethod(), $"Found RFID Chip: '{RFID}', {erg[0][1]}, {erg[0][0]} ({erg[0][3]})");
					id = erg[0][2];
					returns = $"\"user\":{{\"name\":\"{erg[0][1]}, {erg[0][0]}\",\"description\";\"{erg[0][3]}\",\"RFID\":\"{RFID}\"}}";
				} else {
					Debug.Write(MethodBase.GetCurrentMethod(), $"Neuer RFID Chip: '{RFID}', kein User");
					returns = $"\"user\":{{\"name\":\"unknown\",\"RFID\":\"{RFID}\"}}";
				}
				using(Database Sql = new Database("save Historical RFID Data")) {
					Sql.Query($"INSERT INTO [rfidactivity] ([id_rfid], [chipid]) VALUES ({id}, {RFID})");
				}
			} catch(Exception ex) {
				Debug.WriteError(MethodBase.GetCurrentMethod(), ex);
			}
			return returns;
		}
	}
}
