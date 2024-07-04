//###################################################################################
//#                                                                                 #
//#              (C) FreakaZone GmbH                                                #
//#              =======================                                            #
//#                                                                                 #
//###################################################################################
//#                                                                                 #
//# Author       : Christian Scheid                                                 #
//# Date         : 23.12.2019                                                       #
//#                                                                                 #
//# Revision     : $Rev:: 109                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: Dictionaries.cs 109 2024-06-16 15:59:41Z                 $ #
//#                                                                                 #
//###################################################################################
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using WebAutomation.Helper;
using WebAutomation.PlugIns;

namespace WebAutomation {
	public class DatapointsCollection {
		private static Logger EventLog = new Logger(wpEventLog.WebAutomation);
		private static Dictionary<int, Datapoint> _items = new Dictionary<int, Datapoint>();
	}
	public class Datapoint {
		private int _id;
		public int ID { get { return _id; } }
		private int _idGroup;
		private int _idNamespace;
		private int? _idOpc;
		private int? _idMqtt;
		private int? _idTrend;
		public int? idTrend { get { return _idTrend; } set { _idTrend = value; } }
		private int? _idAlarm;
		public int? idAlarm { get { return _idAlarm; } set { _idAlarm = value; } }
		private string _value;
		public string Value { get { return _value; } }
		private string _valueString = "-";
		public String ValueString { get { return _valueString; } }
		private DateTime _lastChange;
		public DateTime LastChange { get { return _lastChange; } }
		private string _name;
		public String Name { get { return _name; } }
		private string _description;
		private int _writeLevel;
		public int WriteLevel { get { return _writeLevel; } set { _writeLevel = value; } }
		private string _unit;
		public string Unit { get { return _unit; } }
		private int? _nks;
		public int NKS { get { return _nks == null ? 0 : (int)_nks; } }
		private double _min;
		public double Min { get { return _min; } set { _min = value; } }
		private double _max;
		public double Max { get { return _max; } set { _max = value; } }
		private double _factor;
		public double Factor { get { return _factor; } set { _factor = value; } }
		private bool _active;
		public Datapoint(int id) {
			_id = id;
			using(SQL sql = new SQL("create instance Datapoint")) {
				string[][] erg = sql.wpQuery("SELECT " +
					"[dp].[id_dpgroup], [dp].[id_opcdatapoint], [dp].[id_mqtttopic], [dp].[name], [dp].[description]," +
					"ISNULL([dp].[usergroupwrite], ISNULL([g].[usergroupwrite], ISNULL([ns].[usergroupwrite], 100))) AS [usergroupwrite], " +
					"[dp].[unit], [dp].[nks], [dp].[min], [dp].[max], [dp].[factor], [dp].[active], " +
					"[g].[id_dpnamespace] " + 
					"FROM [dp] " +
					"INNER JOIN [dpgroup] [g] ON [g].[id_dpgroup] = [dp].[id_dpgroup] " +
					"INNER JOIN [dpnamespace] [ns] ON [ns].[id_dpnamespace] = [g].[id_dpnamespace] " +
					$"WHERE [dp].[id_dp] = {_id}");
				int i = 0;
				if(erg.Length > 0) {
					Int32.TryParse(erg[0][i++], out _idGroup);
					_idOpc = erg[0][i] == "" ? null : Int32.Parse(erg[0][i]); i++;
					_idMqtt = erg[0][i] == "" ? null : Int32.Parse(erg[0][i]); i++;
					_name = erg[0][i++];
					_description = erg[0][i++];
					_writeLevel = Int32.Parse(erg[0][i++]);
					_unit = erg[0][i++];
					_nks = erg[0][i] == "" ? null : Int32.Parse(erg[0][i]); i++;
					_min = Double.Parse(erg[0][i++]);
					_max = Double.Parse(erg[0][i++]);
					_factor = Double.Parse(erg[0][i++]);
					_active = erg[0][i++] == "True" ? true : false;
					Int32.TryParse(erg[0][i++], out _idNamespace);
				}
			}
			if(wpDebug.debugSystem) wpDebug.Write($"Datapoint Created {_name} ({_id})");
		}
		public void setValue(string value) {
			setValue(value, "'Dictionary'");
		}
		/// <summary>
		/// set Value from drivers / PlugIns
		/// </summary>
		public void setValue(string value, string from) {
			DateTime Now = DateTime.Now;
			if(_value != value) {
				_value = value;
				_lastChange = Now;
				_valueString = parseValueString();
				Program.MainProg.lastchange = $"[{from}] '{_name}': {_valueString} ({_value})\r\n";
				Program.MainProg.wpWebSockets.sendDatapoint(this);
				if(_idTrend != null && Trends.Get((int)_idTrend).Intervall == 0)
					Trends.Get((int)_idTrend).SetTrendValue();
				if(_idAlarm != null)
					Alarms.Get((int)_idAlarm).setAlarmValue();
				if(wpDebug.debugTransferID)
					Debug.WriteLine($"Datenpunkt gesetzt '{_name}': {_valueString} ({_value})");
			}
		}
		public string parseValueString() {
			string returns = "";
			double dValue = 0.0;
			NumberStyles style = NumberStyles.Float;
			CultureInfo culture = CultureInfo.InvariantCulture;
			_value = _value.Replace(',', '.');
			if(Double.TryParse(_value, style, culture, out dValue)) {
				if(_factor != 1)
					dValue *= _factor;
				if(_nks != null)
					dValue = Math.Round(dValue, (int)_nks);
				returns = dValue.ToString();
			}
			if(_unit == "") {
				if(returns != "") {
					return returns;
				} else {
					return _value == "" ? " " : _value;
				}
			}
			if(Regex.IsMatch(_unit, @"(\d+):([^;]+)")) {
				foreach(Match m in Regex.Matches(_unit, @"(\d+):([^;]+)")) {
					if(_value == m.Groups[1].Value)
						return m.Groups[2].Value;
				}
			}
			if(Regex.IsMatch(_unit, @"(True|False):([^;]+)")) {
				foreach(Match m in Regex.Matches(_unit, @"(True|False):([^;]+)")) {
					if(_value == m.Groups[1].Value)
						return m.Groups[2].Value;
				}
			}
			return returns + " " + _unit;
		}
		/// <summary>
		/// write to drivers - onChange on success<br />
		/// setValue without driver
		/// </summary>
		/// <param name="value"></param>
		public async void writeValue(string value) {
			NumberStyles style = NumberStyles.Float;
			CultureInfo culture = CultureInfo.InvariantCulture;
			double dValue = 0.0;
			if(Double.TryParse(value, style, culture, out dValue)) {
				if(_factor != 1 && _factor > 0) {
					dValue = dValue / _factor;
				}
				if(dValue > _max) {
					wpDebug.Write($"Schreibwert ({value}) > Maxwert ({_max})\r\n\t'{_name}'");
					dValue = _max;
				}
				if(dValue < _min) {
					wpDebug.Write($"Schreibwert ({value}) < Minwert ({_min})\r\n\t'{_name}'");
					dValue = _min;
				}
				value = dValue.ToString();
			}
			if(_idOpc != null) {
				Program.MainProg.wpOPCClient.setValue((int)_idOpc, value);
			} else if(_idMqtt != null) {
				await Program.MainProg.wpMQTTClient.setValue((int)_idMqtt, value);
				// Simulate MQTT Subscribe
				setValue(value);
			} else {
				setValue(value);
			}
		}
	}
	
