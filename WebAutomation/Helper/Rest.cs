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
//# Revision     : $Rev:: 126                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: Rest.cs 126 2024-07-09 22:53:08Z                         $ #
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
		public void finished() {
			_isFinished = true;
			if(RestServerListener != null) {
				RestServerListener.Stop();
			}
			RestServerListener = null;
			if(RestServerThread != null)
				RestServerThread.Join(threadTimeOut * 2);
			RestServerThread = null;
			eventLog.Write("Rest Server gestoppt");
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
			if(wpDebug.debugREST)
				wpDebug.Write(String.Format("Neue Rest aktion: {0}", tcpClient.Client.RemoteEndPoint));
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
				if(wpDebug.debugREST)
					wpDebug.Write(String.Format("Rest message: {0}", s_message));
				foreach(Match m in Regex.Matches(s_message, @"^GET /\?r\=([0-9ABCDEFabcdef]*)&s\=(true|false)")) {
					if(m.Success) { // found Shelly State
						mac = m.Groups[1].Value.ToLower();
						bool state = m.Groups[2].Value == "true" ? true : false;
						macok = ShellyServer.SetState(mac, state);
						cmdfound = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"^GET /\?r\=([0-9ABCDEFabcdef]*)&state\=(open|close)&lux\=([0-9.]*)&temp\=([0-9.]*)")) {
					if(m.Success) { // found Shelly Window
						mac = m.Groups[1].Value.ToLower();
						bool state = m.Groups[2].Value == "open" ? true : false;
						string temp = m.Groups[3].Value;
						string ldr = m.Groups[4].Value;
						macok = ShellyServer.SetWindow(mac, state, temp, ldr);
						cmdfound = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"^GET /\?r\=([0-9ABCDEFabcdef]*)&door\=(open|close)")) {
					if(m.Success) { // found Shelly Door
						mac = m.Groups[1].Value.ToLower();
						bool door = m.Groups[2].Value == "open" ? true : false;
						macok = ShellyServer.SetWindow(mac, door);
						cmdfound = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"^GET /\?r\=([0-9ABCDEFabcdef]*)&hum\=([0-9.]*)&temp\=([0-9.]*)")) {
					if(m.Success) { // found Shelly hum & temp
						mac = m.Groups[1].Value.ToLower();
						string hum = m.Groups[2].Value;
						string temp = m.Groups[3].Value;
						macok = ShellyServer.SetHumTemp(mac, hum, temp);
						cmdfound = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"^GET /\?r\=([0-9ABCDEFabcdef]*)&temp\=([0-9.]*)")) {
					if(m.Success) { // found Shelly temp
						mac = m.Groups[1].Value.ToLower();
						string temp = m.Groups[2].Value;
						macok = ShellyServer.SetTemp(mac, temp);
						cmdfound = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"^GET /\?r\=([0-9ABCDEFabcdef]*)&hum\=([0-9.]*)")) {
					if(m.Success) { // found Shelly hum
						mac = m.Groups[1].Value.ToLower();
						string hum = m.Groups[2].Value;
						macok = ShellyServer.SetHum(mac, hum);
						cmdfound = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"^GET /\?r\=([0-9ABCDEFabcdef]*)&a\=lp")) {
					if(m.Success) { // found Shelly action: longpress
						mac = m.Groups[1].Value.ToLower();
						macok = ShellyServer.SetLongPress(mac);
						cmdfound = true;
					}
				}
				// D1 Mini
				foreach(Match m in Regex.Matches(s_message, @"^GET /\?m\=([0-9ABCDEFabcdef]*)&bm\=(true|false)")) {
					if(m.Success) { // found D1Mini State
						mac = m.Groups[1].Value.ToLower();
						bool state = m.Groups[2].Value == "true" ? true : false;
						macok = D1MiniServer.SetBM(mac, state);
						cmdfound = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"^GET /\?m\=([0-9ABCDEFabcdef]*)&temp\=([0-9.]*)")) {
					if(m.Success) { // found D1Mini temp
						mac = m.Groups[1].Value.ToLower();
						string temp = m.Groups[3].Value;
						macok = D1MiniServer.SetTemp(mac, temp);
						cmdfound = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"^GET /\?m\=([0-9ABCDEFabcdef]*)&hum\=([0-9.]*)")) {
					if(m.Success) { // found D1Mini hum
						mac = m.Groups[1].Value.ToLower();
						string hum = m.Groups[2].Value;
						macok = D1MiniServer.SetHum(mac, hum);
						cmdfound = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"^GET /\?m\=([0-9ABCDEFabcdef]*)&ldr\=([0-9.]*)")) {
					if(m.Success) { // found D1Mini ldr
						mac = m.Groups[1].Value.ToLower();
						string ldr = m.Groups[2].Value;
						macok = D1MiniServer.SetLdr(mac, ldr);
						cmdfound = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"^GET /\?m\=([0-9ABCDEFabcdef]*)&light\=([0-9.]*)")) {
					if(m.Success) { // found D1Mini light
						mac = m.Groups[1].Value.ToLower();
						string light = m.Groups[2].Value;
						macok = D1MiniServer.SetLight(mac, light);
						cmdfound = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"^GET /\?m\=([0-9ABCDEFabcdef]*)&relais\=(true|false)")) {
					if(m.Success) { // found D1Mini Relais
						mac = m.Groups[1].Value.ToLower();
						bool relais = m.Groups[2].Value == "true" ? true : false;
						macok = D1MiniServer.SetRelais(mac, relais);
						cmdfound = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"^GET /\?m\=([0-9ABCDEFabcdef]*)&rain\=([0-9.]*)")) {
					if(m.Success) { // found D1Mini rain
						mac = m.Groups[1].Value.ToLower();
						string rain = m.Groups[2].Value;
						macok = D1MiniServer.SetRain(mac, rain);
						cmdfound = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"^GET /\?m\=([0-9ABCDEFabcdef]*)&moisture\=([0-9.]*)")) {
					if(m.Success) { // found D1Mini moisture
						mac = m.Groups[1].Value.ToLower();
						string moisture = m.Groups[2].Value;
						macok = D1MiniServer.SetMoisture(mac, moisture);
						cmdfound = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"^GET /\?m\=([0-9ABCDEFabcdef]*)&vol\=([0-9.]*)")) {
					if(m.Success) { // found D1Mini moisture
						mac = m.Groups[1].Value.ToLower();
						string volume = m.Groups[2].Value;
						macok = D1MiniServer.SetVolume(mac, volume);
						cmdfound = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"^GET /\?m\=([0-9ABCDEFabcdef]*)&window\=(true|false)")) {
					if(m.Success) { // found D1Mini Window
						mac = m.Groups[1].Value.ToLower();
						bool window = m.Groups[2].Value == "true" ? true : false;
						macok = D1MiniServer.SetWindow(mac, window);
						cmdfound = true;
					}
				}
				foreach(Match m in Regex.Matches(s_message, @"^GET /\?m\=([0-9ABCDEFabcdef]*)&rssi\=([-0-9]*)")) {
					if(m.Success) {
						mac = m.Groups[1].Value.ToLower();
						wpDebug.Write($"Found D1Mini [{mac}]: RSSI = {m.Groups[2].Value}");
						cmdfound = true;
					}
				}
				if(!cmdfound)
					eventLog.Write("RestServer Message not found: {0}", s_message);
				if(!macok)
					eventLog.Write("RestServer MAC not found: {0}", s_message);
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
