//###################################################################################
//#                                                                                 #
//#              (C) FreakaZone GmbH                                                #
//#              =======================                                            #
//#                                                                                 #
//###################################################################################
//#                                                                                 #
//# Author       : Christian Scheid                                                 #
//# Date         : 30.05.2025                                                       #
//#                                                                                 #
//# Revision     : $Rev:: 223                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: Shelly.cs 223 2025-05-24 15:41:01Z                       $ #
//#                                                                                 #
//###################################################################################
using System.Collections.Generic;

namespace WebAutomation.Shelly {

	public class ShellyType {
		public const string DOOR = "SHDW";
		public const string HT = "SHHT-1";
		public const string HT_PLUS = "PlusHT";
		public const string HT3 = "HTG3";

		public const string SW = "SHSW";
		public const string PM = "SHSW-PM";
		public const string PM_PLUS = "Plus1PM";
		public const string PM_MINI = "Mini1PM";
		public const string PM_MINI_G3 = "Mini1PMG3";
		public const string PLG = "SHPLG-S";
		public const string EM = "SHEM";
		public const string DIMMER = "SHDM-1";
		public const string DIMMER2 = "SHDM-2";
		public const string RGBW = "SHRGBW";
		public const string RGBW2 = "SHRGBW2";

		private static List<string> bat = [DOOR, HT, HT_PLUS, HT3];
		private static List<string> relay = [SW, PM, PM_PLUS, PM_MINI, PM_MINI_G3, PLG, EM];
		private static List<string> light = [DIMMER, DIMMER2, RGBW, RGBW2];
		private static List<string> gen1 = [SW, PM, PLG, EM, DIMMER, DIMMER2, RGBW, RGBW2];
		private static List<string> gen2 = [PM_PLUS, PM_MINI, PM_MINI_G3];

		public static bool IsBat(string st) {
			if(ShellyType.bat.Contains(st))
				return true;
			return false;
		}
		public static bool IsRelay(string st) {
			if(ShellyType.relay.Contains(st))
				return true;
			return false;
		}
		public static bool IsLight(string st) {
			if(ShellyType.light.Contains(st))
				return true;
			return false;
		}
		public static bool IsGen1(string st) {
			if(ShellyType.gen1.Contains(st))
				return true;
			return false;
		}
		public static bool IsGen2(string st) {
			if(ShellyType.gen2.Contains(st))
				return true;
			return false;
		}
	}
}
