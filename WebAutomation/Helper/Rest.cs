//###################################################################################
//#                                                                                 #
//#              (C) FreakaZone GmbH                                                #
//#              =======================                                            #
//#                                                                                 #
//###################################################################################
//#                                                                                 #
//# Author       : Christian Scheid                                                 #
//# Date         : 03.07.2024                                                       #
//#                                                                                 #
//# Revision     : $Rev:: 114                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: Shelly.cs 114 2024-06-30 18:19:57Z                       $ #
//#                                                                                 #
//###################################################################################
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace WebAutomation.Helper {
	public class RestServer {
		/// <summary></summary>
		private Logger eventLog;
		private TcpListener RestServerListener;
		private Thread RestServerThread;
		private UTF8Encoding encoder = new UTF8Encoding();
		private bool _isFinished;
		private const int threadTimeOut = 500;
		public RestServer() {
			wpDebug.Write("Rest Server init");
			_isFinished = false;
			eventLog = new Logger(wpEventLog.Rest);
			RestServerListener = new TcpListener(IPAddress.Any, Ini.getInt("RestServer", "Port"));
			RestServerThread = new Thread(new ThreadStart(RestServer_Listen));
			RestServerThread.Name = "RestServer";
			RestServerThread.Start();
			wpDebug.Write("Rest Server gestartet");
		}
		public void Stop() {
			_isFinished = true;
			if(RestServerListener != null) {
				RestServerListener.Stop();
			}
			RestServerListener = null;
			if(RestServerThread != null)
				RestServerThread.Join(threadTimeOut * 2);
			(RestServerThread = null;
			eventLog.Write("Shelly Server gestoppt");
		}
		private void RestServer_Listen() {
			try {
				RestServerListener.Start();
				eventLog.Write("Shelly Server started");
				do {
					if(!RestServerListener.Pending()) {
						Thread.Sleep(threadTimeOut);
						continue;
					}
					TcpClient RestClient = RestServerListener.AcceptTcpClient();
					Thread ClientThread = new Thread(new ParameterizedThreadStart(RestServer_HandleClient));
					ClientThread.Name = "RestServerHandleClient";
					ClientThread.Start(RestClient);
				} while(!_isFinished);
			} catch(Exception ex) {
				eventLog.WriteError(ex);
			}
		}
		private void RestServer_HandleClient(object client) {
			TcpClient tcpClient = (TcpClient)client;
			if(wpDebug.debugShelly)
				wpDebug.Write(String.Format("Neue Rest aktion: {0}", tcpClient.Client.RemoteEndPoint));
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
				bool cmdfound = false;
				bool macok = false;
				string mac;
				foreach(Match m in Regex.Matches(s_message, @"r\=([0-9ABCDEFabcdef]*)&s\=(true|false)")) {
					if(m.Success) { // found Shelly State
						mac = m.Groups[1].Value.ToLower();
						bool state = m.Groups[2].Value == "true" ? true : false;
						macok = ShellyServer.SetState(mac, state);
						cmdfound = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"r\=([0-9ABCDEFabcdef]*)&state\=(open|close)&lux\=([0-9.]*)&temp\=([0-9.]*)")) {
					if(m.Success) { // found Shelly Door
						mac = m.Groups[1].Value.ToLower();
						bool state = m.Groups[2].Value == "open" ? true : false;
						string temp = m.Groups[3].Value;
						string ldr = m.Groups[4].Value;
						macok = ShellyServer.SetDoor(mac, state, temp, ldr);
						cmdfound = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"r\=([0-9ABCDEFabcdef]*)&hum\=([0-9.]*)&temp\=([0-9.]*)")) {
					if(m.Success) { // found Shelly hum & temp
						mac = m.Groups[1].Value.ToLower();
						string hum = m.Groups[2].Value;
						string temp = m.Groups[3].Value;
						macok = ShellyServer.SetHumTemp(mac, hum, temp);
						cmdfound = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"r\=([0-9ABCDEFabcdef]*)&temp\=([0-9.]*)")) {
					if(m.Success) { // found Shelly temp
						mac = m.Groups[1].Value.ToLower();
						string temp = m.Groups[3].Value;
						macok = ShellyServer.SetTemp(mac, temp);
						cmdfound = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"r\=([0-9ABCDEFabcdef]*)&hum\=([0-9.]*)")) {
					if(m.Success) { // found Shelly hum
						mac = m.Groups[1].Value.ToLower();
						string hum = m.Groups[2].Value;
						macok = ShellyServer.SetHum(mac, hum);
						cmdfound = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"r\=([0-9ABCDEFabcdef]*)&a\=lp")) {
					if(m.Success) { // found Shelly action: longpress
						mac = m.Groups[1].Value.ToLower();
						macok = ShellyServer.SetLongPress(mac);
						cmdfound = true;
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
				if(!cmdfound)
					eventLog.Write("RestServer Message not found: {0}", s_message);
				byte[] answer = encoder.GetBytes($"HTTP/1.0 200 OK\r\nContent-Type: application/json\r\n\r\n{{\"Message\":\"{(cmdfound ? "S_OK" : "S_ERROR")}\",\"MAC\":\"{(macok ? "S_OK" : "S_ERROR")}\"}}");
				clientStream.Write(answer, 0, answer.Length);
				clientStream.Flush();
				clientStream.Close();
			} catch(Exception ex) {
				eventLog.WriteError(ex);
			} finally {
				tcpClient.Close();
			}
		}
	}
}
