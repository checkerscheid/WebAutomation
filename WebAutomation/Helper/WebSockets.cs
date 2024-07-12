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
//# Revision     : $Rev:: 130                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: WebSockets.cs 130 2024-07-12 13:17:54Z                   $ #
//#                                                                                 #
//###################################################################################
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
			wpDebug.Write("WebSockets Server Init");
			string name = Ini.get("Websockets", "Name");
			int port = Ini.getInt("Websockets", "Port");
			try {
				eventLog = new Logger(wpEventLog.WebSockets);

				WatsonClients = new Dictionary<Guid, wpTcpClient>();
				ws = new WatsonWsServer(name, port, false);
				ws.ClientConnected += Ws_ClientConnected;
				ws.ClientDisconnected += Ws_ClientDisconnected;
				ws.MessageReceived += Ws_MessageReceived;
				ws.Start();
				eventLog.Write($"WebSockets Server '{name}' gestartet, auf Port {port} gemappt");
			} catch(Exception ex) {
				wpDebug.WriteError(ex);
			}
		}
		public void finished() {
			if(ws != null && ws.IsListening)
				ws.Stop();
		}
		private void Ws_MessageReceived(object sender, MessageReceivedEventArgs e) {
			string s = Encoding.UTF8.GetString(e.Data.Array);
			if(Regex.IsMatch(s, "^PING", RegexOptions.IgnoreCase)) {
				ws.SendAsync(e.Client.Guid, "PONG");
			} else {
				dynamic stuff = JsonConvert.DeserializeObject(s);
				executeCommand(WatsonClients[e.Client.Guid], stuff);
			}
			if(wpDebug.debugWebSockets)
				wpDebug.Write($"WebSockets Server Message received from {e.Client} : {s}");
		}

		private void Ws_ClientConnected(object sender, ConnectionEventArgs e) {
			WatsonClients.Add(e.Client.Guid, new wpTcpClient(e.Client.Guid));
			wpDebug.Write($"WebSockets Server Client connected: {e.Client.Guid}");
		}

		private void Ws_ClientDisconnected(object sender, DisconnectionEventArgs e) {
			WatsonClients.Remove(e.Client.Guid);
			wpDebug.Write($"WebSockets Server Client disconnected: {e.Client.Guid}");
		}
		private void executeCommand(wpTcpClient client, dynamic cmd) {
			switch(cmd.command.ToString()) {
				case "addDatapoints":
					addDatapoints(client, cmd.data);
					wpDebug.Write($"WebSockets Server command: addDatapoints");
					if(wpDebug.debugWebSockets)
						wpDebug.Write("data: {0}", cmd.data);
					break;
				case "getRegistered":
					getRegistered(client);
					wpDebug.Write($"WebSockets Server question: getRegistered");
					if(wpDebug.debugWebSockets)
						wpDebug.Write("qst: {0}", cmd);
					break;
				case "getD1MiniJson":
					ws.SendAsync(client.id,
						"{\"response\":\"getD1MiniJson\"," +
						"\"data\":{" +
							$"\"ip\":\"{cmd.data}\"," +
							"\"D1Mini\":" + D1MiniServer.getJsonStatus(cmd.data) +
						"}}");
					break;
				case "startD1MiniSearch":
					D1MiniServer.startSearch(client.id);
					break;
				default:
					wpDebug.Write($"WebSockets Server Type not found");
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
						eventLog.WriteError(ex);
					}
				} else {
					wpDebug.Write($"Client {client.id}: Datapoint not found: {dp}");
				}
			}
			ws.SendAsync(client.id, "{\"response\":\"addDatapoints\",\"data\":[" + msg + "]}");
			if(wpDebug.debugWebSockets)
				wpDebug.Write($"addDatapoints {client.id} message: {msg}");
		}
		private void getRegistered(wpTcpClient client) {
			ws.SendAsync(client.id, "{\"response\":\"getRegistered\",\"data\":[" + client.getDatapoints() + "]}");
		}

		public void sendText(wpTcpClient client, string text) {
			try {
				client.message += text;
			} catch(Exception ex) {
				eventLog.WriteError(ex);
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
							wpDebug.WriteError(ex);
						}
					}
					Monitor.Exit(WatsonClients);
				} else {
					wpDebug.Write($"Angefordertes Item not Entered: WebSockets.Client, DP: '{DP.Name} ({DP.ID})'");
				}
			} catch(Exception ex) {
				wpDebug.WriteError(ex);
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
