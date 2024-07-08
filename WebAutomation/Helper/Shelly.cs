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
//# Revision     : $Rev:: 122                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: Shelly.cs 122 2024-07-07 20:05:28Z                       $ #
//#                                                                                 #
//###################################################################################
using Newtonsoft.Json;
using ShellyDevice;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace WebAutomation.Helper {
	public static class ShellyServer {
		/// <summary></summary>
		private static Logger eventLog;

		private static Dictionary<string, ShellyDeviceHelper> Shellys;
		private static List<string> _ForceMqttUpdateAvailable;
		public static List<string> ForceMqttUpdateAvailable { get { return _ForceMqttUpdateAvailable; } }

		public static event EventHandler<valueChangedEventArgs> valueChanged;
		public class valueChangedEventArgs: EventArgs {
			public int idDatapoint { get; set; }
			public string name { get; set; }
			public string value { get; set; }
		}
		public static void Start() {
			wpDebug.Write("Shelly Server Init");
			eventLog = new Logger(wpEventLog.PlugInShelly);
			Shellys = new Dictionary<string, ShellyDeviceHelper>();
			_ForceMqttUpdateAvailable = new List<string>();
			using(SQL SQL = new SQL("Select Shellys")) {
				string[][] Query1 = SQL.wpQuery(@"SELECT
					[s].[id_shelly], [s].[ip], [s].[mac], [s].[id_shellyroom], [s].[name], [s].[type],
					[s].[mqtt_active], [s].[mqtt_server], [s].[mqtt_id], [s].[mqtt_prefix], [s].[mqtt_writeable],
					[r].[id_onoff] ,[r].[id_temp] ,[r].[id_hum] ,[r].[id_ldr], [r].[id_window]
					FROM [shelly] [s]
					LEFT JOIN [rest] [r] ON [s].[id_shelly] = [r].[id_shelly]
					WHERE [s].[active] = 1");
				int id_shelly, idroom;
				string ip, shmac, name, type, mqttserver, idmqtt, mqttprefix;
				bool mqttactive, mqttwriteable;
				for(int ishelly = 0; ishelly < Query1.Length; ishelly++) {
					try {
						Int32.TryParse(Query1[ishelly][0], out id_shelly);
						ip = Query1[ishelly][1];
						shmac = Query1[ishelly][2].ToLower();
						Int32.TryParse(Query1[ishelly][3], out idroom);
						name = Query1[ishelly][4];
						type = Query1[ishelly][5];

						mqttactive = Query1[ishelly][6] == "True";
						mqttserver = Query1[ishelly][7];
						idmqtt = Query1[ishelly][8];
						mqttprefix = Query1[ishelly][9];
						mqttwriteable = Query1[ishelly][10] == "True";
						Shellys.Add(shmac,
							new ShellyDeviceHelper(id_shelly, ip, shmac, idroom, name, type,
								mqttactive, mqttserver, idmqtt, mqttprefix, mqttwriteable));

						if(!String.IsNullOrEmpty(Query1[ishelly][11])) Shellys[shmac].id_onoff = Int32.Parse(Query1[ishelly][11]);
						if(!String.IsNullOrEmpty(Query1[ishelly][12])) Shellys[shmac].id_temp = Int32.Parse(Query1[ishelly][12]);
						if(!String.IsNullOrEmpty(Query1[ishelly][13])) Shellys[shmac].id_hum = Int32.Parse(Query1[ishelly][13]);
						if(!String.IsNullOrEmpty(Query1[ishelly][14])) Shellys[shmac].id_ldr = Int32.Parse(Query1[ishelly][14]);
						if(!String.IsNullOrEmpty(Query1[ishelly][15])) Shellys[shmac].id_window = Int32.Parse(Query1[ishelly][15]);

						if(ShellyType.isGen2(type))
							_ForceMqttUpdateAvailable.Add(idmqtt);
						Shellys[shmac].getStatus();
					} catch(Exception ex) {
						eventLog.WriteError(ex);
					}
				}
			}
			wpDebug.Write("Shelly Server gestartet");
		}
		public static string getAllStatus() {
			foreach(KeyValuePair<string, ShellyDeviceHelper> kvp in Shellys) {
				kvp.Value.getStatus();
			}
			return "S_OK";
		}
		private static void setValue(int idDp, string name, string value) {
			valueChangedEventArgs vcea = new valueChangedEventArgs();
			vcea.idDatapoint = idDp;
			vcea.name = name;
			vcea.value = value;
			if(valueChanged != null)
				valueChanged.Invoke(idDp, vcea);
		}
		public static bool SetState(string mac, bool state) {
			bool returns = false;
			if(Shellys.ContainsKey(mac)) {
				setValue(Shellys[mac].id_onoff, Shellys[mac].name, state ? "True" : "False");
				string DebugNewValue = String.Format("Neuer Wert: Raum: {0}, Status: {1}", Shellys[mac].name, state);
				if(wpDebug.debugShelly)
					eventLog.Write(DebugNewValue);
				Program.MainProg.lastchange = DebugNewValue;
				returns = true;
			} else {
				eventLog.Write($"Shelly nicht gefunden: {mac}\r\n{Shellys}");
			}
			return returns;
		}
		public static bool SetWindow(string mac, bool window, string temp, string ldr) {
			bool returns = false;
			if(Shellys.ContainsKey(mac)) {
				setValue(Shellys[mac].id_window, Shellys[mac].name, window ? "True" : "False");
				setValue(Shellys[mac].id_temp, Shellys[mac].name, temp.Replace(".", ","));
				setValue(Shellys[mac].id_ldr, Shellys[mac].name, ldr.Replace(".", ","));
				string DebugNewValue = String.Format("Raum: {0}", Shellys[mac].name);
				DebugNewValue += String.Format("\r\n\tNeuer Wert: Status: {0}, ", window ? "True" : "False");
				DebugNewValue += String.Format("\r\n\tNeuer Wert: Temp: {0}", temp);
				DebugNewValue += String.Format("\r\n\tNeuer Wert: LDR: {0}, ", ldr);
				if(wpDebug.debugShelly)
					eventLog.Write(DebugNewValue);
				Program.MainProg.lastchange = DebugNewValue;
				returns = true;
			} else {
				eventLog.Write($"Shelly nicht gefunden: {mac}");
			}
			return returns;
		}
		public static bool SetWindow(string mac, bool window) {
			bool returns = false;
			if(Shellys.ContainsKey(mac)) {
				setValue(Shellys[mac].id_window, Shellys[mac].name, window ? "True" : "False");
				string DebugNewValue = String.Format("Raum: {0}", Shellys[mac].name);
				DebugNewValue += String.Format("\r\n\tNeuer Wert: Window: {0}, ", window ? "True" : "False");
				if(wpDebug.debugShelly)
					eventLog.Write(DebugNewValue);
				Program.MainProg.lastchange = DebugNewValue;
				returns = true;
			} else {
				eventLog.Write($"Shelly nicht gefunden: {mac}");
			}
			return returns;
		}
		public static bool SetHumTemp(string mac, string hum, string temp) {
			bool returns = false;
			if(Shellys.ContainsKey(mac)) {
				setValue(Shellys[mac].id_hum, Shellys[mac].name, hum.Replace(".", ","));
				setValue(Shellys[mac].id_temp, Shellys[mac].name, temp.Replace(".", ","));
				string DebugNewValue = String.Format("Raum: {0}", Shellys[mac].name);
				DebugNewValue += String.Format("\r\n\tNeuer Wert: Hum: {0}, ", hum);
				DebugNewValue += String.Format("\r\n\tNeuer Wert: Temp: {0}", temp);
				if(wpDebug.debugShelly)
					eventLog.Write(DebugNewValue);
				Program.MainProg.lastchange = DebugNewValue;
				returns = true;
			} else {
				eventLog.Write($"Shelly nicht gefunden: {mac}");
			}
			return returns;
		}
		public static bool SetTemp(string mac, string temp) {
			bool returns = false;
			if(Shellys.ContainsKey(mac)) {
				setValue(Shellys[mac].id_temp, Shellys[mac].name, temp.Replace(".", ","));
				string DebugNewValue = String.Format("Raum: {0}", Shellys[mac].name);
				DebugNewValue += String.Format("\r\n\tNeuer Wert: Temp: {0}", temp);
				if(wpDebug.debugShelly)
					eventLog.Write(DebugNewValue);
				Program.MainProg.lastchange = DebugNewValue;
				returns = true;
			} else {
				eventLog.Write($"Shelly nicht gefunden: {mac}");
			}
			return returns;
		}
		public static bool SetHum(string mac, string hum) {
			bool returns = false;
			if(Shellys.ContainsKey(mac)) {
				setValue(Shellys[mac].id_hum, Shellys[mac].name, hum.Replace(".", ","));
				string DebugNewValue = String.Format("Raum: {0}", Shellys[mac].name);
				DebugNewValue += String.Format("\r\n\tNeuer Wert: Hum: {0}, ", hum);
				if(wpDebug.debugShelly)
					eventLog.Write(DebugNewValue);
				Program.MainProg.lastchange = DebugNewValue;
				returns = true;
			} else {
				eventLog.Write($"Shelly nicht gefunden: {mac}");
			}
			return returns;
		}
		public static bool SetLongPress(string mac) {
			bool returns = false;
			if(Shellys.ContainsKey(mac)) {
				Shellys[mac].setLongPress();
				string DebugNewValue = String.Format("Raum: {0}", Shellys[mac].name);
				if(wpDebug.debugShelly)
					eventLog.Write(DebugNewValue);
				Program.MainProg.lastchange = DebugNewValue;
				returns = true;
			} else {
				eventLog.Write($"Shelly nicht gefunden: {mac}");
			}
			return returns;
		}
		private class ShellyDeviceHelper {
			private int _id;
			public int id {
				get { return _id; }
			}
			private IPAddress _ip;
			public string ip {
				get { return _ip.ToString(); }
			}
			private string _mac;
			private int _room;
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
			private int _id_window;
			public int id_window {
				get { return _id_window; }
				set { _id_window = value; }
			}
			private string _name;
			public string name {
				get { return _name; }
			}

			private bool _mqtt_enable;
			public bool mqtt_enable { get => _mqtt_enable; }
			private string _mqtt_server;
			public string mqtt_server { get => _mqtt_server; }
			private string _mqtt_id;
			public string mqtt_id { get => _mqtt_id; }
			private string _mqtt_prefix;
			public string mqtt_prefix { get => _mqtt_prefix; }
			private bool _mqtt_writeable;
			public bool mqtt_writeable { get => _mqtt_writeable; }
			private string _type;
			public string type {
				get { return _type; }
			}


			public ShellyDeviceHelper(int id, string ip, string mac, int id_room, string name, string type,
				bool mqtt_enable, string mqtt_server, string mqtt_id, string mqtt_prefix, bool mqtt_writeable) {
				_id = id;
				IPAddress ipaddress;
				if(IPAddress.TryParse(ip, out ipaddress)) {
					_ip = ipaddress;
				}
				_mac = mac;
				_room = id_room;
				_name = name;
				_type = type;
				_mqtt_enable = mqtt_enable;
				_mqtt_server = mqtt_server;
				_mqtt_id = mqtt_id;
				_mqtt_prefix = mqtt_prefix;
				_mqtt_writeable = mqtt_writeable;
			}
			public void getStatus() {
				Thread getStatus = new Thread(new ThreadStart(handleGetStatus));
				getStatus.Name = "ShellyServerGetStatus";
				getStatus.Start();
			}
			public void setLongPress() {
				wpDebug.Write($"register LongPress on {this._name}");
				using(SQL SQL = new SQL("Shellys Long Press")) {
					string[][] Query = SQL.wpQuery(@$"SELECT
						[ip], [type], [un], [pw]
						FROM [shelly]
						WHERE [id_shellyroom] = {this._room}");
					WebClient webClient = new WebClient();
					for(int ishelly = 0; ishelly < Query.Length; ishelly++) {
						try {
							webClient.Credentials = new NetworkCredential(Query[ishelly][2], Query[ishelly][3]);
							if(ShellyType.isRelay(Query[ishelly][1]))
								webClient.DownloadString(new Uri($"http://{Query[ishelly][0]}/relay/0?turn=off"));
							if(ShellyType.isLight(Query[ishelly][1]))
								webClient.DownloadString(new Uri($"http://{Query[ishelly][0]}/light/0?turn=off"));
						} catch(Exception ex) {
							eventLog.WriteError(ex);
						}
					}
				}
			}
			private void handleGetStatus() {
				string url = "http://" + this._ip.ToString();
				string target = "", result = "";
				try {
					if(!ShellyType.isBat(this.type)) {
						WebClient webClient = new WebClient();
						webClient.Credentials = new NetworkCredential("wpLicht", "turner");
						if(ShellyType.isGen2(this.type)) {
							target = $"{url}/rpc/Switch.GetStatus?id=0";
						} else {
							target = $"{url}/status";
						}
						webClient.DownloadStringCompleted += (e, args) => {
							if(args.Error == null) {
								status sds = JsonConvert.DeserializeObject<status>(args.Result);
								//if(sds.update.has_update) eventLog.Write("Hat Update: {0}", sds.update.new_version);
								if(this.id_onoff > 0) {
									if(ShellyType.isLight(this.type) && !ShellyType.isGen2(this.type))
										Datapoints.Get(this.id_onoff).setValue(sds.lights[0].ison ? "True" : "False");
									if(ShellyType.isRelay(this.type) && !ShellyType.isGen2(this.type))
										Datapoints.Get(this.id_onoff).setValue(sds.relays[0].ison ? "True" : "False");
									if(ShellyType.isGen2(this.type))
										Datapoints.Get(this.id_onoff).setValue(sds.output ? "True" : "False");
								}
								if(wpDebug.debugShelly)
									eventLog.Write(result);

								if(ShellyType.isGen2(this.type)) {
									target = $"{url}/rpc/Mqtt.GetConfig";
								} else {
									target = $"{url}/settings";
								}
								result = webClient.DownloadString(new Uri(target));
								mqttstatus sdms = JsonConvert.DeserializeObject<mqttstatus>(result);
								bool res_mqtt_enable, res_mqtt_writeable;
								string res_mqtt_server, res_mqtt_id, res_mqtt_prefix;
								if(ShellyType.isGen2(this.type)) {
									res_mqtt_enable = sdms.enable;
									res_mqtt_server = sdms.server;
									res_mqtt_id = sdms.client_id;
									res_mqtt_prefix = sdms.topic_prefix;
									res_mqtt_writeable = sdms.enable_control;
								} else {
									res_mqtt_enable = sdms.mqtt.enable;
									res_mqtt_server = sdms.mqtt.server;
									res_mqtt_id = sdms.mqtt.id;
									res_mqtt_prefix = "shellies/" + sdms.mqtt.id;
									res_mqtt_writeable = true;
								}
								string updatesql = "";
								if(_mqtt_enable != res_mqtt_enable) {
									_mqtt_enable = res_mqtt_enable;
									updatesql += (updatesql == "" ? "" : ", ") + $"[mqtt_active] = {(_mqtt_enable ? "1" : "0")}";
								}
								if(_mqtt_server != res_mqtt_server) {
									_mqtt_server = res_mqtt_server;
									updatesql += (updatesql == "" ? "" : ", ") + $"[mqtt_server] = '{_mqtt_server}'";
								}
								if(_mqtt_id != res_mqtt_id) {
									_mqtt_id = res_mqtt_id;
									updatesql += (updatesql == "" ? "" : ", ") + $"[mqtt_id] = '{_mqtt_id}'";
								}
								if(_mqtt_prefix != res_mqtt_prefix) {
									_mqtt_prefix = res_mqtt_prefix;
									updatesql += (updatesql == "" ? "" : ", ") + $"[mqtt_prefix] = '{_mqtt_prefix}'";
								}
								if(_mqtt_writeable != res_mqtt_writeable) {
									_mqtt_writeable = res_mqtt_writeable;
									updatesql += (updatesql == "" ? "" : ", ") + $"[mqtt_writeable] = {(_mqtt_writeable ? "1" : "0")}";
								}
								if(updatesql != "") {
									using(SQL SQL = new SQL("Update Shelly MQTT ID")) {
										string sql = $"UPDATE [shelly] SET {updatesql} WHERE [id_shelly] = {_id}";
										wpDebug.Write(sql);
										SQL.wpNonResponse(sql);
									}
								}
							} else {
								wpDebug.WriteError(args.Error, $"{this.name} ({this.ip}), '{target}'");
							}
						};
						webClient.DownloadStringAsync(new Uri(target));
					}
				} catch(Exception ex) {
					eventLog.WriteError(ex, $"{this.name} ({this.ip}), '{target}'");
				}
			}
		}
	}
}
namespace ShellyDevice {
	public class relay {
		public string ison { get; set; }
		public string has_timer { get; set; }
	}
	public class status {
		public wifi_sta wifi_sta { get; set; }
		public cloud cloud { get; set; }
		public mqtt mqtt { get; set; }
		public string time { get; set; }
		public int serial { get; set; }
		public bool has_update { get; set; }
		public string mac { get; set; }
		public List<relays> relays { get; set; }
		public List<lights> lights { get; set; }
		public List<meters> meters { get; set; }
		public List<inputs> inputs { get; set; }
		// GEN2
		public bool output { get; set; }
		public ext_temperature ext_temperature { get; set; }
		public update update { get; set; }
		public int ram_total { get; set; }
		public int ram_free { get; set; }
		public int fs_size { get; set; }
		public int fs_free { get; set; }
		public int uptime { get; set; }
	}
	public class mqttstatus {
		public bool enable { get; set; }
		public string server { get; set; }
		public string client_id { get; set; }
		public string topic_prefix { get; set; }
		public bool enable_control { get; set; }
		public Mqtt mqtt { get; set; }
		public class Mqtt {
			public bool enable { get; set; }
			public string server { get; set; }
			public string id { get; set; }
		}
	}

	public class wifi_sta {
		public bool connected { get; set; }
		public string ssid { get; set; }
		public string ip { get; set; }
		public int rssi { get; set; }
	}
	public class cloud {
		public bool enabled { get; set; }
		public bool connected { get; set; }
	}
	public class mqtt {
		public bool connected { get; set; }
	}
	public class relays {
		public bool ison { get; set; }
		public bool has_timer { get; set; }
	}
	public class lights {
		public bool ison { get; set; }
		public bool has_timer { get; set; }
	}
	public class meters {
		public double power { get; set; }
		public bool is_valid { get; set; }
	}
	public class inputs {
		public int input { get; set; }
	}
	public class ext_temperature {
	}
	public class update {
		public string status { get; set; }
		public bool has_update { get; set; }
		public string new_version { get; set; }
		public string old_version { get; set; }
	}
	public class ShellyJson {
		public class status {
			public class switch0 {
				public bool output { get; set; }
				public double apower { get; set; }
				public double voltage { get; set; }
				public double current { get; set; }
			}
			public class wifi {
				public string sta_ip { get; set; }
				public string status { get; set; }
				public string ssid { get; set; }
				public int rssi { get; set; }
			}
			public class devicepower0 {
				public int id { get; set; }
				public battery Battery { get; set; }
				public external External { get; set; }
				public class battery {
					public float V { get; set; }
					public int percent { get; set; }
				}
				public class external {
					public bool present { get; set; }
				}
			}
			public class temperature0 {
				public double tC { get; set; }
			}
			public class humidity0 {
				public double rh { get; set; }
			}
		}
		public class info {
			public wifi_sta Wifi_sta { get; set; }
			public class wifi_sta {
				public bool connected { get; set; }
				public string ssid { get; set; }
				public string ip { get; set; }
				public int rssi { get; set; }
			}
		}
	}

	public class ShellyType {
		public const string DOOR = "SHDW";
		public const string HT = "SHHT-1";
		public const string HT_PLUS = "PlusHT";
		public const string HT3 = "HTG3";

		public const string SW = "SHSW";
		public const string PM = "SHSW-PM";
		public const string PM_PLUS = "Plus1PM";
		public const string PM_MINI = "Mini1PM";
		public const string PM_MINI_G3 = "Mini1PMG3";
		public const string PLG = "SHPLG-S";
		public const string EM = "SHEM";
		public const string DIMMER = "SHDM-1";
		public const string DIMMER_2 = "SHDM-2";
		public const string RGBW = "SHRGBW";
		public const string RGBW2 = "SHRGBW2";

		public const string D1Mini = "D1Mini";

		private static List<string> bat = [DOOR, HT, HT_PLUS, HT3, D1Mini];
		private static List<string> relay = [SW, PM, PM_PLUS, PM_MINI, PM_MINI_G3, PLG, EM];
		private static List<string> light = [DIMMER, DIMMER_2, RGBW, RGBW2];
		private static List<string> gen1 = [SW, PM, PLG, EM, DIMMER, DIMMER_2, RGBW, RGBW2];
		private static List<string> gen2 = [PM_PLUS, PM_MINI, PM_MINI_G3];

		public static bool isBat(string st) {
			if(ShellyType.bat.Contains(st))
				return true;
			return false;
		}
		public static bool isRelay(string st) {
			if(ShellyType.relay.Contains(st))
				return true;
			return false;
		}
		public static bool isLight(string st) {
			if(ShellyType.light.Contains(st))
				return true;
			return false;
		}
		public static bool isGen1(string st) {
			if(ShellyType.gen1.Contains(st))
				return true;
			return false;
		}
		public static bool isGen2(string st) {
			if(ShellyType.gen2.Contains(st))
				return true;
			return false;
		}
	}
}
