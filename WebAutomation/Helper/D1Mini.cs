//###################################################################################
//#                                                                                 #
//#              (C) FreakaZone GmbH                                                #
//#              =======================                                            #
//#                                                                                 #
//###################################################################################
//#                                                                                 #
//# Author       : Christian Scheid                                                 #
//# Date         : 07.11.2019                                                       #
//#                                                                                 #
//# Revision     : $Rev:: 110                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: D1Mini.cs 110 2024-06-17 15:17:17Z                       $ #
//#                                                                                 #
//###################################################################################
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace WebAutomation.Helper {
	public static class D1MiniServer {
		/// <summary></summary>
		private static Logger eventLog;

		private static Dictionary<string, D1MiniDevice> D1Minis;
		// key: mac, value: name
		private static Dictionary<string, string> D1MinisMac;
		private static List<string> Subscribtions = new List<string>();

		private static UdpClient udpClient;
		private static int udpPort = 51346;
		private static bool searchActive = false;

		// set MQTT Online to 0 - D1 Mini set it back. In Seconds
		private static int _onlineTogglerSendIntervall = 30;
		public static int OnlineTogglerSendIntervall {
			set {
				_onlineTogglerSendIntervall = value;
				foreach(KeyValuePair<string, D1MiniDevice> kvp in D1Minis) {
					kvp.Value.SetOnlineTogglerSendIntervall();
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
				foreach(KeyValuePair<string, D1MiniDevice> kvp in D1Minis) {
					kvp.Value.SetOnlineTogglerWait();
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
		public static void Start() {
			wpDebug.Write("D1 Mini Server Start");
			eventLog = new Logger(wpEventLog.PlugInD1Mini);
			D1Minis = new Dictionary<string, D1MiniDevice>();
			D1MinisMac = new Dictionary<string, string>();
			using(SQL SQL = new SQL("Select Shellys")) {
				string[][] Query1 = SQL.wpQuery(@"SELECT
						[d].[id_d1mini], [d].[name], [d].[description], [d].[ip], [d].[mac],
						[r].[id_onoff], [r].[id_temp], [r].[id_hum], [r].[id_ldr], [r].[id_light],
						[r].[id_relais], [r].[id_rain], [r].[id_moisture], [r].[id_vol], [r].[id_window]
					FROM [d1mini] [d]
					LEFT JOIN [rest] [r] ON [d].[id_d1mini] = [r].[id_d1mini]
					WHERE [active] = 1");
				string name, description, mac;
				int id_d1mini;
				IPAddress ip;
				for(int i = 0; i < Query1.Length; i++) {
					Int32.TryParse(Query1[i][0], out id_d1mini);
					name = Query1[i][1];
					description = Query1[i][2];
					IPAddress.TryParse(Query1[i][3], out ip);
					mac = Query1[i][4];
					D1MiniDevice d1md = new D1MiniDevice(name, ip, mac, description);
					d1md.readDeviceDescription = description;
					D1Minis.Add(name, d1md);
					D1MinisMac.Add(mac, name);

					if(!String.IsNullOrEmpty(Query1[i][5])) d1md.id_onoff = Int32.Parse(Query1[i][5]);
					if(!String.IsNullOrEmpty(Query1[i][6])) d1md.id_temp = Int32.Parse(Query1[i][6]);
					if(!String.IsNullOrEmpty(Query1[i][7])) d1md.id_hum = Int32.Parse(Query1[i][7]);
					if(!String.IsNullOrEmpty(Query1[i][8])) d1md.id_ldr = Int32.Parse(Query1[i][8]);
					if(!String.IsNullOrEmpty(Query1[i][9])) d1md.id_light = Int32.Parse(Query1[i][9]);

					if(!String.IsNullOrEmpty(Query1[i][10])) d1md.id_relais = Int32.Parse(Query1[i][10]);
					if(!String.IsNullOrEmpty(Query1[i][11])) d1md.id_rain = Int32.Parse(Query1[i][11]);
					if(!String.IsNullOrEmpty(Query1[i][12])) d1md.id_moisture = Int32.Parse(Query1[i][12]);
					if(!String.IsNullOrEmpty(Query1[i][13])) d1md.id_vol = Int32.Parse(Query1[i][13]);
					if(!String.IsNullOrEmpty(Query1[i][14])) d1md.id_window = Int32.Parse(Query1[i][14]);

					addSubscribtions(d1md.getSubscribtions());
					//d1md.sendCmd("ForceMqttUpdate");
				}
			}
			OnlineTogglerSendIntervall = Ini.getInt("D1Mini", "OnlineTogglerSendIntervall");
			OnlineTogglerWait = Ini.getInt("D1Mini", "OnlineTogglerWait");
			Program.MainProg.wpMQTTClient.d1MiniChanged += wpMQTTClient_d1MiniChanged;
		}
		public static void Stop() {
			stopSearch();
			foreach(KeyValuePair<string, D1MiniDevice> kvp in D1Minis) kvp.Value.Stop();
		}
		public static void ForceRenewValue() {
			foreach(KeyValuePair<string, D1MiniDevice> kvp in D1Minis) {
				kvp.Value.sendCmd(new D1MiniDevice.cmdList(D1MiniDevice.cmdList.ForceRenewValue));
			}
		}
		public static bool ForceMqttUpdate() {
			bool returns = false;
			foreach(KeyValuePair<string, D1MiniDevice> kvp in D1Minis) {
				if(!kvp.Value.sendCmd(new D1MiniDevice.cmdList(D1MiniDevice.cmdList.ForceMqttUpdate)))
					returns = false;
			}
			return returns;
		}
		public static string getServerSettings() {
			return "{" + 
				$"\"OnlineTogglerSendIntervall\":\"{OnlineTogglerSendIntervall}\"," +
				$"\"OnlineTogglerWait\":\"{OnlineTogglerWait}\"" +
				"}";
		}
		public static string setServerSetting(string setting, string value) {
			int _v = 0;
			int OnlineTogglerSendIntervallMin = 0;
			int OnlineTogglerSendIntervallMax = 120;
			int OnlineTogglerWaitMin = 1;
			int OnlineTogglerWaitMax = 10;

			if(!Int32.TryParse(value, out _v)) {
				return  $"{{\"erg\":\"S_ERROR\",\"msg\":\"'value' is no integer ({value})\"}}";
			}
			switch(setting) {
				case "OnlineTogglerSendIntervall":
					if(_v < OnlineTogglerSendIntervallMin) {
						wpDebug.Write($"ERROR OnlineTogglerSendIntervall, min value is {OnlineTogglerSendIntervallMin} ({_v})");
						return $"{{\"erg\":\"S_ERROR\",\"msg\":\"min value is {OnlineTogglerSendIntervallMin} ({_v})\"}}";
					}
					if(_v > OnlineTogglerSendIntervallMax) {
						wpDebug.Write($"ERROR OnlineTogglerSendIntervall, max value is {OnlineTogglerSendIntervallMax} ({_v})");
						return $"{{\"erg\":\"S_ERROR\",\"msg\":\"max value is {OnlineTogglerSendIntervallMax} ({_v})\"}}";
					}
					OnlineTogglerSendIntervall = _v;
					wpDebug.Write($"OnlineTogglerSendIntervall, new Value: {_v}");
					return $"{{\"erg\":\"S_OK\"}}";
				case "OnlineTogglerWait":
					if(_v < OnlineTogglerWaitMin) {
						wpDebug.Write($"ERROR OnlineTogglerWait, min value is {OnlineTogglerWaitMin} ({_v})");
						return $"{{\"erg\":\"S_ERROR\",\"msg\":\"min value is {OnlineTogglerWaitMin} ({_v})\"}}";
					}
					if(_v > OnlineTogglerWaitMax) {
						wpDebug.Write($"ERROR OnlineTogglerWait, max value is {OnlineTogglerWaitMax} ({_v})");
						return $"{{\"erg\":\"S_ERROR\",\"msg\":\"max value is {OnlineTogglerWaitMax} ({_v})\"}}";
					}
					OnlineTogglerWait = _v;
					wpDebug.Write($"OnlineTogglerWait, new Value: {_v}");
					return $"{{\"erg\":\"S_OK\"}}";
				default:
					return $"{{\"erg\":\"S_ERROR\",\"msg\":\"unknown command\"}}";
			}
		}
		public static void addD1Mini(int idd1mini) {
			using(SQL SQL = new SQL("Select new D1Mini")) {
				string[][] Query1 = SQL.wpQuery(@$"SELECT
						[name], [description], [ip], [mac]
					FROM [d1mini] WHERE [id_d1mini] = {idd1mini}");
				string name, description, mac;
				IPAddress ip;
				if(Query1.Length >= 1) {
					name = Query1[0][0];
					description = Query1[0][1];
					IPAddress.TryParse(Query1[0][2], out ip);
					mac = Query1[0][3];
					D1MiniDevice d1md = new D1MiniDevice(name, ip, mac, description);
					if(!D1Minis.ContainsKey(name)) D1Minis.Add(name, d1md);
					addSubscribtions(d1md.getSubscribtions());
				}
			}
			Program.MainProg.wpMQTTClient.registerNewD1MiniDatapoints();
		}
		public static void removeD1Mini(int idd1mini) {
			using(SQL SQL = new SQL("Delete D1Mini")) {
				string[][] Query1 = SQL.wpQuery($"SELECT [name] FROM [d1mini] WHERE [id_d1mini] = {idd1mini}");
				string name = "";
				if(Query1.Length >= 1) {
					name = Query1[0][0];
					if(D1Minis.ContainsKey(name))
						D1Minis.Remove(name);
					SQL.wpNonResponse($"DELETE FROM [d1mini] WHERE [id_d1mini] = {idd1mini}");
				}
			}
		}
		private static void wpMQTTClient_d1MiniChanged(object sender, MQTTClient.valueChangedEventArgs e) {
			int pos = e.topic.IndexOf("/");
			string name = e.topic.Substring(0, pos);
			string setting = e.topic.Substring(pos + 1);
			if(D1Minis.ContainsKey(name)) {
				if(setting == "info/DeviceName")
					D1Minis[name].readDeviceName = e.value;
				if(setting == "info/DeviceDescription")
					D1Minis[name].readDeviceDescription = e.value;
				if(setting == "info/Version")
					D1Minis[name].readVersion = e.value;
				if(setting == "info/WiFi/Ip")
					D1Minis[name].readIp = e.value;
				if(setting == "info/WiFi/Mac")
					D1Minis[name].readMac = e.value;
				if(setting == "info/WiFi/SSID")
					D1Minis[name].readSsid = e.value;
				if(setting == "UpdateMode")
					D1Minis[name].readUpdateMode = e.value;
				if(setting == "info/Online")
					D1Minis[name].Online = e.value == "0" ? false : true;
			}
		}
		public static void addSubscribtions(List<string> topic) {
			Subscribtions.AddRange(topic);
		}
		public static List<string> getSubscribtions() {
			return Subscribtions;
		}
		public static string getJson() {
			if(wpDebug.debugD1Mini) wpDebug.Write("D1Mini getJson Settings");
			string returns = "{";
			foreach(KeyValuePair<string, D1MiniDevice> kvp in D1Minis) {
				returns += $"\"{kvp.Key}\":{{";
				returns += $"\"DeviceName\":\"{kvp.Value.readDeviceName}\",";
				returns += $"\"DeviceDescription\":\"{kvp.Value.readDeviceDescription}\",";
				returns += $"\"Version\":\"{kvp.Value.readVersion}\",";
				returns += $"\"Ip\":\"{kvp.Value.readIp}\",";
				returns += $"\"Mac\":\"{kvp.Value.readMac}\",";
				returns += $"\"Ssid\":\"{kvp.Value.readSsid}\",";
				returns += $"\"UpdateMode\":\"{kvp.Value.readUpdateMode}\"";
				returns += "},";
			}
			return returns.Remove(returns.Length - 1) + "}";
		}
		public static string getJsonStatus(string ip) {
			IPAddress _ip;
			string returns = "S_ERROR";
			if(IPAddress.TryParse(ip, out _ip)) {
				if(wpDebug.debugD1Mini)
					wpDebug.Write($"D1Mini getJson Status {_ip}");

				string url = $"http://{_ip}/status";
				try {
					WebClient webClient = new WebClient();
					returns = webClient.DownloadString(new Uri(url));
				} catch(Exception ex) {
					wpDebug.WriteError(ex, $"{_ip}: '{returns}'");
				}
			} else {
				wpDebug.Write($"D1Mini getJson Status IpError: '{ip}'");
			}
			return returns;
		}
		public static D1MiniDevice get(string name) {
			if(!D1Minis.ContainsKey(name)) return null;
			return D1Minis[name];
		}
		public static string startSearch() {
			wpDebug.Write("Start FreakaZone search");
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
						if(wpHelp.IsValidJson(recieved)) {
							D1MiniBroadcast D1MiniRecieved = JsonConvert.DeserializeObject<D1MiniBroadcast>(recieved);
							if(D1MiniRecieved != null && D1MiniRecieved.Iam != null &&
								!D1Minis.ContainsKey(D1MiniRecieved.Iam.FreakaZoneClient)) {
								foundNewD1Mini.Add(D1MiniRecieved.Iam.FreakaZoneClient, D1MiniRecieved);
								wpDebug.Write($"Found new D1Mini: {D1MiniRecieved.Iam.FreakaZoneClient}");
							}
						}
						wpDebug.Write(Encoding.UTF8.GetString(recvBuffer));
					} catch(SocketException ex) {
						if(ex.SocketErrorCode == SocketError.TimedOut) {
							searchActive = false;
							wpDebug.Write("Search finished, cause: Timeout");
						} else {
							searchActive = false;
							wpDebug.WriteError(ex);
						}
					} catch(Exception ex) {
						searchActive = false;
						wpDebug.WriteError(ex);
					}
				}
				stopSearch();
			});
			sendSearch();
			task.Wait();
			string returns = JsonConvert.SerializeObject(foundNewD1Mini);
			return returns;
		}
		private static void sendSearch() {
			var data = Encoding.UTF8.GetBytes("FreakaZone Member?");
			udpClient.Send(data, data.Length, "255.255.255.255", udpPort);
		}
		private static void stopSearch() {
			try {
				wpDebug.Write("Stop FreakaZone search");
				searchActive = false;
				if(udpClient != null) {
					udpClient.Close();
					udpClient.Dispose();
				}
			} catch(Exception ex) {
				wpDebug.WriteError(ex);
			}
		}
		private static void setValue(int idDp, string name, string value) {
			valueChangedEventArgs vcea = new valueChangedEventArgs();
			vcea.idDatapoint = idDp;
			vcea.name = name;
			vcea.value = value;
			if(valueChanged != null)
				valueChanged.Invoke(idDp, vcea);
		}
		public static bool SetBM(string mac, bool state) {
			bool returns = false;
			if(D1MinisMac.ContainsKey(mac) && D1Minis.ContainsKey(D1MinisMac[mac])) {
				D1MiniDevice d1m = D1Minis[D1MinisMac[mac]];
				setValue(d1m.id_onoff, d1m.Name, state ? "True" : "False");
				string DebugNewValue = String.Format("Neuer Wert: D1Mini: {0}, BM: {1}", d1m.Name, state);
				if(wpDebug.debugD1Mini)
					eventLog.Write(DebugNewValue);
				Program.MainProg.lastchange = DebugNewValue;
				returns = true;
			} else {
				eventLog.Write($"D1Mini nicht gefunden: {mac}");
			}
			return returns;
		}
		public static bool SetTemp(string mac, string temp) {
			bool returns = false;
			if(D1MinisMac.ContainsKey(mac) && D1Minis.ContainsKey(D1MinisMac[mac])) {
				D1MiniDevice d1m = D1Minis[D1MinisMac[mac]];
				setValue(d1m.id_temp, d1m.Name, temp.Replace(".", ","));
				string DebugNewValue = String.Format("D1Mini: {0}", d1m.Name);
				DebugNewValue += String.Format("\r\n\tNeuer Wert: Temp: {0}", temp);
				if(wpDebug.debugD1Mini)
					eventLog.Write(DebugNewValue);
				Program.MainProg.lastchange = DebugNewValue;
				returns = true;
			} else {
				eventLog.Write($"D1Mini nicht gefunden: {mac}");
			}
			return returns;
		}
		public static bool SetHum(string mac, string hum) {
			bool returns = false;
			if(D1MinisMac.ContainsKey(mac) && D1Minis.ContainsKey(D1MinisMac[mac])) {
				D1MiniDevice d1m = D1Minis[D1MinisMac[mac]];
				setValue(d1m.id_hum, d1m.Name, hum.Replace(".", ","));
				string DebugNewValue = String.Format("D1Mini: {0}", d1m.Name);
				DebugNewValue += String.Format("\r\n\tNeuer Wert: Hum: {0}, ", hum);
				if(wpDebug.debugD1Mini)
					eventLog.Write(DebugNewValue);
				Program.MainProg.lastchange = DebugNewValue;
				returns = true;
			} else {
				eventLog.Write($"D1Mini nicht gefunden: {mac}");
			}
			return returns;
		}
		public static bool SetLdr(string mac, string ldr) {
			bool returns = false;
			if(D1MinisMac.ContainsKey(mac) && D1Minis.ContainsKey(D1MinisMac[mac])) {
				D1MiniDevice d1m = D1Minis[D1MinisMac[mac]];
				setValue(d1m.id_ldr, d1m.Name, ldr.Replace(".", ","));
				string DebugNewValue = String.Format("D1Mini: {0}", d1m.Name);
				DebugNewValue += String.Format("\r\n\tNeuer Wert: LDR: {0}, ", ldr);
				if(wpDebug.debugD1Mini)
					eventLog.Write(DebugNewValue);
				Program.MainProg.lastchange = DebugNewValue;
				returns = true;
			} else {
				eventLog.Write($"D1Mini nicht gefunden: {mac}");
			}
			return returns;
		}
		public static bool SetLight(string mac, string light) {
			bool returns = false;
			if(D1MinisMac.ContainsKey(mac) && D1Minis.ContainsKey(D1MinisMac[mac])) {
				D1MiniDevice d1m = D1Minis[D1MinisMac[mac]];
				setValue(d1m.id_light, d1m.Name, light.Replace(".", ","));
				string DebugNewValue = String.Format("D1Mini: {0}", d1m.Name);
				DebugNewValue += String.Format("\r\n\tNeuer Wert: Light: {0}, ", light);
				if(wpDebug.debugD1Mini)
					eventLog.Write(DebugNewValue);
				Program.MainProg.lastchange = DebugNewValue;
				returns = true;
			} else {
				eventLog.Write($"D1Mini nicht gefunden: {mac}");
			}
			return returns;
		}
		public static bool SetRelais(string mac, bool state) {
			bool returns = false;
			if(D1MinisMac.ContainsKey(mac) && D1Minis.ContainsKey(D1MinisMac[mac])) {
				D1MiniDevice d1m = D1Minis[D1MinisMac[mac]];
				setValue(d1m.id_relais, d1m.Name, state ? "True" : "False");
				string DebugNewValue = String.Format("Neuer Wert: D1Mini: {0}, Relais: {1}", d1m.Name, state);
				if(wpDebug.debugD1Mini)
					eventLog.Write(DebugNewValue);
				Program.MainProg.lastchange = DebugNewValue;
				returns = true;
			} else {
				eventLog.Write($"D1Mini nicht gefunden: {mac}");
			}
			return returns;
		}
		public static bool SetRain(string mac, string rain) {
			bool returns = false;
			if(D1MinisMac.ContainsKey(mac) && D1Minis.ContainsKey(D1MinisMac[mac])) {
				D1MiniDevice d1m = D1Minis[D1MinisMac[mac]];
				setValue(d1m.id_rain, d1m.Name, rain.Replace(".", ","));
				string DebugNewValue = String.Format("D1Mini: {0}", d1m.Name);
				DebugNewValue += String.Format("\r\n\tNeuer Wert: Rain: {0}, ", rain);
				if(wpDebug.debugD1Mini)
					eventLog.Write(DebugNewValue);
				Program.MainProg.lastchange = DebugNewValue;
				returns = true;
			} else {
				eventLog.Write($"D1Mini nicht gefunden: {mac}");
			}
			return returns;
		}
		public static bool SetMoisture(string mac, string moisture) {
			bool returns = false;
			if(D1MinisMac.ContainsKey(mac) && D1Minis.ContainsKey(D1MinisMac[mac])) {
				D1MiniDevice d1m = D1Minis[D1MinisMac[mac]];
				setValue(d1m.id_moisture, d1m.Name, moisture.Replace(".", ","));
				string DebugNewValue = String.Format("D1Mini: {0}", d1m.Name);
				DebugNewValue += String.Format("\r\n\tNeuer Wert: Moisture: {0}, ", moisture);
				if(wpDebug.debugD1Mini)
					eventLog.Write(DebugNewValue);
				Program.MainProg.lastchange = DebugNewValue;
				returns = true;
			} else {
				eventLog.Write($"D1Mini nicht gefunden: {mac}");
			}
			return returns;
		}
		public static bool SetVolume(string mac, string volume) {
			bool returns = false;
			if(D1MinisMac.ContainsKey(mac) && D1Minis.ContainsKey(D1MinisMac[mac])) {
				D1MiniDevice d1m = D1Minis[D1MinisMac[mac]];
				setValue(d1m.id_vol, d1m.Name, volume.Replace(".", ","));
				string DebugNewValue = String.Format("D1Mini: {0}", d1m.Name);
				DebugNewValue += String.Format("\r\n\tNeuer Wert: Volume: {0}, ", volume);
				if(wpDebug.debugD1Mini)
					eventLog.Write(DebugNewValue);
				Program.MainProg.lastchange = DebugNewValue;
				returns = true;
			} else {
				eventLog.Write($"D1Mini nicht gefunden: {mac}");
			}
			return returns;
		}
		public static bool SetWindow(string mac, bool state) {
			bool returns = false;
			if(D1MinisMac.ContainsKey(mac) && D1Minis.ContainsKey(D1MinisMac[mac])) {
				D1MiniDevice d1m = D1Minis[D1MinisMac[mac]];
				setValue(d1m.id_window, d1m.Name, state ? "True" : "False");
				string DebugNewValue = String.Format("Neuer Wert: D1Mini: {0}, Window: {1}", d1m.Name, state);
				if(wpDebug.debugD1Mini)
					eventLog.Write(DebugNewValue);
				Program.MainProg.lastchange = DebugNewValue;
				returns = true;
			} else {
				eventLog.Write($"D1Mini nicht gefunden: {mac}");
			}
			return returns;
		}
	}
	public class D1MiniDevice {
		private string _name;
		public string Name {
			get { return _name; }
		}
		private IPAddress _ipAddress;
		private string _mac;
		private string _description;

		public string readDeviceName;
		public string readDeviceDescription;
		public string readVersion;
		public string readIp;
		public string readMac;
		public string readSsid;
		public string readUpdateMode;

		private int _id_onoff;
		public int id_onoff {
			get { return _id_onoff; }
			set { _id_onoff = value; }
		}
		private int _id_temp;
		public int id_temp {
			get { return _id_temp; }
			set { _id_temp = value; }
		}
		private int _id_hum;
		public int id_hum {
			get { return _id_hum; }
			set { _id_hum = value; }
		}
		private int _id_ldr;
		public int id_ldr {
			get { return _id_ldr; }
			set { _id_ldr = value; }
		}
		private int _id_light;
		public int id_light {
			get { return _id_light; }
			set { _id_light = value; }
		}
		private int _id_relais;
		public int id_relais {
			get { return _id_relais; }
			set { _id_relais = value; }
		}
		private int _id_rain;
		public int id_rain {
			get { return _id_rain; }
			set { _id_rain = value; }
		}
		private int _id_moisture;
		public int id_moisture {
			get { return _id_moisture; }
			set { _id_moisture = value; }
		}
		private int _id_vol;
		public int id_vol {
			get { return _id_vol; }
			set { _id_vol = value; }
		}
		private int _id_window;
		public int id_window {
			get { return _id_window; }
			set { _id_window = value; }
		}
		public bool Online {
			set {
				if(value) {
					if(wpDebug.debugD1Mini)
						wpDebug.Write($"D1 Mini `recived Online`: {_name}/info/Online, 1");
					setOnlineError(false);
					toreset.Stop();
				} else {
					if(wpDebug.debugD1Mini)
						wpDebug.Write($"D1 Mini `recived Online`: {_name}/info/Online, 0 - start resetTimer");
					toreset.Start();
				}
			}
		}
		private Timer t;
		private Timer toreset;

		public class cmdList {
			public const string RestartDevice = "RestartDevice";
			public const string ForceMqttUpdate = "ForceMqttUpdate";
			public const string ForceRenewValue = "ForceRenewValue";
			public const string UpdateFW = "UpdateFW";
			public const string UnknownCmd = "UnknownCmd";
			private string _choosenCmd;
			public cmdList(string cmd) {
				switch(cmd) {
					case RestartDevice:
						_choosenCmd = RestartDevice;
						break;
					case ForceMqttUpdate:
						_choosenCmd = ForceMqttUpdate;
						break;
					case ForceRenewValue:
						_choosenCmd = ForceRenewValue;
						break;
					case UpdateFW:
						_choosenCmd = UpdateFW;
						break;
					default:
						_choosenCmd = UnknownCmd;
						break;
				}
			}
			public string cmd {
				get { return _choosenCmd; }
			}
			public bool isValid {
				get { return !(_choosenCmd  == null || _choosenCmd == UnknownCmd); }
			}
		}
		private readonly List<string> subscribeList = new List<string>() {
			"info/DeviceName", "info/DeviceDescription",
			"info/Version", "info/wpFreakaZone",
			"info/WiFi/Ip", "info/WiFi/Mac", "info/WiFi/SSID",
			"UpdateMode", "info/Online" };
		public D1MiniDevice(String name, IPAddress ip, String mac, String description) {
			_name = name;
			_ipAddress = ip;
			_mac = mac;
			_description = description;
			t = new Timer(D1MiniServer.OnlineTogglerSendIntervall * 1000);
			t.Elapsed += onlineCheck_Elapsed;
			if(D1MiniServer.OnlineTogglerSendIntervall > 0) t.Start();
			toreset = new Timer(D1MiniServer.OnlineTogglerWait * 1000);
			toreset.AutoReset = false;
			toreset.Elapsed += toreset_Elapsed;
		}

		public void Stop() {
			t.Stop();
			toreset.Stop();
			if(wpDebug.debugD1Mini)
				wpDebug.Write($"D1 Mini stopped `{_name} sendOnlineQuestion`");
		}
		public void SetOnlineTogglerSendIntervall() {
			t.Interval = D1MiniServer.OnlineTogglerSendIntervall * 1000;
			t.Stop();
			if(D1MiniServer.OnlineTogglerSendIntervall > 0) {
				t.Start();
			}
		}
		public void SetOnlineTogglerWait() {
			toreset.Interval = D1MiniServer.OnlineTogglerWait * 1000;
			toreset.Stop();
		}
		private void onlineCheck_Elapsed(object sender, ElapsedEventArgs e) {
			sendOnlineQuestion();
		}
		private void toreset_Elapsed(object sender, ElapsedEventArgs e) {
			if(wpDebug.debugD1Mini)
				wpDebug.Write($"D1 Mini `lastChancePing`: {_name} no response, send 'lastChance Ping'");
			//last chance
			Ping _ping = new Ping();
			if(_ping.Send(_ipAddress, 750).Status != IPStatus.Success) {
				setOnlineError();
			}
		}

		public List<string> getSubscribtions() {
			List<string> returns = new List<string>();
			foreach(string topic in subscribeList) {
				returns.Add(_name + "/" + topic);
			}
			return returns;
		}
		public bool sendCmd(cmdList cmd) {
			bool returns = false;
			if(cmd.isValid) {
				_ = Program.MainProg.wpMQTTClient.setValue(_name + "/" + cmd.cmd, "1");
				if(wpDebug.debugD1Mini)
					wpDebug.Write($"D1 Mini `sendCmd` success: {_name}, {cmd.cmd}");
				returns = true;
			} else {
				wpDebug.Write($"D1 Mini `sendCmd` ERROR: {_name}, {cmd.cmd}");
			}
			return returns;
		}
		private void sendOnlineQuestion() {
			if(wpDebug.debugD1Mini)
				wpDebug.Write($"D1 Mini `sendOnlineQuestion`: {_name}/info/Online, 0");
			_ = Program.MainProg.wpMQTTClient.setValue(_name + "/info/Online", "0", MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);
		}
		private void setOnlineError(bool e) {
			if(wpDebug.debugD1Mini)
				wpDebug.Write($"D1 Mini `setOnlineError`: {_name}/ERROR/Online, {(e ? "1" : "0")}");
			_ = Program.MainProg.wpMQTTClient.setValue(_name + "/ERROR/Online", e ? "1" : "0");
		}
		private void setOnlineError() {
			setOnlineError(true);
		}
	}
	class D1MiniBroadcast {
		public cIam Iam { get; set; }
		public class cIam {
			public string FreakaZoneClient { get; set; }
			public string IP { get; set; }
			public string MAC { get; set; }
			public string wpFreakaZoneVersion { get; set; }
			public string Version { get; set; }
		}
	}
}