	public static class Datapoints {
		private static Dictionary<int, Datapoint> DP = new Dictionary<int, Datapoint>();
		private static Dictionary<string, int> DPnames = new Dictionary<string, int>();
		private static Dictionary<int, int> OPCList = new Dictionary<int, int>();
		public static void Init() {
			using(SQL sql = new SQL("fill Datapoints")) {
				string[][] erg = sql.wpQuery("SELECT [id_dp], [id_opcdatapoint] FROM [dp]");
				for(int idp = 0; idp < erg.Length; idp++) {
					int iddp = Int32.Parse(erg[idp][0]);
					try {
						Add(iddp, new Datapoint(iddp));
						if(erg[idp][1] != "") {
							OPCList.Add(Int32.Parse(erg[idp][1]), iddp);
						}
					} catch(Exception ex) {
						wpDebug.WriteError(ex, $"is verkehrt: {iddp}?");
					}
				}
			}
			wpDebug.Write("Datapoints Init");
		}
		public static void Start() {
			Program.MainProg.wpMQTTClient.valueChanged += MQTTClient_valueChanged;
			Program.MainProg.wpOPCClient.valueChanged += wpOPCClient_valueChanged;
			ShellyServer.valueChanged += ShellyServer_valueChanged;
			wpDebug.Write("Datapoints Start");
		}

