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
//# Revision     : $Rev:: 111                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: Program.cs 111 2024-06-20 23:25:28Z                      $ #
//#                                                                                 #
//###################################################################################
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using WebAutomation.Helper;
/**
* @defgroup WebAutomation WebAutomation
* @{
*/
namespace WebAutomation {
	/// <summary>
	/// 
	/// </summary>
	static class Program {
		/// <summary></summary>
		public static WebAutomationServer MainProg;
		public static string myName;
		public const string subversion = "111";
		private static TextWriterTraceListener trtl;
		private static System.Timers.Timer tlog;
		/// <summary>
		/// Der Haupteinstiegspunkt für die Anwendung.
		/// </summary>
		[STAThread]
		static void Main(string[] args) {
			if (!Ini.read()) {
				Application.Exit();
				return;
			}
			string lgA = wpLicense.getHardwareID(true);
			string lkA = wpLicense.getHardwareID(false);
			if (lgA != Ini.get("License", "key") &&
				lkA != Ini.get("License", "key") &&
				"KeyLessVersion" != Ini.get("License", "key")) {
				MessageBox.Show("Keine gültige Lizenz!\r\nDas Programm wird beendet",
					"Lizenzierungsfehler",
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
				Application.Exit();
				return;
			}
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			myName = Application.ProductName;

			bool createdNew = false;
			Mutex m = new Mutex(true, Application.ProductName + Ini.get("TCP", "Port"), out createdNew);
			if(createdNew) {
				try {
#if DEBUG
					tlog = new System.Timers.Timer();
					tlog.AutoReset = true;
					tlog.Elapsed += renewLog;
					renewLog(null, null);
					wpDebug.Write("START" +
						"\r\n####################################################################\r\n\r\n");
#endif
					MainProg = new WebAutomationServer(args);
					if (lgA == Ini.get("License", "key")) {
						MainProg.LicenseAlarming = true;
						wpDebug.Write("Lizenz für großes Alarming gefunden");
					}
					if ("KeyLessVersion" == Ini.get("License", "key")) {
						wpDebug.Write("!!! UNLIZENZIERTE High Availability Version !!!");
						//MainProg.LicenseAlarming = true;
						//PDebug.Write("Lizenz für großes Alarming gefunden");
					}
					Application.Run(MainProg);
#if DEBUG
					tlog.Stop();
					tlog.Dispose();
#endif
				} finally {
					m.ReleaseMutex();
				}
			} else {
				MessageBox.Show(Application.ProductName + " wurde bereits gestartet",
					"kein doppelter Start",
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
			}
			wpDebug.Write("Programm finished");
#if DEBUG
			if(Debug.Listeners != null) Debug.Listeners.Remove(trtl);
#endif
		}

		private static void renewLog(object sender, System.Timers.ElapsedEventArgs e) {
#if DEBUG
			Debug.Listeners.Remove(trtl);
			if (!Directory.Exists("Log")) Directory.CreateDirectory("Log");
			DateTime now = DateTime.Now;
			trtl = new TextWriterTraceListener(String.Format("Log\\{0}_{1:yyyy_MM_dd}.log", Application.ProductName, now));
			Debug.Listeners.Add(trtl);

			DateTime today = new DateTime(now.Year, now.Month, now.Day);
			DateTime tonight = today.AddDays(1).AddSeconds(1);
			TimeSpan ts = tonight - now;

			tlog.Interval = ts.TotalMilliseconds;
			tlog.Enabled = true;

#endif
		}
	}
}
/** @} */
