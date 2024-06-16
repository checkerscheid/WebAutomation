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
//# File-ID      : $Id:: Shelly.cs 107 2024-06-13 09:50:13Z                       $ #
//#                                                                                 #
//###################################################################################
using Newtonsoft.Json;
using ShellyDevice;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace WebAutomation.Helper {
	public static class ShellyServer {
		/// <summary></summary>
		private static Logger eventLog;

		private static Dictionary<string, ShellyDeviceHelper> Shellys;
		private static List<string> _ForceMqttUpdateAvailable;
		public static List<string> ForceMqttUpdateAvailable { get { return _ForceMqttUpdateAvailable; } }
		private static TcpListener WebComListener;
		private static Thread WebComServer;
		private static UTF8Encoding encoder = new UTF8Encoding();
		private static bool isFinished;

		public static event EventHandler<valueChangedEventArgs> valueChanged;
		public class valueChangedEventArgs: EventArgs {
			public int idDatapoint { get; set; }
			public string name { get; set; }
			public string value { get; set; }
		}
		public static void Start() {
			eventLog = new Logger(wpEventLog.PlugInShelly);
			isFinished = false;
			Shellys = new Dictionary<string, ShellyDeviceHelper>();
			_ForceMqttUpdateAvailable = new List<string>();
			using(SQL SQL = new SQL("Select Shellys")) {
				string[][] Query1 = SQL.wpQuery(@"SELECT
					[id_shelly], [ip], [mac], [id_shellyroom],
					[id_onoff], [id_temp], [id_feuchte], [id_lux], [id_rain], [id_vol],
					[name], [type],
					[mqtt_active], [mqtt_server], [mqtt_id], [mqtt_prefix], [mqtt_writeable]
					FROM [shelly] WHERE [active] = 1");
				int id_shelly, idroom, idonoff, idtemp, idfeuchte, idlux, idrain, idvol;
				string ip, shmac, name, type, mqttserver, idmqtt, mqttprefix;
				bool mqttactive, mqttwriteable;
				for(int ishelly = 0; ishelly < Query1.Length; ishelly++) {
					try {
						Int32.TryParse(Query1[ishelly][0], out id_shelly);
						ip = Query1[ishelly][1];
						shmac = Query1[ishelly][2].ToLower();
						Int32.TryParse(Query1[ishelly][3], out idroom);
						Int32.TryParse(Query1[ishelly][4], out idonoff);
						Int32.TryParse(Query1[ishelly][5], out idtemp);
						Int32.TryParse(Query1[ishelly][6], out idfeuchte);
						Int32.TryParse(Query1[ishelly][7], out idlux);
						Int32.TryParse(Query1[ishelly][8], out idrain);
						Int32.TryParse(Query1[ishelly][9], out idvol);
						name = Query1[ishelly][10];
						type = Query1[ishelly][11];
						mqttactive = Query1[ishelly][12] == "True";
						mqttserver = Query1[ishelly][13];
						idmqtt = Query1[ishelly][14];
						mqttprefix = Query1[ishelly][15];
						mqttwriteable = Query1[ishelly][16] == "True";
						Shellys.Add(shmac,
							new ShellyDeviceHelper(id_shelly, ip, shmac, idroom,
								idonoff, idtemp, idfeuchte, idlux, idrain, idvol,
								name, type,
								mqttactive, mqttserver, idmqtt, mqttprefix, mqttwriteable));
						if(ShellyType.isGen2(type))
							_ForceMqttUpdateAvailable.Add(idmqtt);
						Shellys[shmac].getStatus();
					} catch(Exception ex) {
						eventLog.WriteError(ex);
					}
				}
			}
			WebComListener = new TcpListener(IPAddress.Any, Ini.getInt("Shelly", "Port"));
			WebComServer = new Thread(new ThreadStart(TCP_Listener));
			WebComServer.Name = "ShellyServer";
			WebComServer.Start();
			wpDebug.Write("Shelly auf Port {0} gemappt", Ini.getInt("Shelly", "Port"));
		}
		public static void Stop() {
			WebComListener.Stop();
			WebComListener = null;
			isFinished = true;
			WebComServer.Join(1500);
			eventLog.Write("Shelly Server gestoppt");
		}
		public static string getAllStatus() {
			foreach(KeyValuePair<string, ShellyDeviceHelper> kvp in Shellys) {
				kvp.Value.getStatus();
			}
			return "S_OK";
		}
		private static void TCP_Listener() {
			try {
				WebComListener.Start();
				eventLog.Write("Shelly Server started");
				do {
					if(!WebComListener.Pending()) {
						Thread.Sleep(250);
						continue;
					}
					TcpClient Pclient = WebComListener.AcceptTcpClient();
					Thread ClientThread = new Thread(new ParameterizedThreadStart(TCP_HandleClient));
					ClientThread.Name = "ShellyServerHandleClient";
					ClientThread.Start(Pclient);
				} while(!isFinished);
			} catch(Exception ex) {
				eventLog.WriteError(ex);
			}
		}
		private static void TCP_HandleClient(object client) {
			TcpClient tcpClient = (TcpClient)client;
			if(wpDebug.debugShelly)
				wpDebug.Write(String.Format("Neue Shelly aktion: {0}", tcpClient.Client.RemoteEndPoint));
			string newvalue = "";
			try {
				string s_message = "";
				NetworkStream clientStream = tcpClient.GetStream();
				byte[] message = new byte[tcpClient.ReceiveBufferSize];
				int bytesRead = 0;
				do {
					bytesRead = clientStream.Read(message, bytesRead, (int)tcpClient.ReceiveBufferSize);
					s_message += encoder.GetString(message, 0, bytesRead);
				} while(clientStream.DataAvailable);
				bool found = false;
				bool macok = false;
				string mac;
				foreach(Match m in Regex.Matches(s_message, @"r\=([0-9ABCDEFabcdef]*)&s\=(true|false)")) {
					if(m.Success) {
						mac = m.Groups[1].Value.ToLower();
						if(Shellys.ContainsKey(mac)) {
							setValue(Shellys[mac].id_onoff, Shellys[mac].name, m.Groups[2].Value == "true" ? "True" : "False");
							newvalue = String.Format("Neuer Wert: Raum: {0}, Status: {1}", Shellys[mac].name, m.Groups[2].Value);
							if(wpDebug.debugShelly)
								eventLog.Write(newvalue);
							Program.MainProg.lastchange = newvalue;
							macok = true;
						} else {
							eventLog.Write($"Shelly nicht gefunden: {mac}");
						}
						found = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"r\=([0-9ABCDEFabcdef]*)&state\=(open|close)&lux\=([0-9.]*)&temp\=([0-9.]*)")) {
					if(m.Success) {
						mac = m.Groups[1].Value.ToLower();
						if(Shellys.ContainsKey(mac)) {
							newvalue = String.Format("Raum: {0}", Shellys[mac].name);
							setValue(Shellys[mac].id_onoff, Shellys[mac].name, m.Groups[2].Value == "open" ? "True" : "False");
							newvalue += String.Format("\r\n\tNeuer Wert: Status: {0}, ", m.Groups[2].Value);
							setValue(Shellys[mac].id_temp, Shellys[mac].name, m.Groups[3].Value.Replace(".", ","));
							newvalue += String.Format("\r\n\tNeuer Wert: Lux: {0}, ", m.Groups[3].Value);
							setValue(Shellys[mac].id_lux, Shellys[mac].name, m.Groups[4].Value.Replace(".", ","));
							newvalue += String.Format("\r\n\tNeuer Wert: Temp: {0}", m.Groups[4].Value);
							if(wpDebug.debugShelly)
								eventLog.Write(newvalue);
							Program.MainProg.lastchange = newvalue;
							macok = true;
						} else {
							eventLog.Write($"Shelly nicht gefunden: {mac}");
						}
						found = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"r\=([0-9ABCDEFabcdef]*)&hum\=([0-9.]*)&temp\=([0-9.]*)")) {
					if(m.Success) {
						mac = m.Groups[1].Value.ToLower();
						if(Shellys.ContainsKey(mac)) {
							newvalue = String.Format("Raum: {0}", Shellys[mac].name);
							setValue(Shellys[mac].id_feuchte, Shellys[mac].name, m.Groups[2].Value.Replace(".", ","));
							newvalue += String.Format("\r\n\tNeuer Wert: Feuchte: {0}, ", m.Groups[2].Value);
							setValue(Shellys[mac].id_temp, Shellys[mac].name, m.Groups[3].Value.Replace(".", ","));
							newvalue += String.Format("\r\n\tNeuer Wert: Temp: {0}", m.Groups[3].Value);
							if(wpDebug.debugShelly)
								eventLog.Write(newvalue);
							Program.MainProg.lastchange = newvalue;
							macok = true;
						} else {
							eventLog.Write($"Shelly nicht gefunden: {mac}");
						}
						found = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"r\=([0-9ABCDEFabcdef]*)&temp\=([0-9.]*)")) {
					if(m.Success) {
						mac = m.Groups[1].Value.ToLower();
						if(Shellys.ContainsKey(mac)) {
							newvalue = String.Format("Raum: {0}", Shellys[mac].name);
							setValue(Shellys[mac].id_temp, Shellys[mac].name, m.Groups[2].Value.Replace(".", ","));
							newvalue += String.Format("\r\n\tNeuer Wert: Temp: {0}", m.Groups[2].Value);
							if(wpDebug.debugShelly)
								eventLog.Write(newvalue);
							Program.MainProg.lastchange = newvalue;
							macok = true;
						} else {
							eventLog.Write($"Shelly nicht gefunden: {mac}");
						}
						found = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"r\=([0-9ABCDEFabcdef]*)&hum\=([0-9.]*)")) {
					if(m.Success) {
						mac = m.Groups[1].Value.ToLower();
						if(Shellys.ContainsKey(mac)) {
							newvalue = String.Format("Raum: {0}", Shellys[mac].name);
							setValue(Shellys[mac].id_feuchte, Shellys[mac].name, m.Groups[2].Value.Replace(".", ","));
							newvalue += String.Format("\r\n\tNeuer Wert: Feuchte: {0}, ", m.Groups[2].Value);
							if(wpDebug.debugShelly)
								eventLog.Write(newvalue);
							Program.MainProg.lastchange = newvalue;
							macok = true;
						} else {
							eventLog.Write($"Shelly nicht gefunden: {mac}");
						}
						found = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"r\=([0-9ABCDEFabcdef]*)&a\=lp")) {
					if(m.Success) {
						mac = m.Groups[1].Value.ToLower();
						if(Shellys.ContainsKey(mac)) {
							Shellys[mac].setLongPress();
							macok = true;
						} else {
							eventLog.Write($"Shelly nicht gefunden: {mac}");
						}
						found = true;
					}
				}
				// D1 Mini
				foreach(Match m in Regex.Matches(s_message, @"m\=([0-9ABCDEFabcdef]*)&temp\=([-0-9.]*)")) {
					if(m.Success) {
						mac = m.Groups[1].Value.ToLower();
						if(Shellys.ContainsKey(mac)) {
							newvalue = String.Format("Raum: {0}", Shellys[mac].name);
							setValue(Shellys[mac].id_temp, Shellys[mac].name, m.Groups[2].Value.Replace(".", ","));
							newvalue += String.Format("\r\n\tNeuer Wert: Temperatur: {0}, ", m.Groups[2].Value);
							if(wpDebug.debugShelly)
								eventLog.Write(newvalue);
							Program.MainProg.lastchange = newvalue;
							macok = true;
						} else {
							eventLog.Write($"Shelly nicht gefunden: {mac}");
						}
						found = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"m\=([0-9ABCDEFabcdef]*)&hum\=([0-9.]*)")) {
					if(m.Success) {
						mac = m.Groups[1].Value.ToLower();
						if(Shellys.ContainsKey(mac)) {
							newvalue = String.Format("Raum: {0}", Shellys[mac].name);
							setValue(Shellys[mac].id_feuchte, Shellys[mac].name, m.Groups[2].Value.Replace(".", ","));
							newvalue += String.Format("\r\n\tNeuer Wert: Feuchte: {0}", m.Groups[2].Value);
							if(wpDebug.debugShelly)
								eventLog.Write(newvalue);
							Program.MainProg.lastchange = newvalue;
							macok = true;
						} else {
							eventLog.Write($"Shelly nicht gefunden: {mac}");
						}
						found = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"m\=([0-9ABCDEFabcdef]*)&ldr\=([0-9]*)")) {
					if(m.Success) {
						mac = m.Groups[1].Value.ToLower();
						if(Shellys.ContainsKey(mac)) {
							newvalue = String.Format("Raum: {0}", Shellys[mac].name);
							setValue(Shellys[mac].id_lux, Shellys[mac].name, m.Groups[2].Value.Replace(".", ","));
							newvalue += String.Format("\r\n\tNeuer Wert: LDR: {0}", m.Groups[2].Value);
							if(wpDebug.debugShelly)
								eventLog.Write(newvalue);
							Program.MainProg.lastchange = newvalue;
							macok = true;
						} else {
							eventLog.Write($"Shelly nicht gefunden: {mac}");
						}
						found = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"m\=([0-9ABCDEFabcdef]*)&light\=([0-9]*)")) {
					if(m.Success) {
						mac = m.Groups[1].Value.ToLower();
						if(Shellys.ContainsKey(mac)) {
							newvalue = String.Format("Raum: {0}", Shellys[mac].name);
							setValue(Shellys[mac].id_lux, Shellys[mac].name, m.Groups[2].Value.Replace(".", ","));
							newvalue += String.Format("\r\n\tNeuer Wert: Licht: {0}", m.Groups[2].Value);
							if(wpDebug.debugShelly)
								eventLog.Write(newvalue);
							Program.MainProg.lastchange = newvalue;
							macok = true;
						} else {
							eventLog.Write($"Shelly nicht gefunden: {mac}");
						}
						found = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"m\=([0-9ABCDEFabcdef]*)&rain\=([0-9]*)")) {
					if(m.Success) {
						mac = m.Groups[1].Value.ToLower();
						if(Shellys.ContainsKey(mac)) {
							newvalue = String.Format("Raum: {0}", Shellys[mac].name);
							setValue(Shellys[mac].id_rain, Shellys[mac].name, m.Groups[2].Value.Replace(".", ","));
							newvalue += String.Format("\r\n\tNeuer Wert: Regen: {0}", m.Groups[2].Value);
							if(wpDebug.debugShelly)
								eventLog.Write(newvalue);
							Program.MainProg.lastchange = newvalue;
							macok = true;
						} else {
							eventLog.Write($"Shelly nicht gefunden: {mac}");
						}
						found = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"m\=([0-9ABCDEFabcdef]*)&vol\=([0-9]*)")) {
					if(m.Success) {
						mac = m.Groups[1].Value.ToLower();
						if(Shellys.ContainsKey(mac)) {
							newvalue = String.Format("Raum: {0}", Shellys[mac].name);
							setValue(Shellys[mac].id_vol, Shellys[mac].name, m.Groups[2].Value.Replace(".", ","));
							newvalue += String.Format("\r\n\tNeuer Wert: Volume: {0}", m.Groups[2].Value);
							if(wpDebug.debugShelly)
								eventLog.Write(newvalue);
							Program.MainProg.lastchange = newvalue;
							macok = true;
						} else {
							eventLog.Write($"Shelly nicht gefunden: {mac}");
						}
						found = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"m\=([0-9ABCDEFabcdef]*)&rssi\=([-0-9]*)")) {
					if(m.Success) {
						mac = m.Groups[1].Value.ToLower();
						wpDebug.Write($"Found D1Mini [{mac}]: RSSI = {m.Groups[2].Value}");
						found = true;
					}
				}
				if(!found) eventLog.Write("Shelly Message not found: {0}", s_message);
				byte[] answer = encoder.GetBytes($"HTTP/1.0 200 OK\r\nContent-Type: application/json\r\n\r\n{{\"Message\":\"{(found ? "S_OK" : "S_ERROR")}\",\"MAC\":\"{(macok ? "S_OK" : "S_ERROR")}\"}}");
				clientStream.Write(answer, 0, answer.Length);
				clientStream.Flush();
				clientStream.Close();
			} catch(Exception ex) {
				eventLog.WriteError(ex);
			} finally {
				tcpClient.Close();
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
			}
			private int _id_temp;
			public int id_temp {
				get { return _id_temp; }
			}
			private int _id_feuchte;
			public int id_feuchte {
				get { return _id_feuchte; }
			}
			private int _id_lux;
			public int id_lux {
				get { return _id_lux; }
			}
			private int _id_rain;
			public int id_rain {
				get { return _id_rain; }
			}
			private int _id_vol;
			public int id_vol {
				get { return _id_vol; }
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


			public ShellyDeviceHelper(int id, string ip, string mac,
				int id_room, int id_onoff, int id_temp, int id_feuchte, int id_lux, int id_rain, int id_vol,
				string name, string type,
				bool mqtt_enable, string mqtt_server, string mqtt_id, string mqtt_prefix, bool mqtt_writeable) {
				_id = id;
				IPAddress ipaddress;
				if(IPAddress.TryParse(ip, out ipaddress)) {
					_ip = ipaddress;
				}
				_mac = mac;
				_room = id_room;
				_id_onoff = id_onoff;
				_id_temp = id_temp;
				_id_feuchte = id_feuchte;
				_id_lux = id_lux;
				_id_rain = id_rain;
				_id_vol = id_vol;
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
								wpDebug.WriteError(args.Error);
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

		private static List<string> bat = [DOOR, HT, HT_PLUS, D1Mini];
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
