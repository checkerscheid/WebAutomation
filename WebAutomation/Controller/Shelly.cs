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
//# Revision     : $Rev:: 245                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: Shelly.cs 245 2025-06-28 15:07:22Z                       $ #
//#                                                                                 #
//###################################################################################
using FreakaZone.Libraries.wpEventLog;
using FreakaZone.Libraries.wpSQL;
using FreakaZone.Libraries.wpSQL.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using WebAutomation.Controller.ShellyDevice;

namespace WebAutomation.Controller {
	public class Shelly {
		private Logger eventLog;
		private readonly int _id;
		public int Id {
			get { return _id; }
		}
		private readonly IPAddress _ip;
		public string Ip {
			get { return _ip.ToString(); }
		}
		private readonly string _mac;
		public string Mac => _mac;
		private readonly int _room;

		private bool _active;
		public bool Active {
			get { return _active; }
			set { _active = value; }
		}

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
		public int _idVoltage;
		public int IdVoltage {
			get { return _idVoltage; }
			set { _idVoltage = value; }
		}
		private int _idCurrent;
		public int IdCurrent {
			get { return _idCurrent; }
			set { _idCurrent = value; }
		}
		private int _idPower;
		public int IdPower {
			get { return _idPower; }
			set { _idPower = value; }
		}
		private string _name;
		public string Name {
			get { return _name; }
		}
		private DateTime _lastContact;
		public DateTime LastContact {
			get { return _lastContact; }
			set { _lastContact = value; }
		}

		private bool _mqttActive;
		public bool MqttActive { get => _mqttActive; }
		private string _mqttServer;
		public string MqttServer { get => _mqttServer; }
		private string _mqttId;
		public string MqttId { get => _mqttId; }
		private string _mqttPrefix;
		public string MqttPrefix { get => _mqttPrefix; }
		private bool _mqttWriteable;
		public bool MqttWriteable { get => _mqttWriteable; }
		private string _wsId;
		public bool CoIotActive { get => _coIotActive; }
		private bool _coIotActive;
		public string WsId { get => _wsId; }
		private string _type;
		public string Type { get => _type; }
		private Timer _doCheckStatus;
		private long _doCheckStatusIntervall = 120; // min

		private Timer _doCheckShelly;
		private long _doCheckShellyIntervall = 120; // min
		public bool Online {
			set {
				if(value) {
					if(Debug.debugShelly)
						Debug.Write(MethodInfo.GetCurrentMethod(), $"Shelly `recived Online`: {_name}/info/Online, 1");
					SetOnlineError(false);
					toreset?.Stop();
				} else {
					if(Debug.debugShelly)
						Debug.Write(MethodInfo.GetCurrentMethod(), $"Shelly `recived Online`: {_name}/info/Online, 0 - start resetTimer");
					toreset?.Start();
				}
			}
		}
		private Timer t;
		private Timer toreset;
		private readonly List<string> subscribeList = [
			"info/Online" ];

