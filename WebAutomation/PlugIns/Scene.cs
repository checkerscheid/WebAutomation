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
//# Revision     : $Rev:: 77                                                      $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: Scene.cs 77 2024-03-13 18:22:41Z                         $ #
//#                                                                                 #
//###################################################################################
using System;
using System.Collections.Generic;
using WebAutomation.Helper;
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
			using (SQL SQL = new SQL("Scene")) {
				string[][] DBScene = SQL.wpQuery(@"SELECT
					[s].[id_scene], [v].[id_dp], [v].[value]
					FROM [scene] [s]
					INNER JOIN [scenevalue] [v] ON [s].[id_scene] = [v].[id_scene]
					WHERE [s].[id_scene] = {0}", idscene);
				for (int i = 0; i < DBScene.Length; i++) {
					int iddp;
					if (Int32.TryParse(DBScene[i][1], out iddp)) {
						if (returns.ContainsKey(iddp)) returns[iddp] = DBScene[i][2];
						else returns.Add(iddp, DBScene[i][2]);
					}
				}
			}
			return returns;
		}
		public static void writeSceneDP(int idscene) {
			Datapoints.writeValues(getScene(idscene));
		}
	}
}
/** @} */
