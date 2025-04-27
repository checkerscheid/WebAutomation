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
//# Revision     : $Rev:: 203                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: Email.cs 203 2025-04-27 15:09:36Z                        $ #
//#                                                                                 #
//###################################################################################
using FreakaZone.Libraries.wpEventLog;
using FreakaZone.Libraries.wpIniFile;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Threading;
using WebAutomation.PlugIns;
using static FreakaZone.Libraries.wpEventLog.Logger;
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
			Debug.Write(MethodInfo.GetCurrentMethod(), "EMail Client init");
			eventLog = new Logger(Logger.ESource.Mail);
			mailMessage = new MailMessage();
			reset();
			string useSSL = IniFile.get("Email", "useSSL");
			mailMessage.From = new MailAddress(IniFile.get("Email", "Sender"), IniFile.get("Email", "Name"));
			mailMessage.IsBodyHtml = true;
			mailMessage.BodyEncoding = Encoding.UTF8;
			MailClient = new SmtpClient(IniFile.get("Email", "Server"), IniFile.getInt("Email", "Port"));
			if (useSSL.ToLower() == "true") MailClient.EnableSsl = true;
			eventLog.Write(MethodInfo.GetCurrentMethod(), "EMail Client gestartet");
		}
		/// <summary>
		/// 
		/// </summary>
		public void Dispose() {
			eventLog.Write(MethodInfo.GetCurrentMethod(), "EMail Client gestoppt");
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
			return EmailAlarms.getSMS(r);
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

			if (IniFile.get("Email", "ProjectNumberInSubject") == "true") {
				setSubject(String.Format("{0} {1} - {2} Neue Alarm Aktionen",
					IniFile.get("Projekt", "Nummer"), ServiceName, EmailAlarms.getTotalCount(r)));
			} else {
				setSubject(String.Format("{0} - {1} Neue Alarm Aktionen",
					ServiceName, EmailAlarms.getTotalCount(r)));
			}
			string MailToInMail = IniFile.get("Email", "MailToInMail");
			if(MailToInMail != "") MailToInMail = @"E-Mail: <a href='mailto:" + MailToInMail + "'> " + MailToInMail + @" </a><br />";
			string HelpdeskLinkInMail = IniFile.get("Email", "HelpdeskLinkInMail");
			string HelpdeskNameInMail = IniFile.get("Email", "HelpdeskNameInMail");
			if(HelpdeskNameInMail == "") HelpdeskNameInMail = HelpdeskLinkInMail;
			if(HelpdeskLinkInMail != "") HelpdeskLinkInMail = @"24 h PGA Helpdesk Portal: <a href='https://" + HelpdeskLinkInMail + "'>" + HelpdeskNameInMail + @"</a><br />";
			string LinkToInMail = IniFile.get("Email", "LinkToInMail");
			string LinkNameInMail = IniFile.get("Email", "LinkNameInMail");
			if(LinkNameInMail == "") LinkNameInMail = LinkToInMail;
			if(LinkToInMail != "") LinkToInMail = @"WEB: <a href='https://" + LinkToInMail + "'>" + LinkNameInMail + @"</a><br />";
			mailMessage.Body = @"
		<div style='font-family:Arial; font-size:9pt;'>
			<p>In Ihrer Anlage stehen die folgenden Alarme an:</p>
			<p>" + EmailAlarms.getText(r) + @"</p>
			<br />" + String.Format("<p>Projekt: {0} - {1}</p>",
				IniFile.get("Projekt", "Nummer"),
				IniFile.get("Projekt", "Name")) + @"
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
			EmailAlarms.Add(TheAlarm.IdAlarm, @"
	<div style='font-weight:bold; color:" + color + @"'>" + TheAlarm.Alarmgroup + " - " + TheAlarm.Alarmtext + @" - gekommen</div>
	<div style='margin-left:20px;'>gekommen: <span style='font-weight:bold;'>" + TheAlarm.Come.ToString("dd.MM.yyyy HH:mm:ss") + @"</span></div>
	<div style='margin-left:20px; font-weight:bold;'>Alarmdetails:</div>
	<div style='margin-left:40px;'>Beschreibung: " + TheAlarm.Alarmtext + @"</div>" +
	(Alarms.UseAlarmGroup1 ? "<div style='margin-left:40px;'>" + Alarms.NameAlarmGroup1 + ": " + Alarms.GetReadableGroup(Alarms.ALARMGROUP1, TheAlarm.Alarmgroups1) + "</div>" : "") +
	(Alarms.UseAlarmGroup2 ? "<div style='margin-left:40px;'>" + Alarms.NameAlarmGroup2 + ": " + Alarms.GetReadableGroup(Alarms.ALARMGROUP2, TheAlarm.Alarmgroups2) + "</div>" : "") +
	(Alarms.UseAlarmGroup3 ? "<div style='margin-left:40px;'>" + Alarms.NameAlarmGroup3 + ": " + Alarms.GetReadableGroup(Alarms.ALARMGROUP3, TheAlarm.Alarmgroups3) + "</div>" : "") +
	(Alarms.UseAlarmGroup4 ? "<div style='margin-left:40px;'>" + Alarms.NameAlarmGroup4 + ": " + Alarms.GetReadableGroup(Alarms.ALARMGROUP4, TheAlarm.Alarmgroups4) + "</div>" : "") +
	(Alarms.UseAlarmGroup5 ? "<div style='margin-left:40px;'>" + Alarms.NameAlarmGroup5 + ": " + Alarms.GetReadableGroup(Alarms.ALARMGROUP5, TheAlarm.Alarmgroups5) + "</div>" : "") + @"
	<div style='margin-left:40px;'>Gruppe: " + TheAlarm.Alarmgroup + @"</div>
	<div style='margin-left:40px;'>Alarmtype: " + string.Format(getTypeColor(TheAlarm.Alarmtype), TheAlarm.Alarmtype) + @"</div>
	<div style='margin-left:40px;'>Quittierung erforderlich: " + (TheAlarm.Autoquit ? "<span style='color:#090'>Nein</span>" : "<span style='color:#A91919'>Ja</span>") + @"</div>
	<div style='margin-left:40px;'>OPC Item Name: " + TheAlarm.DpName + @"</div>");

			EmailAlarms.AddSMS(TheAlarm.IdAlarm, @"" +
				TheAlarm.Alarmtext + " - " + TheAlarm.Alarmtype + " - " + TheAlarm.Alarmgroup +
				(Alarms.UseAlarmGroup1 ? " - " + Alarms.GetReadableGroup(Alarms.ALARMGROUP1, TheAlarm.Alarmgroups1) : "") +
				(Alarms.UseAlarmGroup2 ? " - " + Alarms.GetReadableGroup(Alarms.ALARMGROUP2, TheAlarm.Alarmgroups2) : "") +
				(Alarms.UseAlarmGroup3 ? " - " + Alarms.GetReadableGroup(Alarms.ALARMGROUP3, TheAlarm.Alarmgroups3) : "") +
				(Alarms.UseAlarmGroup4 ? " - " + Alarms.GetReadableGroup(Alarms.ALARMGROUP4, TheAlarm.Alarmgroups4) : "") +
				(Alarms.UseAlarmGroup5 ? " - " + Alarms.GetReadableGroup(Alarms.ALARMGROUP5, TheAlarm.Alarmgroups5) : "") +
				" ");
			EmailAlarms.countup(TheAlarm.IdAlarm, 0);
			Debug.Write(MethodInfo.GetCurrentMethod(), "Alarm to Mail: {0}", TheAlarm.Alarmtext);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="TheAlarm"></param>
		public void AddQuit(Alarm TheAlarm) {
			AddQuit(TheAlarm, 0);
		}
		private void AddQuit(Alarm TheAlarm, int minutes) {
			EmailAlarms.Add(TheAlarm.IdAlarm, minutes, @"
	<div style='font-weight:bold; color:#090'>" + TheAlarm.Alarmgroup + " - " + TheAlarm.Alarmtext + @" - quittiert</div>
	<div style='margin-left:20px;'>gekommen: " + TheAlarm.Come.ToString("dd.MM.yyyy HH:mm:ss") + @"</div>
	<div style='margin-left:20px;'>quittiert: <span style='font-weight:bold;'>" + TheAlarm.QuitFrom + " (" + TheAlarm.Quit.ToString("dd.MM.yyyy HH:mm:ss") + @")</span></div>
	<div style='margin-left:20px;'>Bemerkung: <span style='font-weight:bold;'>" + TheAlarm.QuitText + @"</span></div>
	<div style='margin-left:20px;'>status: <span style='font-weight:bold;'>" +
	(TheAlarm.Gone == Alarm.Default ? "<span style='color:#A91919'>anstehend</span>" : "gegangen (" + TheAlarm.Gone.ToString() + ")") + @"</span></div>
	<div style='margin-left:20px; font-weight:bold;'>Alarmdetails:</div>
	<div style='margin-left:40px;'>Beschreibung: " + TheAlarm.Alarmtext + @"</div>" +
	(Alarms.UseAlarmGroup1 ? "<div style='margin-left:40px;'>" + Alarms.NameAlarmGroup1 + ": " + Alarms.GetReadableGroup(Alarms.ALARMGROUP1, TheAlarm.Alarmgroups1) + "</div>" : "") +
	(Alarms.UseAlarmGroup2 ? "<div style='margin-left:40px;'>" + Alarms.NameAlarmGroup2 + ": " + Alarms.GetReadableGroup(Alarms.ALARMGROUP2, TheAlarm.Alarmgroups2) + "</div>" : "") +
	(Alarms.UseAlarmGroup3 ? "<div style='margin-left:40px;'>" + Alarms.NameAlarmGroup3 + ": " + Alarms.GetReadableGroup(Alarms.ALARMGROUP3, TheAlarm.Alarmgroups3) + "</div>" : "") +
	(Alarms.UseAlarmGroup4 ? "<div style='margin-left:40px;'>" + Alarms.NameAlarmGroup4 + ": " + Alarms.GetReadableGroup(Alarms.ALARMGROUP4, TheAlarm.Alarmgroups4) + "</div>" : "") +
	(Alarms.UseAlarmGroup5 ? "<div style='margin-left:40px;'>" + Alarms.NameAlarmGroup5 + ": " + Alarms.GetReadableGroup(Alarms.ALARMGROUP5, TheAlarm.Alarmgroups5) + "</div>" : "") + @"
	<div style='margin-left:40px;'>Gruppe: " + TheAlarm.Alarmgroup + @"</div>
	<div style='margin-left:40px;'>Alarmtype: " + string.Format(getTypeColor(TheAlarm.Alarmtype), TheAlarm.Alarmtype) + @"</div>
	<div style='margin-left:40px;'>Datenpunkt Name: " + TheAlarm.DpName + @"</div>");
			EmailAlarms.countup(TheAlarm.IdAlarm, minutes);
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
			if (IniFile.get("Email", "UseUserPassword") == "true") {
				MailClient.Credentials = new NetworkCredential(IniFile.get("Email", "User"),
					IniFile.get("Email", "Password"));
			}
			MailClient.Send(mailMessage);
			string to = "\r\n";
			foreach (MailAddress sender in mailMessage.To) {
				to += sender.Address + "\r\n";
			}
			mailMessage.To.Clear();
			eventLog.Write(MethodInfo.GetCurrentMethod(), String.Format("Alarm EMail geschrieben an:{0}", to));
		}
		/// <summary>
		/// 
		/// </summary>
		public void reset() {
			bool entered = false;
			int notEntered = 0;
			while (!entered && notEntered < 10) {
				if(Monitor.TryEnter(EmailAlarms.Alarmtext)) {
					EmailAlarms.Alarmtext.Clear();
					Monitor.Exit(EmailAlarms.Alarmtext);
					entered = true;
				} else {
					if (++notEntered >= 10) {
						eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Error,
							"Angefordertes Item blockiert.\r\nAlarm.reset nicht möglich");
					} else {
						Thread.Sleep(10);
					}
				}
			}
			entered = false;
			notEntered = 0;
			while (!entered && notEntered < 10) {
				if (Monitor.TryEnter(EmailAlarms.Alarmsms)) {
					EmailAlarms.Alarmsms.Clear();
					Monitor.Exit(EmailAlarms.Alarmsms);
					entered = true;
				} else {
					if (++notEntered >= 10) {
						eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Error,
							"Angefordertes Item blockiert.\r\nAlarm.reset nicht möglich");
					} else {
						Thread.Sleep(10);
					}
				}
			}
			entered = false;
			notEntered = 0;
			while (!entered && notEntered < 10) {
				if(Monitor.TryEnter(EmailAlarms.count)) {
					EmailAlarms.count.Clear();
					Monitor.Exit(EmailAlarms.count);
					entered = true;
				} else {
					if (++notEntered >= 10) {
						eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Error,
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
			}
		}
		/// <summary>
		/// 
		/// </summary>
		public static class EmailAlarms {
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
							Debug.Write(MethodInfo.GetCurrentMethod(),
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
							Debug.Write(MethodInfo.GetCurrentMethod(),
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
						Debug.Write(MethodInfo.GetCurrentMethod(), "User inaktiv {0}, Alarm: {1}", r.Address, kvp.Key);
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
						Debug.Write(MethodInfo.GetCurrentMethod(), "User inaktiv {0}, Alarm: {1}", r.Address, kvp.Key);
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
