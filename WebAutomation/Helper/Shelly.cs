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
//# Revision     : $Rev:: 188                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: Shelly.cs 188 2025-02-17 00:57:33Z                       $ #
//#                                                                                 #
//###################################################################################
using FreakaZone.Libraries.wpEventLog;
using FreakaZone.Libraries.wpIniFile;
using FreakaZone.Libraries.wpSQL;
using FreakaZone.Libraries.wpSQL.Table;
using Newtonsoft.Json;
using ShellyDevice;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;

namespace WebAutomation.Helper {
	public static class ShellyServer {
		/// <summary></summary>
		private static Logger eventLog;

		private static List<Shelly> _shellies;
		public static List<Shelly> ForceMqttUpdateAvailable {
			get {
				return _shellies.FindAll(t => ShellyType.isGen2(t.Type));
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
		public static event EventHandler<valueChangedEventArgs> valueChanged;
		public class valueChangedEventArgs: EventArgs {
			public int idDatapoint { get; set; }
			public string name { get; set; }
			public string value { get; set; }
		}
		public static void Init() {
			Debug.Write(MethodInfo.GetCurrentMethod(), "Shelly Server Init");
			eventLog = new Logger(Logger.ESource.PlugInShelly);
			_shellies = new List<Shelly>();
			using(Database Sql = new Database("Select Shellys")) {
				string[][] Query1 = Sql.wpQuery(@"SELECT
					[s].[id_shelly], [s].[ip], [s].[mac], [s].[id_restroom], [s].[name], [s].[type],
					[s].[mqtt_active], [s].[mqtt_server], [s].[mqtt_id], [s].[mqtt_prefix], [s].[mqtt_writeable],
					[r].[id_onoff] ,[r].[id_temp] ,[r].[id_hum] ,[r].[id_ldr], [r].[id_window], [s].[lastcontact]
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
						Shelly sdh = new Shelly(id_shelly, ip, shmac, idroom, name, type,
								mqttactive, mqttserver, idmqtt, mqttprefix, mqttwriteable);
						if(!String.IsNullOrEmpty(Query1[ishelly][11])) sdh.IdOnOff = Int32.Parse(Query1[ishelly][11]);
						if(!String.IsNullOrEmpty(Query1[ishelly][12])) sdh.IdTemp = Int32.Parse(Query1[ishelly][12]);
						if(!String.IsNullOrEmpty(Query1[ishelly][13])) sdh.IdHum = Int32.Parse(Query1[ishelly][13]);
						if(!String.IsNullOrEmpty(Query1[ishelly][14])) sdh.IdLdr = Int32.Parse(Query1[ishelly][14]);
						if(!String.IsNullOrEmpty(Query1[ishelly][15])) sdh.IdWindow = Int32.Parse(Query1[ishelly][15]);

						if(!String.IsNullOrEmpty(Query1[ishelly][16])) sdh.LastContact = DateTime.Parse(Query1[ishelly][16]);

						sdh.getStatus(true);
						sdh.getMqttStatus();
						_shellies.Add(sdh);
						if(ShellyType.isGen2(type) && sdh.MqttActive) {
							addSubscribtions(sdh.getSubscribtions());
						}
					} catch(Exception ex) {
						eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
					}
				}
			}
			Program.MainProg.wpMQTTClient.shellyChanged += wpMQTTClient_shellyChanged;
			Debug.Write(MethodInfo.GetCurrentMethod(), "Shelly Server gestartet");
		}
		public static void Start() {
			foreach(Shelly s in _shellies) {
				s.Start();
			}
			OnlineTogglerSendIntervall = IniFile.getInt("Shelly", "OnlineTogglerSendIntervall");
			OnlineTogglerWait = IniFile.getInt("Shelly", "OnlineTogglerWait");
		}
		public static void Stop() {
			if(_shellies != null) {
				foreach(Shelly s in _shellies) {
					s.Stop();
				}
			}
		}
		public static void removeShelly(int id) {
			using(Database Sql = new Database("Delete Shelly")) {
				if(_shellies.Exists(t => t.Id == id))
					_shellies.Remove(_shellies.Find(t => t.Id == id));
				Sql.DeleteWithId<TableShelly>(id);
			}
		}
		public static string getAllStatus() {
			foreach(Shelly s in _shellies) {
				s.getStatus(true);
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
			if(_shellies.Exists(t => t.Mac == mac)) {
				Shelly s = _shellies.Find(t => t.Mac == mac);
				setValue(s.IdOnOff, s.Name, state ? "True" : "False");
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
			if(_shellies.Exists(t => t.Mac == mac)) {
				Shelly s = _shellies.Find(t => t.Mac == mac);
				setValue(s.IdWindow, s.Name, window ? "True" : "False");
				setValue(s.IdTemp, s.Name, temp.Replace(".", ","));
				setValue(s.IdLdr, s.Name, ldr.Replace(".", ","));
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
			if(_shellies.Exists(t => t.Mac == mac)) {
				Shelly s = _shellies.Find(t => t.Mac == mac);
				setValue(s.IdWindow, s.Name, window ? "True" : "False");
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
			if(_shellies.Exists(t => t.Mac == mac)) {
				Shelly s = _shellies.Find(t => t.Mac == mac);
				setValue(s.IdHum, s.Name, hum.Replace(".", ","));
				setValue(s.IdTemp, s.Name, temp.Replace(".", ","));
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
			if(_shellies.Exists(t => t.Mac == mac)) {
				Shelly s = _shellies.Find(t => t.Mac == mac);
				setValue(s.IdTemp, s.Name, temp.Replace(".", ","));
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
			if(_shellies.Exists(t => t.Mac == mac)) {
				Shelly s = _shellies.Find(t => t.Mac == mac);
				setValue(s.IdHum, s.Name, hum.Replace(".", ","));
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
			if(_shellies.Exists(t => t.Mac == mac)) {
				Shelly s = _shellies.Find(t => t.Mac == mac);
				s.setLongPress();
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

		private static void wpMQTTClient_shellyChanged(object sender, MQTTClient.valueChangedEventArgs e) {
			int pos = e.topic.IndexOf("/");
			string name = e.topic.Substring(0, pos);
			string setting = e.topic.Substring(pos + 1);
			if(_shellies.Exists(t => t.MqttId == name)) {
				Shelly s = _shellies.Find(t => t.MqttId == name);
				if(setting == "info/Online") s.Online = e.value == "0" ? false : true;
			} else {
				Debug.Write(MethodInfo.GetCurrentMethod(), $"Shelly MQTT not active? '{e.topic}', '{e.value}'");
			}
		}
		public static void addSubscribtions(List<string> topic) {
			Subscribtions.AddRange(topic);
		}
		public static List<string> getSubscribtions() {
			return Subscribtions;
		}
		public class Shelly {
			private int _id;
			public int Id {
				get { return _id; }
			}
			private IPAddress _ip;
			public string Ip {
				get { return _ip.ToString(); }
			}
			private string _mac;
			public string Mac => _mac;
			private int _room;
			private int _idOnOff;
			public int IdOnOff {
				get { return _idOnOff; }
				set { _idOnOff = value; }
			}
			private int _idTemp;
			public int IdTemp {
				get { return _idTemp; }
				set { _idTemp = value; }
			}
			private int _idHum;
			public int IdHum {
				get { return _idHum; }
				set { _idHum = value; }
			}
			private int _idLdr;
			public int IdLdr {
				get { return _idLdr; }
				set { _idLdr = value; }
			}
			private int _idWindow;
			public int IdWindow {
				get { return _idWindow; }
				set { _idWindow = value; }
			}
			private string _name;
			public string Name {
				get { return _name; }
			}
			private DateTime _lastContact;
			public DateTime LastContact {
				get { return _lastContact; }
				set {
					_lastContact = value;
				}
			}

			private bool _mqtt_active;
			public bool MqttActive { get => _mqtt_active; }
			private string _mqttServer;
			public string MqttServer { get => _mqttServer; }
			private string _mqttId;
			public string MqttId { get => _mqttId; }
			private string _mqttPrefix;
			public string MqttPrefix { get => _mqttPrefix; }
			private bool _mqttWriteable;
			public bool MqttWriteable { get => _mqttWriteable; }
			private string _type;
			public string Type {
				get { return _type; }
			}
			private Timer _doCheckStatus;
			private long _doCheckStatusIntervall = 30; // min
			public bool Online {
				set {
					if(value) {
						if(Debug.debugShelly)
							Debug.Write(MethodInfo.GetCurrentMethod(), $"Shelly `recived Online`: {_name}/info/Online, 1");
						setOnlineError(false);
						toreset.Stop();
					} else {
						if(Debug.debugShelly)
							Debug.Write(MethodInfo.GetCurrentMethod(), $"Shelly `recived Online`: {_name}/info/Online, 0 - start resetTimer");
						toreset.Start();
					}
				}
			}
			private Timer t;
			private Timer toreset;
			private readonly List<string> subscribeList = new List<string>() {
				"info/Online" };

			public Shelly(int id, string ip, string mac, int id_room, string name, string type,
				bool mqtt_active, string mqtt_server, string mqtt_id, string mqtt_prefix, bool mqtt_writeable) {
				_id = id;
				IPAddress ipaddress;
				if(IPAddress.TryParse(ip, out ipaddress)) {
					_ip = ipaddress;
				}
				_mac = mac;
				_room = id_room;
				_name = name;
				_type = type;
				_mqtt_active = mqtt_active;
				_mqttServer = mqtt_server;
				_mqttId = mqtt_id;
				_mqttPrefix = mqtt_prefix;
				_mqttWriteable = mqtt_writeable;
				_doCheckStatus = new Timer(_doCheckStatusIntervall * 60 * 1000);
				_doCheckStatus.AutoReset = true;
				_doCheckStatus.Elapsed += _doCheckStatus_Elapsed;
			}

			public Shelly(TableShelly ts) {
				_id = ts.id_shelly;
				IPAddress ipaddress;
				if(IPAddress.TryParse(ts.ip, out ipaddress)) {
					_ip = ipaddress;
				}
				_mac = ts.mac;
				_room = ts.id_restroom;
				_name = ts.name;
				_type = ts.type;
				_mqtt_active = ts.mqtt_active;
				_mqttServer = ts.mqtt_server;
				_mqttId = ts.mqtt_id;
				_mqttPrefix = ts.mqtt_prefix;
				_mqttWriteable = ts.mqtt_writeable;
				_doCheckStatus = new Timer(_doCheckStatusIntervall * 60 * 1000);
				_doCheckStatus.AutoReset = true;
				_doCheckStatus.Elapsed += _doCheckStatus_Elapsed;
			}

			public void Start() {
				if(ShellyType.isGen2(_type) && _mqtt_active) {
					t = new Timer(OnlineTogglerSendIntervall * 1000);
					t.Elapsed += onlineCheck_Elapsed;
					if(OnlineTogglerSendIntervall > 0) {
						t.Start();
						sendOnlineQuestion();
					}
					toreset = new Timer(OnlineTogglerWait * 1000);
					toreset.AutoReset = false;
					toreset.Elapsed += toreset_Elapsed;
				}
			}
			public void Stop() {
				if(ShellyType.isGen2(_type) && _mqtt_active) {
					t.Stop();
					toreset.Stop();
					if(Debug.debugShelly)
						Debug.Write(MethodInfo.GetCurrentMethod(), $"Shelly Device stopped `{_name} sendOnlineQuestion`");
				}
			}

			public void SetOnlineTogglerSendIntervall() {
				t.Interval = OnlineTogglerSendIntervall * 1000;
				t.Stop();
				if(OnlineTogglerSendIntervall > 0) {
					t.Start();
				}
			}
			public void SetOnlineTogglerWait() {
				toreset.Interval = OnlineTogglerWait * 1000;
				toreset.Stop();
			}
			private void onlineCheck_Elapsed(object sender, ElapsedEventArgs e) {
				sendOnlineQuestion();
			}
			private void toreset_Elapsed(object sender, ElapsedEventArgs e) {
				if(Debug.debugShelly)
					Debug.Write(MethodInfo.GetCurrentMethod(), $"Shelly `lastChancePing`: {_name} no response, send 'lastChance Ping'");
				//last chance
				Ping _ping = new Ping();
				if(_ping.Send(_ip.ToString(), 750).Status != IPStatus.Success) {
					setOnlineError();
				} else {
					setOnlineError(false);
					if(Debug.debugShelly)
						Debug.Write(MethodInfo.GetCurrentMethod(), $"Shelly OnlineToggler Script is missing?: {_name} MQTT no response, Ping OK");
				}
			}
			public List<string> getSubscribtions() {
				List<string> returns = new List<string>();
				foreach(string topic in subscribeList) {
					if(_mqtt_active) returns.Add(_mqttId + "/" + topic);
				}
				return returns;
			}
			private void sendOnlineQuestion() {
				if(Debug.debugShelly)
					Debug.Write(MethodInfo.GetCurrentMethod(), $"Shelly `sendOnlineQuestion`: {_mqttId}/info/Online, 0");
				_ = Program.MainProg.wpMQTTClient.setValue(_mqttId + "/info/Online", "0", MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);
			}
			private void setOnlineError(bool e) {
				if(Debug.debugShelly)
					Debug.Write(MethodInfo.GetCurrentMethod(), $"Shelly `setOnlineError`: {_mqttId}/ERROR/Online, {(e ? "1" : "0")}");
				_ = Program.MainProg.wpMQTTClient.setValue(_mqttId + "/ERROR/Online", e ? "1" : "0");
			}
			private void setOnlineError() {
				setOnlineError(true);
			}

			private void _doCheckStatus_Elapsed(object sender, ElapsedEventArgs e) {
				getStatus();
				if(Debug.debugShelly)
					Debug.Write(MethodInfo.GetCurrentMethod(), $"ShellyDevice doCheck gestartet '{this.Name}'");
			}

			public void setLongPress() {
				Debug.Write(MethodInfo.GetCurrentMethod(), $"register LongPress on {this._name}");
				using(Database Sql = new Database("Shellys Long Press")) {
					string[][] Query = Sql.wpQuery(@$"SELECT
						[ip], [type], [un], [pw]
						FROM [shelly]
						WHERE [id_restroom] = {this._room}");
					WebClient webClient = new WebClient();
					for(int ishelly = 0; ishelly < Query.Length; ishelly++) {
						try {
							webClient.Credentials = new NetworkCredential(Query[ishelly][2], Query[ishelly][3]);
							if(ShellyType.isRelay(Query[ishelly][1]))
								webClient.DownloadString(new Uri($"http://{Query[ishelly][0]}/relay/0?turn=off"));
							if(ShellyType.isLight(Query[ishelly][1]))
								webClient.DownloadString(new Uri($"http://{Query[ishelly][0]}/light/0?turn=off"));
						} catch(Exception ex) {
							eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
						}
					}
				}
				using(Database Sql = new Database("Shellys Long Press D1Mini")) {
					string[][] Query = Sql.wpQuery(@$"SELECT
						[ip], [compiledwith]
						FROM [d1mini]
						WHERE [id_restroom] = {this._room}");
					WebClient webClient = new WebClient();
					for(int id1mini = 0; id1mini < Query.Length; id1mini++) {
						try {
							if(Query[id1mini][1].Contains("NeoPixel"))
								webClient.DownloadString(new Uri($"http://{Query[id1mini][0]}/setNeoPixelOff"));
							if(Query[id1mini][1].Contains("CwWw"))
								webClient.DownloadString(new Uri($"http://{Query[id1mini][0]}/setCwWw?turn=0"));
						} catch(Exception ex) {
							eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
						}
					}
				}
			}
			public void getStatus() {
				getStatus(false);
			}
			public void getStatus(bool force) {
				string url = "http://" + this._ip.ToString();
				string target = "";
				if(ShellyType.isGen2(this.Type)) {
					target = $"{url}/rpc/Switch.GetStatus?id=0";
				} else {
					target = $"{url}/status";
				}
				if(!ShellyType.isBat(this.Type)) {
					try {
						using(WebClient webClient = new WebClient()) {
							webClient.Credentials = new NetworkCredential("wpLicht", "turner");
							webClient.DownloadStringCompleted += (e, args) => {
								if(args.Error == null) {
									_lastContact = DateTime.Now;
									status sds = JsonConvert.DeserializeObject<status>(args.Result);
									if(this.IdOnOff > 0) {
										if(ShellyType.isLight(this.Type) && !ShellyType.isGen2(this.Type))
											Datapoints.Get(this.IdOnOff).writeValue(sds.lights[0].ison ? "True" : "False", "Shelly");
										if(ShellyType.isRelay(this.Type) && !ShellyType.isGen2(this.Type))
											Datapoints.Get(this.IdOnOff).writeValue(sds.relays[0].ison ? "True" : "False", "Shelly");
										if(ShellyType.isGen2(this.Type))
											Datapoints.Get(this.IdOnOff).writeValue(sds.output ? "True" : "False", "Shelly");
									}
									using(Database Sql = new Database("Update Shelly MQTT lastContact")) {
										string sql = $"UPDATE [shelly] SET [lastcontact] = '{_lastContact.ToString(Database.DateTimeFormat)}' WHERE [id_shelly] = {_id}";
										Sql.wpNonResponse(sql);
										if(Debug.debugShelly)
											Debug.Write(MethodInfo.GetCurrentMethod(), sql);
									}
								} else {
									Debug.WriteError(MethodInfo.GetCurrentMethod(), args.Error, $"{this.Name} ({this.Ip}), '{target}'");
								}
							};
							if(_lastContact.AddHours(1) < DateTime.Now || force) {
								Task.Run(() => {
									webClient.DownloadStringAsync(new Uri(target));
								});
								_doCheckStatus.Stop();
								_doCheckStatus.Start();
							}
						}
					} catch(Exception ex) {
						eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex, $"{this.Name} ({this.Ip}), '{target}'");
					}
				}
			}
			public void getMqttStatus() {
				string url = "http://" + this._ip.ToString();
				string target = "";
				if(ShellyType.isGen2(this.Type)) {
					target = $"{url}/rpc/Mqtt.GetConfig";
				} else {
					target = $"{url}/settings";
				}
				if(!ShellyType.isBat(this.Type)) {
					try {
						using(WebClient webClient = new WebClient()) {
							webClient.Credentials = new NetworkCredential("wpLicht", "turner");
							webClient.DownloadStringCompleted += (e, args) => {
								if(args.Error == null) {
									mqttstatus sdms = JsonConvert.DeserializeObject<mqttstatus>(args.Result);
									bool res_mqtt_enable, res_mqtt_writeable;
									string res_mqtt_server, res_mqtt_id, res_mqtt_prefix;
									if(ShellyType.isGen2(this.Type)) {
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
									if(_mqtt_active != res_mqtt_enable) {
										_mqtt_active = res_mqtt_enable;
										if(!String.IsNullOrEmpty(updatesql)) updatesql += ", ";
										updatesql += $"[mqtt_active] = {(_mqtt_active ? "1" : "0")}";
									}
									if(_mqttServer != res_mqtt_server) {
										_mqttServer = res_mqtt_server;
										if(!String.IsNullOrEmpty(updatesql)) updatesql += ", ";
										updatesql += $"[mqtt_server] = '{_mqttServer}'";
									}
									if(_mqttId != res_mqtt_id) {
										_mqttId = res_mqtt_id;
										if(!String.IsNullOrEmpty(updatesql)) updatesql += ", ";
										updatesql += $"[mqtt_id] = '{_mqttId}'";
									}
									if(_mqttPrefix != res_mqtt_prefix) {
										_mqttPrefix = res_mqtt_prefix;
										if(!String.IsNullOrEmpty(updatesql)) updatesql += ", ";
										updatesql += $"[mqtt_prefix] = '{_mqttPrefix}'";
									}
									if(_mqttWriteable != res_mqtt_writeable) {
										_mqttWriteable = res_mqtt_writeable;
										if(!String.IsNullOrEmpty(updatesql)) updatesql += ", ";
										updatesql += $"[mqtt_writeable] = {(_mqttWriteable ? "1" : "0")}";
									}
									if(!String.IsNullOrEmpty(updatesql)) {
										using(Database Sql = new Database("Update Shelly MQTT ID")) {
											string sql = $"UPDATE [shelly] SET {updatesql} WHERE [id_shelly] = {_id}";
											Sql.wpNonResponse(sql);
											if(Debug.debugShelly)
												Debug.Write(MethodInfo.GetCurrentMethod(), sql);
										}
									}
								} else {
									Debug.WriteError(MethodInfo.GetCurrentMethod(), args.Error, $"{this.Name} ({this.Ip}), '{target}'");
								}
							};
							webClient.DownloadStringAsync(new Uri(target));
						}
					} catch(Exception ex) {
						eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex, $"{this.Name} ({this.Ip}), '{target}'");
					}
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

		private static List<string> bat = [DOOR, HT, HT_PLUS, HT3];
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
