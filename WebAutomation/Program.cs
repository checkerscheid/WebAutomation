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
//# Revision     : $Rev:: 234                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: Program.cs 234 2025-05-27 14:15:29Z                      $ #
//#                                                                                 #
//###################################################################################
using FreakaZone.Libraries.wpEventLog;
using FreakaZone.Libraries.wpIniFile;
using FreakaZone.Libraries.wpLicense;
using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace WebAutomation {

	/// <summary>
	/// Provides the main entry point for the application.
	/// </summary>
	/// <remarks>This class initializes the application, validates the license, and starts the main program logic.
	/// It ensures that only a single instance of the application is running at a time by using a mutex.</remarks>
	static class Program {
		public static WebAutomationServer MainProg;
		public static string myName;
		public const string subversion = "231";
		private static Debug debug;

		/// <summary>
		/// Serves as the entry point for the application.
		/// </summary>
		/// <remarks>This method initializes the application, verifies licensing, and ensures that only a single
		/// instance of the application is running. If the license validation fails, the application will display an error
		/// message and terminate.  If a valid license is detected, the main program logic is executed.</remarks>
		/// <param name="args">An array of command-line arguments passed to the application.</param>
		[STAThread]
		static void Main(string[] args) {
			if (!IniFile.Read(true)) {
				Application.Exit();
				return;
			}
			string lgA = License.GetHardwareID(true);
			string lkA = License.GetHardwareID(false);
			if (lgA != IniFile.Get("License", "key") &&
				lkA != IniFile.Get("License", "key") &&
				"KeyLessVersion" != IniFile.Get("License", "key")) {
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
			Mutex m = new Mutex(true, Application.ProductName + IniFile.Get("TCP", "Port"), out createdNew);
			if(createdNew) {
				try {
					debug = new Debug(Application.ProductName);
					Debug.Write(MethodInfo.GetCurrentMethod(), "START" +
						"\r\n####################################################################\r\n");
					MainProg = new WebAutomationServer(args);
					if (lgA == IniFile.Get("License", "key")) {
						MainProg.LicenseAlarming = true;
						Debug.Write(MethodInfo.GetCurrentMethod(), "Lizenz für großes Alarming gefunden");
					}
					if ("KeyLessVersion" == IniFile.Get("License", "key")) {
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
			Debug.Write(MethodInfo.GetCurrentMethod(), "Programm finished\r\n\r\n");
			if(System.Diagnostics.Trace.Listeners != null) {
				System.Diagnostics.Trace.Listeners.Clear();
			}
		}
	}
}
