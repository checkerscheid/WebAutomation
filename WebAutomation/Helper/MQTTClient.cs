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
//# Revision     : $Rev:: 135                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: MQTTClient.cs 135 2024-10-07 21:18:50Z                   $ #
//#                                                                                 #
//###################################################################################
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using Newtonsoft.Json;
using ShellyDevice;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WebAutomation.Helper {
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
		public MQTTClient() {
			wpDebug.Write("MQTT Client init");
			string[][] DBBroker;
			using(SQL SQL = new SQL("MQTT Server")) {
				DBBroker = SQL.wpQuery(@"SELECT TOP 1 [id_mqttbroker], [address], [port] FROM [mqttbroker]");
			};
			_idBroker = Int32.Parse(DBBroker[0][0]);
			_port = Int32.Parse(DBBroker[0][2]);
			_ipBroker = DBBroker[0][1];
			fillTopics();
			_clientId = $"{Application.ProductName}-{Environment.MachineName}";
			ForceUpdate = $"{_clientId}/ForceMqttUpdate";
			MqttFactory factory = new MqttFactory();
			_mqttClient = factory.CreateMqttClient();
			wpDebug.Write("MQTT Client gestartet");
		}
		private void fillTopics() {
			wpDebug.Write("MQTT Client fillTopics");
			_topics = new Dictionary<string, Dictionary<string, topic>>();
			subscribed = new List<string>();
			_serverTopics = new List<string>();
			_settings = new Dictionary<string, string>();
			using(SQL SQL = new SQL("MQTT topic")) {
				string[][] DBtopic = SQL.wpQuery(@$"
SELECT
	[t].[id_mqtttopic], [t].[topic], [t].[json], [t].[readable], [t].[writeable], [dp].[id_dp]
FROM [mqtttopic] [t]
LEFT JOIN [mqttgroup] ON [mqttgroup].[id_mqttgroup] = [t].[id_mqttgroup]
LEFT JOIN [dp] ON [dp].[id_mqtttopic] = [t].[id_mqtttopic]
WHERE [mqttgroup].[id_mqttbroker] = {_idBroker} ORDER BY [topic]");
				for(int itopic = 0; itopic < DBtopic.Length; itopic++) {
					int idtopic = Int32.Parse(DBtopic[itopic][0]);
					int iddatapoint = 0; Int32.TryParse(DBtopic[itopic][5], out iddatapoint);
					try {
						topic nt = new topic(idtopic, iddatapoint, DBtopic[itopic][2], DBtopic[itopic][3] == "True", DBtopic[itopic][4] == "True");
						if(!_topics.ContainsKey(DBtopic[itopic][1]))
							_topics.Add(DBtopic[itopic][1], new Dictionary<string, topic>());
						if(!_topics[DBtopic[itopic][1]].ContainsKey(DBtopic[itopic][2]))
							_topics[DBtopic[itopic][1]].Add(DBtopic[itopic][2], nt);
						else
							_topics[DBtopic[itopic][1]][DBtopic[itopic][2]] = nt;
					} catch(Exception ex) {
						wpDebug.WriteError(ex, $"idDatapoint: {iddatapoint}, idTopic: {idtopic}");
					}
				}
			}
			wpDebug.Write("MQTT Client fillTopics ok");
		}
		public async Task Start() {
			wpDebug.Write("MQTT Client start work");
			MqttClientOptions options = new MqttClientOptionsBuilder()
				.WithTcpServer(_ipBroker, _port)
				.WithClientId(_clientId)
				.Build();
			try {
				MqttClientConnectResult connectResult = await _mqttClient.ConnectAsync(options);
				if(connectResult.ResultCode == MqttClientConnectResultCode.Success) {
					wpDebug.Write($"Connected to MQTT broker (mqtt://{_ipBroker}:{_port}) successfully");
					_mqttClient.ApplicationMessageReceivedAsync += MqttClient_ApplicationMessageReceivedAsync;
					await registerDatapoints();
				} else {
					wpDebug.Write($"Failed to connect to MQTT broker ({_ipBroker}): {connectResult.ResultCode}");
				}
			} catch(Exception ex) {
				wpDebug.WriteError(ex);
			}
			D1MiniServer.ForceRenewValue();
			wpDebug.Write("MQTT Client start work OK");
		}
		private async Task<string> registerDatapoints() {
			foreach(KeyValuePair<string, Dictionary<string, topic>> kvp1 in _topics) {
				foreach(KeyValuePair<string, topic> kvp2 in kvp1.Value) {
					if(!subscribed.Contains(kvp1.Key) && kvp2.Value.Readable) {
						try {
							await _mqttClient.SubscribeAsync(kvp1.Key);
							subscribed.Add(kvp1.Key);
							if(wpDebug.debugMQTT) {
								wpDebug.Write($"Add MQTT Topic: {kvp1.Key}");
							}
						} catch(Exception ex) {
							wpDebug.WriteError(ex);
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
			foreach(string d1m in D1MiniServer.getSubscribtions()) {
				if(!subscribed.Contains(d1m)) {
					await _mqttClient.SubscribeAsync(d1m);
					subscribed.Add(d1m);
					if(wpDebug.debugMQTT) {
						wpDebug.Write($"Add D1Mini MQTT Topic: {d1m}");
					}
				}
			}
		}
		public async void registerNewShellyDatapoints() {
			foreach(string shelly in ShellyServer.getSubscribtions()) {
				if(!subscribed.Contains(shelly)) {
					await _mqttClient.SubscribeAsync(shelly);
					subscribed.Add(shelly);
					if(wpDebug.debugMQTT) {
						wpDebug.Write($"Add Shelly MQTT Topic: {shelly}");
					}
				}
			}
		}
		public async void Stop() {
			wpDebug.Write("wpMQTTClient stop");
			if(_mqttClient != null && _mqttClient.IsConnected) {
				_mqttClient.ApplicationMessageReceivedAsync -= MqttClient_ApplicationMessageReceivedAsync;
				foreach(string unsubscribe in subscribed) {
					try {
						await _mqttClient.UnsubscribeAsync(unsubscribe);
						if(wpDebug.debugMQTT) {
							wpDebug.Write($"Unsubscribe MQTT Topic: {unsubscribe}");
						}
					} catch(Exception ex) {
						wpDebug.WriteError(ex, unsubscribe);
					}
				}
				await _mqttClient.UnsubscribeAsync("#");
				try {
					await _mqttClient.DisconnectAsync();
					wpDebug.Write("wpMQTTClient gestoppt");
				} catch(Exception ex) {
					wpDebug.WriteError(ex, "wpMQTTClient nicht gestoppt");
				}
				_mqttClient = null;
			} else {
				wpDebug.Write("MQTT Server schon gestoppt??");
			}
		}
		private Task MqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e) {
			string v = e.ApplicationMessage.ConvertPayloadToString();
			if(wpDebug.debugMQTT) {
				wpDebug.Write($"Topic: {e.ApplicationMessage.Topic}, value: {v}");
			}
			if(!_serverTopics.Contains(e.ApplicationMessage.Topic)) {
				_serverTopics.Add(e.ApplicationMessage.Topic);
			}
			if(e.ApplicationMessage.Topic == ForceUpdate && v != "0") {
				publishSettings();
				doneMyMqttUpdate();
				if(wpDebug.debugMQTT) {
					wpDebug.Write("ForceMqttUpdate finished");
				}
			}
			valueChangedEventArgs vcea = new valueChangedEventArgs();
			vcea.topic = e.ApplicationMessage.Topic;
			vcea.idDatapoint = 0;
			if(_topics.ContainsKey(e.ApplicationMessage.Topic)) {
				if(wpDebug.debugMQTT) {
					wpDebug.Write($"Topic: {e.ApplicationMessage.Topic}, value: {v}");
				}
				if(wpHelp.IsValidJson(v)) {
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
			if(D1MiniServer.getSubscribtions().Contains(e.ApplicationMessage.Topic)) {
				vcea.value = v;
				if(d1MiniChanged != null) {
					d1MiniChanged.Invoke(this, vcea);
				}
			}
			if(ShellyServer.getSubscribtions().Contains(e.ApplicationMessage.Topic)) {
				vcea.value = v;
				if(shellyChanged != null) {
					shellyChanged.Invoke(this, vcea);
				}
			}
			vcea = null;
			return Task.CompletedTask;
		}

		public async Task<string> setBrowseTopics() {
			try {
				Program.MainProg.BrowseMqtt = true;
				await _mqttClient.SubscribeAsync("#");
				wpDebug.Write("MQTT Subscribed #");
				return "S_OK";
			} catch(Exception ex) {
				wpDebug.WriteError(ex);
				return "S_ERROR";
			}
		}
		public async Task<string> unsetBrowseTopics() {
			try {
				Program.MainProg.BrowseMqtt = false;
				await _mqttClient.UnsubscribeAsync("#");
				wpDebug.Write("MQTT Unsubscribed #");
				await registerDatapoints();
				return "S_OK";
			} catch(Exception ex) {
				wpDebug.WriteError(ex);
				return "S_ERROR";
			}
		}

		public async Task setValue(int IdTopic, string value) {
			await setValue(IdTopic, value, MqttQualityOfServiceLevel.AtMostOnce);
		}
		public async Task setValue(int IdTopic, string value, MqttQualityOfServiceLevel QoS) {
			if(getTopicFromId(IdTopic) != null) {
				try {
					MqttApplicationMessage msg = new MqttApplicationMessage {
						Topic = getTopicFromId(IdTopic),
						PayloadSegment = getFromString(value),
						QualityOfServiceLevel = QoS
					};
					await _mqttClient.PublishAsync(msg);
					if(wpDebug.debugMQTT)
						wpDebug.Write($"setValue: {msg.Topic} ({IdTopic}), value: {value}");
				} catch(Exception ex) {
					wpDebug.WriteError(ex);
				}
			} else {
				wpDebug.Write($"setValue: ID not found: {IdTopic}");
			}
		}
		public async Task<string> setValue(string topic, string value) {
			return await setValue(topic, value, MqttQualityOfServiceLevel.AtMostOnce);
		}
		public async Task<string> setValue(string topic, string value, MqttQualityOfServiceLevel QoS) {
			string returns = "{\"erg\":\"S_OK\"}";
			if(topic != string.Empty) {
				try {
					MqttApplicationMessage msg = new MqttApplicationMessage {
						Topic = topic,
						PayloadSegment = getFromString(value),
						QualityOfServiceLevel = QoS
					};
					await _mqttClient.PublishAsync(msg);
					if(wpDebug.debugMQTT)
						wpDebug.Write($"setValue: {topic}, value: {value}");
				} catch(Exception ex) {
					wpDebug.WriteError(ex);
					returns = "{\"erg\":\"S_ERROR\", \"msg\":\"" + ex.Message + "\"}";
				} finally {
					if(!_mqttClient.IsConnected) {
						wpDebug.Write("Connection Lost, try to reconnect");
						_ = Start();
					}
				}
			} else {
				wpDebug.Write($"setValue: topic not set");
				returns = "{\"erg\":\"S_WARNING\", \"msg\":\"topic is Empty\"}";
			}
			return returns;
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
			foreach(KeyValuePair<string, ShellyServer.ShellyDeviceHelper> kvp in ShellyServer.ForceMqttUpdateAvailable) {
				msg.Topic = $"{kvp.Value.mqtt_id}/ForceMqttUpdate";
				try {
					msg.PayloadSegment = getFromString("1");
					_mqttClient.PublishAsync(msg);
					if(wpDebug.debugMQTT)
						wpDebug.Write($"ForceMqttUpdate: {kvp.Value.mqtt_id}");
				} catch(Exception ex) {
					if(wpDebug.debugMQTT)
						wpDebug.WriteError(ex, msg.Topic);
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
			if(wpDebug.debugMQTT) {
				wpDebug.Write("write ForceMqttUpdate");
			}
			msg = null;
		}

		public void publishSettings() {
			string[] pVersion = Application.ProductVersion.Split('.');
			addSetting("ProductName", Application.ProductName);
			addSetting("Version", $"{pVersion[0]}.{pVersion[1]} Build {Program.subversion}");
			addSetting("CompanyName", Application.CompanyName);
			addSetting("Projektnummr", Ini.get("Projekt", "Nummer"));
			bool debug = false;
#if DEBUG
			debug = true;
#endif
			addSetting("Debugmode", debug ? "true" : "false");
			addSetting("DebugModules", wpDebug.getDebugJson());
			MqttApplicationMessage msg = new MqttApplicationMessage();
			msg.Retain = true;
			foreach(KeyValuePair<string, string> kvp in _settings) {
				msg.Topic = $"{_clientId}/{kvp.Key}";
				msg.PayloadSegment = new ArraySegment<byte>(Encoding.UTF8.GetBytes(kvp.Value));
				_mqttClient.PublishAsync(msg);
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
