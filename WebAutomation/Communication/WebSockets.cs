//###################################################################################
//#                                                                                 #
//#              (C) FreakaZone GmbH                                                #
//#              =======================                                            #
//#                                                                                 #
//###################################################################################
//#                                                                                 #
//# Author       : Christian Scheid                                                 #
//# Date         : 08.06.2021                                                       #
//#                                                                                 #
//# Revision     : $Rev:: 213                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: WebSockets.cs 213 2025-05-15 14:50:57Z                   $ #
//#                                                                                 #
//###################################################################################
using FreakaZone.Libraries.wpEventLog;
using FreakaZone.Libraries.wpIniFile;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using WatsonWebsocket;
using WebAutomation.D1Mini;
using WebAutomation.Shelly;

namespace WebAutomation.Communication {
	public class WebSockets {

		#region Server
		private const string cAddDatapoints = "addDatapoints";
		private const string cGetRegistered = "getRegistered";
		#endregion
		#region D1Mini
		private const string cGetD1MiniJson = "getD1MiniJson";
		private const string cStartD1MiniSearch = "startD1MiniSearch";
		#endregion

		/// <summary></summary>
		private Logger eventLog;

		private WatsonWsServer ws;
		private static Dictionary<Guid, wpTcpClient> WatsonClients;

		private int MonitorTimeout = 250;
		public WebSockets() {
			init();
		}
		private void init() {
			Debug.Write(MethodInfo.GetCurrentMethod(), "WebSockets Server Init");
			string name = IniFile.Get("Websockets", "Name");
			int port = IniFile.GetInt("Websockets", "Port");
			try {
				eventLog = new Logger(Logger.ESource.WebSockets);

				WatsonClients = new Dictionary<Guid, wpTcpClient>();
				ws = new WatsonWsServer(name, port, false);
				ws.ClientConnected += Ws_ClientConnected;
				ws.ClientDisconnected += Ws_ClientDisconnected;
				ws.MessageReceived += Ws_MessageReceived;
				ws.Start();
				eventLog.Write(MethodInfo.GetCurrentMethod(), $"WebSockets Server '{name}' gestartet, auf Port {port} gemappt");
			} catch(Exception ex) {
				Debug.WriteError(MethodInfo.GetCurrentMethod(), ex);
			}
		}
		public void finished() {
			if(ws != null && ws.IsListening)
				ws.Stop();
		}
		private void Ws_MessageReceived(object sender, MessageReceivedEventArgs e) {
			string s = Encoding.UTF8.GetString(e.Data.Array).Replace("\0", string.Empty);
			if(Regex.IsMatch(s, "^PING", RegexOptions.IgnoreCase)) {
				ws.SendAsync(e.Client.Guid, "PONG");
			} else {
				try {
					dynamic stuff = JsonConvert.DeserializeObject(s);
					if(WatsonClients.ContainsKey(e.Client.Guid))
						executeCommand(WatsonClients[e.Client.Guid], stuff);
				} catch(Exception ex) {
					Debug.WriteError(MethodInfo.GetCurrentMethod(), ex, s);
				}
			}
			if(Debug.debugWebSockets)
				Debug.Write(MethodInfo.GetCurrentMethod(), $"WebSockets Server Message received from {e.Client} : {s}");
		}

		private void Ws_ClientConnected(object sender, ConnectionEventArgs e) {
			WatsonClients.Add(e.Client.Guid, new wpTcpClient(e.Client.Guid));
			Debug.Write(MethodInfo.GetCurrentMethod(), $"WebSockets Server Client connected: {e.Client.Guid}");
		}

		private void Ws_ClientDisconnected(object sender, DisconnectionEventArgs e) {
			WatsonClients.Remove(e.Client.Guid);
			Debug.Write(MethodInfo.GetCurrentMethod(), $"WebSockets Server Client disconnected: {e.Client.Guid}");
		}

