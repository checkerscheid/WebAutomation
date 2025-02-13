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
//# Revision     : $Rev:: 171                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: WriteLevel.cs 171 2025-02-13 12:28:06Z                   $ #
//#                                                                                 #
//###################################################################################
using FreakaZone.Libraries.wpEventLog;
using FreakaZone.Libraries.wpSQL;
using System;
using System.Reflection;
/**
* @addtogroup WebAutomation
* @{
*/
namespace WebAutomation.Helper {
	/// <summary>
	/// 
	/// </summary>
	public class WriteLevel {
		/// <summary></summary>
		private static Logger eventLog;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="_Me"></param>
		public static void AddWriteLevel() {
			eventLog = new Logger(FreakaZone.Libraries.wpEventLog.Logger.ESource.PlugInWriteLevel);
			using (Database Sql = new Database("Add Write Level")) {
				string[][] DBWriteLevel = Sql.wpQuery(@"SELECT
					[dp].[id_dp],
					ISNULL([dp].[usergroupwrite], ISNULL([g].[usergroupwrite], ISNULL([s].[usergroupwrite], 100)))
					AS [usergroupwrite]
					FROM [dp]
					INNER JOIN [dpgroup] [g] ON [dp].[id_dpgroup] = [g].[id_dpgroup]
					INNER JOIN [dpnamespace] [s] ON [g].[id_dpnamespace] = [s].[id_dpnamespace]
					WHERE [s].[active] = 1 AND [g].[active] = 1 AND [dp].[active] = 1"
				);
				LevelToItem(DBWriteLevel);
			}
			eventLog.Write(MethodInfo.GetCurrentMethod(), "Taster PlugIn geladen");
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="_Me"></param>
		/// <param name="idserver"></param>
		public static void AddWriteLevel(int idserver) {
			using (Database Sql = new Database("Add Write Level for Server")) {
				string[][] DBWriteLevel = Sql.wpQuery(@"SELECT
					[dp].[id_dp],
					ISNULL([dp].[usergroupwrite], ISNULL([g].[usergroupwrite], ISNULL([s].[usergroupwrite], 100)))
					AS [usergroupwrite]
					FROM [dp]
					INNER JOIN [dpgroup] [g] ON [dp].[id_dpgroup] = [g].[id_dpgroup]
					INNER JOIN [dpnamespace] [s] ON [g].[id_dpnamespace] = [s].[id_dpnamespace]
					WHERE [s].[id_opcserver] = {0}
					AND [s].[active] = 1 AND [g].[active] = 1 AND [dp].[active] = 1",
					idserver
				);
				LevelToItem(DBWriteLevel);
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="_Me"></param>
		/// <param name="idserver"></param>
		public static void AddGroupWriteLevel(int idgroup) {
			using (Database Sql = new Database("Add Write Level for Group")) {
				string[][] DBWriteLevel = Sql.wpQuery(@"SELECT
					[dp].[id_dp],
					ISNULL([dp].[usergroupwrite], ISNULL([g].[usergroupwrite], ISNULL([s].[usergroupwrite], 100)))
					AS [usergroupwrite]
					FROM [dp]
					INNER JOIN [dpgroup] [g] ON [dp].[id_dpgroup] = [g].[id_dpgroup]
					INNER JOIN [dpnamespace] [s] ON [g].[id_dpnamespace] = [s].[id_dpnamespace]
					WHERE [g].[id_opcgroup] = {0}
					AND [s].[active] = 1 AND [g].[active] = 1 AND [dp].[active] = 1",
					idgroup
				);
				LevelToItem(DBWriteLevel);
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="_Me"></param>
		/// <param name="idserver"></param>
		public static void AddItemWriteLevel(int iditem) {
			using (Database Sql = new Database("Add Write Level for Item")) {
				string[][] DBWriteLevel = Sql.wpQuery(@"SELECT
					[dp].[id_dp],
					ISNULL([dp].[usergroupwrite], ISNULL([g].[usergroupwrite], ISNULL([s].[usergroupwrite], 100)))
					AS [usergroupwrite]
					FROM [dp]
					INNER JOIN [dpgroup] [g] ON [dp].[id_dpgroup] = [g].[id_dpgroup]
					INNER JOIN [dpnamespace] [s] ON [g].[id_dpnamespace] = [s].[id_dpnamespace]
					WHERE [d].[id_opcdatapoint] = {0}
					AND [s].[active] = 1 AND [g].[active] = 1 AND [dp].[active] = 1",
					iditem
				);
				LevelToItem(DBWriteLevel);
			}
		}
		private static void LevelToItem(string[][] WriteLevel) {
			for (int iWriteLevel = 0; iWriteLevel < WriteLevel.Length; iWriteLevel++) {
				int _iddatapoint;
				int _writelevel;
				double _min;
				double _max;
				if (Int32.TryParse(WriteLevel[iWriteLevel][0], out _iddatapoint) &&
					Int32.TryParse(WriteLevel[iWriteLevel][3], out _writelevel)) {
						Datapoint TheItem = Datapoints.Get(_iddatapoint);
						if(TheItem != null) {
							TheItem.WriteLevel = _writelevel;
							if (Double.TryParse(WriteLevel[iWriteLevel][1], out _min))
								TheItem.Min = _min;
							if (Double.TryParse(WriteLevel[iWriteLevel][2], out _max))
								TheItem.Max = _max;
						}
				}
			}
		}
	}
}
/** @} */
