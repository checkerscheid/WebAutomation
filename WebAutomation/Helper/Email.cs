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
//# File-ID      : $Id:: Email.cs 76 2024-01-24 07:36:57Z                         $ #
//#                                                                                 #
//###################################################################################
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using WebAutomation.PlugIns;
/**
* @addtogroup WebAutomation
* @{
*/
namespace WebAutomation.Helper {
	/// <summary>
	/// 
	/// </summary>
	public class Email : IDisposable {
		/// <summary></summary>
		private Logger eventLog;
		/// <summary></summary>
		private MailMessage mailMessage;
		/// <summary></summary>
		private SmtpClient MailClient;

		private const string ServiceName = "wpAutomation";
		private const string impressumFirma = "<span style='font-weight:bold; color:#888888;'>wp<span style='color:#A91919;'>A</span>utomation GmbH</span>";
		private const string impressumStrasse = "Apfelskopfweg 10";
		private const string impressumStadt = "D-69118 Heidelberg";
		private const string impressumTelefon = "+49 6221 / 6734737";

		/// <summary></summary>
		public Email() {
			init();
		}
		/// <summary>
		/// 
		/// </summary>
		private void init() {
			eventLog = new Logger(wpEventLog.Mail);
			mailMessage = new MailMessage();
			reset();
			string useSSL = Ini.get("Email", "useSSL");
			mailMessage.From = new MailAddress(Ini.get("Email", "Sender"), Ini.get("Email", "Name"));
			mailMessage.IsBodyHtml = true;
			mailMessage.BodyEncoding = Encoding.UTF8;
			MailClient = new SmtpClient(Ini.get("Email", "Server"), Ini.getInt("Email", "Port"));
			if (useSSL.ToLower() == "true") MailClient.EnableSsl = true;
			eventLog.Write("EMail Client gestartet");
		}
		/// <summary>
		/// 
		/// </summary>
		public void Dispose() {
			eventLog.Write("EMail Client gestoppt");
			GC.SuppressFinalize(this);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="to"></param>
		public void setRecipient(string to) {
			mailMessage.To.Add(to);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="Subject"></param>
		public void setSubject(string Subject) {
			mailMessage.Subject = Subject;
		}
		public List<string> getSMSText(PRecipient r) {
			mailMessage.Body = ServiceName + " Alarm";
			return Alarms.getSMS(r);
		}
		public int getFromLength() {
			return mailMessage.From.Address.Length;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="Alarme"></param>
		public string[] setAlarmBody(PRecipient r) {
			mailMessage.Subject = "";
			mailMessage.Body = "";

			if (Ini.get("Email", "ProjectNumberInSubject") == "true") {
				setSubject(String.Format("{0} {1} - {2} Neue Alarm Aktionen",
					Ini.get("Projekt", "Nummer"), ServiceName, Alarms.getTotalCount(r)));
			} else {
				setSubject(String.Format("{0} - {1} Neue Alarm Aktionen",
					ServiceName, Alarms.getTotalCount(r)));
			}
			string MailToInMail = Ini.get("Email", "MailToInMail");
			if(MailToInMail != "") MailToInMail = @"E-Mail: <a href='mailto:" + MailToInMail + "'> " + MailToInMail + @" </a><br />";
			string HelpdeskLinkInMail = Ini.get("Email", "HelpdeskLinkInMail");
			string HelpdeskNameInMail = Ini.get("Email", "HelpdeskNameInMail");
			if(HelpdeskNameInMail == "") HelpdeskNameInMail = HelpdeskLinkInMail;
			if(HelpdeskLinkInMail != "") HelpdeskLinkInMail = @"24 h PGA Helpdesk Portal: <a href='https://" + HelpdeskLinkInMail + "'>" + HelpdeskNameInMail + @"</a><br />";
			string LinkToInMail = Ini.get("Email", "LinkToInMail");
			string LinkNameInMail = Ini.get("Email", "LinkNameInMail");
			if(LinkNameInMail == "") LinkNameInMail = LinkToInMail;
			if(LinkToInMail != "") LinkToInMail = @"WEB: <a href='https://" + LinkToInMail + "'>" + LinkNameInMail + @"</a><br />";
			mailMessage.Body = @"
		<div style='font-family:Arial; font-size:9pt;'>
			<p>In Ihrer Anlage stehen die folgenden Alarme an:</p>
			<p>" + Alarms.getText(r) + @"</p>
			<br />" + String.Format("<p>Projekt: {0} - {1}</p>",
				Ini.get("Projekt", "Nummer"),
				Ini.get("Projekt", "Name")) + @"
			<p style='font-weight:bold; color:#29166f;'>Ihr " + ServiceName + @" Alarm Service</p>
			<p style='font-size:8pt; color:#888;'>
				<span style='font-weight:bold;'>
					<br />
					<span style='color:#555;'>
						Bitte antworten Sie nicht auf diese E-Mail.<br />
						Die Adresse dient nur dem Versand der " + ServiceName + @" Alarme.<br />
						Sollten Sie uns etwas mitteilen wollen nutzen Sie bitte die nachfolgenden Kontaktinformationen.
					</span>
				</span>
			</p>
			<p style='font-size:8pt; color:#888888;'>
				--<br />
				" + impressumFirma + @"<br />
				" + impressumStrasse + @"<br />
				" + impressumStadt + @"<br />
				" + impressumTelefon + @"<br />
				" + MailToInMail + @"
				" + HelpdeskLinkInMail + @"
				" + LinkToInMail + @"
			</p>
		</div>";
			return new string[] { mailMessage.Subject, mailMessage.Body };
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="TheAlarm"></param>
		public void AddAlarm(Alarm TheAlarm) {
			string color = TheAlarm.Autoquit ? "#FFA500" : "#A91919";
			Alarms.Add(TheAlarm.IdAlarm, @"
	<div style='font-weight:bold; color:" + color + @"'>" + TheAlarm.Alarmgroup + " - " + TheAlarm.Alarmtext + @" - gekommen</div>
	<div style='margin-left:20px;'>gekommen: <span style='font-weight:bold;'>" + TheAlarm.Come.ToString("dd.MM.yyyy HH:mm:ss") + @"</span></div>
	<div style='margin-left:20px; font-weight:bold;'>Alarmdetails:</div>
	<div style='margin-left:40px;'>Beschreibung: " + TheAlarm.Alarmtext + @"</div>" +
	(PlugIns.Alarms.useAlarmGroup1 ? "<div style='margin-left:40px;'>" + PlugIns.Alarms.nameAlarmGroup1 + ": " + TheAlarm.getReadableAlarmGroup1() + "</div>" : "") +
	(PlugIns.Alarms.useAlarmGroup2 ? "<div style='margin-left:40px;'>" + PlugIns.Alarms.nameAlarmGroup2 + ": " + TheAlarm.getReadableAlarmGroup2() + "</div>" : "") +
	(PlugIns.Alarms.useAlarmGroup3 ? "<div style='margin-left:40px;'>" + PlugIns.Alarms.nameAlarmGroup3 + ": " + TheAlarm.getReadableAlarmGroup3() + "</div>" : "") +
	(PlugIns.Alarms.useAlarmGroup4 ? "<div style='margin-left:40px;'>" + PlugIns.Alarms.nameAlarmGroup4 + ": " + TheAlarm.getReadableAlarmGroup4() + "</div>" : "") +
	(PlugIns.Alarms.useAlarmGroup5 ? "<div style='margin-left:40px;'>" + PlugIns.Alarms.nameAlarmGroup5 + ": " + TheAlarm.getReadableAlarmGroup5() + "</div>" : "") + @"
	<div style='margin-left:40px;'>Gruppe: " + TheAlarm.Alarmgroup + @"</div>
	<div style='margin-left:40px;'>Alarmtype: " + string.Format(getTypeColor(TheAlarm.Alarmtype), TheAlarm.Alarmtype) + @"</div>
	<div style='margin-left:40px;'>Quittierung erforderlich: " + (TheAlarm.Autoquit ? "<span style='color:#090'>Nein</span>" : "<span style='color:#A91919'>Ja</span>") + @"</div>
	<div style='margin-left:40px;'>OPC Item Name: " + TheAlarm.DpName + @"</div>");

			Alarms.AddSMS(TheAlarm.IdAlarm, @"" +
				TheAlarm.Alarmtext + " - " + TheAlarm.Alarmtype + " - " + TheAlarm.Alarmgroup +
				(PlugIns.Alarms.useAlarmGroup1 ? " - " + TheAlarm.getReadableAlarmGroup1() : "") +
				(PlugIns.Alarms.useAlarmGroup2 ? " - " + TheAlarm.getReadableAlarmGroup2() : "") +
				(PlugIns.Alarms.useAlarmGroup3 ? " - " + TheAlarm.getReadableAlarmGroup3() : "") +
				(PlugIns.Alarms.useAlarmGroup4 ? " - " + TheAlarm.getReadableAlarmGroup4() : "") +
				(PlugIns.Alarms.useAlarmGroup5 ? " - " + TheAlarm.getReadableAlarmGroup5() : "") + " ");
			Alarms.countup(TheAlarm.IdAlarm, 0);
			wpDebug.Write("Alarm to Mail: {0} ({1})", TheAlarm.Alarmname, TheAlarm.Alarmtext);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="TheAlarm"></param>
		public void AddQuit(Alarm TheAlarm) {
			AddQuit(TheAlarm, 0);
		}
		private void AddQuit(Alarm TheAlarm, int minutes) {
			Alarms.Add(TheAlarm.IdAlarm, minutes, @"
	<div style='font-weight:bold; color:#090'>" + TheAlarm.Alarmgroup + " - " + TheAlarm.Alarmtext + @" - quittiert</div>
	<div style='margin-left:20px;'>gekommen: " + TheAlarm.Come.ToString("dd.MM.yyyy HH:mm:ss") + @"</div>
	<div style='margin-left:20px;'>quittiert: <span style='font-weight:bold;'>" + TheAlarm.QuitFrom + " (" + TheAlarm.Quit.ToString("dd.MM.yyyy HH:mm:ss") + @")</span></div>
	<div style='margin-left:20px;'>Bemerkung: <span style='font-weight:bold;'>" + TheAlarm.QuitText + @"</span></div>
	<div style='margin-left:20px;'>status: <span style='font-weight:bold;'>" +
	(TheAlarm.Gone == Alarm.Default ? "<span style='color:#A91919'>anstehend</span>" : "gegangen (" + TheAlarm.Gone.ToString() + ")") + @"</span></div>
	<div style='margin-left:20px; font-weight:bold;'>Alarmdetails:</div>
	<div style='margin-left:40px;'>Beschreibung: " + TheAlarm.Alarmtext + @"</div>" +
	(PlugIns.Alarms.useAlarmGroup1 ? "<div style='margin-left:40px;'>" + PlugIns.Alarms.nameAlarmGroup1 + ": " + TheAlarm.getReadableAlarmGroup1() + "</div>" : "") +
	(PlugIns.Alarms.useAlarmGroup2 ? "<div style='margin-left:40px;'>" + PlugIns.Alarms.nameAlarmGroup2 + ": " + TheAlarm.getReadableAlarmGroup2() + "</div>" : "") +
	(PlugIns.Alarms.useAlarmGroup3 ? "<div style='margin-left:40px;'>" + PlugIns.Alarms.nameAlarmGroup3 + ": " + TheAlarm.getReadableAlarmGroup3() + "</div>" : "") +
	(PlugIns.Alarms.useAlarmGroup4 ? "<div style='margin-left:40px;'>" + PlugIns.Alarms.nameAlarmGroup4 + ": " + TheAlarm.getReadableAlarmGroup4() + "</div>" : "") +
	(PlugIns.Alarms.useAlarmGroup5 ? "<div style='margin-left:40px;'>" + PlugIns.Alarms.nameAlarmGroup5 + ": " + TheAlarm.getReadableAlarmGroup5() + "</div>" : "") + @"
	<div style='margin-left:40px;'>Gruppe: " + TheAlarm.Alarmgroup + @"</div>
	<div style='margin-left:40px;'>Alarmtype: " + string.Format(getTypeColor(TheAlarm.Alarmtype), TheAlarm.Alarmtype) + @"</div>
	<div style='margin-left:40px;'>OPC Item Name: " + TheAlarm.DpName + @"</div>");
			Alarms.countup(TheAlarm.IdAlarm, minutes);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="Alarmtype"></param>
		/// <returns></returns>
		private string getTypeColor(string Alarmtype) {
			string TypeColor;
			switch (Alarmtype) {
				case "Alarm":
					TypeColor = "<span style='color:#A91919; font-weight:bold;'>{0}</span>";
					break;
				case "Störung":
					TypeColor = "<span style='color:#A91919'>{0}</span>";
					break;
				case "Meldung":
					TypeColor = "{0}";
					break;
				case "Handbetrieb":
					TypeColor = "<span style='color:#ffa500'>{0}</span>";
					break;
				case "Nothandebene":
					TypeColor = "<span style='color:#ffa500'>{0}</span>";
					break;
				case "gefallen":
					TypeColor = "<span style='color:#A91919'>{0}</span>";
					break;
				default:
					TypeColor = "{0}";
					break;
			}
			return TypeColor;
		}
		/// <summary>
		/// 
		/// </summary>
		public void send() {
			if (Ini.get("Email", "UseUserPassword") == "true") {
				MailClient.Credentials = new NetworkCredential(Ini.get("Email", "User"),
					Ini.get("Email", "Password"));
			}
			MailClient.Send(mailMessage);
			string to = "\r\n";
			foreach (MailAddress sender in mailMessage.To) {
				to += sender.Address + "\r\n";
			}
			mailMessage.To.Clear();
			eventLog.Write(String.Format("Alarm EMail geschrieben an:{0}", to));
		}
		/// <summary>
		/// 
		/// </summary>
		public void reset() {
			bool entered = false;
			int notEntered = 0;
			while (!entered && notEntered < 10) {
				if(Monitor.TryEnter(Alarms.Alarmtext)) {
					Alarms.Alarmtext.Clear();
					Monitor.Exit(Alarms.Alarmtext);
					entered = true;
				} else {
					if (++notEntered >= 10) {
						eventLog.Write(EventLogEntryType.Error,
							"Angefordertes Item blockiert.\r\nAlarm.reset nicht möglich");
					} else {
						Thread.Sleep(10);
					}
				}
			}
			entered = false;
			notEntered = 0;
			while (!entered && notEntered < 10) {
				if (Monitor.TryEnter(Alarms.Alarmsms)) {
					Alarms.Alarmsms.Clear();
					Monitor.Exit(Alarms.Alarmsms);
					entered = true;
				} else {
					if (++notEntered >= 10) {
						eventLog.Write(EventLogEntryType.Error,
							"Angefordertes Item blockiert.\r\nAlarm.reset nicht möglich");
					} else {
						Thread.Sleep(10);
					}
				}
			}
			entered = false;
			notEntered = 0;
			while (!entered && notEntered < 10) {
				if(Monitor.TryEnter(Alarms.count)) {
					Alarms.count.Clear();
					Monitor.Exit(Alarms.count);
					entered = true;
				} else {
					if (++notEntered >= 10) {
						eventLog.Write(EventLogEntryType.Error,
							"Angefordertes Item blockiert.\r\nAlarm.reset nicht möglich");
					} else {
						Thread.Sleep(10);
					}
				}
			}
		}
		public class PRecipient {
			private Dictionary<int, int> alarmMinutes;
			private Dictionary<int, System.Timers.Timer> alarmTimer;
			private string address;
			public string Address {
				get { return address; }
			}
			private int email;
			public Dictionary<int, int> Minutes() {
				return alarmMinutes;
			}
			public int Minutes(int alarmid) {
				int returns = -1;
				if(alarmMinutes.ContainsKey(alarmid)) {
					returns = alarmMinutes[alarmid];
				}
				return returns;
			}
			private Dictionary<int, bool> active;
			public bool Active(int alarmid) {
				bool returns = true;
				if(active.ContainsKey(alarmid)) {
					returns = active[alarmid];
				}
				return returns;
			}
			private bool isSMS;
			public bool IsSMS {
				get { return isSMS; }
			}
			public PRecipient(int _email, string _address, bool _isSMS, Dictionary<int, int> _alarmMin) {
				email = _email;
				address = _address;
				isSMS = _isSMS;
				alarmMinutes = _alarmMin;
				active = new Dictionary<int, bool>();
				foreach(KeyValuePair<int, int> kvp in _alarmMin) {
					if(active.ContainsKey(kvp.Key)) {
						active[kvp.Key] = true;
					} else {
						active.Add(kvp.Key, true);
					}
				}
				alarmTimer = new Dictionary<int, System.Timers.Timer>();
				getTimes();
			}
			public void getTimes() {
				getTimes(0);
			}
			public void getTimes(int verz) {
				DateTime Now = DateTime.Now;
				int WeekDay = (int)Now.DayOfWeek;
				if(WeekDay == 0) WeekDay = 7;
				DateTime ThisMonday = Now.AddDays((-1 * WeekDay) + 1).Date;

				using(SQL sql = new SQL("Recipient Timer")) {
					foreach(KeyValuePair<int, int> kvp in alarmMinutes) {
						string[][] Query1 = sql.wpQuery(@"
						SELECT [day], [time], [stopfor] FROM [standbyescalation] [se]
						INNER JOIN [escalationgroup] [eg]
							ON [se].[id_escalationgroup] = [eg].[id_escalationgroup]
						INNER JOIN [escalationgroupmember] [egm]
							ON [eg].[id_escalationgroup] = [egm].[id_escalationgroup]
						INNER JOIN [alarmtoescalation] [ae]
							ON [eg].[id_escalationgroup] = [ae].[id_escalationgroup]
						WHERE [id_email] = {0} AND [id_alarm] = {1}
						ORDER BY [day], [time]", email, kvp.Key);
						string[][] Query2 = new string[0][];
						//string[][] Query2 = sql.wpQuery(@"
						//	SELECT [s].[day], [s].[time], [s].[stopfor] FROM [standbyescalation] [s]
						//	INNER JOIN [alarmtoescalation] [ate]
						//		ON [s].[id_alarmtoescalation] = [ate].[id_alarmtoescalation]
						//	INNER JOIN [escalationgroupmember] [g]
						//		ON [ate].[id_escalationgroup] = [g].[id_escalationgroup]
						//	WHERE [g].[id_email] = {0} AND [ate].[id_alarm] = {1}
						//	ORDER BY [day], [time]", email, kvp.Key);
						string[][] Query = new string[Query1.Length + Query2.Length][];
						Array.Copy(Query1, Query, Query1.Length);
						for(int i = Query1.Length; i < Query.Length; i++) {
							if(Query2.Length > 0)
								Query[i] = Query2[i - Query1.Length];
						}

						if(!checklastDate(ThisMonday, Now, Query, kvp.Key)) {
							checklastDate(ThisMonday.AddDays(-7), Now, Query, kvp.Key);
						}
						if(!checknextDate(ThisMonday, Now, Query, kvp.Key, verz)) {
							checknextDate(ThisMonday.AddDays(7), Now, Query, kvp.Key, verz);
						}

					}
				}
			}
			private bool checklastDate(DateTime Monday, DateTime Now, string[][] DBScheduleTimes, int idalarm) {
				bool foundLast = false;
				DateTime CheckDate;
				for(int iScheduleTimes = 0; iScheduleTimes < DBScheduleTimes.Length; iScheduleTimes++) {
					int checkDayOfWeek;
					string[] HourMinute = DBScheduleTimes[iScheduleTimes][1].Split(':');
					int checkHour;
					int checkMinute;
					if(Int32.TryParse(DBScheduleTimes[iScheduleTimes][0], out checkDayOfWeek) &&
						Int32.TryParse(HourMinute[0], out checkHour) &&
						Int32.TryParse(HourMinute[1], out checkMinute)) {
						CheckDate = Monday.Add(new TimeSpan(checkDayOfWeek - 1, checkHour, checkMinute, 0));
						if(CheckDate < Now) {
							active[idalarm] = DBScheduleTimes[iScheduleTimes][2] == "" ? true : false;
							wpDebug.Write("User {0} (alarm:{1}) ist jetzt {2} (last)",
								address, idalarm, active[idalarm]);
							foundLast = true;
						}
					}
				}
				return foundLast;
			}
			/// <summary>
			/// 
			/// </summary>
			/// <param name="Monday"></param>
			/// <param name="Now"></param>
			/// <param name="DBScheduleTimes"></param>
			/// <returns></returns>
			private bool checknextDate(DateTime Monday, DateTime Now, string[][] DBScheduleTimes,
				int idalarm, int verz) {
				bool foundNext = false;
				DateTime CheckDate;
				double next = 0;
				for(int iScheduleTimes = 0; iScheduleTimes < DBScheduleTimes.Length; iScheduleTimes++) {
					if(!foundNext) {
						int checkDayOfWeek;
						string[] HourMinute = DBScheduleTimes[iScheduleTimes][1].Split(':');
						int checkHour;
						int checkMinute;
						if(Int32.TryParse(DBScheduleTimes[iScheduleTimes][0], out checkDayOfWeek) &&
							Int32.TryParse(HourMinute[0], out checkHour) &&
							Int32.TryParse(HourMinute[1], out checkMinute)) {
							CheckDate = Monday.Add(new TimeSpan(checkDayOfWeek - 1, checkHour, checkMinute, 0));
							if(CheckDate > Now) {
								TimeSpan sec = CheckDate - Now;
								next = (double)sec.TotalMilliseconds;
								System.Timers.Timer t = new System.Timers.Timer();
								t.Elapsed += (sender, args) => t_Elapsed(idalarm);
								t.Interval = next + (verz * 1000);
								t.AutoReset = false;
								t.Enabled = true;
								if(alarmTimer.ContainsKey(idalarm)) {
									alarmTimer[idalarm].Stop();
									alarmTimer[idalarm].Dispose();
									alarmTimer[idalarm] = null;
									alarmTimer[idalarm] = t;
								} else {
									alarmTimer.Add(idalarm, t);
								}
								wpDebug.Write("User {0} (alarm:{1}) wird in {2:%d} Tag(e) " +
									"{2:hh\\:mm\\:ss} geschaltet: {3}",
									address, idalarm, sec + new TimeSpan(0, 0, verz), !active[idalarm]);
								foundNext = true;
							}
						}
					}
				}
				return foundNext;
			}
			private void t_Elapsed(int idalarm) {
				active[idalarm] = !active[idalarm];
				wpDebug.Write("User {0} (alarm:{1}) ist jetzt {2}", address, idalarm, active[idalarm]);
				getTimes(5);
			}
		}
		/// <summary>
		/// 
		/// </summary>
		public static class Alarms {
			/// <summary>
			/// 
			/// </summary>
			public static Dictionary<int, Dictionary<int, int>> count =
				new Dictionary<int, Dictionary<int, int>>();
			/// <summary>
			/// 
			/// </summary>
			public static Dictionary<int, Dictionary<int, string>> Alarmtext =
				new Dictionary<int, Dictionary<int, string>>();

			public static Dictionary<int, Dictionary<int, string>> Alarmsms =
				new Dictionary<int, Dictionary<int, string>>();
			/// <summary>
			/// 
			/// </summary>
			/// <param name="idalarm"></param>
			/// <param name="html"></param>
			public static void Add(int idalarm, string html) { Add(idalarm, 0, html); }
			public static void Add(int idalarm, int minutes, string html) {
				bool entered = false;
				int notEntered = 0;
				while (!entered && notEntered < 10) {
					if (Monitor.TryEnter(Alarmtext)) {
						try {
							if (!Alarmtext.ContainsKey(idalarm)) {
								Alarmtext.Add(idalarm, new Dictionary<int, string>() { { minutes, "" } });
							}
							if (!Alarmtext[idalarm].ContainsKey(minutes)) {
								Alarmtext[idalarm].Add(minutes, "");
							}
							Alarmtext[idalarm][minutes] += html;
						} finally {
							Monitor.Exit(Alarmtext);
							entered = true;
						}
					} else {
						if (++notEntered >= 10) {
							wpDebug.Write(
								String.Format("Angefordertes Item blockiert.\r\nAlarms.Add nicht möglich",
									idalarm));
						} else {
							Thread.Sleep(10);
						}
					}
				}
			}
			public static void AddSMS(int idalarm, string html) { AddSMS(idalarm, 0, html); }
			public static void AddSMS(int idalarm, int minutes, string html) {
				bool entered = false;
				int notEntered = 0;
				while (!entered && notEntered < 10) {
					if (Monitor.TryEnter(Alarmsms)) {
						try {
							if (!Alarmsms.ContainsKey(idalarm)) {
								Alarmsms.Add(idalarm, new Dictionary<int, string>() { { minutes, "" } });
							}
							if (!Alarmsms[idalarm].ContainsKey(minutes)) {
								Alarmsms[idalarm].Add(minutes, "");
							}
							Alarmsms[idalarm][minutes] += html;
						} finally {
							Monitor.Exit(Alarmsms);
							entered = true;
						}
					} else {
						if (++notEntered >= 10) {
							wpDebug.Write(
								String.Format("Angefordertes Item blockiert.\r\nAlarms.AddSMS nicht möglich",
									idalarm));
						} else {
							Thread.Sleep(10);
						}
					}
				}
			}
			/// <summary>
			/// 
			/// </summary>
			/// <param name="Alarme"></param>
			/// <returns></returns>
			public static string getText(PRecipient r) {
				string html = "";
				foreach(KeyValuePair<int, int> kvp in r.Minutes()) {
					if (r.Active(kvp.Key)) {
						if (getcount(kvp.Key, kvp.Value) > 0) {
							html += Alarmtext[kvp.Key][kvp.Value];
						}
					} else {
						wpDebug.Write("User inaktiv {0}, Alarm: {1}", r.Address, kvp.Key);
					}
				}
				return html;
			}
			public static List<string> getSMS(PRecipient r) {
				List<string> html = new List<string>();
				foreach (KeyValuePair<int, int> kvp in r.Minutes()) {
					if (r.Active(kvp.Key)) {
						if (getcount(kvp.Key, kvp.Value) > 0) {
							if(Alarmsms.ContainsKey(kvp.Key) && Alarmsms[kvp.Key].ContainsKey(kvp.Value))
								html.Add(Alarmsms[kvp.Key][kvp.Value]);
						}
					} else {
						wpDebug.Write("User inaktiv {0}, Alarm: {1}", r.Address, kvp.Key);
					}
				}
				return html;
			}
			/// <summary>
			/// 
			/// </summary>
			/// <param name="idalarm"></param>
			public static void countup(int idalarm, int minutes) {
				if (!count.ContainsKey(idalarm)) {
					count.Add(idalarm, new Dictionary<int, int>() { {minutes, 0} });
				}
				if (!count[idalarm].ContainsKey(minutes)) {
					count[idalarm].Add(minutes, 0);
				}
				count[idalarm][minutes]++;
			}
			/// <summary>
			/// 
			/// </summary>
			/// <param name="idalarm"></param>
			/// <returns></returns>
			public static int getcount(int idalarm, int minutes) {
				if (count.ContainsKey(idalarm) && count[idalarm].ContainsKey(minutes)) {
					return count[idalarm][minutes];
				} else {
					return 0;
				}
			}
			/// <summary>
			/// 
			/// </summary>
			/// <param name="idalarm"></param>
			/// <returns></returns>
			public static int getTotalCount(PRecipient r) {
				int counter = 0;
				foreach(KeyValuePair<int, int> kvp in r.Minutes()) {
					if (r.Active(kvp.Key)) {
						counter += getcount(kvp.Key, kvp.Value);
					}
				}
				return counter;
			}
		}
	}
}
/** @} */
