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
//# Revision     : $Rev:: 165                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: Calendar.cs 165 2025-02-09 09:15:16Z                     $ #
//#                                                                                 #
//###################################################################################
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using WebAutomation.Helper;
using FreakaZone.Libraries.wpHelper;
using FreakaZone.Libraries.wpEventLog;

namespace WebAutomation.PlugIns {
	/// <summary>
	/// 
	/// </summary>
	public class Calendar {
		/// <summary></summary>
		private Logger eventLog;
		private int calid;
		private int opcid;
		private string name;
		private bool active;
		public bool Active {
			set { active = value; }
		}
		private Dictionary<string, vevent> events;
		private Dictionary<string, c_rrule> times;
		public Calendar(int _calid, int _opcid, string _name, bool _active) {
			eventLog = new Logger(wpLog.ESource.PlugInCalendar);
			calid = _calid;
			opcid = _opcid;
			name = _name;
			active = _active;
			events = new Dictionary<string, vevent>();
			times = new Dictionary<string, c_rrule>();
			if(active)
				getCalendar();
		}
		public string getCalendar() {

			foreach(KeyValuePair<string, c_rrule> kvp in times) {
				kvp.Value.Stop();
			}
			events.Clear();
			times.Clear();

			using(SQL SQL = new SQL("Calendar active")) {
				string[][] CalActive = SQL.wpQuery(@"SELECT [name], [active] FROM [calendar] WHERE [id_calendar] = {0}", calid);
				bool a = CalActive[0][1] == "True";
				if(a != active) {
					active = a;
					wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar {CalActive[0][0]} ({calid}) = {active}");
				}
			}
			Stopwatch sw = new Stopwatch();
			if(wpDebug.debugCalendar) {
				sw.Start();
				wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Start getCalendar {name} ({calid})");
			}
			if(active) {
				using(SQL SQL = new SQL("Add Events")) {
					string[][] DBEvents = SQL.wpQuery(@"SELECT
					[ce].[id_calendarevent],
					CONVERT(VARCHAR(150), [ce].[dtstart], 126) AS [sdtstart], [ce].[vstart], [ce].[sstart],
					CONVERT(VARCHAR(150), [ce].[dtend], 126) AS [sdtend], [ce].[vend], [ce].[send],
					CASE WHEN [cr].[id_calendarrrule] IS NULL THEN 0 ELSE [cr].[id_calendarrrule] END,
					CONCAT(
						CASE WHEN [cr].[freq] IS NULL THEN '' ELSE CONCAT(
							'FREQ=', UPPER([cr].[freq]),
							CASE WHEN [cr].[intervall] IS NULL THEN '' ELSE CONCAT(';INTERVALL=', [cr].[intervall]) END,
							CASE WHEN [cr].[until] IS NULL THEN '' ELSE CONCAT(';UNTIL=', CONVERT(VARCHAR(150), [cr].[until], 126)) END,
							CASE WHEN [cr].[count] IS NULL THEN '' ELSE CONCAT(';COUNT=', [cr].[count]) END,
							CASE WHEN [cr].[byday] IS NULL THEN '' ELSE CONCAT(';BYDAY=', UPPER([cr].[byday])) END
						) END, ''
					) AS [rrule], [cr].[id_calendarrrule]
					FROM [calendarevent] [ce]
					LEFT OUTER JOIN [calendarrrule] [cr] ON [ce].[id_calendarevent] = [cr].[id_calendarevent]
					WHERE [ce].[id_calendar] = {0}", calid
					);
					for(int ievent = 0; ievent < DBEvents.Length; ievent++) {
						try {
							using(SQL SQL2 = new SQL("Add Reminder")) {
								Dictionary<int, int> dtr = new Dictionary<int, int>();
								Dictionary<int, string> vr = new Dictionary<int, string>();
								Dictionary<int, int> sr = new Dictionary<int, int>();
								string[][] DBreminder = SQL2.wpQuery(@"SELECT
									[id_calendareventreminder]
									,[minutes]
									,[vreminder]
									,[sreminder]
									FROM [calendareventreminder] WHERE [id_calendarevent] = '{0}'", DBEvents[ievent][0]);
								for(int ireminder = 0; ireminder < DBreminder.Length; ireminder++) {
									int id;
									int min;
									if(Int32.TryParse(DBreminder[ireminder][0], out id) && Int32.TryParse(DBreminder[ireminder][1], out min)) {
										dtr.Add(id, min);
										if(DBreminder[ireminder][2] != "")
											vr.Add(id, DBreminder[ireminder][2]);
										if(DBreminder[ireminder][3] != "")
											sr.Add(id, Int32.Parse(DBreminder[ireminder][3]));
									}
								}
								events.Add(DBEvents[ievent][0], new vevent(calid, opcid, name,
									DBEvents[ievent][1], DBEvents[ievent][2], DBEvents[ievent][3],
									dtr, vr, sr,
									DBEvents[ievent][4], DBEvents[ievent][5], DBEvents[ievent][6],
									DBEvents[ievent][7], DBEvents[ievent][8]
								));
							}
						} catch(Exception ex) {
							eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
						}
					}
				}

				foreach(KeyValuePair<string, vevent> ev in events) {
					times.Add(ev.Key, new c_rrule(ev.Value));
					if(ev.Value.IdrRule > 0) {
						using(SQL SQL = new SQL("getExDates")) {
							string[][] DBExdate = SQL.wpQuery(@"SELECT
							CONVERT(VARCHAR(150), [DateTime], 126) AS [sDateTime]
							FROM [calendarexdate] WHERE [id_calendarrrule] = {0}", ev.Value.IdrRule);
							for(int iexdate = 0; iexdate < DBExdate.Length; iexdate++) {
								times[ev.Key].ExDate(DBExdate[iexdate][0]);
							}
						}
					}
					times[ev.Key].getNextDate();
				}
			}

			if(wpDebug.debugCalendar) {
				sw.Stop();
				wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Stop getCalendar {name} ({calid}), Dauer: {sw.ElapsedMilliseconds} ms");
			}
			return "S_OK";
		}
		public string getCalendarTimes() {
			string returns = "";
			Dictionary<DateTime, string> StartTimes = new Dictionary<DateTime, string>();
			foreach(KeyValuePair<string, c_rrule> kvp in times) {
				//StartTimes.Add(kvp.Value.getRes()[1];
			}
			return returns;
		}
		private class c_rrule {
			private int _calId;
			private int _dpId;
			private string _calName;
			private Dictionary<int, DateTime> _dtReminder;
			private Dictionary<int, string> _vReminder;
			private Dictionary<int, int> _sReminder;
			private DateTime _dtStart = DateTime.MinValue;
			private string _vStart;
			private int _sStart;
			private DateTime _dtEnd = DateTime.MinValue;
			private string _vEnd;
			private int _sEnd;
			private int _nextIs = c_rrule._isStart;
			private const int _isReminder = 1;
			private const int _isStart = 2;
			private const int _isEnd = 3;
			private static int _nextReminderId;
			private string _rRule;

			private CalendarFrequenz _freq;

			private int _intervall;
			private bool _has_intervall = false;

			private int _count;
			private bool _has_count = false;

			private DateTime _until;
			private bool _has_until = false;

			private int[] _bymonth;
			private bool _has_bymonth = false;

			private int[] _bymonthday;
			private bool _has_bymonthday = false;

			private int[] _byyearday;
			private bool _has_byyearday = false;

			private int[] _byday;
			private bool _has_byday = false;

			private c_weekday _weekstring = new c_weekday("MO");
			private bool _has_weekstring = false;

			private Dictionary<int, DateTime> _res;
			private List<DateTime> _exdatetime;
			private List<DateTime> _exdate;
			private DateTime _last = DateTime.MinValue;
			private DateTime _next = DateTime.MinValue;
			private System.Timers.Timer _t;
			private System.Timers.Timer _renew;
			private Logger _eventLog;

			public c_rrule(int calid, string dpid, string calname,
					string dtstart, string vstart, string sstart,
					Dictionary<int, int> dtreminder, Dictionary<int, string> vreminder, Dictionary<int, int> sreminder,
					string dtend, string vend, string send,
					string rrule) {
				_eventLog = new Logger(wpLog.ESource.PlugInCalendar);
				_calId = calid;
				_dpId = Int32.Parse(dpid);
				_calName = calname;

				_dtStart = CalendarHelp.parse(dtstart);
				_vStart = vstart;
				if(sstart == "")
					_sStart = 0;
				else
					_sStart = Int32.Parse(sstart);

				_dtReminder = new Dictionary<int, DateTime>();
				_vReminder = new Dictionary<int, string>();
				_sReminder = new Dictionary<int, int>();
				foreach(KeyValuePair<int, int> kvp in dtreminder) {
					_dtReminder.Add(kvp.Key, _dtStart.AddMinutes((double)(-1 * kvp.Value)));
					if(vreminder.ContainsKey(kvp.Key)) {
						_vReminder.Add(kvp.Key, vreminder[kvp.Key]);
					}
					if(sreminder.ContainsKey(kvp.Key)) {
						_sReminder.Add(kvp.Key, sreminder[kvp.Key]);
					}
				}

				_dtEnd = CalendarHelp.parse(dtend);
				_vEnd = vend;
				if(send == "")
					_sEnd = 0;
				else
					_sEnd = Int32.Parse(send);

				_rRule = rrule;
				_res = new Dictionary<int, DateTime>();
				_exdate = new List<DateTime>();
				_exdatetime = new List<DateTime>();
				_t = new System.Timers.Timer();
				_t.AutoReset = false;
				_t.Elapsed += new System.Timers.ElapsedEventHandler(t_elapsed);
				_renew = new System.Timers.Timer();
				_renew.AutoReset = false;
				_renew.Elapsed += new System.Timers.ElapsedEventHandler(renew_elapsed);
			}
			public c_rrule(vevent _ev) {
				_eventLog = new Logger(wpLog.ESource.PlugInCalendar);
				_calId = _ev.Idcal;
				_dpId = _ev.IdDp;
				_calName = _ev.CalName;

				_dtStart = CalendarHelp.parse(_ev.DtStart);
				_vStart = _ev.VStart;
				_sStart = _ev.SStart;

				_dtReminder = new Dictionary<int, DateTime>();
				_vReminder = new Dictionary<int, string>();
				_sReminder = new Dictionary<int, int>();
				foreach(KeyValuePair<int, int> kvp in _ev.DtReminder) {
					_dtReminder.Add(kvp.Key, _dtStart.AddMinutes((double)(-1 * kvp.Value)));
					if(_ev.vReminder.ContainsKey(kvp.Key)) {
						_vReminder.Add(kvp.Key, _ev.vReminder[kvp.Key]);
					}
					if(_ev.sReminder.ContainsKey(kvp.Key)) {
						_sReminder.Add(kvp.Key, _ev.sReminder[kvp.Key]);
					}
				}

				_dtEnd = CalendarHelp.parse(_ev.DtEnd);
				_vEnd = _ev.VEnd;
				_sEnd = _ev.SEnd;

				_rRule = _ev.RRule;
				_res = new Dictionary<int, DateTime>();
				_exdate = new List<DateTime>();
				_exdatetime = new List<DateTime>();
				_t = new System.Timers.Timer();
				_t.AutoReset = false;
				_t.Elapsed += new System.Timers.ElapsedEventHandler(t_elapsed);
				_renew = new System.Timers.Timer();
				_renew.AutoReset = false;
				_renew.Elapsed += new System.Timers.ElapsedEventHandler(renew_elapsed);
			}
			public void Stop() {
				_t.Elapsed -= new System.Timers.ElapsedEventHandler(t_elapsed);
				_t.Enabled = false;
				_t.Dispose();
				if(_renew != null) {
					_renew.Elapsed -= new System.Timers.ElapsedEventHandler(renew_elapsed);
					_renew.Enabled = false;
					_renew.Dispose();
				}
				wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Disposed Calendar '{_calName}' Events");
			}
			private void t_elapsed(object sender, System.Timers.ElapsedEventArgs e) {
				switch(_nextIs) {
					case c_rrule._isReminder:
						if(_dtReminder.ContainsKey(_nextReminderId)) {
							if(_vReminder.ContainsKey(_nextReminderId) && _vReminder[_nextReminderId] != "") {
								// Program.MainProg.wpOPCClient.setValue(opcid, vreminder[nextReminderId], TransferId.TransferSchedule);
								// Datapoints.Get(_dpId).setValue(_vReminder[_nextReminderId]); do we need writevalue??
								Datapoints.Get(_dpId).writeValue(_vReminder[_nextReminderId]);
								_eventLog.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Timer ausgelöst, schalte: Reminder (DP: {_dpId}, {_vReminder[_nextReminderId]})");
							}
							if(_sReminder.ContainsKey(_nextReminderId) && _sReminder[_nextReminderId] > 0) {
								Scene.writeSceneDP(_sReminder[_nextReminderId]);
								_eventLog.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Timer ausgelöst, schalte Scene: Reminder (Scene: {_sReminder[_nextReminderId]})");
							}
						}
						break;
					case c_rrule._isStart:
						if(_vStart != "") {
							// Program.MainProg.wpOPCClient.setValue(opcid, vstart, TransferId.TransferSchedule);
							// Datapoints.Get(_dpId).setValue(_vStart); do we need writevalue??
							Datapoints.Get(_dpId).writeValue(_vStart);
							_eventLog.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Timer ausgelöst, schalte: Start (DP: {_dpId}, {_vStart})");
						}
						if(_sStart > 0) {
							Scene.writeSceneDP(_sStart);
							_eventLog.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Timer ausgelöst, schalte Scene: Start (Scene: {_sStart})");
						}
						break;
					case c_rrule._isEnd:
						if(_vEnd != "") {
							// Program.MainProg.wpOPCClient.setValue(opcid, vend, TransferId.TransferSchedule);
							// Datapoints.Get(_dpId).setValue(_vEnd); do we need writevalue??
							Datapoints.Get(_dpId).writeValue(_vEnd);
							_eventLog.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Timer ausgelöst, schalte: End (DP: {_dpId}, {_vEnd})");
						}
						if(_sEnd > 0) {
							Scene.writeSceneDP(_sEnd);
							_eventLog.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Timer ausgelöst, schalte Scene: End (Scene: {_sEnd})");
						}
						break;
				}
				getNextDate();
			}
			private void renew_elapsed(object sender, System.Timers.ElapsedEventArgs e) {
				wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Wird nix geschaltet - nur ein renew");
				getNextDate();
			}
			public void ExDate(string _date) {
				DateTime toAdd = CalendarHelp.parse(_date);
				_exdate.Add(toAdd.Date);
				_exdatetime.Add(toAdd);
			}
			public void ExDate(List<string> _dates) {
				foreach(string _d in _dates) {
					DateTime toAdd = CalendarHelp.parse(_d);
					_exdate.Add(toAdd.Date);
					_exdatetime.Add(toAdd);
				}
			}
			public Dictionary<int, Dictionary<DateTime, string>> getRes() {
				Dictionary<int, Dictionary<DateTime, string>> returns = new Dictionary<int, Dictionary<DateTime, string>>();
				returns.Add(0, new Dictionary<DateTime, string>());
				returns.Add(1, new Dictionary<DateTime, string>());
				returns.Add(2, new Dictionary<DateTime, string>());
				returns.Add(3, new Dictionary<DateTime, string>());
				returns[0].Add(_res[0], _vEnd);
				returns[1].Add(_res[1], _vStart);
				return returns;
			}
			public Dictionary<int, DateTime> getNextDate() {
				bool hasReminder = false;
				foreach(KeyValuePair<int, DateTime> kvp in _dtReminder) {
					if(kvp.Value > DateTime.Now) {
						wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Reminder {kvp.Key} liegt in der Zukunft - start timer");
						hasReminder = true;
						if(_next > kvp.Value || _next == DateTime.MinValue) {
							_next = kvp.Value;
							_nextReminderId = kvp.Key;
							wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) nächster Reminder: {_next} (ID: {_nextReminderId})");
							_nextIs = c_rrule._isReminder;
						}
					}
				}
				if(!hasReminder) {
					if(_dtStart > DateTime.Now) {
						if(wpDebug.debugCalendar)
							wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Start liegt in der Zukunft - start timer");
						_nextIs = c_rrule._isStart;
						_next = _dtStart;
					} else if(_dtStart < DateTime.Now && _dtEnd > DateTime.Now) {
						if(wpDebug.debugCalendar)
							wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Start war schon - Stop liegt in der Zukunft - start timer");
						_nextIs = c_rrule._isEnd;
						_last = _dtStart;
						_next = _dtEnd;
					} else {
						if(wpDebug.debugCalendar)
							wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Start und Stop war schon - ermittle rrule");
						encodeString();
						getDates();
					}
				}
				_res.Clear();
				_res.Add(0, _last);
				_res.Add(1, _next);
				int maxTime;
				if(wpDebug.debugCalendar) {
					maxTime = 1000 * 60 * 5; // in Millisekunden
				} else {
					maxTime = 1000 * 60 * 60 * 24 * 15; // in Millisekunden (ms * sek * min * stunden * tage)
				}
				if(_res[1] > DateTime.MinValue && _res[1] > DateTime.Now) {
					TimeSpan ts = _res[1] - DateTime.Now;
					if(ts.TotalMilliseconds < maxTime) {
						if(ts.TotalMilliseconds < 1000)
							_t.Interval = 1000;
						else
							_t.Interval = ts.TotalMilliseconds;
						_renew.Enabled = false;
						_t.Enabled = true;
						long ms = (long)Math.Round(_t.Interval, 0);
						TimeSpan tstemp = new TimeSpan(ms * 10000);
						_eventLog.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) OPCWrite Timer gestartet - wird ausgelöst in " +
							$"{tstemp:dd\\ \\T\\a\\g\\e\\ hh\\:mm\\:ss} - " +
							$"schaltet: {(_nextIs == c_rrule._isReminder ? "Reminder" : _nextIs == c_rrule._isStart ? "Start" : "End")}");
					} else {
						_renew.Interval = maxTime - (1000 * 60); // maximale Zeit - 1 Minute
						wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) OPCWrite Timer NICHT gestartet - Timespan zu groß " +
							$"{ts:dd\\ \\T\\a\\g\\e\\ hh\\:mm\\:ss}.\r\n\tnächster Versuch in " +
							$"{new TimeSpan((Int32)_renew.Interval * TimeSpan.TicksPerMillisecond):dd\\ \\T\\a\\g\\e\\ hh\\:mm\\:ss}");
						_t.Enabled = false;
						_renew.Enabled = true;
					}
				} else {
					if(wpDebug.debugCalendar)
						wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Keine Schaltpunkte (mehr) gefunden");
				}


