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
//# Revision     : $Rev:: 204                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: Dictionaries.cs 204 2025-05-01 20:19:13Z                 $ #
//#                                                                                 #
//###################################################################################
using FreakaZone.Libraries.wpEventLog;
using FreakaZone.Libraries.wpSQL;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Schema;
using WebAutomation.Helper;
using WebAutomation.PlugIns;

namespace WebAutomation {
	public class Datapoint {
		private static Logger EventLog = new Logger(Logger.ESource.WebAutomation);
		private int _id;
		public int ID { get { return _id; } }
		private int _idGroup;
		private int _idNamespace;
		private int? _idOpc;
		public int? IdOpc => _idOpc;
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
			using(Database Sql = new Database("create instance Datapoint")) {
				string[][] erg = Sql.wpQuery("SELECT " +
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
			if(Debug.debugSystem)
				Debug.Write(MethodInfo.GetCurrentMethod(), $"Datapoint Created {_name} ({_id})");
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
				try {
					_value = value;
					_lastChange = Now;
					_valueString = parseValueString();
					Program.MainProg.lastchange = $"[{from}] '{_name}': {_valueString} ({_value})\r\n";
					Program.MainProg.wpWebSockets.sendDatapoint(this);
					if(_idTrend != null) {
						Trend t = Trends.Get((int)_idTrend);
						if(t != null && t.Intervall == 0) {
							t.SetTrendValue();
						}
					}
					if(_idAlarm != null) {
						Alarm a = Alarms.Get((int)_idAlarm);
						if(a != null) {
							a.setAlarmValue();
						}
					}
					if(Debug.debugTransferID)
						Debug.Write(MethodInfo.GetCurrentMethod(), $"Datenpunkt '{_name}' gesetzt von '{from}': {_valueString} ({_value})");
				} catch(Exception ex) {
					EventLog.WriteError(MethodInfo.GetCurrentMethod(), ex, _name, _value);
				}
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
		public async void writeValue(string value, string from) {
			NumberStyles style = NumberStyles.Float;
			CultureInfo culture = CultureInfo.InvariantCulture;
			double dValue = 0.0;
			if(Double.TryParse(value, style, culture, out dValue)) {
				if(_factor != 1 && _factor > 0) {
					dValue = dValue / _factor;
				}
				if(dValue > _max) {
					Debug.Write(MethodInfo.GetCurrentMethod(), $"Schreibwert ({value}) > Maxwert ({_max})\r\n\t'{_name}'");
					dValue = _max;
				}
				if(dValue < _min) {
					Debug.Write(MethodInfo.GetCurrentMethod(), $"Schreibwert ({value}) < Minwert ({_min})\r\n\t'{_name}'");
					dValue = _min;
				}
				value = dValue.ToString();
			}
			if(_idOpc != null || _idMqtt != null) {
				if(_idOpc != null) {
					Program.MainProg.wpOPCClient.setValue((int)_idOpc, value);
				}
				if(_idMqtt != null) {
					await Program.MainProg.wpMQTTClient.setValue((int)_idMqtt, value);
					// Simulate MQTT Subscribe
					setValue(value);
				}
			} else {
				setValue(value);
			}
		}
		/// <summary>
		/// write to drivers - onChange on success<br />
		/// setValue without driver
		/// </summary>
		/// <param name="value"></param>
		public void writeValue(string value) {
			writeValue(value, "Dictionary");
		}
	}
	
	public static class Datapoints {
		private static List<Datapoint> _dp = new List<Datapoint>();
		public static void Init() {
			Debug.Write(MethodInfo.GetCurrentMethod(), "Datapoints Init");
			using(Database Sql = new Database("fill Datapoints")) {
				string[][] erg = Sql.wpQuery("SELECT [id_dp] FROM [dp]");
				for(int idp = 0; idp < erg.Length; idp++) {
					int iddp = Int32.Parse(erg[idp][0]);
					try {
						Add(iddp, new Datapoint(iddp));
					} catch(Exception ex) {
						Debug.WriteError(MethodInfo.GetCurrentMethod(), ex, $"is verkehrt: {iddp}?");
					}
				}
			}
			Debug.Write(MethodInfo.GetCurrentMethod(), "Datapoints gestartet");
		}
		public static void Start() {
			Program.MainProg.wpMQTTClient.valueChanged += MQTTClient_valueChanged;
			Program.MainProg.wpOPCClient.valueChanged += wpOPCClient_valueChanged;
			ShellyServer.valueChanged += ShellyServer_valueChanged;
			Debug.Write(MethodInfo.GetCurrentMethod(), "Datapoints Start work");
		}

		private static void ShellyServer_valueChanged(object sender, ShellyServer.valueChangedEventArgs e) {
			if(_dp.Exists(t => t.ID == e.idDatapoint))
				_dp.Find(t => t.ID == e.idDatapoint).setValue(e.value, "wpShellyServer");
		}
		private static void D1MiniServer_valueChanged(object sender, D1MiniServer.valueChangedEventArgs e) {
			if(_dp.Exists(t => t.ID == e.idDatapoint))
				_dp.Find(t => t.ID == e.idDatapoint).setValue(e.value, "wpD1MiniServer");
		}

		private static void MQTTClient_valueChanged(object sender, MQTTClient.valueChangedEventArgs e) {
			if(_dp.Exists(t => t.ID == e.idDatapoint))
				_dp.Find(t => t.ID == e.idDatapoint).setValue(e.value, "wpMQTTClient");
		}
		private static void wpOPCClient_valueChanged(object sender, OPCClient.valueChangedEventArgs e) {
			if(_dp.Exists(t => t.IdOpc == e.idOPCPoint))
				_dp.Find(t => t.IdOpc == e.idOPCPoint).setValue(e.value, "wpOPCClient");
		}
		public static void Add(int id, Datapoint dp) {
			if(_dp.Exists(t => t.ID == id)) {
				int i = _dp.FindIndex(t => t.ID == id);
				_dp[i] = dp;
			} else
				_dp.Add(dp);
		}
		public static Datapoint Get(int id) {
			if(_dp.Exists(t => t.ID == id))
				return _dp.Find(t => t.ID == id);
			return null;
		}
		public static Datapoint GetFromAlarmId(int id) {
			if(_dp.Exists(t => t.idAlarm == id))
				return _dp.Find(t => t.idAlarm == id);
			return null;
		}
		public static Datapoint Get(string name) {
			if(_dp.Exists(t => t.Name == name))
				return _dp.Find(t => t.Name == name);
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
	public class OpcDatapoints {
		/// <summary>WebAutomationServer Event Log</summary>
		private static Logger EventLog = new Logger(Logger.ESource.WebAutomation);
		private static List<OPCItem> _items = new List<OPCItem>();
		public static List<OPCItem> Items {
			get { return _items; }
			set { _items = value; }
		}
		//private static List<int> OPCItemsError;
		public static void addItem(OPCItem value) {
			if(Monitor.TryEnter(Items, 5000)) {
				try {
					if(Items.Exists(t => t.Hclt == value.Hclt)) {
						int i = Items.FindIndex(t => t.Hclt == value.Hclt);
						Items[i] = value;
					} else
						Items.Add(value);
				} catch(Exception ex) {
					EventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
				} finally {
					Monitor.Exit(Items);
				}
			} else {
				Debug.Write(MethodInfo.GetCurrentMethod(), "Angefordertes Item not Entered (addItem:{1})", value.Hclt);
			}
		}
		public static OPCItem getItem(int id) {
			OPCItem returns = null;
			if(Monitor.TryEnter(Items, 5000)) {
				try {
					if(Items.Exists(t => t.Hclt == id)) {
						returns = Items.Find(t => t.Hclt == id);
					}
				} catch(Exception ex) {
					EventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
				} finally {
					Monitor.Exit(Items);
				}
			} else {
				Debug.Write(MethodInfo.GetCurrentMethod(), "Angefordertes Item not Entered (getItem:{0})", id);
			}
			return returns;
		}
		public static bool checkItem(int id) {
			bool returns = false;
			if(Monitor.TryEnter(Items, 5000)) {
				try {
					if(Items.Exists(t => t.Hclt == id)) {
							returns = true;
					}
				} catch(Exception ex) {
					EventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
				} finally {
					Monitor.Exit(Items);
				}
			} else {
				Debug.Write(MethodInfo.GetCurrentMethod(), "Angefordertes Item not Entered (getItem:{0})", id);
			}
			return returns;
		}
		public static void deleteItem(int id) {
			if(Monitor.TryEnter(Items, 5000)) {
				try {
					if(Items.Exists(t => t.Hclt == id)) {
						Items[Items.FindIndex(t => t.Hclt == id)] = null;
						Items.Remove(Items.Find(t => t.Hclt == id));
					}
				} catch(Exception ex) {
					EventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
				} finally {
					Monitor.Exit(Items);
				}
			} else {
				Debug.Write(MethodInfo.GetCurrentMethod(), "Angefordertes Item not Entered (deleteItem:{1})", id);
			}
		}
		public static void deleteItems(int[] Datapoints) {
			for(int i = 0; i < Datapoints.Length; i++) deleteItem(Datapoints[i]);
		}
	}
}
