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
//# Revision     : $Rev:: 107                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: D1Mini.cs 107 2024-06-13 09:50:13Z                       $ #
//#                                                                                 #
//###################################################################################
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace WebAutomation.Helper {
	public static class D1MiniServer {
		private static Dictionary<string, D1MiniDevice> D1Minis;
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
		public static void Start() {
			wpDebug.Write("D1 Mini Server Start");
			D1Minis = new Dictionary<string, D1MiniDevice>();
			using(SQL SQL = new SQL("Select Shellys")) {
				string[][] Query1 = SQL.wpQuery(@"SELECT
						[id_d1mini], [name], [description], [ip], [mac]
					FROM [d1mini] WHERE [active] = 1");
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
				if(setting == "info/wpFreakaZone")
					D1Minis[name].readwpFreakaZoneVersion = e.value;
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
			wpDebug.Write("D1Mini getJson Settings");
			string returns = "{";
			foreach(KeyValuePair<string, D1MiniDevice> kvp in D1Minis) {
				returns += $"\"{kvp.Key}\":{{";
				returns += $"\"DeviceName\":\"{kvp.Value.readDeviceName}\",";
				returns += $"\"DeviceDescription\":\"{kvp.Value.readDeviceDescription}\",";
				returns += $"\"wpFreakaZoneVersion\":\"{kvp.Value.readwpFreakaZoneVersion}\",";
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
	}
	public class D1MiniDevice {
		private string _name;
		private IPAddress _ipAddress;
		private string _mac;
		private string _description;

		public string readDeviceName;
		public string readDeviceDescription;
		public string readwpFreakaZoneVersion;
		public string readVersion;
		public string readIp;
		public string readMac;
		public string readSsid;
		public string readUpdateMode;
		public bool Online {
			set {
				if(value) {
					if(Program.MainProg.wpDebugD1Mini)
						wpDebug.Write($"D1 Mini `recived Online`: {_name}/info/Online, 1");
					setOnlineError(false);
					toreset.Stop();
				} else {
					if(Program.MainProg.wpDebugD1Mini)
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
			if(Program.MainProg.wpDebugD1Mini)
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
			if(Program.MainProg.wpDebugD1Mini)
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
				wpDebug.Write($"D1 Mini `sendCmd` success: {_name}, {cmd.cmd}");
				returns = true;
			} else {
				wpDebug.Write($"D1 Mini `sendCmd` ERROR: {_name}, {cmd.cmd}");
			}
			return returns;
		}
		private void sendOnlineQuestion() {
			if(Program.MainProg.wpDebugD1Mini)
				wpDebug.Write($"D1 Mini `sendOnlineQuestion`: {_name}/info/Online, 0");
			_ = Program.MainProg.wpMQTTClient.setValue(_name + "/info/Online", "0", MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);
		}
		private void setOnlineError(bool e) {
			if(Program.MainProg.wpDebugD1Mini)
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