		private static void ShellyServer_valueChanged(object sender, ShellyServer.valueChangedEventArgs e) {
			if(DP.ContainsKey(e.idDatapoint))
				Get(e.idDatapoint).setValue(e.value, "wpShellyServer");
		}
		private static void D1MiniServer_valueChanged(object sender, D1MiniServer.valueChangedEventArgs e) {
			if(DP.ContainsKey(e.idDatapoint))
				Get(e.idDatapoint).setValue(e.value, "wpD1MiniServer");
		}

		private static void MQTTClient_valueChanged(object sender, MQTTClient.valueChangedEventArgs e) {
			if(DP.ContainsKey(e.idDatapoint))
				Get(e.idDatapoint).setValue(e.value, "wpMQTTClient");
		}
		private static void wpOPCClient_valueChanged(object sender, OPCClient.valueChangedEventArgs e) {
			if(OPCList.ContainsKey(e.idOPCPoint))
				Get(OPCList[e.idOPCPoint]).setValue(e.value, "wpOPCClient");
		}
		public static void Add(int id, Datapoint dp) {
			if(DP.ContainsKey(id))
				DP[id] = dp;
			else
				DP.Add(id, dp);
			if(DPnames.ContainsKey(dp.Name))
				DPnames[dp.Name] = id;
			else
				DPnames.Add(dp.Name, id);
		}
		public static Datapoint Get(int id) {
			if(DP.ContainsKey(id))
				return DP[id];
			return null;
		}
		public static Datapoint Get(string name) {
			if(DPnames.ContainsKey(name))
				if(DP.ContainsKey(DPnames[name]))
					return DP[DPnames[name]];
			return null;
		}
		public static void writeValues(List<int> DpIds, string value) {
			Dictionary<int, string> d = new Dictionary<int, string>();
			foreach (int i in DpIds) d.Add(i, value);
			writeValues(d);
		}
		public static void writeValues(Dictionary<int, string> DpIds) {
			foreach(KeyValuePair<int, string> d in DpIds) {
				Get(d.Key).writeValue(d.Value);
			}
		}
	}
	public class Server {
		/// <summary></summary>
		public class Dictionaries {
			/// <summary>WebAutomationServer Event Log</summary>
			private static Logger EventLog = new Logger(wpEventLog.WebAutomation);
			private static Dictionary<int, OPCItem> _items = new Dictionary<int, OPCItem>();
			public static Dictionary<int, OPCItem> Items {
				get { return _items; }
				set { _items = value; }
			}
			//private static List<int> OPCItemsError;
			public static void addItem(int id, OPCItem value) {
				if(Monitor.TryEnter(Items, 5000)) {
					try {
						if(Items.ContainsKey(id)) Items[id] = value;
						else Items.Add(id, value);
					} catch(Exception ex) {
						EventLog.WriteError(ex);
					} finally {
						Monitor.Exit(Items);
					}
				} else {
					wpDebug.Write("Angefordertes Item not Entered (addItem:{1})", id);
				}
			}
			public static OPCItem getItem(int id) {
				OPCItem returns = null;
				if(Monitor.TryEnter(Items, 5000)) {
					try {
						if(Items.ContainsKey(id)) {
							returns = Items[id];
						}
					} catch(Exception ex) {
						EventLog.WriteError(ex);
					} finally {
						Monitor.Exit(Items);
					}
				} else {
					wpDebug.Write("Angefordertes Item not Entered (getItem:{0})", id);
				}
				return returns;
			}
			public static bool checkItem(int id) {
				bool returns = false;
				if(Monitor.TryEnter(Items, 5000)) {
					try {
						if(Items.ContainsKey(id)) {
							returns = true;
						}
					} catch(Exception ex) {
						EventLog.WriteError(ex);
					} finally {
						Monitor.Exit(Items);
					}
				} else {
					wpDebug.Write("Angefordertes Item not Entered (getItem:{0})", id);
				}
				return returns;
			}
			public static void deleteItem(int Datapoints) {
				if(Monitor.TryEnter(Items, 5000)) {
					try {
						if(Items.ContainsKey(Datapoints)) {
							Items[Datapoints] = null;
							Items.Remove(Datapoints);
						}
					} catch(Exception ex) {
						EventLog.WriteError(ex);
					} finally {
						Monitor.Exit(Items);
					}
				} else {
					wpDebug.Write("Angefordertes Item not Entered (deleteItem:{1})", Datapoints);
				}
			}
			public static void deleteItems(int[] Datapoints) {
				for(int i = 0; i < Datapoints.Length; i++) deleteItem(Datapoints[i]);
			}
		}
	}
}