		public Shelly(int id, string ip, string mac, int id_room, string name, string type,
			bool mqtt_active, string mqtt_server, string mqtt_id, string mqtt_prefix, bool mqtt_writeable, string ws_id,
			bool coiot_active) {
			eventLog = new Logger(Logger.ESource.PlugInShelly);
			_id = id;
			IPAddress ipaddress;
			if(IPAddress.TryParse(ip, out ipaddress)) {
				_ip = ipaddress;
			}
			_mac = mac;
			_room = id_room;
			_name = name;
			_type = type;
			_mqttActive = mqtt_active;
			_mqttServer = mqtt_server;
			_mqttId = mqtt_id;
			_mqttPrefix = mqtt_prefix;
			_mqttWriteable = mqtt_writeable;
			_coIotActive = coiot_active;
			_wsId = ws_id;
			_doCheckStatus = new Timer(_doCheckStatusIntervall * 60 * 1000);
			_doCheckStatus.AutoReset = true;
			_doCheckStatus.Elapsed += DoCheckStatus_Elapsed;
			if(ShellyType.IsGen2(_type)) {
				_doCheckShelly = new Timer(_doCheckShellyIntervall * 60 * 1000);
				_doCheckShelly.AutoReset = true;
				_doCheckShelly.Elapsed += DoCheckShelly_Elapsed;
			}
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
			_active = ts.active;
			_mqttActive = ts.mqtt_active;
			_mqttServer = ts.mqtt_server;
			_mqttId = ts.mqtt_id;
			_mqttPrefix = ts.mqtt_prefix;
			_mqttWriteable = ts.mqtt_writeable;
			_coIotActive = ts.coiot_active;
			_wsId = ts.ws_id;
			_lastContact = ts.lastcontact;

			TableRest tr = (TableRest)ts.SubValues.First();
			_idOnOff = tr.id_onoff;
			_idTemp = tr.id_temp;
			_idHum = tr.id_hum;
			_idLdr = tr.id_ldr;
			_idWindow = tr.id_window;
			_idVoltage = tr.id_voltage;
			_idCurrent = tr.id_current;
			_idPower = tr.id_power;

			_doCheckStatus = new Timer(_doCheckStatusIntervall * 60 * 1000);
			_doCheckStatus.AutoReset = true;
			_doCheckStatus.Elapsed += DoCheckStatus_Elapsed;
			if(ShellyType.IsGen2(_type)) {
				_doCheckShelly = new Timer(_doCheckShellyIntervall * 60 * 1000);
				_doCheckShelly.AutoReset = true;
				_doCheckShelly.Elapsed += DoCheckShelly_Elapsed;
			}
		}

		public void Start() {
			if(ShellyType.IsGen2(_type) && _mqttActive) {
				if(ShellyServer.OnlineTogglerSendIntervall > 0) {
					t = new Timer(ShellyServer.OnlineTogglerSendIntervall * 1000);
					t.Elapsed += OnlineCheck_Elapsed;
					t.Start();
					SendOnlineQuestion();
					toreset = new Timer(ShellyServer.OnlineTogglerWait * 1000);
					toreset.AutoReset = false;
					toreset.Elapsed += Toreset_Elapsed;
				}
			}
		}
		public void Stop() {
			if(ShellyType.IsGen2(_type) && _mqttActive) {
				if(t != null)
					t.Stop();
				t = null;
				if(toreset != null)
					toreset.Stop();
				toreset = null;
				if(Debug.debugShelly)
					Debug.Write(MethodInfo.GetCurrentMethod(), $"Shelly Device stopped `{_name} sendOnlineQuestion`");
			}
		}

		public void SetOnlineTogglerSendIntervall() {
			if(t != null && ShellyServer.OnlineTogglerSendIntervall > 0) {
				t.Interval = ShellyServer.OnlineTogglerSendIntervall * 1000;
				t.Stop();
				t.Start();
			}
		}
		public void SetOnlineTogglerWait() {
			if(toreset != null && ShellyServer.OnlineTogglerWait > 0) {
				toreset.Interval = ShellyServer.OnlineTogglerWait * 1000;
				toreset.Stop();
			}
		}
		private void OnlineCheck_Elapsed(object sender, ElapsedEventArgs e) {
			SendOnlineQuestion();
		}
		private void Toreset_Elapsed(object sender, ElapsedEventArgs e) {
			if(Debug.debugShelly)
				Debug.Write(MethodInfo.GetCurrentMethod(), $"Shelly `lastChancePing`: {_name} no response, send 'lastChance Ping'");
			//last chance
			Ping _ping = new Ping();
			if(_ping.Send(_ip.ToString(), 750).Status != IPStatus.Success) {
				SetOnlineError();
			} else {
				SetOnlineError(false);
				if(Debug.debugShelly)
					Debug.Write(MethodInfo.GetCurrentMethod(), $"Shelly OnlineToggler Script is missing?: {_name} MQTT no response, Ping OK");
			}
		}
		public List<string> GetSubscribtions() {
			List<string> returns = new List<string>();
			foreach(string topic in subscribeList) {
				if(_mqttActive)
					returns.Add(_mqttId + "/" + topic);
			}
			return returns;
		}
		private void SendOnlineQuestion() {
			if(Debug.debugShelly)
				Debug.Write(MethodInfo.GetCurrentMethod(), $"Shelly `sendOnlineQuestion`: {_mqttId}/info/Online, 0");
			_ = Program.MainProg.wpMQTTClient.setValue(_mqttId + "/info/Online", "0", MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);
		}
		private void SetOnlineError(bool e) {
			if(Debug.debugShelly)
				Debug.Write(MethodInfo.GetCurrentMethod(), $"Shelly `setOnlineError`: {_mqttId}/ERROR/Online, {(e ? "1" : "0")}");
			_ = Program.MainProg.wpMQTTClient.setValue(_mqttId + "/ERROR/Online", e ? "1" : "0");
		}
		private void SetOnlineError() {
			SetOnlineError(true);
		}

