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
//# Revision     : $Rev:: 118                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: WebAutomationServer.cs 118 2024-07-04 14:20:41Z          $ #
//#                                                                                 #
//###################################################################################
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebAutomation.Helper;
using WebAutomation.PlugIns;
using static WebAutomation.Helper.Email;
using static WebAutomation.Helper.wpServiceStatus;
using static WebAutomation.Helper.wpSystemStatus;
/**
* @addtogroup WebAutomation
* @{
*/
namespace WebAutomation {
	/// <summary>
	/// 
	/// </summary>
	public partial class WebAutomationServer : Form {
		/// <summary>WebAutomationServer Event Log</summary>
		private Logger eventLog;
		/// <summary>Plug In fuer Web Kommunikation</summary>
		public WebCom wpWebCom;
		/// <summary></summary>
		public WebSockets wpWebSockets;
		/// <summary></summary>
		public OPCClient wpOPCClient;
		/// <summary></summary>
		public MQTTClient wpMQTTClient;
		/// <summary></summary>
		public RestServer wpRest;
		/// <summary></summary>
		public Watchdog wpWatchdog;
		public Sun wpSun;
		/// <summary></summary>
		private FormWindowState lastState;
		/// <summary></summary>
		public Calendars CalDav;
		/// <summary></summary>
		private Email TheMail;
		/// <summary></summary>
		private Thread ThreadEmailSender;
		/// <summary></summary>
		private bool isFinished;
		/// <summary></summary>
		private static bool _isInit;
		public static bool isInit { get { return _isInit; } }
		/// <summary></summary>
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
		/// <summary></summary>
		public string lastchange {
			set {
				if (StringChanged != null) StringChanged(new StringChangedEventArgs($"{DateTime.Now.ToString()} {value}"));
			}
		}
		public string Message {
			set {
				try {
					if(lbl_msg.InvokeRequired) {
						lbl_msg.Invoke(new MethodInvoker(() => Message = value));
					} else {
						string[] oldMessage = lbl_msg.Lines;
						List<string> newMessage = new List<string>();
						newMessage.Add(value);
						for(int i = 0; i < 100; i++) {
							if(oldMessage.Length >= i + 1)
								newMessage.Add(oldMessage[i]);
						}
						lbl_msg.Lines = newMessage.ToArray();
					}
				} catch(Exception) { };
			}
		}
		/// <summary></summary>
		private System.Windows.Forms.Timer SystemTimer;
		/// <summary></summary>
		private string ApacheName;
		private wpServiceStatus ApacheService;
		private string MssqlName;
		private wpServiceStatus MssqlService;
		private wpSystemStatus SystemStatus;
		/// <summary></summary>
		private Dictionary<string, string> _SystemItems;
		/// <summary></summary>
		public Dictionary<string, string> SystemItems {
			get { return this._SystemItems; }
		}

