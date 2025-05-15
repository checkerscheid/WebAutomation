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
//# Revision     : $Rev:: 213                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: Dictionaries.cs 213 2025-05-15 14:50:57Z                 $ #
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

	/// <summary>
	/// Represents a data point in the system, encapsulating its properties, state, and behavior.
	/// </summary>
	/// <remarks>A <see cref="Datapoint"/> is used to manage and interact with individual data points, including
	/// their values, metadata, and associated operations. It provides functionality for reading and writing values,
	/// tracking changes, and interacting with external systems such as OPC or MQTT.</remarks>
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

		/// <summary>
		/// Initializes a new instance of the <see cref="Datapoint"/> class by loading its details from the database.
		/// </summary>
		/// <remarks>This constructor retrieves the details of the specified datapoint from the database, including
		/// its group, namespace,  and other metadata. If the datapoint does not exist in the database, the instance will be
		/// initialized with default values.</remarks>
		/// <param name="id">The unique identifier of the datapoint to be loaded.</param>
		public Datapoint(int id) {
			_id = id;
			using(Database Sql = new Database("create instance Datapoint")) {
				string[][] erg = Sql.Query("SELECT " +
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

		/// <summary>
		/// Sets the specified value in the dictionary.
		/// </summary>
		/// <param name="value">The value to set. Cannot be null or empty.</param>
		public void SetValue(string value) {
			SetValue(value, "'Dictionary'");
		}

		/// <summary>
		/// Updates the value of the data point and triggers associated actions if the value changes.
		/// </summary>
		/// <remarks>If the value is updated, this method performs several actions, including: updating the last
		/// change timestamp, parsing the value into a string representation,  logging the change, notifying connected systems
		/// via WebSocket, and triggering any associated  trend or alarm updates. Exceptions during the update process are
		/// logged.</remarks>
		/// <param name="value">The new value to set for the data point.</param>
		/// <param name="from">The source or origin of the value change, typically used for logging purposes.</param>
		public void SetValue(string value, string from) {
			DateTime Now = DateTime.Now;
			if(_value != value) {
				try {
					_value = value;
					_lastChange = Now;
					_valueString = ParseValueString();
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

		/// <summary>
		/// Parses the current value string and returns a formatted representation based on the specified factor, precision,
		/// and unit mappings.
		/// </summary>
		/// <remarks>This method processes the value string by attempting to parse it as a numeric value. If
		/// successful, it applies a scaling factor and rounds the result to the specified number of decimal places. If a unit
		/// mapping is provided, the method may return a mapped value based on the input. If no unit is specified, the
		/// original or processed value is returned.</remarks>
		/// <returns>A string representing the parsed and formatted value. If the value matches a unit mapping, the corresponding
		/// mapped value is returned. If no unit is specified, the numeric value (if parsed) or the original value is
		/// returned. If the value is empty, a single space is returned.</returns>
		public string ParseValueString() {
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
		/// Writes a value to the appropriate target, applying scaling and boundary checks as needed.
		/// </summary>
		/// <remarks>If the value is numeric, it is scaled by the internal factor (if applicable) and clamped to the
		/// defined minimum and maximum bounds. The method writes the processed value to one or more targets, such as OPC or
		/// MQTT clients, if they are configured. If no external targets are configured, the value is set locally.</remarks>
		/// <param name="value">The value to be written, represented as a string. Must be a valid numeric value if scaling or boundary checks are
		/// applied.</param>
		/// <param name="from">The source identifier of the value, used for logging or tracking purposes.</param>
		public async void WriteValue(string value, string from) {
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
					SetValue(value);
				}
			} else {
				SetValue(value);
			}
		}

		/// <summary>
		/// Writes the specified value to the default output format.
		/// </summary>
		/// <remarks>This method writes the provided value using the default format, which is "Dictionary". To specify
		/// a different format, use the overloaded method that accepts a format parameter.</remarks>
		/// <param name="value">The value to be written. Cannot be null.</param>
		public void WriteValue(string value) {
			WriteValue(value, "Dictionary");
		}
	}
	
	/// <summary>
	/// Provides a centralized management system for handling and interacting with datapoints.
	/// </summary>
	/// <remarks>The <see cref="Datapoints"/> class is a static utility class designed to manage a collection of 
	/// <see cref="Datapoint"/> objects. It provides methods for initializing, adding, retrieving, and  updating
	/// datapoints, as well as handling value changes from various external sources such as  MQTT clients, OPC clients, and
	/// other servers.  This class is intended to be used as a global manager for datapoints, ensuring consistent  access
	/// and updates across the application. It also integrates with external systems to  synchronize datapoint values in
	/// real-time.</remarks>
	public static class Datapoints {
		private static List<Datapoint> _dp = new List<Datapoint>();

		/// <summary>
		/// Initializes the datapoints by loading them from the database and adding them to the collection.
		/// </summary>
		/// <remarks>This method retrieves all datapoint IDs from the database and attempts to create and add a  <see
		/// cref="Datapoint"/> instance for each ID. If an error occurs while processing a specific  datapoint, the error is
		/// logged, and the method continues processing the remaining datapoints.</remarks>
		public static void Init() {
			Debug.Write(MethodInfo.GetCurrentMethod(), "Datapoints Init");
			using(Database Sql = new Database("fill Datapoints")) {
				string[][] erg = Sql.Query("SELECT [id_dp] FROM [dp]");
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

		/// <summary>
		/// Initializes the necessary event handlers for monitoring value changes in MQTT, OPC, and Shelly server clients.
		/// </summary>
		/// <remarks>This method sets up event subscriptions to handle value changes from various data sources,
		/// enabling the application         to respond to updates from MQTT, OPC, and Shelly server clients. Ensure that the
		/// associated clients are properly         initialized before calling this method.</remarks>
		public static void Start() {
			Program.MainProg.wpMQTTClient.valueChanged += MQTTClient_valueChanged;
			Program.MainProg.wpOPCClient.valueChanged += OPCClient_valueChanged;
			ShellyServer.ValueChanged += ShellyServer_valueChanged;
			Debug.Write(MethodInfo.GetCurrentMethod(), "Datapoints Start work");
		}

		/// <summary>
		/// Handles the event triggered when a value changes in the Shelly server.
		/// </summary>
		/// <remarks>This method updates the value of the corresponding datapoint in the internal collection if a
		/// datapoint with the specified ID exists.</remarks>
		/// <param name="sender">The source of the event, typically the Shelly server instance.</param>
		/// <param name="e">The event data containing the ID of the datapoint and the new value.</param>
		private static void ShellyServer_valueChanged(object sender, ShellyServer.valueChangedEventArgs e) {
			if(_dp.Exists(t => t.ID == e.IdDatapoint))
				_dp.Find(t => t.ID == e.IdDatapoint).SetValue(e.Value, "wpShellyServer");
		}

		/// <summary>
		/// Handles the event triggered when a value changes in the D1MiniServer.
		/// </summary>
		/// <remarks>This method updates the value of the corresponding datapoint in the internal collection if a
		/// datapoint with the specified ID exists.</remarks>
		/// <param name="sender">The source of the event, typically the D1MiniServer instance.</param>
		/// <param name="e">The event arguments containing the ID of the datapoint and the new value.</param>
		private static void D1MiniServer_valueChanged(object sender, D1MiniServer.valueChangedEventArgs e) {
			if(_dp.Exists(t => t.ID == e.idDatapoint))
				_dp.Find(t => t.ID == e.idDatapoint).SetValue(e.value, "wpD1MiniServer");
		}

		/// <summary>
		/// Handles the event triggered when the value of an MQTT datapoint changes.
		/// </summary>
		/// <remarks>This method updates the value of the corresponding datapoint in the internal collection if a
		/// datapoint with the specified ID exists.</remarks>
		/// <param name="sender">The source of the event, typically the MQTT client instance.</param>
		/// <param name="e">The event arguments containing the ID of the datapoint and its new value.</param>
		private static void MQTTClient_valueChanged(object sender, MQTTClient.valueChangedEventArgs e) {
			if(_dp.Exists(t => t.ID == e.idDatapoint))
				_dp.Find(t => t.ID == e.idDatapoint).SetValue(e.value, "wpMQTTClient");
		}

		/// <summary>
		/// Handles the event triggered when the value of an OPC point changes.
		/// </summary>
		/// <remarks>This method updates the value of the corresponding data point in the internal collection if a
		/// match is found for the specified OPC point ID.</remarks>
		/// <param name="sender">The source of the event, typically the OPC client instance.</param>
		/// <param name="e">The event arguments containing the ID of the OPC point and its new value.</param>
		private static void OPCClient_valueChanged(object sender, OPCClient.valueChangedEventArgs e) {
			if(_dp.Exists(t => t.IdOpc == e.idOPCPoint))
				_dp.Find(t => t.IdOpc == e.idOPCPoint).SetValue(e.value, "wpOPCClient");
		}

		/// <summary>
		/// Adds a new datapoint to the collection or updates an existing one with the specified ID.
		/// </summary>
		/// <remarks>If a datapoint with the specified <paramref name="id"/> already exists in the collection, it will
		/// be replaced with the provided <paramref name="dp"/>. Otherwise, the <paramref name="dp"/> will be added as a new
		/// entry.</remarks>
		/// <param name="id">The unique identifier of the datapoint to add or update.</param>
		/// <param name="dp">The <see cref="Datapoint"/> object to add or update in the collection.</param>
		public static void Add(int id, Datapoint dp) {
			if(_dp.Exists(t => t.ID == id)) {
				int i = _dp.FindIndex(t => t.ID == id);
				_dp[i] = dp;
			} else
				_dp.Add(dp);
		}

		/// <summary>
		/// Retrieves a <see cref="Datapoint"/> object with the specified identifier.
		/// </summary>
		/// <remarks>This method searches for a <see cref="Datapoint"/> in the internal collection by its identifier.
		/// If no matching <see cref="Datapoint"/> is found, the method returns <see langword="null"/>.</remarks>
		/// <param name="id">The unique identifier of the <see cref="Datapoint"/> to retrieve.</param>
		/// <returns>The <see cref="Datapoint"/> object with the specified identifier if it exists; otherwise, <see langword="null"/>.</returns>
		public static Datapoint Get(int id) {
			if(_dp.Exists(t => t.ID == id))
				return _dp.Find(t => t.ID == id);
			return null;
		}

		/// <summary>
		/// Retrieves a <see cref="Datapoint"/> object associated with the specified alarm ID.
		/// </summary>
		/// <param name="id">The unique identifier of the alarm to search for.</param>
		/// <returns>The <see cref="Datapoint"/> object associated with the specified alarm ID,  or <see langword="null"/> if no
		/// matching datapoint is found.</returns>
		public static Datapoint GetFromAlarmId(int id) {
			if(_dp.Exists(t => t.idAlarm == id))
				return _dp.Find(t => t.idAlarm == id);
			return null;
		}

		/// <summary>
		/// Retrieves a <see cref="Datapoint"/> object by its name.
		/// </summary>
		/// <param name="name">The name of the <see cref="Datapoint"/> to retrieve. Cannot be null or empty.</param>
		/// <returns>The <see cref="Datapoint"/> object with the specified name, or <see langword="null"/> if no matching datapoint is
		/// found.</returns>
		public static Datapoint Get(string name) {
			if(_dp.Exists(t => t.Name == name))
				return _dp.Find(t => t.Name == name);
			return null;
		}

		/// <summary>
		/// Writes the specified value to a collection of data points identified by their IDs.
		/// </summary>
		/// <remarks>This method creates a mapping of data point IDs to the specified value and delegates the
		/// operation to an internal method.</remarks>
		/// <param name="DpIds">A list of integers representing the IDs of the data points to which the value will be written. Cannot be null.</param>
		/// <param name="value">The value to write to each data point. Cannot be null.</param>
		public static void WriteValues(List<int> DpIds, string value) {
			Dictionary<int, string> d = new Dictionary<int, string>();
			foreach (int i in DpIds) d.Add(i, value);
			WriteValues(d);
		}

		/// <summary>
		/// Writes the specified values to their corresponding data points.
		/// </summary>
		/// <remarks>Each key-value pair in the dictionary is processed, and the value is written to the data point
		/// identified by the key. Ensure that the dictionary contains valid data point identifiers and corresponding
		/// values.</remarks>
		/// <param name="DpIds">A dictionary where the key represents the data point identifier, and the value represents the value to be written
		/// to that data point.</param>
		public static void WriteValues(Dictionary<int, string> DpIds) {
			foreach(KeyValuePair<int, string> d in DpIds) {
				Get(d.Key).WriteValue(d.Value);
			}
		}
	}

	/// <summary>
	/// Provides a thread-safe collection of OPC (OLE for Process Control) items, allowing for the addition, retrieval, 
	/// validation, and deletion of items. This class is designed to manage OPC data points in a concurrent environment.
	/// </summary>
	/// <remarks>The <see cref="OpcDatapoints"/> class maintains a static list of <see cref="OPCItem"/> objects,
	/// providing methods  to manipulate the collection in a thread-safe manner. Operations such as adding, retrieving,
	/// checking, and deleting  items are synchronized to ensure safe access in multi-threaded scenarios.   Note that the
	/// class uses a timeout of 5000 milliseconds when attempting to acquire a lock on the collection.  If the lock cannot
	/// be acquired within this time, the operation will fail silently, and a debug message will be logged.</remarks>
	public class OpcDatapoints {
		/// <summary>WebAutomationServer Event Log</summary>
		private static Logger EventLog = new Logger(Logger.ESource.WebAutomation);
		private static List<OPCItem> _items = new List<OPCItem>();
		public static List<OPCItem> Items {
			get { return _items; }
			set { _items = value; }
		}
		
		/// <summary>
		/// Adds an OPC item to the collection, updating the existing item if one with the same Hclt value already exists.
		/// </summary>
		/// <remarks>If an item with the same Hclt value as <paramref name="value"/> already exists in the collection,
		/// it will be replaced with the new item. Otherwise, the item is added to the collection. This method uses a
		/// thread-safe mechanism to ensure that the collection is accessed safely.</remarks>
		/// <param name="value">The <see cref="OPCItem"/> to add or update in the collection. Must not be <c>null</c>.</param>
		public static void AddItem(OPCItem value) {
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

		/// <summary>
		/// Retrieves an OPCItem from the collection based on the specified identifier.
		/// </summary>
		/// <remarks>This method attempts to access the collection of items in a thread-safe manner. If the 
		/// collection cannot be accessed within the timeout period, the method logs a debug message  and returns <see
		/// langword="null"/>.</remarks>
		/// <param name="id">The unique identifier of the OPCItem to retrieve.</param>
		/// <returns>The <see cref="OPCItem"/> with the specified identifier if it exists in the collection;  otherwise, <see
		/// langword="null"/>.</returns>
		public static OPCItem GetItem(int id) {
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

		/// <summary>
		/// Determines whether an item with the specified ID exists in the collection.
		/// </summary>
		/// <remarks>This method attempts to acquire a lock on the collection before performing the check.  If the
		/// lock cannot be acquired within 5 seconds, the method will return <see langword="false"/>  and log a debug message.
		/// Exceptions encountered during the operation are logged.</remarks>
		/// <param name="id">The unique identifier of the item to check for existence.</param>
		/// <returns><see langword="true"/> if an item with the specified ID exists in the collection;  otherwise, <see
		/// langword="false"/>.</returns>
		public static bool CheckItem(int id) {
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

		/// <summary>
		/// Deletes an item with the specified identifier from the collection.
		/// </summary>
		/// <remarks>This method attempts to remove an item from the shared collection within a timeout of 5 seconds. 
		/// If the item with the specified identifier exists, it is removed from the collection.  If the lock cannot be
		/// acquired within the timeout, the method logs a debug message.</remarks>
		/// <param name="id">The unique identifier of the item to delete.</param>
		public static void DeleteItem(int id) {
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

		/// <summary>
		/// Deletes the specified items by their unique identifiers.
		/// </summary>
		/// <remarks>Each identifier in the <paramref name="Datapoints"/> array is processed individually.  If the
		/// array is empty, no action is performed. Ensure that the identifiers provided are valid.</remarks>
		/// <param name="Datapoints">An array of integers representing the unique identifiers of the items to delete. Cannot be null.</param>
		public static void DeleteItems(int[] Datapoints) {
			for(int i = 0; i < Datapoints.Length; i++) DeleteItem(Datapoints[i]);
		}
	}
}