		private void DoCheckStatus_Elapsed(object sender, ElapsedEventArgs e) {
			GetStatus();
			if(Debug.debugShelly)
				Debug.Write(MethodInfo.GetCurrentMethod(), $"ShellyDevice doCheck gestartet '{this.Name}'");
		}
		private void DoCheckShelly_Elapsed(object sender, ElapsedEventArgs e) {
			GetHttpShelly();
			if(Debug.debugShelly)
				Debug.Write(MethodInfo.GetCurrentMethod(), $"ShellyDevice doCheckShelly gestartet '{this.Name}'");
		}

		public void SetLongPress() {
			Debug.Write(MethodInfo.GetCurrentMethod(), $"register LongPress on {this._name}");
			using(Database Sql = new Database("Shellys Long Press")) {
				string[][] Query = Sql.Query(@$"SELECT
					[ip], [type], [un], [pw]
					FROM [shelly]
					WHERE [id_restroom] = {this._room}");
				WebClient webClient = new WebClient();
				for(int ishelly = 0; ishelly < Query.Length; ishelly++) {
					try {
						webClient.Credentials = new NetworkCredential(Query[ishelly][2], Query[ishelly][3]);
						if(ShellyType.IsRelay(Query[ishelly][1]))
							webClient.DownloadString(new Uri($"http://{Query[ishelly][0]}/relay/0?turn=off"));
						if(ShellyType.IsLight(Query[ishelly][1]))
							webClient.DownloadString(new Uri($"http://{Query[ishelly][0]}/light/0?turn=off"));
					} catch(Exception ex) {
						eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
					}
				}
			}
			using(Database Sql = new Database("Shellys Long Press D1Mini")) {
				string[][] Query = Sql.Query(@$"SELECT
					[ip], [compiledwith]
					FROM [d1mini]
					WHERE [id_restroom] = {this._room}");
				WebClient webClient = new WebClient();
				for(int id1mini = 0; id1mini < Query.Length; id1mini++) {
					try {
						if(Query[id1mini][1].Contains("NeoPixel"))
							webClient.DownloadString(new Uri($"http://{Query[id1mini][0]}/setNeoPixel?turn=0"));
						if(Query[id1mini][1].Contains("CwWw"))
							webClient.DownloadString(new Uri($"http://{Query[id1mini][0]}/setCwWw?turn=0"));
					} catch(Exception ex) {
						eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
					}
				}
			}
		}
		public void GetStatus() {
			GetStatus(false);
		}
		public void GetStatus(bool force) {
			Task.Run(() => GetStatusAsync(force)).Wait();
		}
		public async Task GetStatusAsync(bool force) {
			string url = "http://" + this._ip.ToString();
			string target = "";
			if(ShellyType.IsGen2(this.Type)) {
				target = $"{url}/rpc/Switch.GetStatus?id=0";
			} else {
				target = $"{url}/status";
			}
			if(!ShellyType.IsBat(this.Type) && this.Active) {
				try {
					using(WebClient webClient = new WebClient()) {
						webClient.Credentials = new NetworkCredential("wpLicht", "turner");
						webClient.DownloadStringCompleted += (e, args) => {
							if(args.Error == null) {
								_lastContact = DateTime.Now;
								status sds = JsonConvert.DeserializeObject<status>(args.Result);
								if(this.IdOnOff > 0) {
									bool? ison = null;
									if(ShellyType.IsLight(this.Type) && !ShellyType.IsGen2(this.Type)) {
										ison = sds.lights[0].ison;
									}
									if(ShellyType.IsRelay(this.Type) && !ShellyType.IsGen2(this.Type)) {
										ison = sds.relays[0].ison;
									}
									if(ShellyType.IsGen2(this.Type)) {
										ison = sds.output;
									}
									if(ison != null && (Datapoints.Get(this.IdOnOff).Value == "True") != ison) {
										Datapoints.Get(this.IdOnOff).WriteValue((bool)ison ? "True" : "False", "Shelly");
										Debug.Write(MethodInfo.GetCurrentMethod(), $"Shelly `recived Status`: {this.Name}, {((bool)ison ? "True" : "False")}");
									}
								}
								using(Database Sql = new Database("Update Shelly MQTT lastContact")) {
									string sql = $"UPDATE [shelly] SET [lastcontact] = '{_lastContact.ToString(Database.DateTimeFormat)}' WHERE [id_shelly] = {_id}";
									Sql.NonResponse(sql);
									if(Debug.debugShelly)
										Debug.Write(MethodInfo.GetCurrentMethod(), sql);
								}
							} else {
								Debug.WriteError(MethodInfo.GetCurrentMethod(), args.Error, $"{this.Name} ({this.Ip}), '{target}'");
							}
						};
						if(_lastContact.AddHours(1) < DateTime.Now || force) {
							await Task.Run(() => webClient.DownloadStringAsync(new Uri(target)));
							_doCheckStatus.Stop();
							_doCheckStatus.Start();
						}
					}
				} catch(Exception ex) {
					eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex, $"{this.Name} ({this.Ip}), '{target}'");
				}
			}
		}
		public void GetHttpShelly() {
			GetHttpShelly(false);
		}
		public void GetHttpShelly(bool force) {
			Task.Run(() => GetHttpShellyAsync(force)).Wait();
		}
		public async void GetHttpShellyAsync(bool force) {
			string target = $"http://{this._ip.ToString()}/shelly";
			if(ShellyType.IsGen2(this.Type) && !ShellyType.IsBat(this.Type) && this.Active) {
				try {
					using(WebClient webClient = new WebClient()) {
						webClient.Credentials = new NetworkCredential("wpLicht", "turner");
						webClient.DownloadStringCompleted += (e, args) => {
							if(args.Error == null) {
								_lastContact = DateTime.Now;
								dynamic stuff = JsonConvert.DeserializeObject(args.Result);
								string updatesql = "";
								if(stuff.id != null && this._wsId != (string)stuff.id) {
									this._wsId = stuff.id;
									updatesql += $"[ws_id] = '{stuff.id}', ";
								}
								using(Database Sql = new Database("Update Shelly lastContact")) {
									string sql = $"UPDATE [shelly] SET {updatesql}[lastcontact] = '{_lastContact.ToString(Database.DateTimeFormat)}' WHERE [id_shelly] = {_id}";
									Sql.NonResponse(sql);
									if(Debug.debugShelly)
										Debug.Write(MethodInfo.GetCurrentMethod(), sql);
								}
							} else {
								Debug.WriteError(MethodInfo.GetCurrentMethod(), args.Error, $"{this.Name} ({this.Ip}), '{target}'");
							}
						};
						if(_lastContact.AddHours(1) < DateTime.Now || force) {
							await Task.Run(() => webClient.DownloadStringAsync(new Uri(target)));
							_doCheckShelly.Stop();
							_doCheckShelly.Start();
						}
					}
				} catch(Exception ex) {
					eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex, $"{this.Name} ({this.Ip}), '{target}'");
				}
			}
		}
		public void GetMqttStatus() {
			Task.Run(() => GetMqttStatusAsync()).Wait();
		}
		public async void GetMqttStatusAsync() {
			string url = "http://" + this._ip.ToString();
			string target = "";
			if(ShellyType.IsGen2(this.Type)) {
				target = $"{url}/rpc/Mqtt.GetConfig";
			} else {
				target = $"{url}/settings";
			}
			if(!ShellyType.IsBat(this.Type) && this.Active) {
				try {
					using(WebClient webClient = new WebClient()) {
						webClient.Credentials = new NetworkCredential("wpLicht", "turner");
						webClient.DownloadStringCompleted += (e, args) => {
							if(args.Error == null) {
								mqttstatus sdms = JsonConvert.DeserializeObject<mqttstatus>(args.Result);
								bool res_mqtt_enable, res_mqtt_writeable, res_coiot_enable;
								string res_mqtt_server, res_mqtt_id, res_mqtt_prefix;
								if(ShellyType.IsGen2(this.Type)) {
									res_mqtt_enable = sdms.enable;
									res_mqtt_server = sdms.server;
									res_mqtt_id = sdms.client_id;
									res_mqtt_prefix = sdms.topic_prefix;
									res_mqtt_writeable = sdms.enable_control;
									res_coiot_enable = false;
								} else {
									res_mqtt_enable = sdms.mqtt.enable;
									res_mqtt_server = sdms.mqtt.server;
									res_mqtt_id = sdms.mqtt.id;
									res_mqtt_prefix = "shellies/" + sdms.mqtt.id;
									res_mqtt_writeable = true;
									res_coiot_enable = sdms.coiot.enabled;
								}
								string updatesql = "";
								if(_mqttActive != res_mqtt_enable) {
									_mqttActive = res_mqtt_enable;
									if(!String.IsNullOrEmpty(updatesql))
										updatesql += ", ";
									updatesql += $"[mqtt_active] = {(_mqttActive ? "1" : "0")}";
								}
								if(_mqttServer != res_mqtt_server) {
									_mqttServer = res_mqtt_server;
									if(!String.IsNullOrEmpty(updatesql))
										updatesql += ", ";
									updatesql += $"[mqtt_server] = '{_mqttServer}'";
								}
								if(_mqttId != res_mqtt_id) {
									_mqttId = res_mqtt_id;
									if(!String.IsNullOrEmpty(updatesql))
										updatesql += ", ";
									updatesql += $"[mqtt_id] = '{_mqttId}'";
								}
								if(_mqttPrefix != res_mqtt_prefix) {
									_mqttPrefix = res_mqtt_prefix;
									if(!String.IsNullOrEmpty(updatesql))
										updatesql += ", ";
									updatesql += $"[mqtt_prefix] = '{_mqttPrefix}'";
								}
								if(_mqttWriteable != res_mqtt_writeable) {
									_mqttWriteable = res_mqtt_writeable;
									if(!String.IsNullOrEmpty(updatesql))
										updatesql += ", ";
									updatesql += $"[mqtt_writeable] = {(_mqttWriteable ? "1" : "0")}";
								}
								if(_coIotActive != res_coiot_enable) {
									_coIotActive = res_coiot_enable;
									if(!String.IsNullOrEmpty(updatesql))
										updatesql += ", ";
									updatesql += $"[coiot_active] = {(_coIotActive ? "1" : "0")}";
								}
								if(!String.IsNullOrEmpty(updatesql)) {
									using(Database Sql = new Database("Update Shelly MQTT ID")) {
										string sql = $"UPDATE [shelly] SET {updatesql} WHERE [id_shelly] = {_id}";
										Sql.NonResponse(sql);
										if(Debug.debugShelly)
											Debug.Write(MethodInfo.GetCurrentMethod(), sql);
									}
								}
							} else {
								Debug.WriteError(MethodInfo.GetCurrentMethod(), args.Error, $"{this.Name} ({this.Ip}), '{target}'");
							}
						};
						await Task.Run(() => webClient.DownloadStringAsync(new Uri(target)));
					}
				} catch(Exception ex) {
					eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex, $"{this.Name} ({this.Ip}), '{target}'");
				}
			}
		}
		public override string ToString() {
			return $"id: {_id}, ip: {_ip}, mac: {_mac}, room: {_room}, name: {_name}, type: {_type}, active: {_active}, mqtt_active: {_mqttActive}, mqtt_server: {_mqttServer}, mqtt_id: {_mqttId}, mqtt_prefix: {_mqttPrefix}, mqtt_writeable: {_mqttWriteable}, ws_id: {_wsId}";
		}
	}
}
