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
//# Revision     : $Rev:: 165                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: WebSockets.cs 165 2025-02-09 09:15:16Z                   $ #
//#                                                                                 #
//###################################################################################
using FreakaZone.Libraries.wpEventLog;
using Newtonsoft.Json;
using ShellyDevice;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using WatsonWebsocket;

namespace WebAutomation.Helper {
	public class WebSockets {
		/// <summary></summary>
		private Logger eventLog;

		private WatsonWsServer ws;
		private static Dictionary<Guid, wpTcpClient> WatsonClients;

		private int MonitorTimeout = 250;
		public WebSockets() {
			init();
		}
		private void init() {
			wpDebug.Write(MethodInfo.GetCurrentMethod(), "WebSockets Server Init");
			string name = Ini.get("Websockets", "Name");
			int port = Ini.getInt("Websockets", "Port");
			try {
				eventLog = new Logger(wpLog.ESource.WebSockets);

				WatsonClients = new Dictionary<Guid, wpTcpClient>();
				ws = new WatsonWsServer(name, port, false);
				ws.ClientConnected += Ws_ClientConnected;
				ws.ClientDisconnected += Ws_ClientDisconnected;
				ws.MessageReceived += Ws_MessageReceived;
				ws.Start();
				eventLog.Write(MethodInfo.GetCurrentMethod(), $"WebSockets Server '{name}' gestartet, auf Port {port} gemappt");
			} catch(Exception ex) {
				wpDebug.WriteError(MethodInfo.GetCurrentMethod(), ex);
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
					executeCommand(WatsonClients[e.Client.Guid], stuff);
				} catch(Exception ex) {
					wpDebug.WriteError(MethodInfo.GetCurrentMethod(), ex, s);
				}
			}
			if(wpDebug.debugWebSockets)
				wpDebug.Write(MethodInfo.GetCurrentMethod(), $"WebSockets Server Message received from {e.Client} : {s}");
		}

		private void Ws_ClientConnected(object sender, ConnectionEventArgs e) {
			WatsonClients.Add(e.Client.Guid, new wpTcpClient(e.Client.Guid));
			wpDebug.Write(MethodInfo.GetCurrentMethod(), $"WebSockets Server Client connected: {e.Client.Guid}");
		}

		private void Ws_ClientDisconnected(object sender, DisconnectionEventArgs e) {
			WatsonClients.Remove(e.Client.Guid);
			wpDebug.Write(MethodInfo.GetCurrentMethod(), $"WebSockets Server Client disconnected: {e.Client.Guid}");
		}
		private void executeCommand(wpTcpClient client, dynamic cmd) {
			switch(cmd.command.ToString()) {
				case "addDatapoints":
					addDatapoints(client, cmd.data);
					wpDebug.Write(MethodInfo.GetCurrentMethod(), $"WebSockets Server command: addDatapoints");
					if(wpDebug.debugWebSockets)
						wpDebug.Write(MethodInfo.GetCurrentMethod(), "data: {0}", cmd.data);
					break;
				case "getRegistered":
					getRegistered(client);
					wpDebug.Write(MethodInfo.GetCurrentMethod(), $"WebSockets Server question: getRegistered");
					if(wpDebug.debugWebSockets)
						wpDebug.Write(MethodInfo.GetCurrentMethod(), "qst: {0}", cmd);
					break;
				case "getD1MiniJson":
					ws.SendAsync(client.id,
						"{\"response\":\"getD1MiniJson\"," +
						"\"data\":{" +
							$"\"ip\":\"{cmd.data}\"," +
							"\"D1Mini\":" + D1MiniServer.getJsonStatus(cmd.data.ToString()) +
						"}}");
					if(wpDebug.debugWebSockets)
						wpDebug.Write(MethodInfo.GetCurrentMethod(), $"WebSockets Server question: getD1MiniJson");
					break;
				case "startD1MiniSearch":
					D1MiniServer.startSearch(client.id);
					if(wpDebug.debugWebSockets)
						wpDebug.Write(MethodInfo.GetCurrentMethod(), $"WebSockets Server question: startD1MiniSearch");
					break;
				default:
					wpDebug.Write(MethodInfo.GetCurrentMethod(), $"WebSockets Server Type not found");
					break;
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
					wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Client {client.id}: Datapoint not found: {dp}");
				}
			}
			ws.SendAsync(client.id, "{\"response\":\"addDatapoints\",\"data\":[" + msg + "]}");
			if(wpDebug.debugWebSockets)
				wpDebug.Write(MethodInfo.GetCurrentMethod(), $"addDatapoints {client.id} message: {msg}");
		}
		private void getRegistered(wpTcpClient client) {
			ws.SendAsync(client.id, "{\"response\":\"getRegistered\",\"data\":[" + client.getDatapoints() + "]}");
		}

		public void sendText(wpTcpClient client, string text) {
			try {
				client.message += text;
			} catch(Exception ex) {
				eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
			}
		}
		public void sendText(Guid id, string response, string msg) {
			string answer = $"{{\"response\":\"{response}\",\"data\":{msg}}}";
			ws.SendAsync(id, answer);
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
							wpDebug.WriteError(MethodInfo.GetCurrentMethod(), ex);
						}
					}
					Monitor.Exit(WatsonClients);
				} else {
					wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Angefordertes Item not Entered: WebSockets.Client, DP: '{DP.Name} ({DP.ID})'");
				}
			} catch(Exception ex) {
				wpDebug.WriteError(MethodInfo.GetCurrentMethod(), ex);
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
				if(!myDatapoints.Contains(Datapoints)) myDatapoints.Add(Datapoints);
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
