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
//# Revision     : $Rev:: 76                                                      $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: EventLog.cs 76 2024-01-24 07:36:57Z                      $ #
//#                                                                                 #
//###################################################################################
using System;
using System.Diagnostics;
using System.Text;
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
		private System.Diagnostics.EventLog eventLog;
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
				eventLog = new System.Diagnostics.EventLog();
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
				additional += obj[i];
				if(i < obj.Length - 1)
					additional += ", ";
			}
			string newErrorString = Logger.ErrorString + "\r\n\r\n{2}";
			Write(newErrorString, ex.Message, ex.StackTrace, additional);
		}
	}
}
/** @} */
