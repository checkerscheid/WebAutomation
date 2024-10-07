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
//# Revision     : $Rev:: 135                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: Alarm.cs 135 2024-10-07 21:18:50Z                        $ #
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
		private static Logger _eventLog;
		/// <summary></summary>
		public static readonly DateTime Default = new DateTime(2000, 01, 01, 0, 0, 0);
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
		private bool _needquit;
		/// <summary></summary>
		public bool NeedQuit {
			get { return _needquit; }
			set { _needquit = value; }
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
		private int _alarmgroups1;
		/// <summary></summary>
		public int Alarmgroups1 {
			get { return _alarmgroups1; }
			set { _alarmgroups1 = value; }
		}
		/// <summary></summary>
		private int _alarmgroups2;
		/// <summary></summary>
		public int Alarmgroups2 {
			get { return _alarmgroups2; }
			set { _alarmgroups2 = value; }
		}
		/// <summary></summary>
		private int _alarmgroups3;
		/// <summary></summary>
		public int Alarmgroups3 {
			get { return _alarmgroups3; }
			set { _alarmgroups3 = value; }
		}
		/// <summary></summary>
		private int _alarmgroups4;
		/// <summary></summary>
		public int Alarmgroups4 {
			get { return _alarmgroups4; }
			set { _alarmgroups4 = value; }
		}
		/// <summary></summary>
		private int _alarmgroups5;
		/// <summary></summary>
		public int Alarmgroups5 {
			get { return _alarmgroups5; }
			set { _alarmgroups5 = value; }
		}
		/// <summary></summary>
		private string _alarmnames1;
		/// <summary></summary>
		public string Alarmnames1 {
			get { return _alarmnames1; }
			set { _alarmnames1 = value; }
		}
		/// <summary></summary>
		private string _alarmnames2;
		/// <summary></summary>
		public string Alarmnames2 {
			get { return _alarmnames2; }
			set { _alarmnames2 = value; }
		}
		/// <summary></summary>
		private string _alarmnames3;
		/// <summary></summary>
		public string Alarmnames3 {
			get { return _alarmnames3; }
			set { _alarmnames3 = value; }
		}
		/// <summary></summary>
		private string _alarmnames4;
		/// <summary></summary>
		public string Alarmnames4 {
			get { return _alarmnames4; }
			set { _alarmnames4 = value; }
		}
		/// <summary></summary>
		private string _alarmnames5;
		/// <summary></summary>
		public string Alarmnames5 {
			get { return _alarmnames5; }
			set { _alarmnames5 = value; }
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
		private Timer _delay;
		/// <summary></summary>
		private bool _hasDelay;
		/// <summary></summary>
		public bool HasDelay {
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
		public bool NoDelayCome {
			set { _nodelaycome = value; }
		}
		private bool _wartung;
		public bool Wartung { 
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
			_eventLog = new Logger(wpEventLog.PlugInAlarm);
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
			this._needquit = true;
			this._timerstarted = false;
			this._wartung = false;
			if (sec > 0) {
				_hasDelay = true;
				_delay = new Timer((double)(sec * 1000));
				_delay.Elapsed += new ElapsedEventHandler(Delay_Tick);
			} else {
				_hasDelay = false;
			}
		}
		public void Stop() {
			TimerStop();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Delay_Tick(object sender, ElapsedEventArgs e) {
			if (_nodelaycome) {
				SetCome(DateTime.Now);
				_eventLog.Write("{0} ({1}) - Alarmdelay finished - real come",
					this._alarmtext, this._dpname);
			}
			_delay.Stop();
			_timerstarted = false;
		}
		/// <summary>
		/// 
		/// </summary>
		private void TimerStart() {
			if (_hasDelay) {
				_delay.Stop();
				_delay.Start();
				_timerstarted = true;
				_eventLog.Write("{0} ({1}) - Alarm come with delay - start",
					this._alarmtext, this._dpname);
			}
		}
		/// <summary>
		/// 
		/// </summary>
		private void TimerStop() {
			if (_hasDelay) {
				_delay.Stop();
				_timerstarted = false;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="value"></param>
		/// <param name="Now"></param>
		public void setAlarmValue() {
			string v = Datapoints.Get(_iddp).Value;
			DateTime Now = DateTime.Now;
			string issep;
			string mustsep;
			string sep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
			if(sep == ".") {
				issep = ",";
				mustsep = ".";
			} else {
				issep = ".";
				mustsep = ",";
			}
			decimal ivaluedec;
			decimal param1dec;
			try {
				switch(_condition) {
					case "=":
						if(v == _min) {
							_nodelaycome = true;
							if(!_inalarm) {
								if(_hasDelay) {
									if(!_timerstarted)
										TimerStart();
								} else {
									SetCome(Now);
								}
							}
						}
						if(v != _min) {
							_nodelaycome = false;
							TimerStop();
							if(_inalarm)
								SetGone(Now);
						}
						break;
					case "<>":
						if(v != _min) {
							_nodelaycome = true;
							if(!_inalarm) {
								if(_hasDelay) {
									if(!_timerstarted)
										TimerStart();
								} else {
									SetCome(Now);
								}
							}
						}
						if(v == _min) {
							_nodelaycome = false;
							TimerStop();
							if(_inalarm)
								SetGone(Now);
						}
						break;
					case ">":
						if(Decimal.TryParse(_min.Replace(issep, mustsep), out param1dec) &&
							Decimal.TryParse(v.Replace(issep, mustsep), out ivaluedec)) {
							if(ivaluedec > param1dec) {
								_nodelaycome = true;
								if(!_inalarm) {
									if(_hasDelay) {
										if(!_timerstarted)
											TimerStart();
									} else {
										SetCome(Now);
									}
								}
							}
							if(ivaluedec <= param1dec) {
								_nodelaycome = false;
								TimerStop();
								if(_inalarm)
									SetGone(Now);
							}
						}
						break;
					case "<":
						if(Decimal.TryParse(_min.Replace(issep, mustsep), out param1dec) &&
							Decimal.TryParse(v.Replace(issep, mustsep), out ivaluedec)) {
							if(ivaluedec < param1dec) {
								_nodelaycome = true;
								if(!_inalarm) {
									if(_hasDelay) {
										if(!_timerstarted)
											TimerStart();
									} else {
										SetCome(Now);
									}
								}
							}
							if(ivaluedec >= param1dec) {
								_nodelaycome = false;
								TimerStop();
								if(_inalarm)
									SetGone(Now);
							}
						}
						break;
					case ">x<":
						// min max
						if(Decimal.TryParse(_min.Replace(issep, mustsep), out param1dec) &&
							Decimal.TryParse(v.Replace(issep, mustsep), out ivaluedec)) {
							if((ivaluedec < param1dec || ivaluedec > _max)) {
								_nodelaycome = true;
								if(!_inalarm) {
									if(_hasDelay) {
										if(!_timerstarted)
											TimerStart();
									} else {
										SetCome(Now);
									}
								}
							}
							if(ivaluedec >= param1dec && ivaluedec <= _max) {
								_nodelaycome = false;
								TimerStop();
								if(_inalarm)
									SetGone(Now);
							}
						}
						break;
					case "<x>":
						// zwischen
						if(Decimal.TryParse(_min.Replace(issep, mustsep), out param1dec) &&
							Decimal.TryParse(v.Replace(issep, mustsep), out ivaluedec)) {
							if((ivaluedec >= param1dec && ivaluedec <= _max)) {
								_nodelaycome = true;
								if(_inalarm) {
									if(_hasDelay) {
										if(!_timerstarted)
											TimerStart();
									} else {
										SetCome(Now);
									}
								}
							}
							if(ivaluedec < param1dec || ivaluedec > _max) {
								_nodelaycome = false;
								TimerStop();
								if(_inalarm)
									SetGone(Now);
							}
						}
						break;
					default:
						break;
				}
			} catch(Exception ex) {
				_eventLog.WriteError(ex);
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="Now"></param>
		private void SetCome(DateTime Now) {
			_alarmupdate = Now;
			_come = Now;
			_gone = Alarm.Default;
			_inalarm = true;
			string sqlautoquit = "NULL";
			string sqlautoquitfrom = "NULL";
			if (_autoquit) {
				_quit = Now;
				_needquit = true;
				sqlautoquit = String.Format("'{0}'",
					Now.ToString(SQL.DateTimeFormat));
				sqlautoquitfrom = "'wpSystem'";
			} else {
				_needquit = false;
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
			wpDebug.Write("Alarm Come: {0} - {2} ({1})", this.Alarmtext, this.IdDp, this.DpName);
			Program.MainProg.AlarmToMail(this);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="Now"></param>
		private void SetGone(DateTime Now) {
			_alarmupdate = Now;
			_gone = Now;
			_inalarm = false;
			using (SQL SQL = new SQL("Alarm Gone in Historic")) {
				SQL.wpNonResponse(
					"UPDATE [alarmhistoric] SET [gone] = '{0}'  WHERE [id_alarm] = {1} AND [gone] IS NULL",
					Now.ToString(SQL.DateTimeFormat),
					_idalarm);
			}
			wpDebug.Write("Alarm Gone: {0} - {2} ({1})", this.Alarmtext, this.IdDp, this.DpName);
		}
		public void UpdateDelay(int sec) {
			TimerStop();
			_delay = null;
			if (sec > 0) {
				_hasDelay = true;
				_delay = new Timer((double)(sec * 1000));
				_delay.Elapsed += new ElapsedEventHandler(Delay_Tick);
			} else {
				_hasDelay = false;
			}
		}
	}
	/// <summary>
	/// 
	/// </summary>
	public static class Alarms {
		/// <summary>
		/// AlarmList mit den hinzuzufuegenden Alarmen<br />
		/// Key: id_alarm<br />
		/// Value: Alarm
		/// </summary>
		private static Dictionary<int, Alarm> _alarmList = new Dictionary<int, Alarm>();
		/// <summary></summary>
		private static Logger _eventLog;

		public const int ALARMGROUP1 = 1;
		public const int ALARMGROUP2 = 2;
		public const int ALARMGROUP3 = 3;
		public const int ALARMGROUP4 = 4;
		public const int ALARMGROUP5 = 5;

		public static string NameAlarmGroup1;
		public static string NameAlarmGroup2;
		public static string NameAlarmGroup3;
		public static string NameAlarmGroup4;
		public static string NameAlarmGroup5;
		public static bool UseAlarmGroup1;
		public static bool UseAlarmGroup2;
		public static bool UseAlarmGroup3;
		public static bool UseAlarmGroup4;
		public static bool UseAlarmGroup5;

		private static Dictionary<int, string> _alarmgroups1member = new Dictionary<int, string>();
		private static Dictionary<int, string> _alarmgroups2member = new Dictionary<int, string>();
		private static Dictionary<int, string> _alarmgroups3member = new Dictionary<int, string>();
		private static Dictionary<int, string> _alarmgroups4member = new Dictionary<int, string>();
		private static Dictionary<int, string> _alarmgroups5member = new Dictionary<int, string>();
		/// <summary>
		/// 
		/// </summary>
		public static void Init() {
			wpDebug.Write("Alarms Init");
			_eventLog = new Logger(wpEventLog.PlugInAlarm);
			FillAlarmGroups();
			using (SQL SQL = new SQL("Init Alarms")) {
				string[][] DBAlarms = SQL.wpQuery(@"
				SELECT
					[a].[id_alarm], [a].[text], [a].[link], [t].[name], [t].[autoquit],
					[g].[name], [dp].[id_dp], [dp].[name], [c].[condition],
					[a].[min], [a].[max], [a].[delay],
					COUNT([atm].[id_email]) AS [emailcounter],
					ISNULL(SUM([atm].[minutes]), 0) AS [emailminutes],
					ISNULL([a].[id_alarmgroups1], 0), ISNULL([a].[id_alarmgroups2], 0),
					ISNULL([a].[id_alarmgroups3], 0), ISNULL([a].[id_alarmgroups4], 0),
					ISNULL([a].[id_alarmgroups5], 0),
					[g1].[name], [g2].[name], [g3].[name], [g4].[name], [g5].[name]
				FROM [alarm] [a]
				INNER JOIN [alarmtype] [t] ON [a].[id_alarmtype] = [t].[id_alarmtype]
				INNER JOIN [alarmgroup] [g] ON [a].[id_alarmgroup] = [g].[id_alarmgroup]
				INNER JOIN [dp] ON [a].[id_dp] = [dp].[id_dp]
				INNER JOIN [alarmcondition] [c] ON [a].[id_alarmcondition] = [c].[id_alarmcondition]
				LEFT JOIN [alarmgroups1] [g1] ON [a].[id_alarmgroups1] = [g1].[id_alarmgroups1]
				LEFT JOIN [alarmgroups2] [g2] ON [a].[id_alarmgroups2] = [g2].[id_alarmgroups2]
				LEFT JOIN [alarmgroups3] [g3] ON [a].[id_alarmgroups3] = [g3].[id_alarmgroups3]
				LEFT JOIN [alarmgroups4] [g4] ON [a].[id_alarmgroups4] = [g4].[id_alarmgroups4]
				LEFT JOIN [alarmgroups5] [g5] ON [a].[id_alarmgroups5] = [g5].[id_alarmgroups5]
				LEFT JOIN [alarmtoemail] [atm] ON [atm].[id_alarm] = [a].[id_alarm]
				GROUP BY [a].[id_alarm], [a].[text], [a].[link], [t].[name], [t].[autoquit],
					[g].[name], [dp].[id_dp], [dp].[name], [c].[condition],
					[a].[min], [a].[max], [a].[delay],
					[a].[id_alarmgroups1], [a].[id_alarmgroups2], [a].[id_alarmgroups3],
					[a].[id_alarmgroups4], [a].[id_alarmgroups5],
					[g1].[name], [g2].[name], [g3].[name], [g4].[name], [g5].[name]");
				for (int ialarms = 0; ialarms < DBAlarms.Length; ialarms++) {
					int idAlarm = Int32.Parse(DBAlarms[ialarms][0]);
					int idDp = Int32.Parse(DBAlarms[ialarms][6]);
					Alarm TheAlarm;
					int delay;
					if (Int32.TryParse(DBAlarms[ialarms][11], out delay)) {
						TheAlarm = new Alarm(idAlarm, idDp, DBAlarms[ialarms][7], delay);
					} else {
						TheAlarm = new Alarm(idAlarm, idDp, DBAlarms[ialarms][7], 0);
					}
					int? emailCounter = SQL.convertNumeric(DBAlarms[ialarms][12]);
					int? emailMinutes = SQL.convertNumeric(DBAlarms[ialarms][13]);
					TheAlarm.Alarmtext = DBAlarms[ialarms][1];
					TheAlarm.Alarmlink = DBAlarms[ialarms][2];
					TheAlarm.Alarmtype = DBAlarms[ialarms][3];
					TheAlarm.Autoquit = DBAlarms[ialarms][4] == "True";
					TheAlarm.Alarmgroup = DBAlarms[ialarms][5];
					TheAlarm.Condition = DBAlarms[ialarms][8];
					TheAlarm.Min = DBAlarms[ialarms][9];
					TheAlarm.Alarmgroups1 = Int32.Parse(DBAlarms[ialarms][14]);
					TheAlarm.Alarmgroups2 = Int32.Parse(DBAlarms[ialarms][15]);
					TheAlarm.Alarmgroups3 = Int32.Parse(DBAlarms[ialarms][16]);
					TheAlarm.Alarmgroups4 = Int32.Parse(DBAlarms[ialarms][17]);
					TheAlarm.Alarmgroups5 = Int32.Parse(DBAlarms[ialarms][18]);
					TheAlarm.Alarmnames1 = DBAlarms[ialarms][19];
					TheAlarm.Alarmnames2 = DBAlarms[ialarms][20];
					TheAlarm.Alarmnames3 = DBAlarms[ialarms][21];
					TheAlarm.Alarmnames4 = DBAlarms[ialarms][22];
					TheAlarm.Alarmnames5 = DBAlarms[ialarms][23];
					if (TheAlarm.Condition == ">x<" || TheAlarm.Condition == "<x>")
						TheAlarm.Max = Int32.Parse(DBAlarms[ialarms][10]);
					_alarmList.Add(idAlarm, TheAlarm);
					Datapoints.Get(idDp).idAlarm = idAlarm;
				}
			}

			using (SQL SQL = new SQL("Add Aktive Alarms")) {
				string[][] DBActiveAlarms = SQL.wpQuery(@"
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
				for (int ialarms = 0; ialarms < DBActiveAlarms.Length; ialarms++) {
					int idAlarm = Int32.Parse(DBActiveAlarms[ialarms][0]);
					int idDp = Int32.Parse(DBActiveAlarms[ialarms][1]);
					Alarm TheAlarm = _alarmList[idAlarm];
					if (TheAlarm != null) {
						TheAlarm.Come = DateTime.Parse(DBActiveAlarms[ialarms][2]);
						TheAlarm.Gone = Alarm.Default;
						TheAlarm.InAlarm = true;
						TheAlarm.Quit = Alarm.Default;
						TheAlarm.NeedQuit = false;
						if (DBActiveAlarms[ialarms][3] != "") {
							TheAlarm.Gone = DateTime.Parse(DBActiveAlarms[ialarms][3]);
							TheAlarm.InAlarm = false;
						}
						if (DBActiveAlarms[ialarms][4] != "") {
							TheAlarm.Quit = DateTime.Parse(DBActiveAlarms[ialarms][4]);
							TheAlarm.NeedQuit = true;
						}
					}
				}
			}
			_eventLog.Write("Alarms gestartet");
		}
		public static Dictionary<int, Alarm> getActiveAlarms() {
			Dictionary<int, Alarm> returns = new Dictionary<int, Alarm>();
			foreach(KeyValuePair<int, Alarm> kvp in _alarmList) {
				kvp.Value.setAlarmValue();
				if (kvp.Value.InAlarm || !kvp.Value.NeedQuit) {
					returns.Add(kvp.Key, kvp.Value);
				}
			}
			return returns;
		}
		public static Alarm Get(int? idAlarm) {
			if(idAlarm == null) return null;
			return _alarmList[(int)idAlarm];
		}
		public static void RemoveAlarm(int? idAlarm) {
			if(idAlarm != null) {
				_alarmList[(int)idAlarm].Stop();
				_alarmList.Remove((int)idAlarm);
			}
		}
		public static string FillAlarmGroups() {
			using(SQL SQL = new SQL("Fill AlarmGroups")) {
				string[][] DBAlarmGroups = SQL.wpQuery(@"SELECT [key], [value] FROM [cfg] WHERE
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
					ORDER BY [key]");
				if(DBAlarmGroups.Length == 10) {
					NameAlarmGroup1 = DBAlarmGroups[0][1];
					NameAlarmGroup2 = DBAlarmGroups[1][1];
					NameAlarmGroup3 = DBAlarmGroups[2][1];
					NameAlarmGroup4 = DBAlarmGroups[3][1];
					NameAlarmGroup5 = DBAlarmGroups[4][1];
					UseAlarmGroup1 = DBAlarmGroups[5][1] == "True";
					UseAlarmGroup2 = DBAlarmGroups[6][1] == "True";
					UseAlarmGroup3 = DBAlarmGroups[7][1] == "True";
					UseAlarmGroup4 = DBAlarmGroups[8][1] == "True";
					UseAlarmGroup5 = DBAlarmGroups[9][1] == "True";
				}
			}
			using(SQL SQL = new SQL("FillAlarmGroups Alarmgroups Member")) {
				string[][] DBAlarm1Member = SQL.wpQuery(@"SELECT [id_alarmgroups1], [name] FROM [alarmgroups1]");
				for(int ialarms = 0; ialarms < DBAlarm1Member.Length; ialarms++) {
					if(!_alarmgroups1member.ContainsKey(Int32.Parse(DBAlarm1Member[ialarms][0]))) {
						_alarmgroups1member.Add(Int32.Parse(DBAlarm1Member[ialarms][0]), DBAlarm1Member[ialarms][1]);
					}
				}
			}
			using(SQL SQL = new SQL("FillAlarmGroups Alarmgroups Member")) {
				string[][] DBAlarm2Member = SQL.wpQuery(@"SELECT [id_alarmgroups2], [name] FROM [alarmgroups2]");
				for(int ialarms = 0; ialarms < DBAlarm2Member.Length; ialarms++) {
					if(!_alarmgroups2member.ContainsKey(Int32.Parse(DBAlarm2Member[ialarms][0]))) {
						_alarmgroups2member.Add(Int32.Parse(DBAlarm2Member[ialarms][0]), DBAlarm2Member[ialarms][1]);
					}
				}
			}
			using(SQL SQL = new SQL("FillAlarmGroups Alarmgroups Member")) {
				string[][] DBAlarm3Member = SQL.wpQuery(@"SELECT [id_alarmgroups3], [name] FROM [alarmgroups3]");
				for(int ialarms = 0; ialarms < DBAlarm3Member.Length; ialarms++) {
					if(!_alarmgroups3member.ContainsKey(Int32.Parse(DBAlarm3Member[ialarms][0]))) {
						_alarmgroups3member.Add(Int32.Parse(DBAlarm3Member[ialarms][0]), DBAlarm3Member[ialarms][1]);
					}
				}
			}
			using(SQL SQL = new SQL("FillAlarmGroups Alarmgroups Member")) {
				string[][] DBAlarm4Member = SQL.wpQuery(@"SELECT [id_alarmgroups4], [name] FROM [alarmgroups4]");
				for(int ialarms = 0; ialarms < DBAlarm4Member.Length; ialarms++) {
					if(!_alarmgroups4member.ContainsKey(Int32.Parse(DBAlarm4Member[ialarms][0]))) {
						_alarmgroups4member.Add(Int32.Parse(DBAlarm4Member[ialarms][0]), DBAlarm4Member[ialarms][1]);
					}
				}
			}
			using(SQL SQL = new SQL("FillAlarmGroups Alarmgroups Member")) {
				string[][] DBAlarm5Member = SQL.wpQuery(@"SELECT [id_alarmgroups5], [name] FROM [alarmgroups5]");
				for(int ialarms = 0; ialarms < DBAlarm5Member.Length; ialarms++) {
					if(!_alarmgroups5member.ContainsKey(Int32.Parse(DBAlarm5Member[ialarms][0]))) {
						_alarmgroups5member.Add(Int32.Parse(DBAlarm5Member[ialarms][0]), DBAlarm5Member[ialarms][1]);
					}
				}
			}
			return "S_OK";
		}
		public static string GetReadableGroup(int GroupNo, int IdGroup) {
			string returns = IdGroup.ToString();
			switch(GroupNo) {
				case ALARMGROUP1:
					if(_alarmgroups1member.ContainsKey(IdGroup))
						returns = _alarmgroups1member[IdGroup];
					break;
				case ALARMGROUP2:
					if(_alarmgroups2member.ContainsKey(IdGroup))
						returns = _alarmgroups2member[IdGroup];
					break;
				case ALARMGROUP3:
					if(_alarmgroups3member.ContainsKey(IdGroup))
						returns = _alarmgroups3member[IdGroup];
					break;
				case ALARMGROUP4:
					if(_alarmgroups4member.ContainsKey(IdGroup))
						returns = _alarmgroups4member[IdGroup];
					break;
				case ALARMGROUP5:
					if(_alarmgroups5member.ContainsKey(IdGroup))
						returns = _alarmgroups5member[IdGroup];
					break;
				default:
					returns = "-";
					break;
			}
			return returns;
		}
	}
}
