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
//# Revision     : $Rev:: 188                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: Trend.cs 188 2025-02-17 00:57:33Z                        $ #
//#                                                                                 #
//###################################################################################
using FreakaZone.Libraries.wpEventLog;
using FreakaZone.Libraries.wpIniFile;
using FreakaZone.Libraries.wpSQL;
using FreakaZone.Libraries.wpSQL.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using static FreakaZone.Libraries.wpEventLog.Logger;

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
				Debug.Write(MethodInfo.GetCurrentMethod(), $"Trend intervall changed {_trendname}: {_intervall} sec");
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
		public Trend(TableTrend tt) {
			Init(tt.id_trend, tt.id_dp, tt.name, tt.intervall, tt.max, tt.maxAge, tt.active);
		}
		public void Stop() {
			if(onChangeMinValue != null)
				onChangeMinValue.Stop();
			onChangeMinValue = null;
			if(Debug.debugTrend)
				Debug.Write(MethodInfo.GetCurrentMethod(), $"Trend Stop {_trendname}");
		}
		public async void SetTrendValue(bool withReset) {
			if(_active) {
				string v = Datapoints.Get(_iddp).Value;
				if(v != null && v != "") {
					await Task.Run(() => {
						using(Database Sql = new Database("Trend intervall")) {
							string sql = @$"MERGE INTO [trendvalue] AS [TARGET]
	USING (
		VALUES ({_idtrend}, '{v}', '{DateTime.Now.ToString(Database.DateTimeFormat)}')
	) AS [SOURCE] ([id_trend], [value], [time])
	ON ([TARGET].[id_trend] = [SOURCE].[id_trend] AND [TARGET].[time] = [SOURCE].[time])
	WHEN NOT MATCHED THEN
		INSERT ([id_trend], [value], [time])
		VALUES ([SOURCE].[id_trend], [SOURCE].[value], [SOURCE].[time]);";
							//string sql = "INSERT INTO [trendvalue] ([id_trend], [value], [time]) VALUES " +
							//	$"({_idtrend}, '{v}', '{DateTime.Now.ToString(SQL.DateTimeFormat)}')";
							if(Sql.wpNonResponse(sql) == 0) {
								Debug.Write(MethodInfo.GetCurrentMethod(), $"setTrendValue: 0 Rows Inserted ({Datapoints.Get(_iddp).Name})");
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
			Debug.Write(MethodInfo.GetCurrentMethod(), $"Trend activated {_trendname}");
		}
		public void Deactivate() {
			_active = false;
			onChangeMinValue.Stop();
			Debug.Write(MethodInfo.GetCurrentMethod(), $"Trend deactivated {_trendname}");
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
			if(Debug.debugTrend) Debug.Write(MethodInfo.GetCurrentMethod(), $"Trend Init {_trendname}");
		}
		private int SetMinIntervall() {
			if(_intervall == 0) {
				if(Debug.debugTrend)
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
		private static List<Trend> _trendList = new List<Trend>();
		/// <summary></summary>
		private static Logger _eventLog = new Logger(Logger.ESource.PlugInTrend);
		private static TrendCleanDB _threadCleanDB;

		public static void Init() {
			Debug.Write(MethodInfo.GetCurrentMethod(), "Trends Init");
			using(Database Sql = new Database("get Trend Dictionary")) {
				List<TableTrend> tt = Sql.getTable<TableTrend>();
				foreach(TableTrend t in tt) {
					_trendList.Add(new Trend(t));
					Datapoints.Get(t.id_dp).idTrend = t.id_trend;
				}
			}
			_threadCleanDB = new TrendCleanDB();
			_threadCleanDB.Start();
			Debug.Write(MethodInfo.GetCurrentMethod(), "Trends gestartet");
		}

		public static void Stop() {
			if(_threadCleanDB != null)
				_threadCleanDB.Stop();
			if(_trendList != null) {
				foreach(Trend t in _trendList) {
					t.Stop();
				}
			}
			Debug.Write(MethodInfo.GetCurrentMethod(), "Trends Stop");
		}
		public static Trend Get(int idTrend) {
			return _trendList.Find(t => t.IdTrend == idTrend);
		}
		public static void RemoveTrend(int idTrend) {
			if(_trendList.Exists(t => t.IdTrend == idTrend)) {
				_trendList.Find(t => t.IdTrend == idTrend).Stop();
				_trendList.Remove(_trendList.Find(t => t.IdTrend == idTrend));
			}
		}
		public static void AddTrend(int idDp) {
			using(Database Sql = new Database("Add Trend to Dictionary")) {
				TableTrend tt = Sql.GetWithId<TableTrend>(idDp);
				if(!_trendList.Exists(t => t.IdDP == idDp))
					_trendList.Add(new Trend(tt));
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
				_folderBase = IniFile.get("Trend", "Pfad");
				_projekt = IniFile.get("Projekt", "Nummer");
				_projekt += (IniFile.get("Projekt", "Name") != "") ? (_projekt != "" ? " - " : "") + IniFile.get("Projekt", "Name") : "";

				TrendArchivFolder();

				if (Debug.debugTrend) {
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
						_eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Warning, "Formatdatei erzeugt {0}", formatpath);
					}
				} else {
					_eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Error, "Rootpath '{0}' not found for Trendarchiv", p);
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
				System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
				System.Diagnostics.Stopwatch watchTrend = new System.Diagnostics.Stopwatch();
				Dictionary<DateTime, string> DataforExport;
				watch.Start();
				Debug.Write(MethodInfo.GetCurrentMethod(), "Start Trend cleaner");
				foreach (Trend t in _trendList) {
					if (_doStop) break;
					deleteToOld = 0;
					deleteToMuch = 0;
					saveToOld = 0;
					saveToMuch = 0;
					DataforExport = new Dictionary<DateTime, string>();
					watchTrend.Restart();
					try {
						if (t.MaxDays > 0 && t.MaxEntries > 0) {
							using (Database Sql = new Database("Save into Archive")) {
								erg = Sql.wpQuery(@$"SELECT TOP {_maxEntries} [time], [value]
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
								deleteToOld = Sql.wpNonResponse(@$"WITH CTE AS (
									SELECT TOP {_maxEntries} * FROM [trendvalue]
									WHERE [id_trend] = {t.IdTrend} AND [time] < DATEADD(day, -{t.MaxDays}, GETDATE())
									ORDER BY [time])
									DELETE FROM CTE");

								erg = Sql.wpQuery(@$"SELECT [time] FROM [trendvalue]
									WHERE [id_trend] = {t.IdTrend} ORDER BY [time] DESC
									OFFSET {t.MaxEntries} ROWS FETCH NEXT 1 ROWS ONLY");
								DateTime latest;
								if(erg.Length > 0 && DateTime.TryParse(erg[0][0], out latest)) {
									string newLastDate = latest.ToString(Database.DateTimeFormat);
									erg = Sql.wpQuery(@$"SELECT TOP {_maxEntries} [time], [value]
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
									deleteToMuch = Sql.wpNonResponse(@$"WITH CTE AS (
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
								if (Debug.debugTrend) Debug.Write(MethodInfo.GetCurrentMethod(), saveedToOld);
								ev_save += String.Format("\r\n\t{0}", saveedToOld);
							}
							if (saveToMuch > 0) {
								string saveedToMuch = String.Format("{0} Datensätze aus {1} archiviert - zu viel ({2})",
									saveToMuch, t.TrendName, watchTrend.Elapsed);
								if (Debug.debugTrend) Debug.Write(MethodInfo.GetCurrentMethod(), saveedToMuch);
								ev_save += String.Format("\r\n\t{0}", saveedToMuch);
							}
						} else {
							if (DataforExport.Count > 0) Debug.Write(MethodInfo.GetCurrentMethod(), "Archivierung deaktiviert");
						}
						if (deleteToOld > 0) {
							string deletedToOld = String.Format("{0} Datensätze aus {1} gelöscht - zu alt ({2})",
								deleteToOld, t.TrendName, watchTrend.Elapsed);
							if (Debug.debugTrend) Debug.Write(MethodInfo.GetCurrentMethod(), deletedToOld);
							ev_del += String.Format("\r\n\t{0}", deletedToOld);
							trendsToOld++;
						}
						if (deleteToMuch > 0) {
							string deletedToMuch = String.Format("{0} Datensätze aus {1} gelöscht - zu viel ({2})",
								deleteToMuch, t.TrendName, watchTrend.Elapsed);
							if (Debug.debugTrend) Debug.Write(MethodInfo.GetCurrentMethod(), deletedToMuch);
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