		private void executeCommand(wpTcpClient client, dynamic cmd) {
			if(cmd.src != null) {
				Shelly.Shelly shelly = ShellyServer.GetShellyFromWsId(cmd.src.ToString());
				if(shelly != null) {
					//if(shelly.IdOnOff > 0) {
					//	Datapoint dp = Datapoints.Get(shelly.IdOnOff);
					//	if(dp != null &&
					//		cmd["params"] != null &&
					//		cmd["params"]["switch:0"] != null &&
					//		cmd["params"]["switch:0"]["output"] != null
					//		) {
					//		dp.setValue((string)cmd["params"]["switch:0"]["output"]);
					//	}
					//}
					if(shelly.IdVoltage > 0) {
						Datapoint dp = Datapoints.Get(shelly.IdVoltage);
						if(dp != null &&
							cmd["params"] != null &&
							cmd["params"]["switch:0"] != null &&
							cmd["params"]["switch:0"]["voltage"] != null
							) {
							dp.SetValue((string)cmd["params"]["switch:0"]["voltage"]);
						}
					}
					if(shelly.IdCurrent > 0) {
						Datapoint dp = Datapoints.Get(shelly.IdCurrent);
						if(dp != null &&
							cmd["params"] != null &&
							cmd["params"]["switch:0"] != null &&
							cmd["params"]["switch:0"]["current"] != null
							) {
							dp.SetValue((string)cmd["params"]["switch:0"]["current"]);
						}
					}
					if(shelly.IdPower > 0) {
						Datapoint dp = Datapoints.Get(shelly.IdPower);
						if(dp != null &&
							cmd["params"] != null &&
							cmd["params"]["switch:0"] != null &&
							cmd["params"]["switch:0"]["apower"] != null
							) {
							dp.SetValue((string)cmd["params"]["switch:0"]["apower"]);
						}
					}
					if(Debug.debugWebSockets)
						Debug.Write(MethodInfo.GetCurrentMethod(), $"Shelly: {shelly.ToString()}");
				} else {
					Debug.Write(MethodInfo.GetCurrentMethod(), $"Websocket Shelly unknown: {cmd.src.ToString()}");
				}
			}
			if(cmd.command != null) {
				switch(cmd.command?.ToString()) {
					case cAddDatapoints:
						addDatapoints(client, cmd.data);
						Debug.Write(MethodInfo.GetCurrentMethod(), $"WebSockets Server command: {cAddDatapoints}");
						if(Debug.debugWebSockets)
							Debug.Write(MethodInfo.GetCurrentMethod(), "data: {0}", cmd.data);
						break;
					case cGetRegistered:
						getRegistered(client);
						Debug.Write(MethodInfo.GetCurrentMethod(), $"WebSockets Server question: {cGetRegistered}");
						if(Debug.debugWebSockets)
							Debug.Write(MethodInfo.GetCurrentMethod(), "qst: {0}", cmd);
						break;
					case cGetD1MiniJson:
						ws.SendAsync(client.id,
							"{\"response\":\"getD1MiniJson\"," +
							"\"data\":{" +
								$"\"ip\":\"{cmd.data}\"," +
								"\"D1Mini\":" + D1MiniServer.GetJsonStatus(cmd.data.ToString()) +
							"}}");
						if(Debug.debugWebSockets)
							Debug.Write(MethodInfo.GetCurrentMethod(), $"WebSockets Server question: {cGetD1MiniJson}");
						break;
					case cStartD1MiniSearch:
						D1MiniServer.StartSearch(client.id);
						if(Debug.debugWebSockets)
							Debug.Write(MethodInfo.GetCurrentMethod(), $"WebSockets Server question: {cStartD1MiniSearch}");
						break;
					default:
						Debug.Write(MethodInfo.GetCurrentMethod(), $"WebSockets Server Type not found");
						break;
				}
			}
		}
		public void sendAll(string msg) {
			foreach(KeyValuePair<Guid, wpTcpClient> entry in WatsonClients) {
				ws.SendAsync(entry.Key, msg);
			}
		}
		private void addDatapoints(wpTcpClient client, dynamic datapoints) {
			client.Clear();
			bool first = true;
			string msg = "";
			foreach(string dp in datapoints) {
				client.Add(dp);
				Datapoint datapoint = Datapoints.Get(dp);
				if(datapoint != null) {
					try {
						if(first) {
							first = false;
						} else {
							msg += ",";
						}
						msg += "{" +
							$"\"id\":{datapoint.ID}," +
							$"\"name\":\"{dp}\"," +
							$"\"value\":\"{datapoint.Value}\"," +
							$"\"valuestring\":\"{datapoint.ValueString}\"," +
							$"\"nks\":{datapoint.NKS}," +
							$"\"unit\":\"{datapoint.Unit}\"," +
							$"\"lastchange\":\"{datapoint.LastChange.ToString("s")}\"" +
						"}";
					} catch(Exception ex) {
						eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
					}
				} else {
					Debug.Write(MethodInfo.GetCurrentMethod(), $"Client {client.id}: Datapoint not found: {dp}");
				}
			}
			ws.SendAsync(client.id, "{\"response\":\"addDatapoints\",\"data\":[" + msg + "]}");
			if(Debug.debugWebSockets)
				Debug.Write(MethodInfo.GetCurrentMethod(), $"addDatapoints {client.id} message: {msg}");
		}
		private void getRegistered(wpTcpClient client) {
			ws.SendAsync(client.id, "{\"response\":\"getRegistered\",\"data\":[" + client.getDatapoints() + "]}");
		}