		/// <summary>
		/// 
		/// </summary>
		public WebAutomationServer(string[] args) {
			InitializeComponent();
			wpHelp.Epsilon = wpHelp.getEpsilon();
			wpEventLog.fill();
			string[] pVersion = Application.ProductVersion.Split('.');
			this.toolStripStatusLabel1.Text = String.Format("{0} V {1}.{2} Build {3}, © {4}",
				Application.ProductName,
				pVersion[0], pVersion[1],
				Program.subversion,
				Application.CompanyName);
			this.Text += " - " + Ini.get("Projekt", "Nummer");
			this.SystemIcon.Text = Application.ProductName + " - " + Ini.get("Projekt", "Nummer");
#if DEBUG
			this.Text += " [Debug]";
#endif
			_isInit = false;
			eventLog = new Logger(wpEventLog.WebAutomation);

			_wpWartung = false;
			_wpStartMinimized = false;
			_wpAllowCloseBrowser = false;
			_wpBigProject = false;
			_wpForceRead = false;
			_wpPSOPC = false;
			if (args.Length > 0) {
				for (int i = 0; i < args.Length; i++) args[i] = args[i].ToLower();
				wpDebug.setDebugs(args);
				if (Arrays.inArray("wpWartung".ToLower(), args)) {
					_wpWartung = true;
					eventLog.Write(EventLogEntryType.Warning,
						"{0} Server im 'Wartungsmodus' gestartet", Application.ProductName);
				}
				if (Arrays.inArray("wpMinStart".ToLower(), args)) {
					_wpStartMinimized = true;
					eventLog.Write(EventLogEntryType.Information,
						"{0} Server im 'minimiertem Modus' gestartet", Application.ProductName);
				}
				if (Arrays.inArray("wpAllowCloseBrowser".ToLower(), args)) {
					_wpAllowCloseBrowser = true;
					eventLog.Write(EventLogEntryType.Warning,
						"{0} Server darf den lokalen Browser schließen", Application.ProductName);
				}
				if(Arrays.inArray("wpBigProject".ToLower(), args)) {
					_wpBigProject = true;
					eventLog.Write(EventLogEntryType.Warning,
						"{0} Server im 'Big Project Modus' gestartet", Application.ProductName);
				}
				if (Arrays.inArray("wpForceRead".ToLower(), args)) {
					_wpForceRead = true;
					eventLog.Write(EventLogEntryType.Warning,
						"{0} Server im 'Force Read Modus' gestartet", Application.ProductName);
				}
				if (Arrays.inArray("wpPSOPC".ToLower(), args)) {
					_wpPSOPC = true;
					eventLog.Write(EventLogEntryType.Warning,
						"{0} Server im 'PSOPC (Analphabet) Modus' gestartet", Application.ProductName);
				}
			}
			Helper.wpDebug.Write("CultureInfo: {0}", CultureInfo.CurrentUICulture.Name);
			Helper.wpDebug.Write("System NumberDecimalSeparator: {0}", NumberFormatInfo.InvariantInfo.NumberDecimalSeparator);
			Helper.wpDebug.Write("UI NumberDecimalSeparator: {0}", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator);
			this.lbl_db.Text = Ini.get("SQL", "Database");
			Helper.wpDebug.Write("Connected Database: " + Ini.get("SQL", "Database"));
		}
		public async Task initAsync() {
			await Task.Delay(100);

			// Update DB
			checkTable("alarmhistoric", "text");
			checkTable("opcdatapoint", "startuptype", "VARCHAR(10)");
			checkTable("opcdatapoint", "startupquality");
			checkTable("alarm", "link", "varchar(500)");
			checkTable("email", "sms", "bit", false, "0");
			checkTable("email", "phone2", "varchar(150)");
			checkTable("user", "startpage", "varchar(100)");

			checkTable("webpages", "id_parent_webpage", "int");
			checkTable("webpages", "position", "int");
			checkTable("webpages", "id_src", "varchar(200)");
			checkTable("webpages", "inwork", "bit", false, "0");

			using(SQL SQL = new SQL("startup")) {
				string[][] Tables = SQL.wpQuery("SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE [TABLE_NAME] = 'emailhistoric'");
				if(Tables.Length == 0) {
					SQL.wpNonResponse(@"CREATE TABLE [emailhistoric]
					([email][varchar](150) NOT NULL,[send][datetime] NOT NULL,[subject][text] NOT NULL,[message][text] NOT NULL,[error][text] NULL)");
					Helper.wpDebug.Write("Tabelle emailhistoric wurde erstellt");
				}
				SQL.wpNonResponse("UPDATE [opcdatapoint] SET [startuptype] = NULL, [startupquality] = NULL");
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
			ShellyServer.Start();
			D1MiniServer.Start();
			wpRest = new RestServer();
			await wpMQTTClient.Start();

			wpWatchdog = new Watchdog();
			CalDav = new Calendars();
			wpSun = new Sun();
			lastState = this.WindowState;
			isFinished = false;
			_isInit = true;
			eventLog.Write("{0} Server initialisiert", Application.ProductName);

			ThreadEmailSender = new Thread(new ThreadStart(createEmail));
			ThreadEmailSender.Name = "PGA Email Sender";
			ThreadEmailSender.Start();

			_SystemItems = new Dictionary<string, string>();
			SystemTimer = new System.Windows.Forms.Timer();
			SystemTimer.Interval = 1000 * 10;
			SystemTimer.Tick += new EventHandler(SystemTimer_Tick);
			getVolumeInfo();
			SystemTimer.Enabled = true;

			ApacheName = Ini.get("Watchdog", "ServiceNameApache");
			ApacheService = new wpServiceStatus(ApacheName);
			ApacheService.ServiceStatusChanged += ApacheService_ServiceStatusChanged;
			MssqlName = Ini.get("Watchdog", "ServiceNameMssql");
			MssqlService = new wpServiceStatus(MssqlName);
			MssqlService.ServiceStatusChanged += MssqlService_ServiceStatusChanged;
			SystemStatus = new wpSystemStatus();
			SystemStatus.MemoryStatusChanged += SystemStatus_MemoryStatusChanged;
			SystemStatus.ProzessorStatusChanged += SystemStatus_ProzessorStatusChanged;
		}

		private void checkTable(string table, string column, string type, bool canBeNull, string defaultValue) {
			using (SQL SQL = new SQL("Check Database")) {
				string[][] DB = SQL.wpQuery(@"SELECT [COLUMN_NAME] FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{0}' AND [COLUMN_NAME] = '{1}'", table, column);
				try {
					if (DB.Length == 0 || DB[0].Length == 0 || DB[0][0] != column) {
						SQL.wpNonResponse("ALTER TABLE [{0}] ADD [{1}] {2} {3}",
							table, column, type,
							canBeNull ? "NULL" : "NOT NULL CONSTRAINT [DF_" + table + "_" + column + "] DEFAULT(" + defaultValue + ")");
						Helper.wpDebug.Write("Add [{1}] to [{0}]", table, column);
					}
				} catch (Exception ex) {
					eventLog.WriteError(ex);
				}
			}
		}
		private void checkTable(string table, string column, string type) {
			checkTable(table, column, type, true, "");
		}
		private void checkTable(string table, string column) {
			checkTable(table, column, "VARCHAR(100)", true, "");
		}
		public bool TryConnectDatabase() {
			SQL SQL;
			int SQLCounter = 0;
			int SQLCounterMax;
			int SQLCounterTime;
			if (Int32.TryParse(Ini.get("SQL", "reconnect"), out SQLCounterMax) &&
				Int32.TryParse(Ini.get("SQL", "reconnectTime"), out SQLCounterTime)) {
				do {
					using (SQL = new SQL("Test SQL Connection")) { }
					if (!SQL.Available) {
						eventLog.Write(EventLogEntryType.Error,
							"{0} Server kann Datenbank nicht erreichen.\r\n\tReconnect nach {1} Sekunden\r\n\tVerbleibende Versuche: {2}",
							Application.ProductName, SQLCounterTime, SQLCounterMax - 1 - SQLCounter);
						SQLCounter++;
						Thread.Sleep(SQLCounterTime * 1000);
					}
				} while (SQL.Available == false && SQLCounter < SQLCounterMax);
				if (SQLCounter >= SQLCounterMax) {
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

		private void ApacheService_ServiceStatusChanged(ServiceStatusChangedEventArgs e) {
			ServiceControllerStatus s = (ServiceControllerStatus)e.newStatus;
			switch (s) {
				case ServiceControllerStatus.Running:
					eventLog.Write("Apache Server Status changed: {0} ({1})", s, ApacheName);
					break;
				case ServiceControllerStatus.Stopped:
					SystemIcon.BalloonTipTitle = "Apache Webservice";
					SystemIcon.BalloonTipText = "Dienststatus 'Stopped'";
					SystemIcon.BalloonTipIcon = ToolTipIcon.Error;
					SystemIcon.ShowBalloonTip(1000);
					eventLog.Write(EventLogEntryType.Error, "Apache Server Status changed: {0} ({1})", s, ApacheName);
					break;
				case ServiceControllerStatus.StopPending:
					SystemIcon.BalloonTipTitle = "Apache Webservice";
					SystemIcon.BalloonTipText = "Dienststatus 'StopPending'";
					SystemIcon.BalloonTipIcon = ToolTipIcon.Warning;
					SystemIcon.ShowBalloonTip(500);
					eventLog.Write(EventLogEntryType.Error, "Apache Server Status changed: {0} ({1})", s, ApacheName);
					break;
				case ServiceControllerStatus.StartPending:
					SystemIcon.BalloonTipTitle = "Apache Webservice";
					SystemIcon.BalloonTipText = "Dienststatus 'StartPending'";
					SystemIcon.BalloonTipIcon = ToolTipIcon.Info;
					SystemIcon.ShowBalloonTip(500);
					eventLog.Write(EventLogEntryType.Warning, "Apache Server Status changed: {0} ({1})", s, ApacheName);
					break;
				default:
					SystemIcon.BalloonTipTitle = "Apache Webservice";
					SystemIcon.BalloonTipText = "Dienststatus 'unbekannt'";
					SystemIcon.BalloonTipIcon = ToolTipIcon.Warning;
					SystemIcon.ShowBalloonTip(1000);
					eventLog.Write(EventLogEntryType.Warning, "Apache Server Status changed: {0} ({1})", s, ApacheName);
					break;
			}
		}
		private void SystemStatus_MemoryStatusChanged(SystemStatusChangedEventArgs e) {
			setText(lbl_memory, String.Format("Speicher: {0} KB", e.newStatus));
		}
		private void SystemStatus_ProzessorStatusChanged(SystemStatusChangedEventArgs e) {
			setText(lbl_prozessor, String.Format("Prozessor: {0} %", e.newStatus));
		}

		private void MssqlService_ServiceStatusChanged(ServiceStatusChangedEventArgs e) {
			ServiceControllerStatus s = (ServiceControllerStatus)e.newStatus;
			switch (s) {
				case ServiceControllerStatus.Running:
					eventLog.Write("Mssql Server Status changed: {0} ({1})", s, MssqlName);
					break;
				case ServiceControllerStatus.Stopped:
					SystemIcon.BalloonTipTitle = "Mssql Databaseservice";
					SystemIcon.BalloonTipText = "Dienststatus 'Stopped'";
					SystemIcon.BalloonTipIcon = ToolTipIcon.Error;
					SystemIcon.ShowBalloonTip(1000);
					eventLog.Write(EventLogEntryType.Error, "Mssql Server Status changed: {0} ({1})", s, MssqlName);
					break;
				case ServiceControllerStatus.StopPending:
					SystemIcon.BalloonTipTitle = "Mssql Databaseservice";
					SystemIcon.BalloonTipText = "Dienststatus 'StopPending'";
					SystemIcon.BalloonTipIcon = ToolTipIcon.Warning;
					SystemIcon.ShowBalloonTip(500);
					eventLog.Write(EventLogEntryType.Error, "Mssql Server Status changed: {0} ({1})", s, MssqlName);
					break;
				case ServiceControllerStatus.StartPending:
					SystemIcon.BalloonTipTitle = "Mssql Databaseservice";
					SystemIcon.BalloonTipText = "Dienststatus 'StartPending'";
					SystemIcon.BalloonTipIcon = ToolTipIcon.Info;
					SystemIcon.ShowBalloonTip(500);
					eventLog.Write(EventLogEntryType.Warning, "Mssql Server Status changed: {0} ({1})", s, MssqlName);
					break;
				default:
					SystemIcon.BalloonTipTitle = "Mssql Databaseservice";
					SystemIcon.BalloonTipText = "Dienststatus 'unbekannt'";
					SystemIcon.BalloonTipIcon = ToolTipIcon.Warning;
					SystemIcon.ShowBalloonTip(1000);
					eventLog.Write(EventLogEntryType.Warning, "Mssql Server Status changed: {0} ({1})", s, MssqlName);
					break;
			}
		}

		public delegate void ControlString(Control control, string text);
		public void setText(Control control, string text) {
			try {
				if (control.InvokeRequired) {
					control.Invoke(new ControlString(setText), new object[] { control, text });
				} else {
					control.Text = text;
				}
			} catch (Exception ex) {
				Helper.wpDebug.Write(ex.ToString());
			}
		}

		private void lbl_msg_Enter(object sender, EventArgs e) {
			nonsens.Focus();
		}


		#region alarm

		/// <summary>
		/// 
		/// </summary>
		/// <param name="newAlarm"></param>
		public void AlarmToMail(Alarm newAlarm) {
			bool entered = false;
			int notEntered = 0;
			while(!entered && notEntered < 10) {
				if(Monitor.TryEnter(TheMail, 5000)) {
					try {
						TheMail.AddAlarm(newAlarm);
					} catch(Exception ex) {
						eventLog.WriteError(ex);
					} finally {
						Monitor.Exit(TheMail);
						entered = true;
					}
				} else {
					if(++notEntered >= 10) {
						eventLog.Write(EventLogEntryType.Error,
							String.Format(@"Angeforderter Alarm blockiert: {0}.\r\n
								AlarmToMail nicht möglich", newAlarm.IdAlarm));
					} else {
						Thread.Sleep(10);
					}
				}
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="newAlarm"></param>
		public void QuitToMail(Alarm newAlarm) {
			bool entered = false;
			int notEntered = 0;
			while(!entered && notEntered < 10) {
				if(Monitor.TryEnter(TheMail, 5000)) {
					try {
						TheMail.AddQuit(newAlarm);
					} catch(Exception ex) {
						eventLog.WriteError(ex);
					} finally {
						Monitor.Exit(TheMail);
						entered = true;
					}
				} else {
					if(++notEntered >= 10) {
						eventLog.Write(EventLogEntryType.Error,
							String.Format(@"Angeforderter Alarm blockiert: {0}.\r\n
								QuitToMail nicht möglich", newAlarm.IdAlarm));
					} else {
						Thread.Sleep(10);
					}
				}
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="newAlarm"></param>
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
						eventLog.WriteError(ex);
					} finally {
						Monitor.Exit(TheMail);
						entered = true;
					}
				} else {
					if(++notEntered >= 10) {
						foreach(Alarm TheAlarm in newAlarm) {
							eventLog.Write(EventLogEntryType.Error,
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
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Alarm getAlarmFromAlarmid(int id) {
			Alarm returns = null;
			bool entered = false;
			int notEntered = 0;
			while(!entered && notEntered < 10) {
				if(Monitor.TryEnter(Server.Dictionaries.Items, 5000)) {
					try {
						foreach(KeyValuePair<int, OPCItem> TheItems in Server.Dictionaries.Items) {
							//if (TheItems.Value.Alarm != null) {
							//	if (TheItems.Value.Alarm.Idalarm == id) {
							//		returns = TheItems.Value.Alarm;
							//	}
							//}
						}
					} catch(Exception ex) {
						eventLog.WriteError(ex);
					} finally {
						Monitor.Exit(Server.Dictionaries.Items);
						entered = true;
					}
				} else {
					if(++notEntered >= 10) {
						eventLog.Write(EventLogEntryType.Error,
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
		/// 
		/// </summary>
		public void createEmail() {
			Dictionary<string, PRecipient> recipient = renewRecipient(out RecipientRequired);
			do {
				try {
					if(!_wpWartung) {
						if(RecipientRequired) {
							recipient.Clear();
							recipient = renewRecipient(out RecipientRequired);
						}
						foreach(KeyValuePair<string, PRecipient> Alarmstosend in recipient) {
							if(Email.EmailAlarms.getTotalCount(Alarmstosend.Value) > 0) {
								string[] MailContent = new string[2];
								try {
									if(Alarmstosend.Value.IsSMS) {
										MailContent[1] = Application.ProductName;
										foreach(string sms in TheMail.getSMSText(Alarmstosend.Value)) {
											TheMail.setRecipient(Alarmstosend.Key);
											string subject = Ini.get("Projekt", "Nummer") + " " + sms;
											int max = 160 - TheMail.getFromLength() - 2;
											if(subject.Length > max)
												subject = subject.Substring(0, max);
											TheMail.setSubject(subject);
											MailContent[0] = subject;
											if(MailContent[0].Length > 0 && MailContent[1].Length > 0) {
												TheMail.send();
												using(SQL SQL = new SQL("Mail send")) {
													SQL.wpNonResponse(@"INSERT INTO [emailhistoric]
										([email], [send], [subject], [message]) VALUES
										('{0}', '{1}', '{2}', '{3}')",
													Alarmstosend.Value.Address,
													DateTime.Now.ToString(SQL.DateTimeFormat),
													MailContent[0].Replace('\'', '"').Replace('\\', ' '),
													MailContent[1].Replace('\'', '"').Replace('\\', ' '));
												}
												Helper.wpDebug.Write("Send Mail to {0}", Alarmstosend.Key);
											}
										}
									} else {
										TheMail.setRecipient(Alarmstosend.Key);
										MailContent = TheMail.setAlarmBody(Alarmstosend.Value);
										if(MailContent[0].Length > 0 && MailContent[1].Length > 0) {
											TheMail.send();
											using(SQL SQL = new SQL("Mail send")) {
												SQL.wpNonResponse(@"INSERT INTO [emailhistoric]
										([email], [send], [subject], [message]) VALUES
										('{0}', '{1}', '{2}', '{3}')",
												Alarmstosend.Value.Address,
												DateTime.Now.ToString(SQL.DateTimeFormat),
												MailContent[0].Replace('\'', '"').Replace('\\', ' '),
												MailContent[1].Replace('\'', '"').Replace('\\', ' '));
											}
											Helper.wpDebug.Write("Send Mail to {0}", Alarmstosend.Key);
										}
									}
								} catch(Exception ex) {
									using(SQL SQL = new SQL("Mail send")) {
										SQL.wpNonResponse(@"INSERT INTO [emailhistoric]
										([email], [send], [subject], [message], [error]) VALUES
										('{0}', '{1}', '{2}', '{3}', '{4}')",
										Alarmstosend.Value.Address,
										DateTime.Now.ToString(SQL.DateTimeFormat),
										MailContent[0].Replace('\'', '"').Replace('\\', ' '),
										MailContent[1].Replace('\'', '"').Replace('\\', ' '),
										ex.Message);
									}
									eventLog.Write(EventLogEntryType.Error,
										String.Format("SendMail Error: {0}\r\nTrace:\r\n{1}", ex.Message, ex.StackTrace));
								}
							}
						}
					}
				} catch(Exception ex) {
					eventLog.Write(EventLogEntryType.Error,
						String.Format("SendMail Error: {0}\r\nTrace:\r\n{1}", ex.Message, ex.StackTrace));
				} finally {
					TheMail.reset();
				}
				Thread.Sleep(1000);
			} while(!isFinished);
			TheMail.Dispose();
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="required"></param>
		/// <returns></returns>
		private Dictionary<string, PRecipient> renewRecipient(out bool required) {
			Dictionary<string, PRecipient> recipient = new Dictionary<string, PRecipient>();
			using(SQL SQL = new SQL("renew Recipient Table")) {
				string[][] Query = SQL.wpQuery(@"SELECT
					([name] + ' ' + [lastname] + ' <' + [address] + '>'),
					[id_email], [sms], [ticketmail] FROM [email] WHERE [active] = 1");
				for(int j = 0; j < Query.Length; j++) {
					Dictionary<int, int> AlarmperUser = new Dictionary<int, int>();
					using(SQL SQL2 = new SQL("renew Recipient Table - Alarm per User")) {
						string[][] Alarme = SQL2.wpQuery(@"SELECT [id_alarm], [minutes]
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
			eventLog.Write("Recipient Table wurde erneuert.");
			return recipient;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public string SetRecipientRequired() {
			RecipientRequired = true;
			return "S_OK";
		}

		#endregion

		#region system

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SystemTimer_Tick(object sender, EventArgs e) {
			getVolumeInfo();
		}
		/// <summary>
		/// 
		/// </summary>
		private void getVolumeInfo() {
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
						eventLog.WriteError(ex);
					}
				}
			}
			foreach(string s in lbl) {
				lbl_volumeinfo.Text += s + "\r\n";
			}

		}
		/// <summary>
		/// 
		/// </summary>
		internal class DriveInformation {
			/// <summary></summary>
			public long totalspace;
			/// <summary></summary>
			public long usedspace;
			/// <summary></summary>
			public double prozent;
			/// <summary>
			/// 
			/// </summary>
			/// <param name="_totalspace"></param>
			/// <param name="_freespace"></param>
			public DriveInformation(long _totalspace, long _freespace) {
				totalspace = _totalspace / (1024 * 1024 * 1024);
				usedspace = (_totalspace - _freespace) / (1024 * 1024 * 1024);
				prozent = getProzent(_totalspace, _freespace);
			}
			/// <summary>
			/// 
			/// </summary>
			/// <param name="_totalspace"></param>
			/// <param name="_freespace"></param>
			/// <returns></returns>
			private double getProzent(long _totalspace, long _freespace) {
				long usedspace = _totalspace - _freespace;
				double returns = ((double)usedspace / (double)_totalspace) * 100;
				return returns;
			}
		}

		#endregion
	}
}
/** @} */