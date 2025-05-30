//###################################################################################
//#                                                                                 #
//#              (C) FreakaZone GmbH                                                #
//#              =======================                                            #
//#                                                                                 #
//###################################################################################
//#                                                                                 #
//# Author       : Christian Scheid                                                 #
//# Date         : 29.11.2023                                                       #
//#                                                                                 #
//# Revision     : $Rev:: 238                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: MQTTClient.cs 238 2025-05-30 11:25:05Z                   $ #
//#                                                                                 #
//###################################################################################
using FreakaZone.Libraries.wpCommen;
using FreakaZone.Libraries.wpEventLog;
using FreakaZone.Libraries.wpIniFile;
using FreakaZone.Libraries.wpSQL;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebAutomation.Controller;
using WebAutomation.Controller.ShellyDevice;

namespace WebAutomation.Communication {
	public class MQTTClient {
		public event EventHandler<valueChangedEventArgs> valueChanged;
		public event EventHandler<valueChangedEventArgs> d1MiniChanged;
		public event EventHandler<valueChangedEventArgs> shellyChanged;
		public class valueChangedEventArgs: EventArgs {
			public int idDatapoint { get; set; }
			public string topic { get; set; }
			public string value { get; set; }
		}
		private class topic {
			public int IdTopic { get; }
			public int IdDatapoint { get; }
			public string Json { get; }
			public bool Writeable { get; }
			public bool Readable { get; }
			public topic(int idTopic, int idDatapoint, string json, bool readable, bool writeable) {
				IdTopic = idTopic;
				IdDatapoint = idDatapoint;
				Json = json;
				Writeable = writeable;
				Readable = readable;
			}
		}
		private int _idBroker;
		private int _port;
		private string _ipBroker;
		private string _clientId;
		private IMqttClient _mqttClient;
		/// <summary>
		/// topic (string) - topic
		/// </summary>
		private Dictionary<string, Dictionary<string, topic>> _topics;
		private List<string> subscribed;
		public List<string> ServerTopics { get { return _serverTopics; } }
		private List<string> _serverTopics;
		private Dictionary<string, string> _settings;
		private string ForceUpdate;
		private bool connectPending;
		public MQTTClient() {
			Debug.Write(MethodInfo.GetCurrentMethod(), "MQTT Client init");
			string[][] DBBroker;
			connectPending = false;
			using(Database Sql = new Database("MQTT Server")) {
				DBBroker = Sql.Query(@"SELECT TOP 1 [id_mqttbroker], [address], [port] FROM [mqttbroker]");
			}
			;
			_idBroker = Int32.Parse(DBBroker[0][0]);
			_port = Int32.Parse(DBBroker[0][2]);
			_ipBroker = DBBroker[0][1];
			fillTopics();
			_clientId = $"{Application.ProductName}-{Environment.MachineName}";
			ForceUpdate = $"{_clientId}/ForceMqttUpdate";
			MqttFactory factory = new MqttFactory();
			_mqttClient = factory.CreateMqttClient();
			Debug.Write(MethodInfo.GetCurrentMethod(), "MQTT Client gestartet");
		}
		private void fillTopics() {
			Debug.Write(MethodInfo.GetCurrentMethod(), "MQTT Client fillTopics");
			_topics = new Dictionary<string, Dictionary<string, topic>>();
			subscribed = new List<string>();
			_serverTopics = new List<string>();
			_settings = new Dictionary<string, string>();
			using(Database Sql = new Database("MQTT topic")) {
				string[][] DBtopic = Sql.Query(@$"
SELECT
	[t].[id_mqtttopic], [t].[topic], [t].[json], [t].[readable], [t].[writeable], [dp].[id_dp]
FROM [mqtttopic] [t]
LEFT JOIN [mqttgroup] ON [mqttgroup].[id_mqttgroup] = [t].[id_mqttgroup]
LEFT JOIN [dp] ON [dp].[id_mqtttopic] = [t].[id_mqtttopic]
WHERE [mqttgroup].[id_mqttbroker] = {_idBroker} ORDER BY [topic]");
				for(int itopic = 0; itopic < DBtopic.Length; itopic++) {
					int idtopic = Int32.Parse(DBtopic[itopic][0]);
					int iddatapoint = 0;
					Int32.TryParse(DBtopic[itopic][5], out iddatapoint);
					try {
						topic nt = new topic(idtopic, iddatapoint, DBtopic[itopic][2], DBtopic[itopic][3] == "True", DBtopic[itopic][4] == "True");
						if(!_topics.ContainsKey(DBtopic[itopic][1]))
							_topics.Add(DBtopic[itopic][1], new Dictionary<string, topic>());
						if(!_topics[DBtopic[itopic][1]].ContainsKey(DBtopic[itopic][2]))
							_topics[DBtopic[itopic][1]].Add(DBtopic[itopic][2], nt);
						else
							_topics[DBtopic[itopic][1]][DBtopic[itopic][2]] = nt;
					} catch(Exception ex) {
						Debug.WriteError(MethodInfo.GetCurrentMethod(), ex, $"idDatapoint: {iddatapoint}, idTopic: {idtopic}");
					}
				}
			}
			Debug.Write(MethodInfo.GetCurrentMethod(), "MQTT Client fillTopics ok");
		}
		public async Task Start() {
			connectPending = true;
			Debug.Write(MethodInfo.GetCurrentMethod(), "MQTT Client start work");
			MqttClientOptions options = new MqttClientOptionsBuilder()
				.WithTcpServer(_ipBroker, _port)
				.WithClientId(_clientId)
				.Build();
			try {
				MqttClientConnectResult connectResult = await _mqttClient.ConnectAsync(options);
				if(connectResult.ResultCode == MqttClientConnectResultCode.Success) {
					Debug.Write(MethodInfo.GetCurrentMethod(), $"Connected to MQTT broker (mqtt://{_ipBroker}:{_port}) successfully");
					_mqttClient.ApplicationMessageReceivedAsync += MqttClient_ApplicationMessageReceivedAsync;
					await registerDatapoints();
					connectPending = false;
				} else {
					Debug.Write(MethodInfo.GetCurrentMethod(), $"Failed to connect to MQTT broker ({_ipBroker}): {connectResult.ResultCode}");
				}
			} catch(Exception ex) {
				Debug.WriteError(MethodInfo.GetCurrentMethod(), ex);
			}
			D1MiniServer.ForceRenewValue();
			Debug.Write(MethodInfo.GetCurrentMethod(), "MQTT Client start work OK");
		}
		private async Task<string> registerDatapoints() {
			foreach(KeyValuePair<string, Dictionary<string, topic>> kvp1 in _topics) {
				foreach(KeyValuePair<string, topic> kvp2 in kvp1.Value) {
					if(!subscribed.Contains(kvp1.Key) && kvp2.Value.Readable) {
						try {
							await _mqttClient.SubscribeAsync(kvp1.Key);
							subscribed.Add(kvp1.Key);
							if(Debug.debugMQTT) {
								Debug.Write(MethodInfo.GetCurrentMethod(), $"Add MQTT Topic: {kvp1.Key}");
							}
						} catch(Exception ex) {
							Debug.WriteError(MethodInfo.GetCurrentMethod(), ex);
						}
					}
				}
			}
			registerNewD1MiniDatapoints();
			registerNewShellyDatapoints();
			await _mqttClient.SubscribeAsync(ForceUpdate);
			publishSettings();
			forceMqttUpdate();
			return "S_OK";
		}
		public async void registerNewD1MiniDatapoints() {
			foreach(string d1m in D1MiniServer.GetSubscribtions()) {
				if(!subscribed.Contains(d1m)) {
					await _mqttClient.SubscribeAsync(d1m);
					subscribed.Add(d1m);
					if(Debug.debugMQTT) {
						Debug.Write(MethodInfo.GetCurrentMethod(), $"Add D1Mini MQTT Topic: {d1m}");
					}
				}
			}
		}
		public async void registerNewShellyDatapoints() {
			foreach(string shelly in ShellyServer.GetSubscribtions()) {
				if(!subscribed.Contains(shelly)) {
					await _mqttClient.SubscribeAsync(shelly);
					subscribed.Add(shelly);
					if(Debug.debugMQTT) {
						Debug.Write(MethodInfo.GetCurrentMethod(), $"Add Shelly MQTT Topic: {shelly}");
					}
				}
			}
		}
		public async void Stop() {
			Debug.Write(MethodInfo.GetCurrentMethod(), "wpMQTTClient stop");
			if(_mqttClient != null && _mqttClient.IsConnected) {
				_mqttClient.ApplicationMessageReceivedAsync -= MqttClient_ApplicationMessageReceivedAsync;
				foreach(string unsubscribe in subscribed) {
					try {
						await _mqttClient.UnsubscribeAsync(unsubscribe);
						if(Debug.debugMQTT) {
							Debug.Write(MethodInfo.GetCurrentMethod(), $"Unsubscribe MQTT Topic: {unsubscribe}");
						}
					} catch(Exception ex) {
						Debug.WriteError(MethodInfo.GetCurrentMethod(), ex, unsubscribe);
					}
				}
				await _mqttClient.UnsubscribeAsync("#");
				try {
					await _mqttClient.DisconnectAsync();
					Debug.Write(MethodInfo.GetCurrentMethod(), "wpMQTTClient gestoppt");
				} catch(Exception ex) {
					Debug.WriteError(MethodInfo.GetCurrentMethod(), ex, "wpMQTTClient nicht gestoppt");
				}
				_mqttClient = null;
			} else {
				Debug.Write(MethodInfo.GetCurrentMethod(), "MQTT Server schon gestoppt??");
			}
		}
		private Task MqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e) {
			string v = e.ApplicationMessage.ConvertPayloadToString();
			if(Debug.debugMQTT) {
				Debug.Write(MethodInfo.GetCurrentMethod(), $"Topic: {e.ApplicationMessage.Topic}, value: {v}");
			}
			if(!_serverTopics.Contains(e.ApplicationMessage.Topic)) {
				_serverTopics.Add(e.ApplicationMessage.Topic);
			}
			if(e.ApplicationMessage.Topic == ForceUpdate && v != "0") {
				publishSettings();
				doneMyMqttUpdate();
				if(Debug.debugMQTT) {
					Debug.Write(MethodInfo.GetCurrentMethod(), "ForceMqttUpdate finished");
				}
			}
			valueChangedEventArgs vcea = new valueChangedEventArgs();
			vcea.topic = e.ApplicationMessage.Topic;
			vcea.idDatapoint = 0;
			if(_topics.ContainsKey(e.ApplicationMessage.Topic)) {
				if(Debug.debugMQTT) {
					Debug.Write(MethodInfo.GetCurrentMethod(), $"Topic: {e.ApplicationMessage.Topic}, value: {v}");
				}
				if(Common.IsValidJson(v)) {
					// Shelly HT RSSI
					if(e.ApplicationMessage.Topic.EndsWith("/info")) {
						ShellyJson.info Info = JsonConvert.DeserializeObject<ShellyJson.info>(v);
						if(_topics[e.ApplicationMessage.Topic].ContainsKey("wifi_sta.rssi")) {
							vcea.idDatapoint = _topics[e.ApplicationMessage.Topic]["wifi_sta.rssi"].IdDatapoint;
							vcea.value = Info.Wifi_sta.rssi.ToString();
							if(valueChanged != null && vcea.idDatapoint > 0)
								valueChanged.Invoke(this, vcea);
						}
					}
					// Shelly GEN2 GEN3 Relay
					if(e.ApplicationMessage.Topic.EndsWith("/status/switch:0")) {
						ShellyJson.status.switch0 StatusSwitch0 = JsonConvert.DeserializeObject<ShellyJson.status.switch0>(v);
						if(_topics[e.ApplicationMessage.Topic].ContainsKey("output")) {
							vcea.idDatapoint = _topics[e.ApplicationMessage.Topic]["output"].IdDatapoint;
							vcea.value = StatusSwitch0.output.ToString();
							if(valueChanged != null && vcea.idDatapoint > 0)
								valueChanged.Invoke(this, vcea);
						}
						if(_topics[e.ApplicationMessage.Topic].ContainsKey("apower")) {
							vcea.idDatapoint = _topics[e.ApplicationMessage.Topic]["apower"].IdDatapoint;
							vcea.value = StatusSwitch0.apower.ToString();
							if(valueChanged != null && vcea.idDatapoint > 0)
								valueChanged.Invoke(this, vcea);
						}
					}
					if(e.ApplicationMessage.Topic.EndsWith("/status/temperature:0") ||
						e.ApplicationMessage.Topic.EndsWith("/status/temperature:100")) {
						ShellyJson.status.temperature0 StatusTemperature0 = JsonConvert.DeserializeObject<ShellyJson.status.temperature0>(v);
						if(_topics[e.ApplicationMessage.Topic].ContainsKey("tC")) {
							vcea.idDatapoint = _topics[e.ApplicationMessage.Topic]["tC"].IdDatapoint;
							vcea.value = StatusTemperature0.tC.ToString();
							if(valueChanged != null && vcea.idDatapoint > 0)
								valueChanged.Invoke(this, vcea);
						}
					}
					if(e.ApplicationMessage.Topic.EndsWith("/status/humidity:0") ||
						e.ApplicationMessage.Topic.EndsWith("/status/humidity:100")) {
						ShellyJson.status.humidity0 StatusHumidity0 = JsonConvert.DeserializeObject<ShellyJson.status.humidity0>(v);
						if(_topics[e.ApplicationMessage.Topic].ContainsKey("rh")) {
							vcea.idDatapoint = _topics[e.ApplicationMessage.Topic]["rh"].IdDatapoint;
							vcea.value = StatusHumidity0.rh.ToString();
							if(valueChanged != null && vcea.idDatapoint > 0)
								valueChanged.Invoke(this, vcea);
						}
					}
					if(e.ApplicationMessage.Topic.EndsWith("/status/devicepower:0")) {
						ShellyJson.status.devicepower0 StatusDevicepower0 = JsonConvert.DeserializeObject<ShellyJson.status.devicepower0>(v);
						if(_topics[e.ApplicationMessage.Topic].ContainsKey("battery.percent")) {
							vcea.idDatapoint = _topics[e.ApplicationMessage.Topic]["battery.percent"].IdDatapoint;
							vcea.value = StatusDevicepower0.Battery.percent.ToString();
							if(valueChanged != null && vcea.idDatapoint > 0)
								valueChanged.Invoke(this, vcea);
						}
					}
					if(e.ApplicationMessage.Topic.EndsWith("/status/wifi")) {
						ShellyJson.status.wifi StatusWifi = JsonConvert.DeserializeObject<ShellyJson.status.wifi>(v);
						if(_topics[e.ApplicationMessage.Topic].ContainsKey("rssi")) {
							vcea.idDatapoint = _topics[e.ApplicationMessage.Topic]["rssi"].IdDatapoint;
							vcea.value = StatusWifi.rssi.ToString();
							if(valueChanged != null && vcea.idDatapoint > 0)
								valueChanged.Invoke(this, vcea);
						}
					}
				} else {
					if(_topics[e.ApplicationMessage.Topic].ContainsKey("") &&
						_topics[e.ApplicationMessage.Topic][""].Readable) {
						vcea.idDatapoint = _topics[e.ApplicationMessage.Topic][""].IdDatapoint;
						vcea.value = v;
						if(valueChanged != null && vcea.idDatapoint > 0)
							valueChanged.Invoke(this, vcea);
					}
				}
			}
			if(D1MiniServer.GetSubscribtions().Contains(e.ApplicationMessage.Topic)) {
				vcea.value = v;
				if(d1MiniChanged != null) {
					d1MiniChanged.Invoke(this, vcea);
				}
			}
			if(ShellyServer.GetSubscribtions().Contains(e.ApplicationMessage.Topic)) {
				vcea.value = v;
				if(shellyChanged != null) {
					shellyChanged.Invoke(this, vcea);
				}
			}
			vcea = null;
			return Task.CompletedTask;
		}