				return _res;
			}

			private void encodeString() {
				foreach(Match m in Regex.Matches(_rRule, "(\\w+)=([A-Za-z0-9,:\\- ]+)")) {
					if(m.Groups.Count > 2) {
						switch(m.Groups[1].Value.ToLower()) {
							case "freq":
								_freq = new CalendarFrequenz(m.Groups[2].Value);
								if(wpDebug.debugCalendar)
									wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Found freq: {m.Groups[2].Value}");
								break;
							case "intervall":
								Int32.TryParse(m.Groups[2].Value, out _intervall);
								_has_intervall = true;
								if(wpDebug.debugCalendar)
									wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Found intervall: {m.Groups[2].Value}");
								break;
							case "count":
								Int32.TryParse(m.Groups[2].Value, out _count);
								_has_count = true;
								if(wpDebug.debugCalendar)
									wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Found count: {m.Groups[2].Value}");
								break;
							case "until":
								_until = CalendarHelp.parse(m.Groups[2].Value);
								_until = _until.Add(new TimeSpan(1, 0, 0, 0));
								_has_until = true;
								if(wpDebug.debugCalendar)
									wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Found until: {m.Groups[2].Value}");
								break;
							case "bymonth":
								_bymonth = CalendarHelp.getIntArray(m.Groups[2].Value);
								_has_bymonth = true;
								if(wpDebug.debugCalendar)
									wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Found bymonth: {m.Groups[2].Value}");
								break;
							case "bymonthday":
								_bymonthday = CalendarHelp.getIntArray(m.Groups[2].Value);
								_has_bymonthday = true;
								if(wpDebug.debugCalendar)
									wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Found bymonthday: {m.Groups[2].Value}");
								break;
							case "byyearday":
								_byyearday = CalendarHelp.getIntArray(m.Groups[2].Value);
								_has_byyearday = true;
								if(wpDebug.debugCalendar)
									wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Found byyearday: {m.Groups[2].Value}");
								break;
							case "byday":
								_byday = CalendarHelp.getWeekDayArray(m.Groups[2].Value);
								_has_byday = true;
								if(wpDebug.debugCalendar)
									wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Found byday: {m.Groups[2].Value}");
								break;
							case "wkst":
								_weekstring = new c_weekday(m.Groups[2].Value);
								_has_weekstring = true;
								if(wpDebug.debugCalendar)
									wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Found wkst: {m.Groups[2].Value}");
								break;
						}
					}
				}
			}

			private void getDates() {
				// nextIsStart = true;
				if(_rRule.Length > 0) {
					switch(_freq.get) {
						case c_frequenz.yearly:
							getYearlySchedule();
							wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) yearly Schedule: last:{_last}, next:{_next}");
							break;
						case c_frequenz.monthly:
							getMonthlySchedule();
							wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) monthly Schedule: last:{_last}, next:{_next}");
							break;
						case c_frequenz.weekly:
							if(_has_byday) {
								getBydaySchedule();
							} else {
								getDaylySchedule(7);
							}
							wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) weekly Schedule: last:{_last}, next:{_next}");
							break;
						case c_frequenz.daily:
							getDaylySchedule();
							wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) daily Schedule: last:{_last}, next:{_next}");
							break;
						case c_frequenz.hourly:
							break;
						case c_frequenz.minutly:
							break;
						case c_frequenz.secondly:
							break;
					}
				}
			}

			private void getBydaySchedule() {
				DateTime dts = _dtStart;
				int wds = (int)dts.DayOfWeek + 1;
				TimeSpan dte = _dtEnd - dts;
				int intervalltemp = 1;
				if(_has_count) {
					for(int i = 0; i < _count; i++) {
						for(int iday = wds; iday <= 7; iday++) {
							if(Array.IndexOf(_byday, iday) >= 0) {
								dts = CalendarHelp.getNextDay(dts, iday);
								if(wpDebug.debugCalendar)
									wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) ByDay Schedule found: {dts}");
								if(_exdate.Contains(dts.Date) || _exdatetime.Contains(dts)) {
									if(wpDebug.debugCalendar)
										wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Ausnahme gefunden: {dts}");
								} else {
									if(_has_until && dts > _until) {
										i = _count;
										break;
									} else {
										if(dts > DateTime.Now) {
											_next = dts;
											i = _count;
											_nextIs = c_rrule._isStart;
											break;
										} else {
											if(dts + dte > DateTime.Now) {
												_next = dts + dte;
												_last = dts;
												i = _count;
												_nextIs = c_rrule._isEnd;
												break;
											} else {
												_last = dts + dte;
											}
										}
									}
								}
							}
						}
						if(_has_intervall) {
							while(++intervalltemp <= _intervall) {
								dts = dts.Add(new TimeSpan(7, 0, 0, 0));
							}
							intervalltemp = 1;
						}
						wds = 0;
					}
				} else {
					bool stop = false;
					do {
						for(int iday = wds; iday <= 7; iday++) {
							if(Array.IndexOf(_byday, iday) >= 0) {
								dts = CalendarHelp.getNextDay(dts, iday);
								if(wpDebug.debugCalendar)
									wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) ByDay Schedule found: {dts}");
								if(_exdate.Contains(dts.Date) || _exdatetime.Contains(dts)) {
									if(wpDebug.debugCalendar)
										wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Ausnahme gefunden: {dts}");
								} else {
									if(_has_until && dts > _until) {
										stop = true;
										break;
									} else {
										if(dts > DateTime.Now) {
											_next = dts;
											stop = true;
											_nextIs = c_rrule._isStart;
											break;
										} else {
											if(dts + dte > DateTime.Now) {
												_next = dts + dte;
												_last = dts;
												stop = true;
												_nextIs = c_rrule._isEnd;
												break;
											} else {
												_last = dts + dte;
											}
										}
									}
								}
							}
						}
						if(_has_intervall) {
							while(++intervalltemp <= _intervall) {
								dts = dts.Add(new TimeSpan(7, 0, 0, 0));
							}
							intervalltemp = 1;
						}
						wds = 0;
					} while(!stop);
				}
			}

			private void getDaylySchedule() {
				getDaylySchedule(1);
			}
			private void getDaylySchedule(int defaultadd) {
				int days = defaultadd;
				DateTime dts = _dtStart;
				TimeSpan dte = _dtEnd - dts;
				if(_has_intervall) {
					days *= _intervall;
				}
				if(_has_count) {
					for(int i = 0; i < _count; i++) {
						dts = dts + new TimeSpan(days, 0, 0, 0);
						if(wpDebug.debugCalendar)
							wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Daily Schedule found: {dts}");
						if(_exdate.Contains(dts.Date) || _exdatetime.Contains(dts)) {
							if(wpDebug.debugCalendar)
								wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Ausnahme gefunden: {dts}");
						} else {
							if(_has_until && dts > _until) {
								i = _count;
							} else {
								if(dts > DateTime.Now) {
									_next = dts;
									i = _count;
									_nextIs = c_rrule._isStart;
								} else {
									if(dts + dte > DateTime.Now) {
										_next = dts + dte;
										_last = dts;
										_nextIs = c_rrule._isEnd;
										i = _count;
									} else {
										_last = dts + dte;
									}
								}
							}
						}
					}
				} else {
					bool stop = false;
					do {
						dts = dts + new TimeSpan(days, 0, 0, 0);
						if(wpDebug.debugCalendar)
							wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Daily Schedule found: {dts}");
						if(_exdate.Contains(dts.Date) || _exdatetime.Contains(dts)) {
							if(wpDebug.debugCalendar)
								wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Ausnahme gefunden: {dts}");
						} else {
							if(_has_until && dts > _until) {
								stop = true;
							} else {
								if(dts > DateTime.Now) {
									_next = dts;
									stop = true;
									_nextIs = c_rrule._isStart;
								} else {
									if(dts + dte > DateTime.Now) {
										_next = dts + dte;
										_last = dts;
										_nextIs = c_rrule._isEnd;
										stop = true;
									} else {
										_last = dts + dte;
									}
								}
							}
						}
					} while(!stop);
				}
			}

			private void getMonthlySchedule() {
				int month = 1;
				DateTime dts = _dtStart;
				int originalDay = dts.Day;
				TimeSpan dte = _dtEnd - dts;
				if(_has_intervall) {
					month *= _intervall;
				}
				if(_has_count) {
					for(int i = 0; i < _count; i++) {
						dts = dts.AddMonths(month);
						if(dts.Day != originalDay) {
							dts = dts.AddMonths(month);
							while(dts.Day != originalDay) {
								dts = dts.AddDays(1);
							}
						}
						if(wpDebug.debugCalendar)
							wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Monthly Schedule found: {dts}");
						if(_exdate.Contains(dts.Date) || _exdatetime.Contains(dts)) {
							if(wpDebug.debugCalendar)
								wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Ausnahme gefunden: {dts}");
						} else {
							if(_has_until && dts > _until) {
								i = _count;
							} else {
								if(dts > DateTime.Now) {
									_next = dts;
									i = _count;
									_nextIs = c_rrule._isStart;
								} else {
									if(dts + dte > DateTime.Now) {
										_next = dts + dte;
										_last = dts;
										_nextIs = c_rrule._isEnd;
										i = _count;
									} else {
										_last = dts + dte;
									}
								}
							}
						}
					}
				} else {
					bool stop = false;
					do {
						dts = dts.AddMonths(month);
						if(dts.Day != originalDay) {
							dts = dts.AddMonths(month);
							while(dts.Day != originalDay) {
								dts = dts.AddDays(1);
							}
						}
						if(wpDebug.debugCalendar)
							wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Monthly Schedule found: {dts}");
						if(_exdate.Contains(dts.Date) || _exdatetime.Contains(dts)) {
							if(wpDebug.debugCalendar)
								wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Ausnahme gefunden: {dts}");
						} else {
							if(_has_until && dts > _until) {
								stop = true;
							} else {
								if(dts > DateTime.Now) {
									_next = dts;
									stop = true;
									_nextIs = c_rrule._isStart;
								} else {
									if(dts + dte > DateTime.Now) {
										_next = dts + dte;
										_last = dts;
										_nextIs = c_rrule._isEnd;
										stop = true;
									} else {
										_last = dts + dte;
									}
								}
							}
						}
					} while(!stop);
				}
			}

			private void getYearlySchedule() {
				int year = 1;
				DateTime dts = _dtStart;
				TimeSpan dte = _dtEnd - dts;
				if(_has_intervall) {
					year *= _intervall;
				}
				if(_has_count) {
					for(int i = 0; i < _count; i++) {
						dts = dts.AddYears(year);
						if(_exdate.Contains(dts.Date) || _exdatetime.Contains(dts)) {
							if(wpDebug.debugCalendar)
								wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Ausnahme gefunden: {dts}");
						} else {
							if(_has_until && dts > _until) {
								i = _count;
							} else {
								if(dts > DateTime.Now) {
									_next = dts;
									i = _count;
									_nextIs = c_rrule._isStart;
								} else {
									if(dts + dte > DateTime.Now) {
										_next = dts + dte;
										_last = dts;
										_nextIs = c_rrule._isEnd;
										i = _count;
									} else {
										_last = dts + dte;
									}
								}
							}
						}
					}
				} else {
					bool stop = false;
					do {
						dts = dts.AddYears(year);
						if(_exdate.Contains(dts.Date) || _exdatetime.Contains(dts)) {
							if(wpDebug.debugCalendar)
								wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Calendar '{_calName}' ({_calId}) Ausnahme gefunden: {dts}");
						} else {
							if(_has_until && dts > _until) {
								stop = true;
							} else {
								if(dts > DateTime.Now) {
									_next = dts;
									stop = true;
									_nextIs = c_rrule._isStart;
								} else {
									if(dts + dte > DateTime.Now) {
										_next = dts + dte;
										_last = dts;
										_nextIs = c_rrule._isEnd;
										stop = true;
									} else {
										_last = dts + dte;
									}
								}
							}
						}
					} while(!stop);
				}
			}

			/// <summary>
			/// Nur damit die Fehlermeldung 'nicht verwendete Variable' beim Kompilieren verschwindet
			/// </summary>
			/// <returns></returns>
			private bool DamitsVerwendetWird() {
				return _has_byday && _has_bymonth && _has_bymonthday && _has_byyearday && _has_count && _has_intervall && _has_until && _has_weekstring;
			}
		}

		private class vevent {
			private int _idrRule;
			public int IdrRule {
				get { return _idrRule; }
			}
			private int _idCal;
			public int Idcal {
				get { return _idCal; }
			}
			private int _idDp;
			public int IdDp {
				get { return _idDp; }
			}
			private string _calName;
			public string CalName {
				get { return _calName; }
			}
			private Dictionary<int, int> _dtReminder;
			public Dictionary<int, int> DtReminder {
				get { return _dtReminder; }
			}
			private Dictionary<int, string> _vReminder;
			public Dictionary<int, string> vReminder {
				get { return _vReminder; }
			}
			private Dictionary<int, int> _sReminder;
			public Dictionary<int, int> sReminder {
				get { return _sReminder; }
			}
			private string _dtStart;
			public string DtStart {
				get { return _dtStart; }
			}
			private string _vStart;
			public string VStart {
				get { return _vStart; }
			}
			private int _sStart;
			public int SStart {
				get { return _sStart; }
			}
			private string _dtEnd;
			public string DtEnd {
				get { return _dtEnd; }
			}
			private string _vEnd;
			public string VEnd {
				get { return _vEnd; }
			}
			private int _sEnd;
			public int SEnd {
				get { return _sEnd; }
			}
			private string _rRule;
			public string RRule {
				get { return _rRule; }
			}
			public vevent(int idcal, int iddp, string calname,
					string dtstart, string vstart, string sstart,
					Dictionary<int, int> dtreminder, Dictionary<int, string> vreminder, Dictionary<int, int> sreminder,
					string dtend, string vend, string send,
					string idrrule, string rrule) {
				_idCal = idcal;
				_calName = calname;
				_idDp = iddp;
				_dtReminder = dtreminder;
				_vReminder = vreminder;
				_sReminder = sreminder;
				_dtStart = dtstart;
				_vStart = vstart;
				if(sstart == "")
					_sStart = 0;
				else
					_sStart = Int32.Parse(sstart);
				_dtEnd = dtend;
				_vEnd = vend;
				if(send == "")
					_sEnd = 0;
				else
					_sEnd = Int32.Parse(send);
				_idrRule = Int32.Parse(idrrule);
				_rRule = rrule;
			}
			public vevent(int idcal, int idopc, string calname,
				string dtstart, string vstart, string sstart,
				Dictionary<int, int> dtreminder, Dictionary<int, string> vreminder, Dictionary<int, int> sreminder,
				string dtend, string vend, string send)
				: this(idcal, idopc, calname, dtstart, vstart, sstart, dtreminder, vreminder, sreminder, dtend, vend, send, "0", "") {
			}
		}
	}

	public class Calendars {
		private static Dictionary<int, Calendar> _calendarDic = new Dictionary<int, Calendar>();
		private Logger eventLog;
		public Calendars() {
			wpDebug.Write(MethodInfo.GetCurrentMethod(), "Calendars init");
			eventLog = new Logger(wpLog.ESource.PlugInCalendar);
			using(SQL SQL = new SQL("Calendars")) {
				string[][] DBCalendar = SQL.wpQuery("SELECT [id_calendar], [id_dp], [name], [active] FROM [calendar]");
				int calint;
				int opcint;
				for(int iCalendar = 0; iCalendar < DBCalendar.Length; iCalendar++) {
					if(!Int32.TryParse(DBCalendar[iCalendar][1], out opcint)) {
						opcint = 0;
					}
					if(Int32.TryParse(DBCalendar[iCalendar][0], out calint)) {
						eventLog.Write(MethodInfo.GetCurrentMethod(), "Add Calendar '{1}' ({0})", DBCalendar[iCalendar][0], DBCalendar[iCalendar][2]);
						_calendarDic.Add(calint, new Calendar(calint, opcint, DBCalendar[iCalendar][2], DBCalendar[iCalendar][3] == "True"));
					}
				}
			}
			eventLog.Write(MethodInfo.GetCurrentMethod(), "Calendars gestartet");
		}
		public string renewCalendar(int _id) {
			if(_calendarDic.ContainsKey(_id)) {
				eventLog.Write(MethodInfo.GetCurrentMethod(), "Renew Calendar {0}", _id);
				return _calendarDic[_id].getCalendar();
			} else {
				eventLog.Write(MethodInfo.GetCurrentMethod(), "Add Calendar {0}", _id);
				using(SQL SQL = new SQL("Calendar")) {
					int opcint;
					string[][] DBCalendar = SQL.wpQuery("SELECT [id_dp], [name], [active] FROM [calendar] WHERE [id_calendar] = {0}", _id);
					for(int iCalendar = 0; iCalendar < DBCalendar.Length; iCalendar++) {
						if(!Int32.TryParse(DBCalendar[iCalendar][0], out opcint)) {
							opcint = 0;
						}
						_calendarDic.Add(_id, new Calendar(_id, opcint, DBCalendar[iCalendar][1], DBCalendar[iCalendar][2] == "True"));
					}
				}
				return "S_OK";
			}
		}
	}
}
