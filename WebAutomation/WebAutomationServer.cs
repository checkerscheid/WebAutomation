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
//# Revision     : $Rev:: 213                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: WebAutomationServer.cs 213 2025-05-15 14:50:57Z          $ #
//#                                                                                 #
//###################################################################################
using FreakaZone.Libraries.wpCommen;
using FreakaZone.Libraries.wpEventLog;
using FreakaZone.Libraries.wpIniFile;
using FreakaZone.Libraries.wpSQL;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebAutomation.Communication;
using WebAutomation.D1Mini;
using WebAutomation.Helper;
using WebAutomation.PlugIns;
using WebAutomation.Shelly;
using static FreakaZone.Libraries.wpEventLog.Logger;
using static WebAutomation.Helper.Email;
using static WebAutomation.Helper.wpServiceStatus;
using static WebAutomation.Helper.wpSystemStatus;

namespace WebAutomation {

	/// <summary>
	/// Represents the main server application for web automation, providing functionality for  communication, monitoring,
	/// and integration with various services and protocols.
	/// </summary>
	/// <remarks>The <see cref="WebAutomationServer"/> class is a Windows Forms application that serves as the 
	/// central hub for managing web communication, database interactions, and system monitoring.  It integrates with
	/// multiple components such as WebSockets, MQTT, OPC, and REST services,  and provides features like alarm handling,
	/// email notifications, and system status tracking.  This class is designed to be extensible and supports various
	/// modes of operation, including  maintenance mode and minimized startup. It also includes mechanisms for service
	/// monitoring  (e.g., Apache and MSSQL) and resource usage tracking (e.g., memory and processor status).</remarks>
	public partial class WebAutomationServer: Form {
		private Logger eventLog;
		public WebCom wpWebCom;
		public WebSockets wpWebSockets;
		public OPCClient wpOPCClient;
		public MQTTClient wpMQTTClient;
		public RestServer wpRest;
		public Watchdog wpWatchdog;
		public Sun wpSun;
		private FormWindowState lastState;
		public Calendars CalDav;
		private Email TheMail;
		private Thread ThreadEmailSender;
		private bool isFinished;
		private static bool _isInit;
		public static bool isInit { get { return _isInit; } }
		private bool RecipientRequired;

		private bool _wpWartung;
		public bool wpWartung {
			set { _wpWartung = value; }
			get { return _wpWartung; }
		}

		private bool _browseMqtt;
		public bool BrowseMqtt {
			set { _browseMqtt = value; }
			get { return _browseMqtt; }
		}

		private bool _wpBigProject;
		public bool wpBigProject {
			get { return _wpBigProject; }
		}

		private bool _wpStartMinimized;

		private bool _wpAllowCloseBrowser;
		public bool wpAllowCloseBrowser {
			get { return _wpAllowCloseBrowser; }
		}

		private bool _wpForceRead;
		public bool wpForceRead {
			get { return _wpForceRead; }
		}

		private bool _wpPSOPC;
		public bool wpPSOPC {
			get { return _wpPSOPC; }
		}

		private bool _LicenseAlarming;
		public bool LicenseAlarming {
			get { return _LicenseAlarming; }
			set { _LicenseAlarming = value; }
		}

