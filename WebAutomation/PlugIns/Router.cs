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
//# Revision     : $Rev:: 245                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: Router.cs 245 2025-06-28 15:07:22Z                       $ #
//#                                                                                 #
//###################################################################################
using FreakaZone.Libraries.wpEventLog;
using FreakaZone.Libraries.wpSQL;
using FreakaZone.Libraries.wpSQL.Table;
using System;
using System.Collections.Generic;
using System.Reflection;
using static FreakaZone.Libraries.wpEventLog.Logger;

namespace WebAutomation.PlugIns {
	public class Router {
		/// <summary></summary>
		private static Logger eventLog;
		private static Dictionary<int, List<int>> RouterItems = new Dictionary<int, List<int>>();
		public static void Start() {
			Debug.Write(MethodBase.GetCurrentMethod(), "Router Start");
			eventLog = new Logger(Logger.ESource.PlugInRouter);
			using(Database Sql = new Database("Add Router")) {
				List<TableRouter> ltr = Sql.Select<TableRouter>("[id_to] IS NOT NULL");
				foreach(TableRouter tr in ltr) {
					try {
						if(!RouterItems.ContainsKey(tr.id_dp)) {
							RouterItems.Add(tr.id_dp, new List<int>());
						}
						if(!RouterItems.ContainsKey(tr.id_to) && !RouterItems[tr.id_dp].Contains(tr.id_to)) {
							RouterItems[tr.id_dp].Add(tr.id_to);
						} else {
							eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Error,
								"Route würde einen Loop erzeugen! {0}", tr.id_to);
						}
					} catch(Exception ex) {
						eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
					}
				}
			}
			Debug.Write(MethodInfo.GetCurrentMethod(), "Router Started");
		}
		public static List<int> GetRoute(int id_dp) {
			if(RouterItems.ContainsKey(id_dp)) {
				return RouterItems[id_dp];
			} else {
				return new List<int>();
			}
		}
		public static void UpdateRouter(int fromid) {
			eventLog = new Logger(Logger.ESource.PlugInRouter);
			using(Database Sql = new Database("Update Router for Item")) {
				string[][] DBRouter = Sql.Query(@"SELECT [id_to] FROM [router] WHERE [id_dp] = {0} AND [id_to] IS NOT NULL", fromid);
				if(DBRouter.Length == 0) {
					if(RouterItems.ContainsKey(fromid))
						RouterItems.Remove(fromid);
				} else {
					if(RouterItems.ContainsKey(fromid)) {
						RouterItems[fromid].Clear();
					} else {
						RouterItems.Add(fromid, new List<int>());
					}
					for(int irouter = 0; irouter < DBRouter.Length; irouter++) {
						try {
							int idto;
							if(Int32.TryParse(DBRouter[irouter][0], out idto)) {
								RouterItems[fromid].Add(idto);
							}
						} catch(Exception ex) {
							eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
						}
					}
				}
			}
			Datapoints.Get(fromid).Route = GetRoute(fromid);
			eventLog.Write(MethodInfo.GetCurrentMethod(), "Router PlugIn geupdatet");
		}
	}
}
