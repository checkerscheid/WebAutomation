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
//# Revision     : $Rev:: 111                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: WebSockets.cs 111 2024-06-20 23:25:28Z                   $ #
//#                                                                                 #
//###################################################################################
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WebAutomation.Helper {
	public class WebSockets {
		/// <summary></summary>
		private Logger eventLog;
		private TcpListener WebSocketsListener;
		private Thread WebSocketsServer;
		private static int clientid = 0;
		private static Dictionary<int, wpTcpClient> Clients;
		private bool isFinished;
		private const int MonitorTimeout = 200;
		private int ThreadSleep = 250;
		public WebSockets() {
			init();
		}
		private void init() {
			isFinished = false;
			Clients = new Dictionary<int, wpTcpClient>();
			eventLog = new Logger(wpEventLog.Com);
			WebSocketsListener = new TcpListener(IPAddress.Any, Ini.getInt("Websockets", "Port"));
			WebSocketsServer = new Thread(new ThreadStart(TCP_Listener));
			WebSocketsServer.Name = "WebSocketsServer";
			WebSocketsServer.Start();
			eventLog.Write($"{WebSocketsServer.Name} auf Port {Ini.getInt("Websockets", "Port")} gemappt");
		}
		public void finished() {
			WebSocketsListener.Stop();
			WebSocketsListener = null;
			isFinished = true;
			WebSocketsServer.Join(ThreadSleep);
			eventLog.Write($"{WebSocketsServer.Name} gestoppt");
		}
		private void TCP_Listener() {
			try {
				WebSocketsListener.Start();
				eventLog.Write(String.Format($"{WebSocketsServer.Name} gestartet"));
				do {
					if(!WebSocketsListener.Pending()) {
						Thread.Sleep(ThreadSleep);
						continue;
					}
					wpTcpClient TCPclient = new wpTcpClient(clientid++, WebSocketsListener.AcceptTcpClient());
					Thread ClientThread = new Thread(new ParameterizedThreadStart(TCP_HandleClient));
					ClientThread.Name = "WebSocketsHandleClient";
					Clients.Add(TCPclient.id, TCPclient);
					wpDebug.Write($"{WebSocketsServer.Name} new client {TCPclient.id}?");
					ClientThread.Start(TCPclient);
				} while(!isFinished);
			} catch(Exception ex) {
				wpDebug.Write(ex.Message);
			}
		}
		private async void TCP_HandleClient(object client) {
			wpTcpClient tcpClient = (wpTcpClient)client;
			NetworkStream stream = tcpClient.tcpClient.GetStream();
			await Task.Run(() => {
				while(!tcpClient.isFinished) { //tcpClient is connected
					if(isFinished) { // Program exited
						stream.Close();
						tcpClient.tcpClient.Close();
						tcpClient.isFinished = true;
						Thread.CurrentThread.Join(ThreadSleep * 2);
						return;
					}
					// write to Client
					try {
						while(!stream.DataAvailable) {
							WriteToClient(stream, tcpClient);
							Task.Delay(ThreadSleep); //Prozessor 100%?
						}
					} catch(Exception ex) {
						wpDebug.Write("Write to Client {0}: {1}", tcpClient.id, ex.Message);
					}
					// read from Client
					try {
						if(stream.CanRead) ReadFromClient(stream, tcpClient);
					} catch(Exception ex) {
						wpDebug.Write("Read from Client {0}: {1}", tcpClient.id, ex.Message);
					}
					Task.Delay(ThreadSleep); //Prozessor 100%?
				}
			});
		}
		private void WriteToClient(NetworkStream stream, wpTcpClient tcpClient) {
			try {
				if(Monitor.TryEnter(tcpClient, MonitorTimeout)) {
					if(tcpClient.message != "") {
						try {
							byte[] answerbytes = WebsocketsProtokoll.GetFrameFromString("{\"data\":[" + tcpClient.message + "]}");
							if(stream.CanWrite) {
								stream.Write(answerbytes, 0, answerbytes.Length);
								if(wpDebug.debugWebSockets)
									wpDebug.Write("Server to {0}: {1}", tcpClient.id, tcpClient.message);
							} else {
								if(wpDebug.debugWebSockets)
									wpDebug.Write("Server to {0} failed: {1}", tcpClient.id, tcpClient.message);
							}
						} catch(Exception ex) {
							wpDebug.Write("Client {0}: {1}", tcpClient.id, ex.Message);
							Clients.Remove(tcpClient.id);
							stream.Close();
							tcpClient.tcpClient.Close();
						} finally {
							tcpClient.message = "";
						}
					}
				} else {
					wpDebug.Write("Clients blockiert: setDatapoint");
				}
			} finally {
				Monitor.Exit(tcpClient);
			}
		}
		private void ReadFromClient(NetworkStream stream, wpTcpClient tcpClient) {
			byte[] bytes = new byte[tcpClient.tcpClient.Available];
			stream.Read(bytes, 0, tcpClient.tcpClient.Available);
			string s = Encoding.UTF8.GetString(bytes);

			if(Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase)) {
				wpDebug.Write($"{WebSocketsServer.Name} Handshaking: new client {tcpClient.id}");
				handshake(tcpClient, s);
			} else {
				s = WebsocketsProtokoll.GetDecodedData(bytes);
				if(Regex.IsMatch(s, "^\u0003", RegexOptions.IgnoreCase)) {
					stream.Close();
					tcpClient.tcpClient.Close();
					tcpClient.isFinished = true;
					Thread.CurrentThread.Join(MonitorTimeout);
					wpDebug.Write($"{WebSocketsServer.Name} Client {tcpClient.id} Closed: {s}");
				} else if(Regex.IsMatch(s, "^PING", RegexOptions.IgnoreCase)) {
					byte[] answerbytes = WebsocketsProtokoll.GetFrameFromString("PONG");
					stream.Write(answerbytes, 0, answerbytes.Length);
				} else {
					if(wpDebug.debugWebSockets)
						wpDebug.Write("Client {0}: {1}", tcpClient.id, s);
					try {
						dynamic stuff = JsonConvert.DeserializeObject(s);
						executeJson(tcpClient, stuff);
					} catch(Exception ex) {
						wpDebug.Write("JSON not parseable: {0}", ex.Message);
					}
				}
			}
		}
		private void executeJson(wpTcpClient tcpClient, dynamic cmd) {
			switch(cmd.type.ToString()) {
				case "command":
					wpDebug.Write($"{WebSocketsServer.Name} Type: command");
					getCommand(tcpClient, cmd);
					break;
				case "question":
					wpDebug.Write($"{WebSocketsServer.Name} Type: question");
					getQuestion(tcpClient, cmd);
					break;
				default:
					wpDebug.Write($"{WebSocketsServer.Name} Type not found");
					break;
			}
		}
		private void getCommand(wpTcpClient tcpClient, dynamic cmd) {
			switch(cmd.command.ToString()) {
				case "addDatapoints":
					addDatapoints(tcpClient, cmd.data);
					wpDebug.Write($"{WebSocketsServer.Name} command: addDatapoints");
					if(wpDebug.debugWebSockets)
						wpDebug.Write("data: {0}", cmd.data);
					break;
				default:
					wpDebug.Write($"{WebSocketsServer.Name} command not found");
					if(wpDebug.debugWebSockets)
						wpDebug.Write("cmd: {0}", cmd);
					break;
			}
		}
		private void getQuestion(wpTcpClient tcpClient, dynamic cmd) {
			switch(cmd.question.ToString()) {
				case "getRegistered":
					getRegistered(tcpClient);
					wpDebug.Write($"{WebSocketsServer.Name} question: getRegistered");
					if(wpDebug.debugWebSockets)
						wpDebug.Write("qst: {0}", cmd);
					break;
				default:
					wpDebug.Write($"{WebSocketsServer.Name} question not found");
					if(wpDebug.debugWebSockets)
						wpDebug.Write("qst: {0}", cmd);
					break;
			}
		}
		private void addDatapoints(wpTcpClient client, dynamic datapoints) {
			client.Clear();
			bool first = true;
			string response = "";
			foreach(string dp in datapoints) {
				client.Add(dp);
				Datapoint datapoint = Datapoints.Get(dp);
				if(datapoint != null) {
					try {
						if(first) {
							first = false;
						} else {
							response += ",";
						}
						response += "{" +
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
			client.message = response;
			if(wpDebug.debugWebSockets)
				wpDebug.Write($"addDatapoints {client.id} message: {client.message}");
		}
		private void getRegistered(wpTcpClient client) {
			string answer = client.getDatapoints();
			byte[] answerbytes = WebsocketsProtokoll.GetFrameFromString(answer);
			client.tcpClient.GetStream().Write(answerbytes, 0, answerbytes.Length);
		}

		public void sendText(wpTcpClient client, string text) {
			try {
				client.message += text;
			} catch(Exception ex) {
				eventLog.WriteError(ex);
			}
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
				if(Monitor.TryEnter(Clients, MonitorTimeout)) {
					if(Clients.Count > 0) {
						try {
							foreach(KeyValuePair<int, wpTcpClient> entry in Clients) {
								if(entry.Value.hasDatapoint(DP.Name)) {
									entry.Value.message += String.IsNullOrEmpty(entry.Value.message) ? "" : ",";
									entry.Value.message += msg;
								}
							}
						} catch(Exception ex) {
							wpDebug.WriteError(ex);
						}
					}
					Monitor.Exit(Clients);
				} else {
					wpDebug.Write($"Angefordertes Item not Entered: WebSockets.Client, DP: '{DP.Name} ({DP.ID})'");
				}
			} catch(Exception ex) {
				wpDebug.WriteError(ex);
			}
		}
		private void handshake(wpTcpClient client, string s) {
			string swk = Regex.Match(s, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
			string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
			byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
			string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

			byte[] response = Encoding.UTF8.GetBytes(
				"HTTP/1.1 101 Switching Protocols\r\n" +
				"Connection: Upgrade\r\n" +
				"Upgrade: websocket\r\n" +
				"Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

			client.tcpClient.GetStream().Write(response, 0, response.Length);
		}
		public class wpTcpClient {
			public TcpClient tcpClient { get; set; }
			public int id { get; set; }
			public string message { get; set; }
			private List<string> myDatapoints;
			public bool isFinished = false;
			public wpTcpClient(int id, TcpClient tcpClient) {
				this.id = id;
				this.tcpClient = tcpClient;
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
		class WebsocketsProtokoll {
			public enum EOpcodeType {
				/* Denotes a continuation code */
				Fragment = 0,
				/* Denotes a text code */
				Text = 1,
				/* Denotes a binary code */
				Binary = 2,
				/* Denotes a closed connection */
				ClosedConnection = 8,
				/* Denotes a ping*/
				Ping = 9,
				/* Denotes a pong */
				Pong = 10
			}
			public static string GetDecodedData(byte[] buffer) {
				byte b = buffer[1];
				int dataLength = 0;
				int totalLength = 0;
				int keyIndex = 0;

				if(b - 128 <= 125) {
					dataLength = b - 128;
					keyIndex = 2;
					totalLength = dataLength + 6;
				}

				if(b - 128 == 126) {
					dataLength = BitConverter.ToInt16(new byte[] { buffer[3], buffer[2] }, 0);
					keyIndex = 4;
					totalLength = dataLength + 8;
				}

				if(b - 128 == 127) {
					dataLength = (int)BitConverter.ToInt64(new byte[] { buffer[9], buffer[8], buffer[7], buffer[6], buffer[5], buffer[4], buffer[3], buffer[2] }, 0);
					keyIndex = 10;
					totalLength = dataLength + 14;
				}

				byte[] key = new byte[] { buffer[keyIndex], buffer[keyIndex + 1], buffer[keyIndex + 2], buffer[keyIndex + 3] };

				int dataIndex = keyIndex + 4;
				int count = 0;
				for(int i = dataIndex; i < totalLength; i++) {
					buffer[i] = (byte)(buffer[i] ^ key[count % 4]);
					count++;
				}
				if(dataLength > 0)
					return Encoding.UTF8.GetString(buffer, dataIndex, dataLength);
				else
					return "";
			}
			public static byte[] GetFrameFromString(string Message, EOpcodeType Opcode = EOpcodeType.Text) {
				byte[] response;
				byte[] bytesRaw = Encoding.UTF8.GetBytes(Message);
				byte[] frame = new byte[10];

				int indexStartRawData = -1;
				int length = bytesRaw.Length;

				frame[0] = (byte)(128 + (int)Opcode);
				if(length <= 125) {
					frame[1] = (byte)length;
					indexStartRawData = 2;
				} else if(length >= 126 && length <= 65535) {
					frame[1] = (byte)126;
					frame[2] = (byte)((length >> 8) & 255);
					frame[3] = (byte)(length & 255);
					indexStartRawData = 4;
				} else {
					frame[1] = (byte)127;
					frame[2] = (byte)((length >> 56) & 255);
					frame[3] = (byte)((length >> 48) & 255);
					frame[4] = (byte)((length >> 40) & 255);
					frame[5] = (byte)((length >> 32) & 255);
					frame[6] = (byte)((length >> 24) & 255);
					frame[7] = (byte)((length >> 16) & 255);
					frame[8] = (byte)((length >> 8) & 255);
					frame[9] = (byte)(length & 255);

					indexStartRawData = 10;
				}

				response = new byte[indexStartRawData + length];

				int i, reponseIdx = 0;

				//Add the frame bytes to the reponse
				for(i = 0; i < indexStartRawData; i++) {
					response[reponseIdx] = frame[i];
					reponseIdx++;
				}

				//Add the data bytes to the response
				for(i = 0; i < length; i++) {
					response[reponseIdx] = bytesRaw[i];
					reponseIdx++;
				}

				return response;
			}
		}
	}
}
