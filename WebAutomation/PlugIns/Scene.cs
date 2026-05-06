//###################################################################################
//#                                                                                 #
//#              (C) FreakaZone GmbH                                                #
//#              =======================                                            #
//#                                                                                 #
//###################################################################################
//#                                                                                 #
//# Author       : Christian Scheid                                                 #
//# Date         : 10.09.2015                                                       #
//#                                                                                 #
//# Revision     : $Rev:: 251                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: Scene.cs 251 2025-12-23 12:06:40Z                        $ #
//#                                                                                 #
//###################################################################################
using FreakaZone.Libraries.wpEventLog;
using FreakaZone.Libraries.wpSQL;
using FreakaZone.Libraries.wpSQL.Table;
using MQTTnet.Server;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
/**
* @addtogroup WebAutomation
* @{
*/
namespace WebAutomation.PlugIns {
	/// <summary>
	/// 
	/// </summary>
	public class Scene {
		public static List<TableSceneValue> getScene(int idscene) {
			List<TableSceneValue> returns = new List<TableSceneValue>();
			using(Database Sql = new Database("Scene")) {
				TableScene ts = Sql.Select<TableScene, TableSceneValue>(idscene);
				foreach(TableSceneValue tsv in ts.SubValues) {
					returns.Add(tsv);
				}
			}
			return returns;
		}
		public static void writeSceneDP(int idscene) {
			foreach(TableSceneValue tsv in getScene(idscene)) {
				switch (tsv.type) {
					case FreakaZone.Libraries.wpSQL.Enum.SceneValueType.datapoint:
						Datapoints.Get(tsv.id_dp).WriteValue(tsv.value);
						break;
					case FreakaZone.Libraries.wpSQL.Enum.SceneValueType.url:
						string returns = "{\"erg\":\"S_ERROR\"}";
						try
						{
							WebClient webClient = new WebClient();
							Task.Run(() => returns = webClient.DownloadString(new Uri(tsv.value))).Wait();
						}
						catch (Exception ex)
						{
							Debug.WriteError(MethodInfo.GetCurrentMethod(), ex, $"{tsv.value}: '{returns}'");
						}
						break;
					default:
						break;
				}
			}
		}
	}
}
/** @} */