		public async Task<ret> setBrowseTopics() {
			try {
				Program.MainProg.BrowseMqtt = true;
				await _mqttClient.SubscribeAsync("#");
				Debug.Write(MethodInfo.GetCurrentMethod(), "MQTT Subscribed #");
				return new ret() { erg = ret.OK };
			} catch(Exception ex) {
				Debug.WriteError(MethodInfo.GetCurrentMethod(), ex);
				return new ret() { erg = ret.ERROR, message = ex.Message, trace = ex.StackTrace };
			}
		}
		public async Task<ret> unsetBrowseTopics() {
			try {
				Program.MainProg.BrowseMqtt = false;
				await _mqttClient.UnsubscribeAsync("#");
				Debug.Write(MethodInfo.GetCurrentMethod(), "MQTT Unsubscribed #");
				await registerDatapoints();
				return new ret() { erg = ret.OK };
			} catch(Exception ex) {
				Debug.WriteError(MethodInfo.GetCurrentMethod(), ex);
				return new ret() { erg = ret.ERROR, message = ex.Message, trace = ex.StackTrace };
			}
		}

		public async Task<string> setValue(int IdTopic, string value) {
			return await setValue(IdTopic, value, MqttQualityOfServiceLevel.AtMostOnce);
		}
		public async Task<string> setValue(int IdTopic, string value, MqttQualityOfServiceLevel QoS) {
			if(getTopicFromId(IdTopic) != null) {
				return await setValue(getTopicFromId(IdTopic), value, QoS);
			} else {
				Debug.Write(MethodInfo.GetCurrentMethod(), $"setValue: ID not found: {IdTopic}");
			}
			return new ret() { erg = ret.ERROR }.ToString();
		}
		public async Task<string> setValue(string topic, string value) {
			return await setValue(topic, value, MqttQualityOfServiceLevel.AtMostOnce);
		}
		public async Task<string> setValue(string topic, string value, MqttQualityOfServiceLevel QoS) {
			ret returns = new ret() { erg = ret.OK };
			if(topic != string.Empty) {
				try {
					MqttApplicationMessage msg = new MqttApplicationMessage {
						Topic = topic,
						PayloadSegment = getFromString(value),
						QualityOfServiceLevel = QoS
					};
					if(_mqttClient.IsConnected) {
						await _mqttClient.PublishAsync(msg);
					} else {
						SetMqttAlarm();
						Debug.Write(MethodInfo.GetCurrentMethod(), $"MQTT OFFLINE, setValue: {topic}, value: {value}");
					}
					if(Debug.debugMQTT) {
						Debug.Write(MethodInfo.GetCurrentMethod(), $"setValue: {topic}, value: {value}");
					}
				} catch(Exception ex) {
					Debug.WriteError(MethodInfo.GetCurrentMethod(), ex, $"setValue: topic: {topic}, value: {value}");
					returns.erg = ret.ERROR;
					returns.message = ex.Message;
				} finally {
					if(!_mqttClient.IsConnected && !connectPending) {
						Debug.Write(MethodInfo.GetCurrentMethod(), "Connection Lost, try to reconnect");
						await Start();
					}
				}
			} else {
				Debug.Write(MethodInfo.GetCurrentMethod(), $"setValue: topic not set");
				returns.erg = ret.WARNING;
				returns.message = "topic is Empty";
			}
			return returns.ToString();
		}
		private void SetMqttAlarm(bool value = true) {
			int DpIdMqttAlarm;
			if(Int32.TryParse(IniFile.Get("Watchdog", "DpIdMqttAlarm"), out DpIdMqttAlarm)) {
				Datapoints.Get(DpIdMqttAlarm)?.SetValue(value ? "1" : "0");
			}
		}
		private ArraySegment<byte> getFromString(string m) {
			return new ArraySegment<byte>(Encoding.UTF8.GetBytes(m));
		}
		private string getTopicFromId(int IdTopic) {
			foreach(KeyValuePair<string, Dictionary<string, topic>> kvp1 in _topics) {
				foreach(KeyValuePair<string, topic> kvp2 in kvp1.Value) {
					if(kvp2.Value.IdTopic == IdTopic) {
						return kvp1.Key;
					}
				}
			}
			return null;
		}
		public bool forceMqttUpdate() {
			bool returns = true;
			if(!shellyMqttUpdate())
				returns = false;
			if(!d1MiniMqttUpdate())
				returns = false;
			return returns;
		}
		public bool shellyMqttUpdate() {
			bool returns = true;
			MqttApplicationMessage msg = new MqttApplicationMessage();
			foreach(Shelly s in ShellyServer.ForceMqttUpdateAvailable) {
				msg.Topic = $"{s.MqttId}/ForceMqttUpdate";
				try {
					msg.PayloadSegment = getFromString("1");
					if(_mqttClient.IsConnected) {
						_mqttClient.PublishAsync(msg);
					} else {
						Debug.Write(MethodInfo.GetCurrentMethod(), $"MQTT OFFLINE, shellyMqttUpdate: {msg.Topic}, value: 1");
					}
					if(Debug.debugMQTT)
						Debug.Write(MethodInfo.GetCurrentMethod(), $"ForceMqttUpdate: {s.MqttId}");
				} catch(Exception ex) {
					if(Debug.debugMQTT)
						Debug.WriteError(MethodInfo.GetCurrentMethod(), ex, msg.Topic);
					returns = false;
				}
			}
			return returns;
		}
		public bool d1MiniMqttUpdate() {
			bool returns = true;
			D1MiniServer.ForceMqttUpdate();
			return returns;
		}
		private async void doneMyMqttUpdate() {
			MqttApplicationMessage msg = new MqttApplicationMessage();
			msg.Topic = ForceUpdate;
			msg.PayloadSegment = new ArraySegment<byte>(Encoding.UTF8.GetBytes("0"));
			await _mqttClient.PublishAsync(msg);
			if(Debug.debugMQTT) {
				Debug.Write(MethodInfo.GetCurrentMethod(), "write ForceMqttUpdate");
			}
			msg = null;
		}

