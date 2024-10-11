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
//# Revision     : $Rev:: 136                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: SQL.cs 136 2024-10-11 08:03:37Z                          $ #
//#                                                                                 #
//###################################################################################
using System;
using System.Data.SqlClient;
using System.Reflection;
using System.Threading.Tasks;
/**
* @addtogroup WebAutomation
* @{
*/
namespace WebAutomation.Helper {
	/// <summary>
	/// 
	/// </summary>
	public class SQL : IDisposable {
		/// <summary></summary>
		private Logger eventLog;
		/// <summary></summary>
		private SqlConnection connection;
		/// <summary></summary>
		public const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fff";
		/// <summary></summary>
		public const string DateFormat = "yyyy-MM-ddT00:00:00";
		/// <summary></summary>
		private SqlCommand command;
		/// <summary></summary>
		private SqlDataReader Reader;
		/// <summary></summary>
		private string LastError = "";
		/// <summary></summary>
		private string _forwhat;
		/// <summary></summary>
		private bool _available;
		/// <summary></summary>
		public bool Available {
			get { return _available; }
		}
		/// <summary>
		/// 
		/// </summary>
		public SQL(string forwhat) {
			_available = false;
			_forwhat = forwhat;
			eventLog = new Logger(wpEventLog.SQL);
			try {
				connection = new SqlConnection(String.Format(
					"Server={0};Trusted_Connection=true;database={1}",
					Ini.get("SQL", "Server"),
					Ini.get("SQL", "Database")));
				command = connection.CreateCommand();
				connection.Open();
				_available = true;
			} catch (Exception ex) {
				eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
				LastError = ex.Message;
				_available = false;
			}
			if(wpDebug.debugSQL) {
				wpDebug.Write(MethodInfo.GetCurrentMethod(), "SQL Client gestartet - {0}", _forwhat);
			}
		}
		/// <summary>
		/// 
		/// </summary>
		public void Dispose() {
			if(wpDebug.debugSQL) {
				wpDebug.Write(MethodInfo.GetCurrentMethod(), "SQL Client gestoppt - {0}", _forwhat);
			}
			connection.Close();
			connection.Dispose();
			connection = null;
			GC.SuppressFinalize(this);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public string getlastError() {
			string temperror = LastError;
			LastError = "";
			return temperror;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="m_command"></param>
		/// <returns></returns>
		public string[][] wpQuery(string m_command) {
			string[][] row = new string[][] {};
			command.CommandText = m_command;
			try {
				Reader = command.ExecuteReader();
				int j = 0;
				while (Reader.Read()) {
					Array.Resize(ref row, ++j);
					Array.Resize(ref row[(j - 1)], Reader.FieldCount);
					for (int i = 0; i < Reader.FieldCount; i++) {
						object v = Reader.GetValue(i);
						row[(j - 1)][i] = v.ToString();
					}
				}
				Reader.Close();
			} catch (Exception ex) {
				eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex, m_command, _forwhat);
				LastError = ex.Message;
			}
			return row;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="m_command"></param>
		/// <param name="obj"></param>
		/// <returns></returns>
		public string[][] wpQuery(string m_command, params object[] obj) {
			return wpQuery(String.Format(m_command, obj));
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="m_command"></param>
		/// <returns></returns>
		public int wpNonResponse(string m_command) {
			command.CommandText = m_command;
			int returns = 0;
			try {
				if (command.Connection.State == System.Data.ConnectionState.Open) {
					returns = command.ExecuteNonQuery();
				}
			} catch (Exception ex) {
				eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex, m_command, _forwhat);
				LastError = ex.Message;
			}
			return returns;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="m_command"></param>
		/// <param name="obj"></param>
		/// <returns></returns>
		public int wpNonResponse(string m_command, params object[] obj) {
			return wpNonResponse(String.Format(m_command, obj));
		}
		/// <summary>
		/// ohne Timeout
		/// </summary>
		/// <param name="m_command"></param>
		/// <returns></returns>
		public int wpNonResponseNT(string m_command) {
			command.CommandText = m_command;
			command.CommandTimeout = 0;
			int returns = 0;
			try {
				returns = command.ExecuteNonQuery();
			} catch (Exception ex) {
				eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex, m_command, _forwhat);
				LastError = ex.Message;
			}
			return returns;
		}
		/// <summary>
		/// ohne Timeout
		/// </summary>
		/// <param name="m_command"></param>
		/// <param name="obj"></param>
		/// <returns></returns>
		public int wpNonResponseNT(string m_command, params object[] obj) {
			return wpNonResponseNT(String.Format(m_command, obj));
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="m_bool"></param>
		/// <returns></returns>
		public static bool? convertBool(string m_bool) {
			bool? temp_bool = null;
			if (m_bool.ToLower() == "false" || m_bool == "0") temp_bool = false;
			if (m_bool.ToLower() == "true" || m_bool == "1") temp_bool = true;
			return temp_bool;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="m_bool"></param>
		/// <returns></returns>
		public static string convertBool(bool? m_bool) {
			string temp_bool = "";
			if (m_bool == true) temp_bool = "true";
			if (m_bool == false) temp_bool = "false";
			return temp_bool;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="s_numeric"></param>
		/// <returns></returns>
		public static int? convertNumeric(string s_numeric) {
			int? temp_numeric = null;
			int m_numeric;
			if (Int32.TryParse(s_numeric, out m_numeric)) {
				temp_numeric = m_numeric;
			}
			return temp_numeric;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="s_numeric"></param>
		/// <returns></returns>
		public static string convertNumeric(int? s_numeric) {
			string temp_numeric = "";
			if (s_numeric != null) temp_numeric = s_numeric.ToString();
			return temp_numeric;
		}

		public void HistoryCleaner() {
			string sql;
			DateTime OneYearAgo = DateTime.Now.AddMonths(-3);
			wpDebug.Write(MethodInfo.GetCurrentMethod(), "start HistoryCleaner");
			sql = $"DELETE FROM [alarmhistoric] WHERE [come] < '{OneYearAgo.ToString(DateFormat)}'";
			wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Delete {wpNonResponse(sql)} entries from [alarmhistoric]");
			sql = $"DELETE FROM [emailhistoric] WHERE [send] < '{OneYearAgo.ToString(DateFormat)}'";
			wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Delete {wpNonResponse(sql)} entries from [emailhistoric]");
			sql = $"DELETE FROM [useractivity] WHERE [writetime] < '{OneYearAgo.ToString(DateFormat)}'";
			wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Delete {wpNonResponse(sql)} entries from [useractivity]");
			sql = $"DELETE FROM [visitors] WHERE [datetime] < '{OneYearAgo.ToString(DateFormat)}'";
			wpDebug.Write(MethodInfo.GetCurrentMethod(), $"Delete {wpNonResponse(sql)} entries from [visitors]");
			wpDebug.Write(MethodInfo.GetCurrentMethod(), "start HistoryCleaner finished");
		}
	}
}
/** @} */
