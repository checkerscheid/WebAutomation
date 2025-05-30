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
//# Revision     : $Rev:: 237                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: Scene.cs 237 2025-05-30 11:23:27Z                        $ #
//#                                                                                 #
//###################################################################################
using FreakaZone.Libraries.wpSQL;
using FreakaZone.Libraries.wpSQL.Table;
using System.Collections.Generic;
/**
* @addtogroup WebAutomation
* @{
*/
namespace WebAutomation.PlugIns {
	/// <summary>
	/// 
	/// </summary>
	public class Scene {
		public static Dictionary<int, string> getScene(int idscene) {
			Dictionary<int, string> returns = new Dictionary<int, string>();
			using(Database Sql = new Database("Scene")) {
				TableScene ts = Sql.Select<TableScene, TableSceneValue>(idscene);
				foreach(TableSceneValue tsv in ts.SubValues) {
					returns.Add(tsv.id_dp, tsv.value);
				}
			}
			return returns;
		}
		public static void writeSceneDP(int idscene) {
			Datapoints.WriteValues(getScene(idscene));
		}
	}
}
/** @} */
