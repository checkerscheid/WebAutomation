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
//# Revision     : $Rev:: 153                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: Trend.cs 153 2024-12-18 14:41:55Z                        $ #
//#                                                                                 #
//###################################################################################
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using WebAutomation.Helper;

namespace WebAutomation.PlugIns {
	/// <summary>
	/// 
	/// </summary>
	public class Trend {
		/// <summary></summary>
		private int _idtrend;
		/// <summary></summary>
		public int IdTrend {
			get { return _idtrend; }
			set { _idtrend = value; }
		}
		/// <summary></summary>
		private int _iddp;
		/// <summary></summary>
		public int IdDP {
			get { return _iddp; }
		}
		/// <summary></summary>
		private string _trendname;
		/// <summary></summary>
		public string TrendName {
			get { return _trendname; }
			set { _trendname = value; }
		}
		/// <summary>
		/// 0 = cov
		/// 1 .. x = in Sekunden
		/// </summary>
		private int _intervall;
		/// <summary></summary>
		public int Intervall {
			get { return _intervall; }
			set {
				onChangeMinValue.Stop();
				_intervall = value;
				onChangeMinValue.Interval = SetMinIntervall();
				onChangeMinValue.Start();
				wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Trend intervall changed {_trendname}: {_intervall} sec");
			}
		}
		/// <summary></summary>
		private int _maxentries;
		/// <summary></summary>
		public int MaxEntries {
			get { return _maxentries; }
			set { _maxentries = value; }
		}
		/// <summary></summary>
		private int _maxdays;
		/// <summary></summary>
		public int MaxDays {
			get { return _maxdays; }
			set { _maxdays = value; }
		}
		/// <summary></summary>
		private bool _active;
		private System.Timers.Timer onChangeMinValue;
		private int minMinutes;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="idtrend"></param>
		/// <param name="opcitemid"></param>
		/// <param name="intervall"></param>
		public Trend(int idtrend, int iddp, string trendname, int intervall, bool active) {
			Init(idtrend, iddp, trendname, intervall, 50400, 35, active);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="idtrend"></param>
		/// <param name="opcitemid"></param>
		/// <param name="intervall"></param>
		public Trend(int idtrend, int iddp, string trendname, int intervall, int maxentries, int maxdays, bool active) {
			Init(idtrend, iddp, trendname, intervall, maxentries, maxdays, active);
		}
		public void Stop() {
			if(onChangeMinValue != null)
				onChangeMinValue.Stop();
			onChangeMinValue = null;
			if(wpDebug.debugTrend)
				wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Trend Stop {_trendname}");
		}
		public async void SetTrendValue(bool withReset) {
			if(_active) {
				string v = Datapoints.Get(_iddp).Value;
				if(v != null && v != "") {
					await Task.Run(() => {
						using(SQL SQL = new SQL("Trend intervall")) {
							string sql = @$"MERGE INTO [trendvalue] AS [TARGET]
	USING (
		VALUES ({_idtrend}, '{v}', '{DateTime.Now.ToString(SQL.DateTimeFormat)}')
	) AS [SOURCE] ([id_trend], [value], [time])
	ON ([TARGET].[id_trend] = [SOURCE].[id_trend] AND [TARGET].[time] = [SOURCE].[time])
	WHEN NOT MATCHED THEN
		INSERT ([id_trend], [value], [time])
		VALUES ([SOURCE].[id_trend], [SOURCE].[value], [SOURCE].[time]);";
							//string sql = "INSERT INTO [trendvalue] ([id_trend], [value], [time]) VALUES " +
							//	$"({_idtrend}, '{v}', '{DateTime.Now.ToString(SQL.DateTimeFormat)}')";
							if(SQL.wpNonResponse(sql) == 0) {
								wpDebug.Write(MethodInfo.GetCurrentMethod(), $"setTrendValue: 0 Rows Inserted ({Datapoints.Get(_iddp).Name})");
							}
						}
					});
					if(withReset)
						ResetMinValue();
				}
			}
		}
		public void SetTrendValue() { SetTrendValue(true); }
		public void Activate() {
			_active = true;
			onChangeMinValue.Start();
			wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Trend activated {_trendname}");
		}
		public void Deactivate() {
			_active = false;
			onChangeMinValue.Stop();
			wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Trend deactivated {_trendname}");
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="idtrend"></param>
		/// <param name="opcitemid"></param>
		/// <param name="intervall"></param>
		private void Init(int idtrend, int iddp, string trendname, int intervall, int maxentries, int maxdays, bool active) {
			_idtrend = idtrend;
			_iddp = iddp;
			_trendname = trendname;
			_intervall = intervall;
			_maxentries = maxentries;
			_maxdays = maxdays;
			_active = active;
			onChangeMinValue = new System.Timers.Timer();
			onChangeMinValue.Interval = SetMinIntervall();
			onChangeMinValue.Elapsed += OnChangeMinValue_Tick;
			onChangeMinValue.AutoReset = true;
			if(_active) onChangeMinValue.Start();
			if(wpDebug.debugTrend) wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Trend Init {_trendname}");
		}
		private int SetMinIntervall() {
			if(_intervall == 0) {
				if(wpDebug.debugTrend)
					minMinutes = 1 * 60;
				else
					minMinutes = 14 * 60;
			} else {
				minMinutes = _intervall;
			}
			return 1000 * minMinutes;
		}
		private void OnChangeMinValue_Tick(object sender, EventArgs e) {
			SetTrendValue(false);
		}
		private void ResetMinValue() {
			if (_intervall == 0) {
				onChangeMinValue.Stop();
				onChangeMinValue.Start();
			}
		}
	}
	/// <summary>
	/// 
	/// </summary>
	public static class Trends {
		/// <summary>
		/// TrendListe mit den hinzuzufuegenden Trends<br />
		/// Key: id_trend<br />
		/// Value: Trend
		/// </summary>
		private static Dictionary<int, Trend> _trendList = new Dictionary<int, Trend>();
		/// <summary></summary>
		private static Logger _eventLog = new Logger(wpEventLog.PlugInTrend);
		private static TrendCleanDB _threadCleanDB;

		public static void Init() {
			wpDebug.Write(MethodInfo.GetCurrentMethod(), "Trends Init");
			using(SQL SQL = new SQL("get Trend Dictionary")) {
				string[][] erg = SQL.wpQuery(@"SELECT
					[t].[id_trend], [dp].[id_dp], [t].[name], [t].[intervall], [t].[max], [t].[maxage], [t].[active]
					FROM [trend] [t]
					INNER JOIN [dp] ON [t].[id_dp] = [dp].[id_dp]"
				);
				int idTrend, idDp, intervall, max, maxage;
				for(int i = 0; i < erg.Length; i++) {
					idTrend = Int32.Parse(erg[i][0]);
					idDp = Int32.Parse(erg[i][1]);
					intervall = Int32.Parse(erg[i][3]);
					max = Int32.Parse(erg[i][4]);
					maxage = Int32.Parse(erg[i][5]);
					_trendList.Add(idTrend, new Trend(idTrend, idDp, erg[i][2], intervall, max, maxage, erg[i][6] == "True"));
					Datapoints.Get(idDp).idTrend = idTrend;
				}
			}
			_threadCleanDB = new TrendCleanDB();
			_threadCleanDB.Start();
			wpDebug.Write(MethodInfo.GetCurrentMethod(), "Trends gestartet");
		}

		public static void Stop() {
			if(_threadCleanDB != null)
				_threadCleanDB.Stop();
			if(_trendList != null) {
				foreach(KeyValuePair<int, Trend> kvp in _trendList) {
					kvp.Value.Stop();
				}
			}
			wpDebug.Write(MethodInfo.GetCurrentMethod(), "Trends Stop");
		}
		public static Trend Get(int idTrend) {
			return _trendList[idTrend];
		}
		public static void RemoveTrend(int idTrend) {
			_trendList[idTrend].Stop();
			_trendList.Remove(idTrend);
		}
		public static void AddTrend(int idDp) {
			using(SQL SQL = new SQL("Add Trend to Dictionary")) {
				string[][] erg = SQL.wpQuery(@$"SELECT TOP 1
					[id_trend], [name], [intervall], [max], [maxage], [active]
					FROM [trend] [t] WHERE [id_dp] = {idDp}");
				int idTrend, intervall, max, maxage;
				idTrend = Int32.Parse(erg[0][0]);
				intervall = Int32.Parse(erg[0][2]);
				max = Int32.Parse(erg[0][3]);
				maxage = Int32.Parse(erg[0][4]);
				_trendList.Add(idTrend, new Trend(idTrend, idDp, erg[0][1], intervall, max, maxage, erg[0][5] == "True"));
			}
		}
		/// <summary>
		/// Eingeschränkte Speicherkapazität bei MSSQL Express erfordert ein Auslagern der TrenddDaten in Dateien
		/// </summary>
		internal class TrendCleanDB {
			private volatile bool _doStop;
			private int _counter;
			private int _maxCounter;
			private string _folderBase;
			private string _projekt;
			private int _maxEntries = 1000;
			public TrendCleanDB() {
				_doStop = false;
				_counter = 0;
				_folderBase = Ini.get("Trend", "Pfad");
				_projekt = Ini.get("Projekt", "Nummer");
				_projekt += (Ini.get("Projekt", "Name") != "") ? (_projekt != "" ? " - " : "") + Ini.get("Projekt", "Name") : "";

				TrendArchivFolder();

				if (wpDebug.debugTrend) {
					_maxCounter = 1 * 60;
				} else {
					_maxCounter = 90 * 60;
				}
			}
			public async void Start() {
				while (!_doStop) {
					if (++_counter > _maxCounter) {
						await Task.Run(() => {
							DBcleaner();
						});
						_counter = 0;
					} else {
						await Task.Delay(1000);
					}
				}
			}
			public void Stop() {
				_doStop = true;
			}
			private void TrendArchivFolder() {
				string p = Directory.GetDirectoryRoot(_folderBase);
				if(Directory.Exists(p)) {
					if(!Directory.Exists(_folderBase))
						Directory.CreateDirectory(_folderBase);
					string formatpath = _folderBase + "\\trendvalue.fmt";
					if(!File.Exists(formatpath)) {
						using(StreamWriter sw = File.CreateText(formatpath)) {
							sw.WriteLine("9.0");
							sw.WriteLine("2");
							sw.WriteLine("1  SQLCHAR  0  255 \";\"  1  time  Latin1_General_CI_AS");
							sw.WriteLine("2  SQLCHAR  0  255 \";\\r\\n\"  2  value  Latin1_General_CI_AS");
						}
						_eventLog.Write(MethodInfo.GetCurrentMethod(), EventLogEntryType.Warning, "Formatdatei erzeugt {0}", formatpath);
					}
				} else {
					_eventLog.Write(MethodInfo.GetCurrentMethod(), EventLogEntryType.Error, "Rootpath '{0}' not found for Trendarchiv", p);
				}
			}
			private void DBcleaner() {
				int deleteToOld = 0;
				int deleteToMuch = 0;
				int saveToOld = 0;
				int saveToMuch = 0;
				int trendsToOld = 0;
				int trendsToMuch = 0;
				string ev_del = "";
				string ev_save = "";
				string[][] erg;
				DateTime parsed;
				Stopwatch watch = new Stopwatch();
				Stopwatch watchTrend = new Stopwatch();
				Dictionary<DateTime, string> DataforExport;
				watch.Start();
				wpDebug.Write(MethodInfo.GetCurrentMethod(), "Start Trend cleaner");
				foreach (KeyValuePair<int, Trend> kvpTrend in _trendList) {
					if (_doStop) break;
					deleteToOld = 0;
					deleteToMuch = 0;
					saveToOld = 0;
					saveToMuch = 0;
					DataforExport = new Dictionary<DateTime, string>();
					watchTrend.Restart();
					try {
						Trend t = kvpTrend.Value;
						if (t.MaxDays > 0 && t.MaxEntries > 0) {
							using (SQL SQL = new SQL("Save into Archive")) {
								erg = SQL.wpQuery(@$"SELECT TOP {_maxEntries} [time], [value]
									FROM [trendvalue] WHERE [id_trend] = {t.IdTrend}
									AND [time] < DATEADD(day, -{t.MaxDays}, GETDATE())
									ORDER BY [time]");
								for (int i = 0; i < erg.Length; i++) {
									if (DateTime.TryParse(erg[i][0], out parsed)) {
										if (!DataforExport.ContainsKey(parsed.Date)) {
											DataforExport.Add(parsed.Date,
												erg[i][0] + ";" + erg[i][1] + ";\r\n");
										} else {
											DataforExport[parsed.Date] +=
												erg[i][0] + ";" + erg[i][1] + ";\r\n";
										}
										saveToOld++;
									}
								}
								deleteToOld = SQL.wpNonResponse(@$"WITH CTE AS (
									SELECT TOP {_maxEntries} * FROM [trendvalue]
									WHERE [id_trend] = {t.IdTrend} AND [time] < DATEADD(day, -{t.MaxDays}, GETDATE())
									ORDER BY [time])
									DELETE FROM CTE");

								erg = SQL.wpQuery(@$"SELECT [time] FROM [trendvalue]
									WHERE [id_trend] = {t.IdTrend} ORDER BY [time] DESC
									OFFSET {t.MaxEntries} ROWS FETCH NEXT 1 ROWS ONLY");
								DateTime latest;
								if(erg.Length > 0 && DateTime.TryParse(erg[0][0], out latest)) {
									string newLastDate = latest.ToString(SQL.DateTimeFormat);
									erg = SQL.wpQuery(@$"SELECT TOP {_maxEntries} [time], [value]
										FROM [trendvalue] WHERE [time] < '{newLastDate}'
										AND [id_trend] = {t.IdTrend} ORDER BY [time]");
									for(int i = 0; i < erg.Length; i++) {
										if(DateTime.TryParse(erg[i][0], out parsed)) {
											if(!DataforExport.ContainsKey(parsed.Date)) {
												DataforExport.Add(parsed.Date,
													erg[i][0] + ";" + erg[i][1] + ";\r\n");
											} else {
												DataforExport[parsed.Date] +=
													erg[i][0] + ";" + erg[i][1] + ";\r\n";
											}
											saveToMuch++;
										}
									}
									deleteToMuch = SQL.wpNonResponse(@$"WITH CTE AS (
										SELECT TOP {_maxEntries} * FROM [trendvalue]
										WHERE [time] < '{newLastDate}' AND [id_trend] = {t.IdTrend}
										ORDER BY [time])
										DELETE FROM CTE");
								}
							}
							watchTrend.Stop();
							foreach (KeyValuePair<DateTime, string> kvp in DataforExport) {
								try {
									writeToFile(
										String.Format("{0} - {1}", t.IdTrend, Datapoints.Get(t.IdDP).Name),
										kvp.Key, kvp.Value);
								} catch(Exception ex) {
									_eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
								};
							}
							// Log
							if (saveToOld > 0) {
								string saveedToOld = String.Format("{0} Datensätze aus {1} archiviert - zu alt ({2})",
									saveToOld, t.TrendName, watchTrend.Elapsed);
								if (wpDebug.debugTrend) wpDebug.Write(MethodInfo.GetCurrentMethod(), saveedToOld);
								ev_save += String.Format("\r\n\t{0}", saveedToOld);
							}
							if (saveToMuch > 0) {
								string saveedToMuch = String.Format("{0} Datensätze aus {1} archiviert - zu viel ({2})",
									saveToMuch, t.TrendName, watchTrend.Elapsed);
								if (wpDebug.debugTrend) wpDebug.Write(MethodInfo.GetCurrentMethod(), saveedToMuch);
								ev_save += String.Format("\r\n\t{0}", saveedToMuch);
							}
						} else {
							if (DataforExport.Count > 0) wpDebug.Write(MethodInfo.GetCurrentMethod(), "Archivierung deaktiviert");
						}
						if (deleteToOld > 0) {
							string deletedToOld = String.Format("{0} Datensätze aus {1} gelöscht - zu alt ({2})",
								deleteToOld, t.TrendName, watchTrend.Elapsed);
							if (wpDebug.debugTrend) wpDebug.Write(MethodInfo.GetCurrentMethod(), deletedToOld);
							ev_del += String.Format("\r\n\t{0}", deletedToOld);
							trendsToOld++;
						}
						if (deleteToMuch > 0) {
							string deletedToMuch = String.Format("{0} Datensätze aus {1} gelöscht - zu viel ({2})",
								deleteToMuch, t.TrendName, watchTrend.Elapsed);
							if (wpDebug.debugTrend) wpDebug.Write(MethodInfo.GetCurrentMethod(), deletedToMuch);
							ev_del += String.Format("\r\n\t{0}", deletedToMuch);
							trendsToMuch++;
						}
					} catch (Exception ex) {
						_eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
					}
				}
				watch.Stop();
				if (ev_save != "") {
					_eventLog.Write(MethodInfo.GetCurrentMethod(), "Dauer: {0}, Trenddaten archiviert", watch.Elapsed);
				} else {
					_eventLog.Write(MethodInfo.GetCurrentMethod(), "Dauer: {0}, keine Trenddaten archiviert", watch.Elapsed);
				}
				if (ev_del != "") {
					_eventLog.Write(MethodInfo.GetCurrentMethod(), "Dauer: {0}, Trenddaten gelöscht ({1})", watch.Elapsed, trendsToOld + trendsToMuch);
				} else {
					_eventLog.Write(MethodInfo.GetCurrentMethod(), "Dauer: {0}, keine Trenddaten gelöscht", watch.Elapsed);
				}
			}
			private bool writeToFile(string filename, DateTime DateForFolder, string text) {
				bool returns = false;
				string y = DateForFolder.ToString("yyyy");
				string m = DateForFolder.ToString("MM");
				string d = DateForFolder.ToString("dd");
				try {
					if (_folderBase != "") {
						string p = Directory.GetDirectoryRoot(_folderBase);
						if (Directory.Exists(p)) {
							if (!Directory.Exists(_folderBase))
								Directory.CreateDirectory(_folderBase);
							if (!Directory.Exists(_folderBase + "\\" + _projekt))
								Directory.CreateDirectory(_folderBase + "\\" + _projekt);
							if (!Directory.Exists(_folderBase + "\\" + _projekt + "\\" + y))
								Directory.CreateDirectory(_folderBase + "\\" + _projekt + "\\" + y);
							if (!Directory.Exists(_folderBase + "\\" + _projekt + "\\" + y + "\\" + m))
								Directory.CreateDirectory(_folderBase + "\\" + _projekt + "\\" + y + "\\" + m);
							if (!Directory.Exists(_folderBase + "\\" + _projekt + "\\" + y + "\\" + m + "\\" + d))
								Directory.CreateDirectory(_folderBase + "\\" + _projekt + "\\" + y + "\\" + m + "\\" + d);
							string path = _folderBase + "\\" + _projekt + "\\" + y + "\\" + m + "\\" + d + "\\" + filename + ".csv";
							if (!File.Exists(path)) {
								using (StreamWriter sw = File.CreateText(path)) {
									sw.Write(text);
								}
							} else {
								using (StreamWriter sw = File.AppendText(path)) {
									sw.Write(text);
								}
							}
							returns = true;
						} else {
							throw new Exception(String.Format("Rootpath '{0}' not found for Trendarchiv", p));
						}
					} else {
						returns = false;
					}
				} catch (Exception ex) {
					_eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
				}
				return returns;
			}
		}
	}
}
