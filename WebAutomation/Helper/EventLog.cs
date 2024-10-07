//###################################################################################
//#                                                                                 #
//#              (C) FreakaZone GmbH                                                #
//#              =======================                                            #
//#                                                                                 #
//###################################################################################
//#                                                                                 #
//# Author       : Christian Scheid                                                 #
//# Date         : 06.03.2013                                                       #
//#                                                                                 #
//# Revision     : $Rev:: 135                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: EventLog.cs 135 2024-10-07 21:18:50Z                     $ #
//#                                                                                 #
//###################################################################################
using System;
using System.Diagnostics;
using System.Windows.Forms;
/**
* @addtogroup WebAutomation
* @{
*/
namespace WebAutomation.Helper {
	/// <summary>
	/// 
	/// </summary>
	public class Logger: IDisposable {
		public const string ErrorString = "Message: {0}\r\nTrace:\r\n{1}";
		/// <summary></summary>
		private EventLog eventLog;
		/// <summary></summary>
		private int EventLogID;
		/// <summary></summary>
		private static int EventCounter = 0;
		/// <summary></summary>
		private short ProjectCategory = 1001;
		/// <summary></summary>
		private const int MaxLength = 31839;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="Source"></param>
		public Logger(int Source) {
			try {
				eventLog = new EventLog();
				eventLog.Source = wpEventLog.getSrc(Source);
				EventLogID = Source;
				eventLog.Log = wpEventLog.LogName;
				wpEventLog.exists[Source] = true;
			} catch(Exception) {
				if(wpEventLog.exists[Source]) {
					wpEventLog.exists[Source] = false;
					MessageBox.Show(String.Format("Zugriff verweigert beim erstellen des wpEventLog '{0}'",
						wpEventLog.getSrc(Source)));
				}
			}
			if(!Int16.TryParse(Ini.get("Log", "Category"), out ProjectCategory)) {
				ProjectCategory = 1001;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		public void Dispose() {
			eventLog = null;
			GC.SuppressFinalize(this);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="Type"></param>
		/// <param name="Message"></param>
		public void Write(EventLogEntryType Type, string Message) {
			string shortMessage = Message;
			if (Message.Length > MaxLength)
				shortMessage = Message.Substring(0, MaxLength);
			try {
				if (wpEventLog.exists[EventLogID]) {
					if (++EventCounter >= Int16.MaxValue) EventCounter = 0;
					eventLog.WriteEntry(shortMessage, Type, EventCounter, ProjectCategory);
					wpDebug.Write(Message);
				}
			} catch(Exception ex) {
				ex.ToString();
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="Type"></param>
		/// <param name="Message"></param>
		public void Write(EventLogEntryType Type, string Message, params object[] obj) {
			Write(Type, String.Format(Message, obj));
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="Message"></param>
		public void Write(string Message) {
			Write(EventLogEntryType.Information, Message);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="Message"></param>
		/// <param name="obj"></param>
		public void Write(string Message, params object[] obj) {
			Write(String.Format(Message, obj));
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="ex"></param>
		public void WriteError(Exception ex) {
			Write(EventLogEntryType.Error, String.Format(ErrorString, ex.Message, ex.StackTrace));
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="ex"></param>
		/// <param name="obj"></param>
		public void WriteError(Exception ex, params string[] obj) {
			string additional = "";
			for(int i = 0; i < obj.Length; i++) {
				additional += obj[i];
				if(i < obj.Length - 1) additional += ", ";
			}
			string newErrorString = ErrorString + "\r\n\r\n{2}";
			Write(EventLogEntryType.Error, newErrorString, ex.Message, ex.StackTrace, additional);
		}
	}
	public class wpDebug {

		public static bool debugSystem = false;
		public static bool debugSQL = false;
		public static bool debugOPC = false;
		public static bool debugTransferID = false;
		public static bool debugWatchdog = false;
		public static bool debugFactor = false;
		public static bool debugTrend = false;
		public static bool debugCalendar = false;
		public static bool debugOpcRouter = false;
		public static bool debugWebSockets = false;
		public static bool debugREST = false;
		public static bool debugShelly = false;
		public static bool debugD1Mini = false;
		public static bool debugMQTT = false;


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public static string getDebugJson() {
			string returns = "{";
			returns += $"\"debug\":{(debugSystem ? "true" : "false")},";
			returns += $"\"debugSQL\":{(debugSQL ? "true" : "false")},";
			returns += $"\"debugOPC\":{(debugOPC ? "true" : "false")},";
			returns += $"\"debugTransferID\":{(debugTransferID ? "true" : "false")},";
			returns += $"\"debugWatchdog\":{(debugWatchdog ? "true" : "false")},";
			returns += $"\"debugFactor\":{(debugFactor ? "true" : "false")},";
			returns += $"\"debugTrend\":{(debugTrend ? "true" : "false")},";
			returns += $"\"debugCalendar\":{(debugCalendar ? "true" : "false")},";
			returns += $"\"debugOpcRouter\":{(debugOpcRouter ? "true" : "false")},";
			returns += $"\"debugWebSockets\":{(debugWebSockets ? "true" : "false")},";
			returns += $"\"debugREST\":{(debugREST ? "true" : "false")},";
			returns += $"\"debugShelly\":{(debugShelly ? "true" : "false")},";
			returns += $"\"debugD1Mini\":{(debugD1Mini ? "true" : "false")},";
			returns += $"\"debugMQTT\":{(debugMQTT ? "true" : "false")}";
			return returns + "}";
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="debugArea"></param>
		/// <returns></returns>
		public static string changeDebug(string[] args) {
			string debugArea;
			if(args == null)
				debugArea = "";
			else
				debugArea = args[0];
			string returns = "{";
			switch(debugArea) {
				case "debugSystem":
					debugSystem = !debugSystem;
					Write($"success: Setting {debugArea} changed ({debugSystem})");
					returns += $"\"erg\":\"S_OK\",\"msg\":\"Setting {debugArea} changed ({debugSystem})\"";
					break;
				case "debugSQL":
					debugSQL = !debugSQL;
					Write($"success: Setting {debugArea} changed ({debugSQL})");
					returns += $"\"erg\":\"S_OK\",\"msg\":\"Setting {debugArea} changed ({debugSQL})\"";
					break;
				case "debugOPC":
					debugOPC = !debugOPC;
					Write($"success: Setting {debugArea} changed ({debugOPC})");
					returns += $"\"erg\":\"S_OK\",\"msg\":\"Setting {debugArea} changed ({debugOPC})\"";
					break;
				case "debugTransferID":
					debugTransferID = !debugTransferID;
					Write($"success: Setting {debugArea} changed ({debugTransferID})");
					returns += $"\"erg\":\"S_OK\",\"msg\":\"Setting {debugArea} changed ({debugTransferID})\"";
					break;
				case "debugWatchdog":
					debugWatchdog = !debugWatchdog;
					Write($"success: Setting {debugArea} changed ({debugWatchdog})");
					returns += $"\"erg\":\"S_OK\",\"msg\":\"Setting {debugArea} changed ({debugWatchdog})\"";
					break;
				case "debugFactor":
					debugFactor = !debugFactor;
					Write($"success: Setting {debugArea} changed ({debugFactor})");
					returns += $"\"erg\":\"S_OK\",\"msg\":\"Setting {debugArea} changed ({debugFactor})\"";
					break;
				case "debugTrend":
					debugTrend = !debugTrend;
					Write($"success: Setting {debugArea} changed ({debugTrend})");
					returns += $"\"erg\":\"S_OK\",\"msg\":\"Setting {debugArea} changed ({debugTrend})\"";
					break;
				case "debugCalendar":
					debugCalendar = !debugCalendar;
					Write($"success: Setting {debugArea} changed ({debugCalendar})");
					returns += $"\"erg\":\"S_OK\",\"msg\":\"Setting {debugArea} changed ({debugCalendar})\"";
					break;
				case "debugOpcRouter":
					debugOpcRouter = !debugOpcRouter;
					Write($"success: Setting {debugArea} changed ({debugOpcRouter})");
					returns += $"\"erg\":\"S_OK\",\"msg\":\"Setting {debugArea} changed ({debugOpcRouter})\"";
					break;
				case "debugWebSockets":
					debugWebSockets = !debugWebSockets;
					Write($"success: Setting {debugArea} changed ({debugWebSockets})");
					returns += $"\"erg\":\"S_OK\",\"msg\":\"Setting {debugArea} changed ({debugWebSockets})\"";
					break;
				case "debugREST":
					debugREST = !debugREST;
					Write($"success: Setting {debugArea} changed ({debugREST})");
					returns += $"\"erg\":\"S_OK\",\"msg\":\"Setting {debugArea} changed ({debugREST})\"";
					break;
				case "debugShelly":
					debugShelly = !debugShelly;
					Write($"success: Setting {debugArea} changed ({debugShelly})");
					returns += $"\"erg\":\"S_OK\",\"msg\":\"Setting {debugArea} changed ({debugShelly})\"";
					break;
				case "debugD1Mini":
					debugD1Mini = !debugD1Mini;
					Write($"success: Setting {debugArea} changed ({debugD1Mini})");
					returns += $"\"erg\":\"S_OK\",\"msg\":\"Setting {debugArea} changed ({debugD1Mini})\"";
					break;
				case "debugMQTT":
					debugMQTT = !debugMQTT;
					Write($"success: Setting {debugArea} changed ({debugMQTT})");
					returns += $"\"erg\":\"S_OK\",\"msg\":\"Setting {debugArea} changed ({debugMQTT})\"";
					break;
				default:
					returns += "\"erg\":\"S_ERROR\",\"msg\":\"undefined command\"";
					break;
			}
			return returns + "}";
		}
		public static void setDebugs(string[] args) {
			if(Arrays.inArray("debugSystem".ToLower(), args)) {
				debugSystem = true;
				Write("{0} Server im 'Debug Modus' gestartet", Application.ProductName);
			}
			if(Arrays.inArray("debugSQL".ToLower(), args)) {
				debugSQL = true;
				Write("{0} Server im 'Debug SQL Modus' gestartet", Application.ProductName);
			}
			if(Arrays.inArray("debugOPC".ToLower(), args)) {
				debugOPC = true;
				Write("{0} Server im 'Debug OPC Modus' gestartet", Application.ProductName);
			}
			if(Arrays.inArray("debugTransferID".ToLower(), args)) {
				debugTransferID = true;
				Write("{0} Server im 'Debug Transfer ID Modus' gestartet", Application.ProductName);
			}
			if(Arrays.inArray("debugWatchdog".ToLower(), args)) {
				debugWatchdog = true;
				Write("{0} Server im 'Debug Watchdog Modus' gestartet", Application.ProductName);
			}
			if(Arrays.inArray("debugFactor".ToLower(), args)) {
				debugFactor = true;
				Write("{0} Server im 'Debug Factor Modus' gestartet", Application.ProductName);
			}
			if(Arrays.inArray("debugTrend".ToLower(), args)) {
				debugTrend = true;
				Write("{0} Server im 'Debug Trend Modus' gestartet", Application.ProductName);
			}
			if(Arrays.inArray("debugCalendar".ToLower(), args)) {
				debugCalendar = true;
				Write("{0} Server im 'Debug Calendar Modus' gestartet", Application.ProductName);
			}
			if(Arrays.inArray("debugOpcRouter".ToLower(), args)) {
				debugOpcRouter = true;
				Write("{0} Server im 'Debug OPC Router Modus' gestartet", Application.ProductName);
			}
			if(Arrays.inArray("debugWebSocket".ToLower(), args)) {
				debugWebSockets = true;
				Write("{0} Server im 'Debug WebSockets Modus' gestartet", Application.ProductName);
			}
			if(Arrays.inArray("debugREST".ToLower(), args)) {
				debugREST = true;
				Write("{0} Server im 'Debug REST Modus' gestartet", Application.ProductName);
			}
			if(Arrays.inArray("debugShelly".ToLower(), args)) {
				debugShelly = true;
				Write("{0} Server im 'Debug Shelly Modus' gestartet", Application.ProductName);
			}
			if(Arrays.inArray("debugD1Mini".ToLower(), args)) {
				debugD1Mini = true;
				Write("{0} Server im 'Debug D1Mini Modus' gestartet", Application.ProductName);
			}
			if(Arrays.inArray("debugMQTT".ToLower(), args)) {
				debugMQTT = true;
				Write("{0} Server im 'Debug MQTT Modus' gestartet", Application.ProductName);
			}
		}
		public static void Write(string msg) {
			Debug.AutoFlush = true;
			string dmsg = String.Format("{0:dd.MM.yy HH:mm:ss.fff} - {1}", DateTime.Now, msg);
			Debug.WriteLine(dmsg);
			if(Program.MainProg != null)
				Program.MainProg.Message = dmsg;
		}
		public static void Write(string msg, params object[] args) {
			wpDebug.Write(String.Format(msg, args));
		}
		public static void WriteError(Exception ex, params string[] obj) {
			string additional = "";
			for(int i = 0; i < obj.Length; i++) {
				additional += "'" + obj[i] + "'";
				if(i < obj.Length - 1)
					additional += ", ";
			}
			string newErrorString = Logger.ErrorString + "\r\n\r\n{2}";
			Write(newErrorString, ex.Message, ex.StackTrace, additional);
		}
	}
}
/** @} */