		public delegate void StringChangedEventHandler(StringChangedEventArgs e);
		public class StringChangedEventArgs: EventArgs {
			public string newValue;
			public StringChangedEventArgs(string _newValue) {
				newValue = _newValue;
			}
		}
		public event StringChangedEventHandler StringChanged;
		public string lastchange {
			set {
				if(StringChanged != null)
					StringChanged(new StringChangedEventArgs($"{DateTime.Now.ToString()} {value}"));
			}
		}
		//public string Message {
		//	set {
		//		try {
		//			if(lbl_msg.InvokeRequired) {
		//				lbl_msg.Invoke(new MethodInvoker(() => Message = value));
		//			} else {
		//				string[] oldMessage = lbl_msg.Lines;
		//				List<string> newMessage = new List<string>();
		//				newMessage.Add(value);
		//				for(int i = 0; i < 100; i++) {
		//					if(oldMessage.Length >= i + 1)
		//						newMessage.Add(oldMessage[i]);
		//				}
		//				lbl_msg.Lines = newMessage.ToArray();
		//			}
		//		} catch(Exception) { };
		//	}
		//	get {  return lbl_msg.Text; }
		//}
		private System.Windows.Forms.Timer SystemTimer;
		private string ApacheName;
		private wpServiceStatus ApacheService;
		private string MssqlName;
		private wpServiceStatus MssqlService;
		private wpSystemStatus SystemStatus;
		private Dictionary<string, string> _SystemItems;
		public Dictionary<string, string> SystemItems {
			get { return this._SystemItems; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="WebAutomationServer"/> class.
		/// </summary>
		/// <remarks>This constructor initializes the server with the specified configuration options and sets up
		/// various components, including logging, debugging, and UI elements. The server's behavior is influenced by the
		/// provided command-line arguments. If no arguments are provided, default settings are applied. <para> The server's
		/// title and system tray icon are dynamically updated based on the project number retrieved from the configuration
		/// file. </para> <para> Debugging information, such as culture and number format settings, is logged during
		/// initialization. </para></remarks>
		/// <param name="args">An array of command-line arguments used to configure the server's behavior.  Supported arguments include: <list
		/// type="bullet"> <item><description><c>wpWartung</c>: Starts the server in maintenance mode.</description></item>
		/// <item><description><c>wpMinStart</c>: Starts the server in minimized mode.</description></item>
		/// <item><description><c>wpAllowCloseBrowser</c>: Allows the server to close the local browser.</description></item>
		/// <item><description><c>wpBigProject</c>: Enables the "Big Project" mode for handling larger
		/// projects.</description></item> <item><description><c>wpForceRead</c>: Forces the server to operate in "Force Read"
		/// mode.</description></item> <item><description><c>wpPSOPC</c>: Activates the "PSOPC (Analphabet)"
		/// mode.</description></item> </list></param>
		public WebAutomationServer(string[] args) {
			List<string> argList = args.ToList().ConvertAll(x => x.ToLower());
			InitializeComponent();
			Debug.SetRefString(lbl_msg);
			Common.Epsilon = Common.GetEpsilon();
			Logger.Fill();
			string[] pVersion = Application.ProductVersion.Split('.');
			this.toolStripStatusLabel1.Text = String.Format("{0} V {1}.{2} Build {3}, © {4}",
				Application.ProductName,
				pVersion[0], pVersion[1],
				Program.subversion,
				Application.CompanyName);
			this.Text += " - " + IniFile.Get("Projekt", "Nummer");
			this.SystemIcon.Text = Application.ProductName + " - " + IniFile.Get("Projekt", "Nummer");
#if DEBUG
			this.Text += " [Debug]";
#endif
			_isInit = false;
			eventLog = new Logger(Logger.ESource.WebAutomation);

			_wpWartung = false;
			_wpStartMinimized = false;
			_wpAllowCloseBrowser = false;
			_wpBigProject = false;
			_wpForceRead = false;
			_wpPSOPC = false;
			if(argList.Count > 0) {
				Debug.SetDebug(argList, Application.ProductName);
				if(argList.Contains("wpWartung".ToLower())) {
					_wpWartung = true;
					eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Warning,
						"{0} Server im 'Wartungsmodus' gestartet", Application.ProductName);
				}
				if(argList.Contains("wpMinStart".ToLower())) {
					_wpStartMinimized = true;
					eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Information,
						"{0} Server im 'minimiertem Modus' gestartet", Application.ProductName);
				}
				if(argList.Contains("wpAllowCloseBrowser".ToLower())) {
					_wpAllowCloseBrowser = true;
					eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Warning,
						"{0} Server darf den lokalen Browser schließen", Application.ProductName);
				}
				if(argList.Contains("wpBigProject".ToLower())) {
					_wpBigProject = true;
					eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Warning,
						"{0} Server im 'Big Project Modus' gestartet", Application.ProductName);
				}
				if(argList.Contains("wpForceRead".ToLower())) {
					_wpForceRead = true;
					eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Warning,
						"{0} Server im 'Force Read Modus' gestartet", Application.ProductName);
				}
				if(argList.Contains("wpPSOPC".ToLower())) {
					_wpPSOPC = true;
					eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Warning,
						"{0} Server im 'PSOPC (Analphabet) Modus' gestartet", Application.ProductName);
				}
			}
			Debug.Write(MethodInfo.GetCurrentMethod(), "CultureInfo: {0}", CultureInfo.CurrentUICulture.Name);
			Debug.Write(MethodInfo.GetCurrentMethod(), "System NumberDecimalSeparator: {0}", NumberFormatInfo.InvariantInfo.NumberDecimalSeparator);
			Debug.Write(MethodInfo.GetCurrentMethod(), "UI NumberDecimalSeparator: {0}", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator);
			this.lbl_db.Text = IniFile.Get("SQL", "Database");
			Debug.Write(MethodInfo.GetCurrentMethod(), "Connected Database: " + IniFile.Get("SQL", "Database"));
		}

		/// <summary>
		/// Initializes the application asynchronously by setting up database tables, initializing components,  and starting
		/// background services and timers.
		/// </summary>
		/// <remarks>This method performs a series of initialization tasks, including: <list type="bullet">
		/// <item><description>Ensuring required database tables and columns exist, creating them if
		/// necessary.</description></item> <item><description>Initializing various application components such as email
		/// handling, web communication, and data points.</description></item> <item><description>Starting background
		/// services, including MQTT, Shelly, and D1 Mini servers.</description></item> <item><description>Setting up system
		/// monitoring, including memory and processor status tracking.</description></item> </list> This method must be
		/// called before using other application features to ensure all dependencies are properly initialized.</remarks>
		/// <returns>A task that represents the asynchronous initialization operation.</returns>
		public async Task InitAsync() {
			await Task.Delay(100);

			// Update DB
			CheckTable("alarmhistoric", "text");
			CheckTable("opcdatapoint", "startuptype", "VARCHAR(10)");
			CheckTable("opcdatapoint", "startupquality");
			CheckTable("alarm", "link", "varchar(500)");
			CheckTable("email", "sms", "bit", false, "0");
			CheckTable("email", "phone2", "varchar(150)");
			CheckTable("user", "startpage", "varchar(100)");
			CheckTable("rest", "id_analogout", "int");

			CheckTable("webpages", "id_parent_webpage", "int");
			CheckTable("webpages", "position", "int");
			CheckTable("webpages", "id_src", "varchar(200)");
			CheckTable("webpages", "inwork", "bit", false, "0");


			using(Database Sql = new Database("startup")) {
				string[][] Tables = Sql.Query("SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE [TABLE_NAME] = 'emailhistoric'");
				if(Tables.Length == 0) {
					Sql.NonResponse(@"CREATE TABLE [emailhistoric]
					([email][varchar](150) NOT NULL,[send][datetime] NOT NULL,[subject][text] NOT NULL,[message][text] NOT NULL,[error][text] NULL)");
					Debug.Write(MethodInfo.GetCurrentMethod(), "Tabelle emailhistoric wurde erstellt");
				}
				Sql.NonResponse("UPDATE [opcdatapoint] SET [startuptype] = NULL, [startupquality] = NULL");
			}
			TheMail = new Email();
			wpWebCom = new WebCom();

			Datapoints.Init();
			Trends.Init();
			Alarms.Init();
			wpWebSockets = new WebSockets();
			wpMQTTClient = new MQTTClient();
			wpOPCClient = new OPCClient();
			Datapoints.Start();
			ShellyServer.Init();
			D1MiniServer.Init();
			wpRest = new RestServer();
			await wpMQTTClient.Start();
			ShellyServer.Start();
			D1MiniServer.Start();
			wpWatchdog = new Watchdog();
			CalDav = new Calendars();
			wpSun = new Sun();
			lastState = this.WindowState;
			isFinished = false;
			_isInit = true;
			eventLog.Write(MethodInfo.GetCurrentMethod(), "{0} Server initialisiert", Application.ProductName);

			ThreadEmailSender = new Thread(new ThreadStart(CreateEmail));
			ThreadEmailSender.Name = "wpEmail Sender";
			ThreadEmailSender.Start();

			_SystemItems = new Dictionary<string, string>();
			SystemTimer = new System.Windows.Forms.Timer();
			SystemTimer.Interval = 1000 * 10;
			SystemTimer.Tick += new EventHandler(SystemTimer_Tick);
			GetVolumeInfo();
			SystemTimer.Enabled = true;

			ApacheName = IniFile.Get("Watchdog", "ServiceNameApache");
			ApacheService = new wpServiceStatus(ApacheName);
			ApacheService.ServiceStatusChanged += ApacheService_ServiceStatusChanged;
			MssqlName = IniFile.Get("Watchdog", "ServiceNameMssql");
			MssqlService = new wpServiceStatus(MssqlName);
			MssqlService.ServiceStatusChanged += MssqlService_ServiceStatusChanged;
			SystemStatus = new wpSystemStatus();
			SystemStatus.MemoryStatusChanged += SystemStatus_MemoryStatusChanged;
			SystemStatus.ProzessorStatusChanged += SystemStatus_ProzessorStatusChanged;
		}

		/// <summary>
		/// Ensures that a specified column exists in a database table with the given properties.
		/// </summary>
		/// <remarks>If the specified column does not exist in the table, this method adds it with the specified type,
		/// nullability, and default value. If the column already exists, no changes are made.</remarks>
		/// <param name="table">The name of the table to check or modify. Cannot be null or empty.</param>
		/// <param name="column">The name of the column to check or add. Cannot be null or empty.</param>
		/// <param name="type">The data type of the column to add, if it does not exist. For example, "VARCHAR(50)" or "INT".</param>
		/// <param name="canBeNull">A value indicating whether the column can contain null values. If <see langword="false"/>, a default value will be
		/// applied.</param>
		/// <param name="defaultValue">The default value to assign to the column if <paramref name="canBeNull"/> is <see langword="false"/>. This value
		/// must be compatible with the specified <paramref name="type"/>.</param>
		private void CheckTable(string table, string column, string type, bool canBeNull, string defaultValue) {
			using(Database Sql = new Database("Check Database")) {
				string[][] DB = Sql.Query(@"SELECT [COLUMN_NAME] FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{0}' AND [COLUMN_NAME] = '{1}'", table, column);
				try {
					if(DB.Length == 0 || DB[0].Length == 0 || DB[0][0] != column) {
						Sql.NonResponse("ALTER TABLE [{0}] ADD [{1}] {2} {3}",
							table, column, type,
							canBeNull ? "NULL" : "NOT NULL CONSTRAINT [DF_" + table + "_" + column + "] DEFAULT(" + defaultValue + ")");
						Debug.Write(MethodInfo.GetCurrentMethod(), "Add [{1}] to [{0}]", table, column);
					}
				} catch(Exception ex) {
					eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
				}
			}
		}

		/// <summary>
		/// Ensures that a specified table and column exist in the database with the given type.
		/// </summary>
		/// <param name="table">The name of the table to check.</param>
		/// <param name="column">The name of the column to check within the table.</param>
		/// <param name="type">The expected data type of the column.</param>
		private void CheckTable(string table, string column, string type) {
			CheckTable(table, column, type, true, "");
		}

		/// <summary>
		/// Ensures that the specified table contains the specified column with the default configuration.
		/// </summary>
		/// <remarks>This method verifies the existence of the specified column in the given table, using a default
		/// data type and configuration. For more control over the column's properties, use the overloaded method that accepts
		/// additional parameters.</remarks>
		/// <param name="table">The name of the table to check. Cannot be null or empty.</param>
		/// <param name="column">The name of the column to check within the table. Cannot be null or empty.</param>
		private void CheckTable(string table, string column) {
			CheckTable(table, column, "VARCHAR(100)", true, "");
		}

		/// <summary>
		/// Attempts to establish a connection to the database, retrying a specified number of times if the initial connection
		/// fails.
		/// </summary>
		/// <remarks>The method reads the maximum number of retry attempts and the delay between retries (in seconds)
		/// from the configuration file. If the connection cannot be established after the specified number of retries, an
		/// error message is displayed, and the method returns <see langword="false"/>.</remarks>
		/// <returns><see langword="true"/> if the database connection is successfully established; otherwise, <see langword="false"/>.</returns>
		public bool TryConnectDatabase() {
			Database Sql;
			int SQLCounter = 0;
			int SQLCounterMax;
			int SQLCounterTime;
			if(Int32.TryParse(IniFile.Get("SQL", "reconnect"), out SQLCounterMax) &&
				Int32.TryParse(IniFile.Get("SQL", "reconnectTime"), out SQLCounterTime)) {
				do {
					Sql = new Database("Test SQL Connection");
					if(!Sql.Available) {
						eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Error,
							"{0} Server kann Datenbank nicht erreichen.\r\n\tReconnect nach {1} Sekunden\r\n\tVerbleibende Versuche: {2}",
							Application.ProductName, SQLCounterTime, SQLCounterMax - 1 - SQLCounter);
						SQLCounter++;
						Thread.Sleep(SQLCounterTime * 1000);
					}
				} while(Sql.Available == false && SQLCounter < SQLCounterMax);
				if(SQLCounter >= SQLCounterMax) {
					MessageBox.Show("Keine Verbindung zur Datenbank!\r\nDas Programm wird beendet",
						"Datenbankfehler",
						MessageBoxButtons.OK,
						MessageBoxIcon.Warning);
					return false;
				}
				return true;
			}
			return false;
		}

		/// <summary>
		/// Handles changes to the status of the Apache service and performs appropriate actions based on the new status.
		/// </summary>
		/// <remarks>This method responds to changes in the Apache service's status by logging the event and
		/// displaying notifications to the user. Notifications are shown as balloon tips with different icons and messages
		/// depending on the new status. The method also writes detailed log entries for each status change.  Supported
		/// statuses include: <list type="bullet"> <item><description><see cref="ServiceControllerStatus.Running"/>: Indicates
		/// the service is running.</description></item> <item><description><see cref="ServiceControllerStatus.Stopped"/>:
		/// Indicates the service has stopped.</description></item> <item><description><see
		/// cref="ServiceControllerStatus.StopPending"/>: Indicates the service is in the process of
		/// stopping.</description></item> <item><description><see cref="ServiceControllerStatus.StartPending"/>: Indicates
		/// the service is in the process of starting.</description></item> <item><description>Other statuses are treated as
		/// unknown and logged with a warning.</description></item> </list></remarks>
		/// <param name="e">An object containing information about the service status change, including the new status.</param>
		private void ApacheService_ServiceStatusChanged(ServiceStatusChangedEventArgs e) {
			ServiceControllerStatus s = (ServiceControllerStatus)e.newStatus;
			switch(s) {
				case ServiceControllerStatus.Running:
					eventLog.Write(MethodInfo.GetCurrentMethod(), "Apache Server Status changed: {0} ({1})", s, ApacheName);
					break;
				case ServiceControllerStatus.Stopped:
					SystemIcon.BalloonTipTitle = "Apache Webservice";
					SystemIcon.BalloonTipText = "Dienststatus 'Stopped'";
					SystemIcon.BalloonTipIcon = ToolTipIcon.Error;
					SystemIcon.ShowBalloonTip(1000);
					eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Error, "Apache Server Status changed: {0} ({1})", s, ApacheName);
					break;
				case ServiceControllerStatus.StopPending:
					SystemIcon.BalloonTipTitle = "Apache Webservice";
					SystemIcon.BalloonTipText = "Dienststatus 'StopPending'";
					SystemIcon.BalloonTipIcon = ToolTipIcon.Warning;
					SystemIcon.ShowBalloonTip(500);
					eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Error, "Apache Server Status changed: {0} ({1})", s, ApacheName);
					break;
				case ServiceControllerStatus.StartPending:
					SystemIcon.BalloonTipTitle = "Apache Webservice";
					SystemIcon.BalloonTipText = "Dienststatus 'StartPending'";
					SystemIcon.BalloonTipIcon = ToolTipIcon.Info;
					SystemIcon.ShowBalloonTip(500);
					eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Warning, "Apache Server Status changed: {0} ({1})", s, ApacheName);
					break;
				default:
					SystemIcon.BalloonTipTitle = "Apache Webservice";
					SystemIcon.BalloonTipText = "Dienststatus 'unbekannt'";
					SystemIcon.BalloonTipIcon = ToolTipIcon.Warning;
					SystemIcon.ShowBalloonTip(1000);
					eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Warning, "Apache Server Status changed: {0} ({1})", s, ApacheName);
					break;
			}
		}

		/// <summary>
		/// Handles the event triggered when the system's memory status changes.
		/// </summary>
		/// <remarks>This method updates the memory status display with the new memory value provided in the event
		/// arguments.</remarks>
		/// <param name="e">An instance of <see cref="SystemStatusChangedEventArgs"/> containing the updated memory status information.</param>
		private void SystemStatus_MemoryStatusChanged(SystemStatusChangedEventArgs e) {
			SetText(lbl_memory, String.Format("Speicher: {0} KB", e.newStatus));
		}

		/// <summary>
		/// Handles the event triggered when the processor status changes.
		/// </summary>
		/// <remarks>This method updates the processor status label to reflect the new status provided in the event
		/// arguments.</remarks>
		/// <param name="e">An instance of <see cref="SystemStatusChangedEventArgs"/> containing the updated processor status.</param>
		private void SystemStatus_ProzessorStatusChanged(SystemStatusChangedEventArgs e) {
			SetText(lbl_prozessor, String.Format("Prozessor: {0} %", e.newStatus));
		}

		/// <summary>
		/// Handles changes to the status of the MSSQL service and performs appropriate actions such as logging the status
		/// change and displaying notifications.
		/// </summary>
		/// <remarks>This method responds to changes in the MSSQL service status by logging the new status and
		/// displaying a notification to the user. The notification includes details about the service status, such as whether
		/// it is running, stopped, or in a transitional state (e.g., starting or stopping).</remarks>
		/// <param name="e">An instance of <see cref="ServiceStatusChangedEventArgs"/> containing information about the new service status.</param>
		private void MssqlService_ServiceStatusChanged(ServiceStatusChangedEventArgs e) {
			ServiceControllerStatus s = (ServiceControllerStatus)e.newStatus;
			switch(s) {
				case ServiceControllerStatus.Running:
					eventLog.Write(MethodInfo.GetCurrentMethod(), "Mssql Server Status changed: {0} ({1})", s, MssqlName);
					break;
				case ServiceControllerStatus.Stopped:
					SystemIcon.BalloonTipTitle = "Mssql Databaseservice";
					SystemIcon.BalloonTipText = "Dienststatus 'Stopped'";
					SystemIcon.BalloonTipIcon = ToolTipIcon.Error;
					SystemIcon.ShowBalloonTip(1000);
					eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Error, "Mssql Server Status changed: {0} ({1})", s, MssqlName);
					break;
				case ServiceControllerStatus.StopPending:
					SystemIcon.BalloonTipTitle = "Mssql Databaseservice";
					SystemIcon.BalloonTipText = "Dienststatus 'StopPending'";
					SystemIcon.BalloonTipIcon = ToolTipIcon.Warning;
					SystemIcon.ShowBalloonTip(500);
					eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Error, "Mssql Server Status changed: {0} ({1})", s, MssqlName);
					break;
				case ServiceControllerStatus.StartPending:
					SystemIcon.BalloonTipTitle = "Mssql Databaseservice";
					SystemIcon.BalloonTipText = "Dienststatus 'StartPending'";
					SystemIcon.BalloonTipIcon = ToolTipIcon.Info;
					SystemIcon.ShowBalloonTip(500);
					eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Warning, "Mssql Server Status changed: {0} ({1})", s, MssqlName);
					break;
				default:
					SystemIcon.BalloonTipTitle = "Mssql Databaseservice";
					SystemIcon.BalloonTipText = "Dienststatus 'unbekannt'";
					SystemIcon.BalloonTipIcon = ToolTipIcon.Warning;
					SystemIcon.ShowBalloonTip(1000);
					eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Warning, "Mssql Server Status changed: {0} ({1})", s, MssqlName);
					break;
			}
		}

		public delegate void ControlString(Control control, string text);

		/// <summary>
		/// Sets the text of the specified <see cref="Control"/> in a thread-safe manner.
		/// </summary>
		/// <remarks>If the calling thread is different from the thread that created the <see cref="Control"/>, this
		/// method uses <see cref="Control.Invoke"/> to perform the operation on the control's owning thread.</remarks>
		/// <param name="control">The <see cref="Control"/> whose text is to be set. Cannot be <see langword="null"/>.</param>
		/// <param name="text">The text to set on the specified <see cref="Control"/>. Can be <see langword="null"/> or empty.</param>
		public void SetText(Control control, string text) {
			try {
				if(control.InvokeRequired) {
					control.Invoke(new ControlString(SetText), new object[] { control, text });
				} else {
					control.Text = text;
				}
			} catch(Exception ex) {
				Debug.Write(MethodInfo.GetCurrentMethod(), ex.ToString());
			}
		}

		/// <summary>
		/// Handles the <see cref="Control.Enter"/> event for the label.
		/// </summary>
		/// <param name="sender">The source of the event, typically the label control.</param>
		/// <param name="e">An <see cref="EventArgs"/> instance containing the event data.</param>
		private void lbl_msg_Enter(object sender, EventArgs e) {
			nonsens.Focus();
		}


		#region alarm

		/// <summary>
		/// Adds a new alarm to the mail system, ensuring thread-safe access to the shared mail resource.
		/// </summary>
		/// <remarks>This method attempts to add the specified alarm to the mail system by acquiring a lock on the
		/// shared mail resource. If the lock cannot be acquired after 10 attempts, an error is logged, and the operation is
		/// aborted.</remarks>
		/// <param name="newAlarm">The alarm to be added to the mail system. Cannot be null.</param>
		public void AlarmToMail(Alarm newAlarm) {
			bool entered = false;
			int notEntered = 0;
			while(!entered && notEntered < 10) {
				if(Monitor.TryEnter(TheMail, 5000)) {
					try {
						TheMail.AddAlarm(newAlarm);
					} catch(Exception ex) {
						eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
					} finally {
						Monitor.Exit(TheMail);
						entered = true;
					}
				} else {
					if(++notEntered >= 10) {
						eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Error,
							String.Format(@"Angeforderter Alarm blockiert: {0}.\r\n
								AlarmToMail nicht möglich", newAlarm.IdAlarm));
					} else {
						Thread.Sleep(10);
					}
				}
			}
		}

		/// <summary>
		/// Attempts to add the specified alarm to the mail queue, retrying up to a maximum of 10 times if the operation is
		/// blocked.
		/// </summary>
		/// <remarks>This method uses a retry mechanism to handle potential contention when accessing the mail queue. 
		/// If the operation cannot be completed after 10 attempts, an error is logged.</remarks>
		/// <param name="newAlarm">The alarm to be added to the mail queue. Cannot be null.</param>
		public void QuitToMail(Alarm newAlarm) {
			bool entered = false;
			int notEntered = 0;
			while(!entered && notEntered < 10) {
				if(Monitor.TryEnter(TheMail, 5000)) {
					try {
						TheMail.AddQuit(newAlarm);
					} catch(Exception ex) {
						eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
					} finally {
						Monitor.Exit(TheMail);
						entered = true;
					}
				} else {
					if(++notEntered >= 10) {
						eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Error,
							String.Format(@"Angeforderter Alarm blockiert: {0}.\r\n
								QuitToMail nicht möglich", newAlarm.IdAlarm));
					} else {
						Thread.Sleep(10);
					}
				}
			}
		}

		/// <summary>
		/// Attempts to add a list of alarms to the mail queue, ensuring thread-safe access to the shared resource.
		/// </summary>
		/// <remarks>This method uses a locking mechanism to ensure thread-safe access to the mail queue.  If the lock
		/// cannot be acquired after 10 attempts, an error is logged for each alarm in the list.</remarks>
		/// <param name="newAlarm">A list of <see cref="Alarm"/> objects to be added to the mail queue.</param>
		public void QuitsToMail(List<Alarm> newAlarm) {
			bool entered = false;
			int notEntered = 0;
			while(!entered && notEntered < 10) {
				if(Monitor.TryEnter(TheMail, 5000)) {
					try {
						foreach(Alarm TheAlarm in newAlarm) {
							TheMail.AddQuit(TheAlarm);
						}
					} catch(Exception ex) {
						eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
					} finally {
						Monitor.Exit(TheMail);
						entered = true;
					}
				} else {
					if(++notEntered >= 10) {
						foreach(Alarm TheAlarm in newAlarm) {
							eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Error,
								String.Format(@"Angeforderter Alarm blockiert: {0}.\r\n
									QuitsToMail nicht möglich", TheAlarm.IdAlarm));
						}
					} else {
						Thread.Sleep(10);
					}
				}
			}
		}

		/// <summary>
		/// Retrieves an <see cref="Alarm"/> object by its unique identifier.
		/// </summary>
		/// <remarks>This method attempts to retrieve the alarm in a thread-safe manner by acquiring a lock on  the
		/// shared resource. If the lock cannot be acquired after multiple attempts, an error is logged.</remarks>
		/// <param name="id">The unique identifier of the alarm to retrieve.</param>
		/// <returns>The <see cref="Alarm"/> object associated with the specified <paramref name="id"/>,  or <see langword="null"/> if
		/// the alarm could not be retrieved.</returns>
		public Alarm GetAlarmFromAlarmid(int id) {
			Alarm returns = null;
			bool entered = false;
			int notEntered = 0;
			while(!entered && notEntered < 10) {
				if(Monitor.TryEnter(OpcDatapoints.Items, 5000)) {
					try {
						returns = Alarms.Get(id);
					} catch(Exception ex) {
						eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
					} finally {
						Monitor.Exit(OpcDatapoints.Items);
						entered = true;
					}
				} else {
					if(++notEntered >= 10) {
						eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Error,
							String.Format(@"Angefordertes Item blockiert: {0}.\r\n
								getAlarmFromAlarmid nicht möglich", id));
					} else {
						Thread.Sleep(10);
					}
				}
			}
			return returns;
		}

		/// <summary>
		/// Sends email notifications based on the current alarm state and recipient configuration.
		/// </summary>
		/// <remarks>This method processes a list of recipients and sends email or SMS notifications for alarms that
		/// meet the criteria. It handles both email and SMS notifications, logs the sent messages to a database, and retries
		/// in case of errors. The method runs in a loop until the operation is marked as finished.</remarks>
		public void CreateEmail() {
			Dictionary<string, PRecipient> recipient = RenewRecipient(out RecipientRequired);
			do {
				try {
					if(!_wpWartung) {
						if(RecipientRequired) {
							recipient.Clear();
							recipient = RenewRecipient(out RecipientRequired);
						}
						foreach(KeyValuePair<string, PRecipient> Alarmstosend in recipient) {
							if(Email.EmailAlarms.getTotalCount(Alarmstosend.Value) > 0) {
								string[] MailContent = new string[2];
								try {
									if(Alarmstosend.Value.IsSMS) {
										MailContent[1] = Application.ProductName;
										foreach(string sms in TheMail.getSMSText(Alarmstosend.Value)) {
											TheMail.setRecipient(Alarmstosend.Key);
											string subject = IniFile.Get("Projekt", "Nummer") + " " + sms;
											int max = 160 - TheMail.getFromLength() - 2;
											if(subject.Length > max)
												subject = subject.Substring(0, max);
											TheMail.setSubject(subject);
											MailContent[0] = subject;
											if(MailContent[0].Length > 0 && MailContent[1].Length > 0) {
												TheMail.send();
												using(Database Sql = new Database("Mail send")) {
													Sql.NonResponse(@"INSERT INTO [emailhistoric]
										([email], [send], [subject], [message]) VALUES
										('{0}', '{1}', '{2}', '{3}')",
													Alarmstosend.Value.Address,
													DateTime.Now.ToString(Database.DateTimeFormat),
													MailContent[0].Replace('\'', '"').Replace('\\', ' '),
													MailContent[1].Replace('\'', '"').Replace('\\', ' '));
												}
												Debug.Write(MethodInfo.GetCurrentMethod(), "Send Mail to {0}", Alarmstosend.Key);
											}
										}
									} else {
										TheMail.setRecipient(Alarmstosend.Key);
										MailContent = TheMail.setAlarmBody(Alarmstosend.Value);
										if(MailContent[0].Length > 0 && MailContent[1].Length > 0) {
											TheMail.send();
											using(Database Sql = new Database("Mail send")) {
												Sql.NonResponse(@"INSERT INTO [emailhistoric]
										([email], [send], [subject], [message]) VALUES
										('{0}', '{1}', '{2}', '{3}')",
												Alarmstosend.Value.Address,
												DateTime.Now.ToString(Database.DateTimeFormat),
												MailContent[0].Replace('\'', '"').Replace('\\', ' '),
												MailContent[1].Replace('\'', '"').Replace('\\', ' '));
											}
											Debug.Write(MethodInfo.GetCurrentMethod(), "Send Mail to {0}", Alarmstosend.Key);
										}
									}
								} catch(Exception ex) {
									using(Database Sql = new Database("Mail send")) {
										Sql.NonResponse(@"INSERT INTO [emailhistoric]
										([email], [send], [subject], [message], [error]) VALUES
										('{0}', '{1}', '{2}', '{3}', '{4}')",
										Alarmstosend.Value.Address,
										DateTime.Now.ToString(Database.DateTimeFormat),
										MailContent[0].Replace('\'', '"').Replace('\\', ' '),
										MailContent[1].Replace('\'', '"').Replace('\\', ' '),
										ex.Message);
									}
									eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Error,
										String.Format("SendMail Error: {0}\r\nTrace:\r\n{1}", ex.Message, ex.StackTrace));
								}
							}
						}
					}
				} catch(Exception ex) {
					eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Error,
						String.Format("SendMail Error: {0}\r\nTrace:\r\n{1}", ex.Message, ex.StackTrace));
				} finally {
					TheMail.reset();
				}
				Thread.Sleep(1000);
			} while(!isFinished);
			TheMail.Dispose();
		}

		/// <summary>
		/// Retrieves and renews the list of active recipients along with their associated alarm configurations.
		/// </summary>
		/// <remarks>This method queries the database to retrieve active recipients and their associated alarm
		/// configurations.  Only recipients with at least one alarm configuration are included in the returned dictionary. 
		/// The method logs the renewal process for auditing purposes.</remarks>
		/// <param name="required">An output parameter that indicates whether the renewal process was required.  This is always set to <see
		/// langword="false"/> in the current implementation.</param>
		/// <returns>A dictionary where the key is a string representing the recipient's full name and email address,  and the value is
		/// a <see cref="PRecipient"/> object containing the recipient's details and alarm configurations.</returns>
		private Dictionary<string, PRecipient> RenewRecipient(out bool required) {
			Dictionary<string, PRecipient> recipient = new Dictionary<string, PRecipient>();
			using(Database Sql = new Database("renew Recipient Table")) {
				string[][] Query = Sql.Query(@"SELECT
					([name] + ' ' + [lastname] + ' <' + [address] + '>'),
					[id_email], [sms], [ticketmail] FROM [email] WHERE [active] = 1");
				for(int j = 0; j < Query.Length; j++) {
					Dictionary<int, int> AlarmperUser = new Dictionary<int, int>();
					using(Database Sql2 = new Database("renew Recipient Table - Alarm per User")) {
						string[][] Alarme = Sql2.Query(@"SELECT [id_alarm], [minutes]
						FROM [alarmtoemail] WHERE [id_email] = {0}", Query[j][1]);
						for(int k = 0; k < Alarme.Length; k++) {
							int checker;
							int minutes;
							if(Int32.TryParse(Alarme[k][0], out checker) &&
								Int32.TryParse(Alarme[k][1], out minutes)) {
								if(AlarmperUser.ContainsKey(checker)) {
									AlarmperUser[checker] = minutes;
								} else {
									AlarmperUser.Add(checker, minutes);
								}
							}
						}
					}
					if(AlarmperUser.Count > 0) {
						recipient.Add(Query[j][0],
							new PRecipient(Int32.Parse(Query[j][1]), Query[j][0], Query[j][2] == "True", AlarmperUser));
					}
				}
			}
			required = false;
			eventLog.Write(MethodInfo.GetCurrentMethod(), "Recipient Table wurde erneuert.");
			return recipient;
		}

		/// <summary>
		/// Marks the recipient as required and returns a status indicating the operation's success.
		/// </summary>
		/// <returns>A string representing the status of the operation. Returns <see langword="S_OK"/> if the operation completes
		/// successfully.</returns>
		public string SetRecipientRequired() {
			RecipientRequired = true;
			return "S_OK";
		}

		#endregion

		#region system

		/// <summary>
		/// Handles the system timer's tick event and updates the volume information.
		/// </summary>
		/// <remarks>This method is triggered at regular intervals by the system timer to retrieve and update the
		/// current volume information. Ensure that the timer is properly configured and started to invoke this method as
		/// expected.</remarks>
		/// <param name="sender">The source of the event, typically the system timer.</param>
		/// <param name="e">An <see cref="EventArgs"/> instance containing the event data.</param>
		private void SystemTimer_Tick(object sender, EventArgs e) {
			GetVolumeInfo();
		}

		/// <summary>
		/// Updates the volume information display with details about all fixed drives on the system.
		/// </summary>
		/// <remarks>This method retrieves information about all fixed drives, including their total size, free space,
		/// and percentage of used space. The information is formatted and displayed in the associated label. If an error
		/// occurs while retrieving drive information, the error is logged.</remarks>
		private void GetVolumeInfo() {
			lbl_volumeinfo.Text = "";
			DriveInfo[] Drives = DriveInfo.GetDrives();

			List<string> lbl = new List<string>();
			foreach(DriveInfo d in Drives) {
				if(d.DriveType == DriveType.Fixed) {
					try {
						DriveInformation di = new DriveInformation(d.TotalSize, d.TotalFreeSpace);
						lbl.Add(String.Format("{0} - {1} GB / {2} GB ({3} % belegt)",
							d.Name, di.usedspace, di.totalspace, Math.Round(di.prozent, 1)));
					} catch(Exception ex) {
						eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
					}
				}
			}
			foreach(string s in lbl) {
				lbl_volumeinfo.Text += s + "\r\n";
			}

		}

		/// <summary>
		/// Represents information about a drive, including its total space, used space, and usage percentage.
		/// </summary>
		/// <remarks>This class provides basic details about a drive's storage, such as the total capacity, the amount
		/// of space used,  and the percentage of the drive that is currently utilized. The values are calculated in gigabytes
		/// (GB).</remarks>
		internal class DriveInformation {
			/// <summary></summary>
			public long totalspace;
			/// <summary></summary>
			public long usedspace;
			/// <summary></summary>
			public double prozent;

			/// <summary>
			/// Initializes a new instance of the <see cref="DriveInformation"/> class with the specified total and free space
			/// values.
			/// </summary>
			/// <remarks>The constructor calculates the total space, used space, and percentage of used space in
			/// gigabytes.</remarks>
			/// <param name="_totalspace">The total space of the drive, in bytes. Must be a non-negative value.</param>
			/// <param name="_freespace">The free space available on the drive, in bytes. Must be a non-negative value and less than or equal to <paramref
			/// name="_totalspace"/>.</param>
			public DriveInformation(long _totalspace, long _freespace) {
				totalspace = _totalspace / (1024 * 1024 * 1024);
				usedspace = (_totalspace - _freespace) / (1024 * 1024 * 1024);
				prozent = GetProzent(_totalspace, _freespace);
			}

			/// <summary>
			/// Calculates the percentage of used space based on the total space and free space provided.
			/// </summary>
			/// <param name="_totalspace">The total space, in bytes. Must be greater than zero.</param>
			/// <param name="_freespace">The free space, in bytes. Must be less than or equal to <paramref name="_totalspace"/>.</param>
			/// <returns>The percentage of used space as a <see cref="double"/>. The value will be between 0 and 100.</returns>
			private double GetProzent(long _totalspace, long _freespace) {
				long usedspace = _totalspace - _freespace;
				double returns = ((double)usedspace / (double)_totalspace) * 100;
				return returns;
			}
		}

		#endregion
	}
}
