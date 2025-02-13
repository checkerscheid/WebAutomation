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
//# File-ID      : $Id:: Router.cs 171 2025-02-13 12:28:06Z                       $ #
//#                                                                                 #
//###################################################################################
using FreakaZone.Libraries.wpEventLog;
using FreakaZone.Libraries.wpSQL;
using System;
using System.Collections.Generic;
using System.Reflection;
using WebAutomation.Helper;
using static FreakaZone.Libraries.wpEventLog.Logger;

namespace WebAutomation.PlugIns {
	public class Router {
		/// <summary></summary>
		private static Logger eventLog;
		private static Dictionary<int, List<int>> RouterItems = new Dictionary<int, List<int>>();
		public static void AddRouter() {
			eventLog = new Logger(FreakaZone.Libraries.wpEventLog.Logger.ESource.PlugInRouter);
			using (Database Sql = new Database("Add Router")) {
				string[][] DBRouter = Sql.wpQuery(@"SELECT [id_dp], [id_to] FROM [router]");
				for (int irouter = 0; irouter < DBRouter.Length; irouter++) {
					try {
						int idfrom;
						int idto;
						if (Int32.TryParse(DBRouter[irouter][0], out idfrom) &&
							Int32.TryParse(DBRouter[irouter][1], out idto)) {
							if(!RouterItems.ContainsKey(idfrom)) {
								RouterItems.Add(idfrom, new List<int>());
							}
							if(!RouterItems.ContainsKey(idto) && !RouterItems[idfrom].Contains(idto)) {
								RouterItems[idfrom].Add(idto);
							} else {
								eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Error,
									"Route würde einen Loop erzeugen! {0}", idto);
							}
						}
					} catch (Exception ex) {
						eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
					}
				}
			}
			eventLog.Write(MethodInfo.GetCurrentMethod(), "Router PlugIn geladen");
		}
		public static void UpdateRouter(int fromid) {
			eventLog = new Logger(FreakaZone.Libraries.wpEventLog.Logger.ESource.PlugInRouter);
			using (Database Sql = new Database("Update Router for Item")) {
				string[][] DBRouter = Sql.wpQuery(@"SELECT [id_to] FROM [opcrouter] WHERE [id_dp] = {0}", fromid);
				if (DBRouter.Length == 0) {
					if(RouterItems.ContainsKey(fromid)) RouterItems.Remove(fromid);
				} else {
					if (RouterItems.ContainsKey(fromid)) {
						RouterItems[fromid].Clear();
					} else {
						RouterItems.Add(fromid, new List<int>());
					}
					for (int irouter = 0; irouter < DBRouter.Length; irouter++) {
						try {
							int idto;
							if (Int32.TryParse(DBRouter[irouter][0], out idto)) {
								RouterItems[fromid].Add(idto);
							}
						} catch (Exception ex) {
							eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
						}
					}
				}
			}
			eventLog.Write(MethodInfo.GetCurrentMethod(), "Router PlugIn geupdatet");
		}
	}
}