		public void sendText(wpTcpClient client, string response, string msg) {
			string answer = $"{{\"response\":\"{response}\",\"data\":{msg}}}";
			ws.SendAsync(client.id, answer);
		}
		public void sendDatapoint(string name) {
			Datapoint datapoint = Datapoints.Get(name);
			sendDatapoint(datapoint);
		}
		public void sendDatapoint(Datapoint DP) {
			try {
				string msg = "{" +
					$"\"id\":{DP.ID}," +
					$"\"name\":\"{DP.Name}\"," +
					$"\"value\":\"{DP.Value}\"," +
					$"\"valuestring\":\"{DP.ValueString}\"," +
					$"\"nks\":{DP.NKS}," +
					$"\"unit\":\"{DP.Unit}\"," +
					$"\"lastchange\":\"{DP.LastChange.ToString("s")}\"" +
				"}";
				if(Monitor.TryEnter(WatsonClients, MonitorTimeout)) {
					if(WatsonClients.Count > 0) {
						try {
							foreach(KeyValuePair<Guid, wpTcpClient> entry in WatsonClients) {
								if(entry.Value.hasDatapoint(DP.Name)) {
									ws.SendAsync(entry.Key, "{\"response\":\"sendDatapoint\",\"data\":[" + msg + "]}");
								}
							}
						} catch(Exception ex) {
							Debug.WriteError(MethodInfo.GetCurrentMethod(), ex);
						}
					}
					Monitor.Exit(WatsonClients);
				} else {
					Debug.Write(MethodInfo.GetCurrentMethod(), $"Angefordertes Item not Entered: WebSockets.Client, DP: '{DP.Name} ({DP.ID})'");
				}
			} catch(Exception ex) {
				Debug.WriteError(MethodInfo.GetCurrentMethod(), ex);
			}
		}
		public class wpTcpClient {
			public Guid id { get; set; }
			public string message { get; set; }
			private List<string> myDatapoints;
			public bool isFinished = false;
			public wpTcpClient(Guid id) {
				this.id = id;
				this.message = "";
				myDatapoints = new List<string>();
			}
			public void Add(string Datapoints) {
				if(!myDatapoints.Contains(Datapoints))
					myDatapoints.Add(Datapoints);
			}
			public bool hasDatapoint(string check) {
				return myDatapoints.Contains(check);
			}
			public string getDatapoints() {
				return String.Join(", ", myDatapoints.ToArray());
			}
			public void Clear() {
				myDatapoints.Clear();
			}
		}
	}
}
