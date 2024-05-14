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
//# File-ID      : $Id:: Alarm.cs 76 2024-01-24 07:36:57Z                         $ #
//#                                                                                 #
//###################################################################################
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Timers;
using WebAutomation.Helper;

namespace WebAutomation.PlugIns {
	/// <summary>
	/// 
	/// </summary>
	public class Alarm {
		/// <summary></summary>
		private static Logger eventLog;
		/// <summary></summary>
		public static DateTime Default = new DateTime(2000, 01, 01, 0, 0, 0);
		/// <summary></summary>
		private int _idalarm;
		/// <summary></summary>
		public int IdAlarm {
			get { return _idalarm; }
			set { _idalarm = value; }
		}
		/// <summary></summary>
		private string _dpname;
		/// <summary></summary>
		public string DpName {
			get { return _dpname; }
		}
		/// <summary></summary>
		private int _iddp;
		/// <summary></summary>
		public int IdDp {
			get { return _iddp; }
		}
		/// <summary></summary>
		private bool _inalarm;
		/// <summary></summary>
		public bool InAlarm {
			get { return _inalarm; }
			set { _inalarm = value; }
		}
		/// <summary></summary>
		private bool _isquit;
		/// <summary></summary>
		public bool IsQuit {
			get { return _isquit; }
			set { _isquit = value; }
		}
		/// <summary></summary>
		private DateTime _come;
		/// <summary></summary>
		public DateTime Come {
			get { return _come; }
			set { _come = value; }
		}
		/// <summary></summary>
		private DateTime _gone;
		/// <summary></summary>
		public DateTime Gone {
			get { return _gone; }
			set { _gone = value; }
		}
		/// <summary></summary>
		private DateTime _quit;
		/// <summary></summary>
		public DateTime Quit {
			get { return _quit; }
			set { _quit = value; }
		}
		/// <summary></summary>
		private DateTime _alarmupdate;
		/// <summary></summary>
		public DateTime AlarmUpdate {
			get { return _alarmupdate; }
			set { _alarmupdate = value; }
		}
		/// <summary></summary>
		private string _quitfrom;
		/// <summary></summary>
		public string QuitFrom {
			get { return _quitfrom; }
			set { _quitfrom = value; }
		}
		/// <summary></summary>
		private string _quittext;
		/// <summary></summary>
		public string QuitText {
			get { return _quittext; }
			set { _quittext = value; }
		}
		/// <summary></summary>
		private string _alarmgroup;
		/// <summary></summary>
		public string Alarmgroup {
			get { return _alarmgroup; }
			set { _alarmgroup = value; }
		}
		/// <summary></summary>
		private string _alarmgroups1;
		/// <summary></summary>
		public string Alarmgroups1 {
			get { return _alarmgroups1; }
			set { _alarmgroups1 = value; }
		}
		/// <summary></summary>
		private string _alarmgroups2;
		/// <summary></summary>
		public string Alarmgroups2 {
			get { return _alarmgroups2; }
			set { _alarmgroups2 = value; }
		}
		/// <summary></summary>
		private string _alarmgroups3;
		/// <summary></summary>
		public string Alarmgroups3 {
			get { return _alarmgroups3; }
			set { _alarmgroups3 = value; }
		}
		/// <summary></summary>
		private string _alarmgroups4;
		/// <summary></summary>
		public string Alarmgroups4 {
			get { return _alarmgroups4; }
			set { _alarmgroups4 = value; }
		}
		/// <summary></summary>
		private string _alarmgroups5;
		/// <summary></summary>
		public string Alarmgroups5 {
			get { return _alarmgroups5; }
			set { _alarmgroups5 = value; }
		}
		/// <summary></summary>
		private string _alarmtype;
		/// <summary></summary>
		public string Alarmtype {
			get { return _alarmtype; }
			set { _alarmtype = value; }
		}
		/// <summary></summary>
		private bool _autoquit;
		/// <summary></summary>
		public bool Autoquit {
			get { return _autoquit; }
			set { _autoquit = value; }
		}
		/// <summary></summary>
		private string _alarmname;
		/// <summary></summary>
		public string Alarmname {
			get { return _alarmname; }
			set { _alarmname = value; }
		}
		/// <summary></summary>
		private string _alarmtext;
		/// <summary></summary>
		public string Alarmtext {
			get { return _alarmtext; }
			set { _alarmtext = value; }
		}
		/// <summary></summary>
		private string _alarmlink;
		/// <summary></summary>
		public string Alarmlink {
			get { return _alarmlink; }
			set { _alarmlink = value; }
		}
		/// <summary></summary>
		private string _condition;
		/// <summary></summary>
		public string Condition {
			get { return _condition; }
			set { _condition = value; }
		}
		private int _lastPlantMode;
		public int LastPlantMode {
			get { return _lastPlantMode; }
			set { _lastPlantMode = value; }
		}
		/// <summary></summary>
		private string _min;
		/// <summary></summary>
		public string Min {
			get { return _min; }
			set { _min = value; }
		}
		/// <summary></summary>
		private int _max;
		/// <summary></summary>
		public int Max {
			get { return _max; }
			set { _max = value; }
		}
		/// <summary></summary>
		private Timer Delay;
		/// <summary></summary>
		private bool _hasDelay;
		/// <summary></summary>
		public bool hasDelay {
			get { return _hasDelay; }
		}
		/// <summary></summary>
		private bool _timerstarted;
		/// <summary></summary>
		public bool TimerStarted {
			get { return _timerstarted; }
		}
		/// <summary></summary>
		private bool _nodelaycome;
		/// <summary></summary>
		public bool noDelayCome {
			set { _nodelaycome = value; }
		}
		private bool _wartung;
		public bool wartung { 
			set { _wartung = value; }
			get { return _wartung; }
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="globalServer"></param>
		/// <param name="idalarm"></param>
		/// <param name="dpid"></param>
		/// <param name="dpname"></param>
		/// <param name="sec"></param>
		public Alarm(int idalarm, int dpid, string dpname, int sec) {
			eventLog = new Logger(wpEventLog.PlugInAlarm);
			init(idalarm, dpid, dpname, sec);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="idalarm"></param>
		/// <param name="dpid"></param>
		/// <param name="dpname"></param>
		/// <param name="sec"></param>
		private void init(int idalarm, int dpid, string dpname, int sec) {
			this._idalarm = idalarm;
			this._iddp = dpid;
			this._dpname = dpname;
			this._come = Default;
			this._gone = Default;
			this._quit = Default;
			this._inalarm = false;
			this._isquit = false;
			this._timerstarted = false;
			this._wartung = false;
			if (sec > 0) {
				_hasDelay = true;
				Delay = new Timer((double)(sec * 1000));
				Delay.Elapsed += new ElapsedEventHandler(Delay_Tick);
			} else {
				_hasDelay = false;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Delay_Tick(object sender, ElapsedEventArgs e) {
			if (_nodelaycome) {
				setCome(DateTime.Now);
				eventLog.Write("{0} ({1}) - Alarmdelay finished - real come",
					this._alarmtext, this._dpname);
			}
			Delay.Stop();
			_timerstarted = false;
		}
		/// <summary>
		/// 
		/// </summary>
		public void TimerStart() {
			if (_hasDelay) {
				Delay.Stop();
				Delay.Start();
				_timerstarted = true;
				eventLog.Write("{0} ({1}) - Alarm come with delay - start",
					this._alarmtext, this._dpname);
			}
		}
		/// <summary>
		/// 
		/// </summary>
		public void TimerStop() {
			if (_hasDelay) {
				Delay.Stop();
				_timerstarted = false;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="Now"></param>
		public void setCome(DateTime Now) {
			_alarmupdate = Now;
			_come = Now;
			_gone = Alarm.Default;
			_inalarm = true;
			string sqlautoquit = "NULL";
			string sqlautoquitfrom = "NULL";
			if (_autoquit) {
				_quit = Now;
				_isquit = true;
				sqlautoquit = String.Format("'{0}'",
					Now.ToString(SQL.DateTimeFormat));
				sqlautoquitfrom = "'wpSystem'";
			} else {
				_isquit = false;
				_quit = Alarm.Default;
			}
			using (SQL SQL = new SQL("Alarm Come in Historic")) {
				SQL.wpNonResponse(
					"INSERT INTO [alarmhistoric] ([id_alarm], [come], [quit], [quitfrom], [text]) " +
					"VALUES ({0}, '{1}', {2}, {3}, '{4}')",
					_idalarm,
					Now.ToString(SQL.DateTimeFormat),
					sqlautoquit, sqlautoquitfrom, this.Alarmtext);
			}
			wpDebug.Write("Alarm Come: {0} - {2} ({1})", this.Alarmname, this.IdDp, this.DpName);
			Program.MainProg.AlarmToMail(this);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="Now"></param>
		public void setGone(DateTime Now) {
			_alarmupdate = Now;
			_gone = Now;
			_inalarm = false;
			using (SQL SQL = new SQL("Alarm Gone in Historic")) {
				SQL.wpNonResponse(
					"UPDATE [alarmhistoric] SET [gone] = '{0}'  WHERE [id_alarm] = {1} AND [gone] IS NULL",
					Now.ToString(SQL.DateTimeFormat),
					_idalarm);
			}
			wpDebug.Write("Alarm Gone: {0} - {2} ({1})", this.Alarmname, this.IdDp, this.DpName);
		}
		public void updateDelay(int sec) {
			TimerStop();
			Delay = null;
			if (sec > 0) {
				_hasDelay = true;
				Delay = new Timer((double)(sec * 1000));
				Delay.Elapsed += new ElapsedEventHandler(Delay_Tick);
			} else {
				_hasDelay = false;
			}
		}
		public string getReadableAlarmGroup1() {
			return Alarmgroups1 == "" ? "-" : Alarmgroups1;
		}
		public string getReadableAlarmGroup2() {
			return Alarmgroups2 == "" ? "-" : Alarmgroups2;
		}
		public string getReadableAlarmGroup3() {
			return Alarmgroups3 == "" ? "-" : Alarmgroups3;
		}
		public string getReadableAlarmGroup4() {
			return Alarmgroups4 == "" ? "-" : Alarmgroups4;
		}
		public string getReadableAlarmGroup5() {
			return Alarmgroups5 == "" ? "-" : Alarmgroups5;
		}
	}
	/// <summary>
	/// 
	/// </summary>
	public class Alarms {
		/// <summary>
		/// AlarmList mit den hinzuzufuegenden Alarmen<br />
		/// Key: id_alarm<br />
		/// Value: Alarm
		/// </summary>
		private static Dictionary<int, Alarm> AlarmList = new Dictionary<int, Alarm>();
		/// <summary></summary>
		private static Logger eventLog;
		public static bool useAlarmGroup1 = false;
		public static bool useAlarmGroup2 = false;
		public static bool useAlarmGroup3 = false;
		public static bool useAlarmGroup4 = false;
		public static bool useAlarmGroup5 = false;
		public static string nameAlarmGroup1 = "";
		public static string nameAlarmGroup2 = "";
		public static string nameAlarmGroup3 = "";
		public static string nameAlarmGroup4 = "";
		public static string nameAlarmGroup5 = "";
		/// <summary>
		/// 
		/// </summary>
		public static void Init() {
			eventLog = new Logger(wpEventLog.PlugInAlarm);
			using (SQL SQL = new SQL("Init Alarms")) {
				string[][] AlarmGroups = SQL.wpQuery(@"
					SELECT [key], [value] FROM [cfg] WHERE
					[key] = 'usealarmgroup1' OR
					[key] = 'usealarmgroup2' OR
					[key] = 'usealarmgroup3' OR
					[key] = 'usealarmgroup4' OR
					[key] = 'usealarmgroup5' OR
					[key] = 'namealarmgroup1' OR
					[key] = 'namealarmgroup2' OR
					[key] = 'namealarmgroup3' OR
					[key] = 'namealarmgroup4' OR
					[key] = 'namealarmgroup5'
					ORDER BY [key]
				");
				if (AlarmGroups.Length == 10) {
					nameAlarmGroup1 = AlarmGroups[0][1];
					nameAlarmGroup2 = AlarmGroups[1][1];
					nameAlarmGroup3 = AlarmGroups[2][1];
					nameAlarmGroup4 = AlarmGroups[3][1];
					nameAlarmGroup5 = AlarmGroups[4][1];
					useAlarmGroup1 = AlarmGroups[5][1] == "True";
					useAlarmGroup2 = AlarmGroups[6][1] == "True";
					useAlarmGroup3 = AlarmGroups[7][1] == "True";
					useAlarmGroup4 = AlarmGroups[8][1] == "True";
					useAlarmGroup5 = AlarmGroups[9][1] == "True";
				}
				string[][] DBAlarms = SQL.wpQuery(@"
				SELECT
					[a].[id_alarm], [a].[name], [a].[text], [a].[link], [t].[name], [t].[autoquit],
					[g].[name], [dp].[id_dp], [dp].[name], [c].[condition],
					[a].[min], [a].[max], [a].[delay],
					COUNT([atm].[id_email]) AS [emailcounter],
					ISNULL(SUM([atm].[minutes]), 0) AS [emailminutes],
					[ag1].[name], [ag2].[name], [ag3].[name], [ag4].[name], [ag5].[name]
				FROM [alarm] [a]
				INNER JOIN [alarmtype] [t] ON [a].[id_alarmtype] = [t].[id_alarmtype]
				INNER JOIN [alarmgroup] [g] ON [a].[id_alarmgroup] = [g].[id_alarmgroup]
				INNER JOIN [dp] ON [a].[id_dp] = [dp].[id_dp]
				INNER JOIN [alarmcondition] [c] ON [a].[id_alarmcondition] = [c].[id_alarmcondition]
				LEFT JOIN [alarmgroups1] [ag1] ON [a].[id_alarmgroups1] = [ag1].[id_alarmgroups1]
				LEFT JOIN [alarmgroups2] [ag2] ON [a].[id_alarmgroups2] = [ag2].[id_alarmgroups2]
				LEFT JOIN [alarmgroups3] [ag3] ON [a].[id_alarmgroups3] = [ag3].[id_alarmgroups3]
				LEFT JOIN [alarmgroups4] [ag4] ON [a].[id_alarmgroups4] = [ag4].[id_alarmgroups4]
				LEFT JOIN [alarmgroups5] [ag5] ON [a].[id_alarmgroups5] = [ag5].[id_alarmgroups5]
				LEFT JOIN [alarmtoemail] [atm] ON [atm].[id_alarm] = [a].[id_alarm]
				GROUP BY [a].[id_alarm], [a].[name], [a].[text], [a].[link], [t].[name], [t].[autoquit],
					[g].[name], [dp].[id_dp], [dp].[name], [c].[condition],
					[a].[min], [a].[max], [a].[delay],
					[ag1].[name], [ag2].[name], [ag3].[name], [ag4].[name], [ag5].[name]");
				for (int ialarms = 0; ialarms < DBAlarms.Length; ialarms++) {
					int idalarm = Int32.Parse(DBAlarms[ialarms][0]);
					int idpoint = Int32.Parse(DBAlarms[ialarms][7]);
					Alarm TheAlarm;
					int delay;
					if (Int32.TryParse(DBAlarms[ialarms][12], out delay)) {
						TheAlarm = new Alarm(idalarm, idpoint, DBAlarms[ialarms][8], delay);
					} else {
						TheAlarm = new Alarm(idalarm, idpoint, DBAlarms[ialarms][8], 0);
					}
					int? emailCounter = SQL.convertNumeric(DBAlarms[ialarms][13]);
					int? emailMinutes = SQL.convertNumeric(DBAlarms[ialarms][14]);
					TheAlarm.Alarmname = DBAlarms[ialarms][1];
					TheAlarm.Alarmtext = DBAlarms[ialarms][2];
					TheAlarm.Alarmlink = DBAlarms[ialarms][3];
					TheAlarm.Alarmtype = DBAlarms[ialarms][4];
					TheAlarm.Autoquit = DBAlarms[ialarms][5] == "True";
					TheAlarm.Alarmgroup = DBAlarms[ialarms][6];
					TheAlarm.Condition = DBAlarms[ialarms][9];
					TheAlarm.Min = DBAlarms[ialarms][10];
					TheAlarm.Alarmgroups1 = DBAlarms[ialarms][15];
					TheAlarm.Alarmgroups2 = DBAlarms[ialarms][16];
					TheAlarm.Alarmgroups3 = DBAlarms[ialarms][17];
					TheAlarm.Alarmgroups4 = DBAlarms[ialarms][18];
					TheAlarm.Alarmgroups5 = DBAlarms[ialarms][19];
					if (TheAlarm.Condition == ">x<" || TheAlarm.Condition == "<x>")
						TheAlarm.Max = Int32.Parse(DBAlarms[ialarms][11]);
					Program.MainProg.setAlarm(idpoint, TheAlarm);
				}
			}

			using (SQL SQL = new SQL("Add Aktive Alarms")) {
				string[][] ActiveAlarms = SQL.wpQuery(@"
SELECT 
	[t].[id_alarm], [t].[id_dp], [t].[come], [t].[gone], [t].[quit]
FROM (
	SELECT
		ROW_NUMBER() OVER (PARTITION BY [ah].[id_alarm] ORDER BY [ah].[come] DESC) AS [ranking],
		[ah].[id_alarm], [a].[id_dp], [ah].[come], [ah].[gone], [ah].[quit]
	FROM [alarmhistoric] [ah]
	INNER JOIN [alarm] [a] ON [a].[id_alarm] = [ah].[id_alarm]
	WHERE ([ah].[gone] IS NULL OR [ah].[quit] IS NULL)
) [t] WHERE [t].[ranking] = 1");
				for (int ialarms = 0; ialarms < ActiveAlarms.Length; ialarms++) {
					int idpoint = Int32.Parse(ActiveAlarms[ialarms][1]);
					Alarm TheAlarm = Program.MainProg.getAlarm(idpoint);
					if (TheAlarm != null) {
						TheAlarm.Come = DateTime.Parse(ActiveAlarms[ialarms][2]);
						TheAlarm.Gone = Alarm.Default;
						TheAlarm.InAlarm = false;
						TheAlarm.Quit = Alarm.Default;
						TheAlarm.IsQuit = false;
						if (ActiveAlarms[ialarms][3] != "") {
							TheAlarm.Gone = DateTime.Parse(ActiveAlarms[ialarms][3]);
							TheAlarm.InAlarm = true;
						}
						if (ActiveAlarms[ialarms][4] != "") {
							TheAlarm.Quit = DateTime.Parse(ActiveAlarms[ialarms][4]);
							TheAlarm.IsQuit = true;
						}
						TheAlarm.InAlarm = true;
					}
				}
			}
			eventLog.Write("Alarm PlugIn geladen");
		}
		public static string updateAlarmGroups() {
			using (SQL SQL = new SQL("Add Alarms")) {
				string[][] AlarmGroups = SQL.wpQuery(@"
					SELECT [key], [value] FROM [cfg] WHERE
					[key] = 'usealarmgroup1' OR
					[key] = 'usealarmgroup2' OR
					[key] = 'usealarmgroup3' OR
					[key] = 'usealarmgroup4' OR
					[key] = 'usealarmgroup5'
					ORDER BY [key]
				");
				if (AlarmGroups.Length == 5) {
					useAlarmGroup1 = AlarmGroups[0][1] == "True";
					useAlarmGroup2 = AlarmGroups[1][1] == "True";
					useAlarmGroup3 = AlarmGroups[2][1] == "True";
					useAlarmGroup4 = AlarmGroups[3][1] == "True";
					useAlarmGroup5 = AlarmGroups[4][1] == "True";
				}
				string[][] DBAlarms = SQL.wpQuery(@"
					SELECT
						[a].[id_alarm], [dp].[id_dp], 
						[ag1].[name], [ag2].[name], [ag3].[name], [ag4].[name], [ag5].[name]
					FROM [alarm] [a]
					INNER JOIN [dp] ON [a].[id_dp] = [dp].[id_dp]
					LEFT JOIN [alarmgroups1] [ag1] ON [a].[id_alarmgroups1] = [ag1].[id_alarmgroups1]
					LEFT JOIN [alarmgroups2] [ag2] ON [a].[id_alarmgroups2] = [ag2].[id_alarmgroups2]
					LEFT JOIN [alarmgroups3] [ag3] ON [a].[id_alarmgroups3] = [ag3].[id_alarmgroups3]
					LEFT JOIN [alarmgroups4] [ag4] ON [a].[id_alarmgroups4] = [ag4].[id_alarmgroups4]
					LEFT JOIN [alarmgroups5] [ag5] ON [a].[id_alarmgroups5] = [ag5].[id_alarmgroups5]");

				for (int ialarms = 0; ialarms < DBAlarms.Length; ialarms++) {
					int idpoint = Int32.Parse(DBAlarms[ialarms][1]);
					Alarm TheAlarm = Program.MainProg.getAlarm(idpoint);
					if (TheAlarm != null) {
						TheAlarm.Alarmgroups1 = DBAlarms[ialarms][2];
						TheAlarm.Alarmgroups2 = DBAlarms[ialarms][3];
						TheAlarm.Alarmgroups3 = DBAlarms[ialarms][4];
						TheAlarm.Alarmgroups4 = DBAlarms[ialarms][5];
						TheAlarm.Alarmgroups5 = DBAlarms[ialarms][6];
						TheAlarm.AlarmUpdate = DateTime.Now;
					}
				}
			}
			wpDebug.Write("Update Alarmgroups");
			return "S_OK";
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="value"></param>
		/// <param name="Now"></param>
		public static void setAlarm(int id, string value, DateTime Now) {
			Alarm TheAlarm = Program.MainProg.getAlarm(id);
			string issep;
			string mustsep;
			string sep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
			if (sep == ".") {
				issep = ",";
				mustsep = ".";
			} else {
				issep = ".";
				mustsep = ",";
			}
			decimal ivaluedec;
			int ivalueint;
			decimal param1dec;
			try {
				switch (TheAlarm.Condition) {
					case "=":
						if (value == TheAlarm.Min) {
							TheAlarm.noDelayCome = true;
							if (!TheAlarm.InAlarm) {
								if (TheAlarm.hasDelay) {
									if (!TheAlarm.TimerStarted) TheAlarm.TimerStart();
								} else {
									TheAlarm.setCome(Now);
								}
							}
						}
						if (value != TheAlarm.Min) {
							TheAlarm.noDelayCome = false;
							TheAlarm.TimerStop();
							if (TheAlarm.InAlarm) TheAlarm.setGone(Now);
						}
						break;
					case "<>":
						if (value != TheAlarm.Min) {
							TheAlarm.noDelayCome = true;
							if (!TheAlarm.InAlarm) {
								if (TheAlarm.hasDelay) {
									if (!TheAlarm.TimerStarted) TheAlarm.TimerStart();
								} else {
									TheAlarm.setCome(Now);
								}
							}
						}
						if (value == TheAlarm.Min) {
							TheAlarm.noDelayCome = false;
							TheAlarm.TimerStop();
							if (TheAlarm.InAlarm) TheAlarm.setGone(Now);
						}
						break;
					case ">":
						if (Decimal.TryParse(TheAlarm.Min.Replace(issep, mustsep), out param1dec) &&
							Decimal.TryParse(value.Replace(issep, mustsep), out ivaluedec)) {
							if (ivaluedec > param1dec) {
								TheAlarm.noDelayCome = true;
								if (!TheAlarm.InAlarm) {
									if (TheAlarm.hasDelay) {
										if (!TheAlarm.TimerStarted) TheAlarm.TimerStart();
									} else {
										TheAlarm.setCome(Now);
									}
								}
							}
							if (ivaluedec <= param1dec) {
								TheAlarm.noDelayCome = false;
								TheAlarm.TimerStop();
								if (TheAlarm.InAlarm) TheAlarm.setGone(Now);
							}
						}
						break;
					case "<":
						if (Decimal.TryParse(TheAlarm.Min.Replace(issep, mustsep), out param1dec) &&
							Decimal.TryParse(value.Replace(issep, mustsep), out ivaluedec)) {
							if (ivaluedec < param1dec) {
								TheAlarm.noDelayCome = true;
								if (!TheAlarm.InAlarm) {
									if (TheAlarm.hasDelay) {
										if (!TheAlarm.TimerStarted) TheAlarm.TimerStart();
									} else {
										TheAlarm.setCome(Now);
									}
								}
							}
							if (ivaluedec >= param1dec) {
								TheAlarm.noDelayCome = false;
								TheAlarm.TimerStop();
								if (TheAlarm.InAlarm) TheAlarm.setGone(Now);
							}
						}
						break;
					case ">x<":
						// min max
						if (Decimal.TryParse(TheAlarm.Min.Replace(issep, mustsep), out param1dec) &&
							Decimal.TryParse(value.Replace(issep, mustsep), out ivaluedec)) {
							if ((ivaluedec < param1dec || ivaluedec > TheAlarm.Max)) {
								TheAlarm.noDelayCome = true;
								if (!TheAlarm.InAlarm) {
									if (TheAlarm.hasDelay) {
										if (!TheAlarm.TimerStarted) TheAlarm.TimerStart();
									} else {
										TheAlarm.setCome(Now);
									}
								}
							}
							if (ivaluedec >= param1dec && ivaluedec <= TheAlarm.Max) {
								TheAlarm.noDelayCome = false;
								TheAlarm.TimerStop();
								if (TheAlarm.InAlarm) TheAlarm.setGone(Now);
							}
						}
						break;
					case "<x>":
						// zwischen
						if (Decimal.TryParse(TheAlarm.Min.Replace(issep, mustsep), out param1dec) &&
							Decimal.TryParse(value.Replace(issep, mustsep), out ivaluedec)) {
							if ((ivaluedec >= param1dec && ivaluedec <= TheAlarm.Max)) {
								TheAlarm.noDelayCome = true;
								if (!TheAlarm.InAlarm) {
									if (TheAlarm.hasDelay) {
										if (!TheAlarm.TimerStarted) TheAlarm.TimerStart();
									} else {
										TheAlarm.setCome(Now);
									}
								}
							}
							if (ivaluedec < param1dec || ivaluedec > TheAlarm.Max) {
								TheAlarm.noDelayCome = false;
								TheAlarm.TimerStop();
								if (TheAlarm.InAlarm) TheAlarm.setGone(Now);
							}
						}
						break;
					case "PM":
						if (Int32.TryParse(value.Replace(issep, mustsep), out ivalueint)) {
							string os;
							if (Alarms.PlantMode.TryGetValue(ivalueint, out os)) TheAlarm.Alarmtext = os;
							else TheAlarm.Alarmtext = "unbekannt: " + ivalueint.ToString();
							if (ivalueint == 20 || ivalueint == 21 || ivalueint == 30) {
								TheAlarm.noDelayCome = true;
								if (!TheAlarm.InAlarm || TheAlarm.LastPlantMode != ivalueint) {
									if (TheAlarm.hasDelay) {
										if (!TheAlarm.TimerStarted) TheAlarm.TimerStart();
									} else {
										TheAlarm.setCome(Now);
									}
								}
							} else {
								TheAlarm.noDelayCome = false;
								TheAlarm.TimerStop();
								if (TheAlarm.InAlarm || TheAlarm.LastPlantMode != ivalueint) TheAlarm.setGone(Now);
							}
							TheAlarm.LastPlantMode = ivalueint;
						}
						break;
					case "PMb":
						if (Int32.TryParse(value.Replace(issep, mustsep), out ivalueint)) {
							string os;
							if (Alarms.PlantModeBac.TryGetValue(ivalueint, out os)) TheAlarm.Alarmtext = os;
							else TheAlarm.Alarmtext = "unbekannt: " + ivalueint.ToString();
							if (ivalueint == 21 || ivalueint == 22 || ivalueint == 31) {
								TheAlarm.noDelayCome = true;
								if (!TheAlarm.InAlarm || TheAlarm.LastPlantMode != ivalueint) {
									if (TheAlarm.hasDelay) {
										if (!TheAlarm.TimerStarted) TheAlarm.TimerStart();
									} else {
										TheAlarm.setCome(Now);
									}
								}
							} else {
								TheAlarm.noDelayCome = false;
								TheAlarm.TimerStop();
								if (TheAlarm.InAlarm || TheAlarm.LastPlantMode != ivalueint) TheAlarm.setGone(Now);
							}
							TheAlarm.LastPlantMode = ivalueint;
						}
						break;
					default:
						break;
				}
				Program.MainProg.setAlarm(id, TheAlarm);
			} catch (Exception ex) {
				eventLog.WriteError(ex);
			}
		}
	}
}