		public void publishSettings() {
			string[] pVersion = Application.ProductVersion.Split('.');
			addSetting("ProductName", Application.ProductName);
			addSetting("Version", $"{pVersion[0]}.{pVersion[1]} Build {Program.subversion}");
			addSetting("CompanyName", Application.CompanyName);
			addSetting("Projektnummr", IniFile.Get("Projekt", "Nummer"));
			bool debug = false;
#if DEBUG
			debug = true;
#endif
			addSetting("Debugmode", debug ? "true" : "false");
			addSetting("DebugModules", Debug.GetDebugJson());
			MqttApplicationMessage msg = new MqttApplicationMessage();
			msg.Retain = true;
			foreach(KeyValuePair<string, string> kvp in _settings) {
				msg.Topic = $"{_clientId}/{kvp.Key}";
				msg.PayloadSegment = new ArraySegment<byte>(Encoding.UTF8.GetBytes(kvp.Value));
				if(_mqttClient.IsConnected) {
					_mqttClient.PublishAsync(msg);
				} else {
					Debug.Write(MethodInfo.GetCurrentMethod(), $"MQTT OFFLINE, publishSettings: {msg.Topic}, value: {kvp.Value}");
				}
			}
			msg = null;
		}
		private void addSetting(string s, string v) {
			if(!_settings.ContainsKey(s))
				_settings.Add(s, v);
			else
				_settings[s] = v;
		}
	}
}
