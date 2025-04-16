//###################################################################################
//#                                                                                 #
//#              (C) FreakaZone GmbH                                                #
//#              =======================                                            #
//#                                                                                 #
//###################################################################################
//#                                                                                 #
//# Author       : Christian Scheid                                                 #
//# Date         : 16.04.2025                                                       #
//#                                                                                 #
//# Revision     : $Rev:: 201                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: Shopping.cs 201 2025-04-16 02:59:55Z                     $ #
//#                                                                                 #
//###################################################################################

using FreakaZone.Libraries.wpSQL;

namespace WebAutomation.Helper {
	public class Shopping {
		public static void setProductChecked(bool isChecked, int idGroup, int idProduct) {
			if(idGroup == 0) {
				setProductChecked(isChecked, idProduct);
			} else {
				setGroupProductChecked(isChecked, idGroup, idProduct);
			}
		}
		private static void setGroupProductChecked(bool isChecked, int idGroup, int idProduct) {
			using(Database Sql = new Database("Set Shopping Product checked")) {
				Sql.wpNonResponse($"UPDATE [shoppinggroupproduct] SET [ok] = {(isChecked ? 1 : 0)} WHERE [id_group] = {idGroup} AND [id_product] = {idProduct}");
			}
			Program.MainProg.wpWebSockets.sendAll($"{{\"response\":\"setShoppingChecked\",\"idGroup\":{idGroup},\"idProduct\":{idProduct},\"isChecked\":{(isChecked ? "true" : "false")}}}");
		}
		private static void setProductChecked(bool isChecked, int idProduct) {
			using(Database Sql = new Database("Set Shopping Product checked")) {
				Sql.wpNonResponse($"UPDATE [shoppinglistproduct] SET [ok] = {(isChecked ? 1 : 0)} WHERE [id_list] = 1 AND [id_product] = {idProduct}");
			}
			Program.MainProg.wpWebSockets.sendAll($"{{\"response\":\"setShoppingChecked\",\"idGroup\":0,\"idProduct\":{idProduct},\"isChecked\":{(isChecked ? "true" : "false")}}}");
		}
	}
}
