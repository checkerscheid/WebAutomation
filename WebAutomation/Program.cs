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
//# Revision     : $Rev:: 196                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: Program.cs 196 2025-03-30 13:06:32Z                      $ #
//#                                                                                 #
//###################################################################################
using FreakaZone.Libraries.wpEventLog;
using FreakaZone.Libraries.wpIniFile;
using FreakaZone.Libraries.wpLicense;
using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
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
		public const string subversion = "194";
		private static Debug debug;
		/// <summary>
		/// Der Haupteinstiegspunkt für die Anwendung.
		/// </summary>
		[STAThread]
		static void Main(string[] args) {
			if (!IniFile.read()) {
				Application.Exit();
				return;
			}
			string lgA = wpLicense.getHardwareID(true);
			string lkA = wpLicense.getHardwareID(false);
			if (lgA != IniFile.get("License", "key") &&
				lkA != IniFile.get("License", "key") &&
				"KeyLessVersion" != IniFile.get("License", "key")) {
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
			Mutex m = new Mutex(true, Application.ProductName + IniFile.get("TCP", "Port"), out createdNew);
			if(createdNew) {
				try {
					debug = new Debug(Application.ProductName);
					Debug.Write(MethodInfo.GetCurrentMethod(), "START" +
						"\r\n####################################################################\r\n\r\n");
					MainProg = new WebAutomationServer(args);
					if (lgA == IniFile.get("License", "key")) {
						MainProg.LicenseAlarming = true;
						Debug.Write(MethodInfo.GetCurrentMethod(), "Lizenz für großes Alarming gefunden");
					}
					if ("KeyLessVersion" == IniFile.get("License", "key")) {
						Debug.Write(MethodInfo.GetCurrentMethod(), "!!! UNLIZENZIERTE High Availability Version !!!");
						//MainProg.LicenseAlarming = true;
						//PDebug.Write("Lizenz für großes Alarming gefunden");
					}
					Application.Run(MainProg);
					debug.Dispose();
				} finally {
					m.ReleaseMutex();
				}
			} else {
				MessageBox.Show(Application.ProductName + " wurde bereits gestartet",
					"kein doppelter Start",
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
			}
			Debug.Write(MethodInfo.GetCurrentMethod(), "Programm finished");
			if(System.Diagnostics.Trace.Listeners != null) {
				System.Diagnostics.Trace.Listeners.Clear();
			}
		}
	}
}
/** @} */
