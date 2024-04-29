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
//# Revision     : $Rev:: 76                                                      $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: Helferlein.cs 76 2024-01-24 07:36:57Z                    $ #
//#                                                                                 #
//###################################################################################
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text.RegularExpressions;
/**
* @defgroup FreakaZoneintern FreakaZoneintern
* @{
*/
namespace WebAutomation.Helper {
	/// <summary>
	/// 
	/// </summary>
	public class Arrays {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="arr"></param>
		/// <returns></returns>
		public static int[] StringArrayToIntArray(string[] arr) {
			return Arrays.StringArrayToIntArray(arr, 0);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="arr"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public static int[] StringArrayToIntArray(string[] arr, int defaultValue) {
			int[] returns = new int[]{ arr.Length };
			for(int i = 0; i < arr.Length; i++) {
				if(!Int32.TryParse(arr[i], out returns[i])) {
					returns[i] = defaultValue;
				}
			}
			return returns;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="needed"></param>
		/// <param name="arr"></param>
		/// <returns></returns>
		public static bool inArray(string needed, string[] arr) {
			for (int i = 0; i < arr.Length; i++) if (arr[i] == needed) return true;
			return false;
		}
	}
	public class wpHelp {
		public static double Epsilon;
		public static double getEpsilon() {
			double tau = 1.0;
			double walt = 1.0;
			double wneu = 0.0;
			while(wneu != walt) {
				tau *= 0.5;
				wneu = walt + tau;
			}
			return 2.0 * tau;
		}
		public static bool isAdmin() {
			WindowsIdentity identity = WindowsIdentity.GetCurrent();
			WindowsPrincipal principal = new WindowsPrincipal(identity);
			return principal.IsInRole(WindowsBuiltInRole.Administrator);
		}
		public static bool IsValidJson(string strInput) {
			if(string.IsNullOrWhiteSpace(strInput)) { return false; }
			strInput = strInput.Trim();
			if((strInput.StartsWith("{") && strInput.EndsWith("}")) ||
				(strInput.StartsWith("[") && strInput.EndsWith("]"))) {
				try {
					var obj = JToken.Parse(strInput);
					return true;
				} catch(JsonReaderException jex) {
					wpDebug.WriteError(jex);
					return false;
				} catch(Exception ex) {
					wpDebug.WriteError(ex);
					return false;
				}
			} else {
				return false;
			}
		}
	}

	public class CalendarHelp {
		public static int[] getIntArray(string s) {
			List<int> r = new List<int>();
			int forList;
			foreach (Match m in Regex.Matches(s, "(\\d+)")) {
				if (m.Groups.Count > 1) {
					if (Int32.TryParse(m.Groups[1].Value, out forList)) r.Add(forList);
				}
			}
			return r.ToArray();
		}

		public static DateTime getNextDay(DateTime dt, int dow) {
			int daysToAdd = (dow - (int)dt.DayOfWeek + 7) % 7;
			if (daysToAdd == 0) daysToAdd = 7;
			return dt.AddDays(daysToAdd);
		}

		public static int[] getWeekDayArray(string s) {
			List<int> r = new List<int>();
			foreach (Match m in Regex.Matches(s, "(\\w+)")) {
				if (m.Groups.Count > 1) {
					c_weekday c_w = new c_weekday(m.Groups[1].Value);
					r.Add(c_w.get);
				}
			}
			return r.ToArray();
		}

		public static DateTime parse(string _dt) {
			bool found = false;
			//if (_dt.Contains(":")) _dt = _dt.Split(':')[1];
			DateTime returns = new DateTime();
			if (Regex.IsMatch(_dt, "^([0-9]{4})-([0-1][0-9])-([0-3][0-9])T([0-2][0-9]):([0-5][0-9]):([0-5][0-9])([Z]?)$")) {
				Match matches = Regex.Match(_dt, "^([0-9]{4})-([0-1][0-9])-([0-3][0-9])T([0-2][0-9]):([0-5][0-9]):([0-5][0-9])([Z]?)$");
				DateTimeKind zone = matches.Groups[7].Value == "Z" ? DateTimeKind.Utc : DateTimeKind.Local;
				returns = new DateTime(
					Int32.Parse(matches.Groups[1].Value),
					Int32.Parse(matches.Groups[2].Value),
					Int32.Parse(matches.Groups[3].Value),
					Int32.Parse(matches.Groups[4].Value),
					Int32.Parse(matches.Groups[5].Value),
					Int32.Parse(matches.Groups[6].Value),
					zone
				);
				found = true;
			}
			if (Regex.IsMatch(_dt, "^([0-9]{4})([0-1][0-9])([0-3][0-9])T([0-2][0-9])([0-5][0-9])([0-5][0-9])([Z]?)$")) {
				Match matches = Regex.Match(_dt, "^([0-9]{4})([0-1][0-9])([0-3][0-9])T([0-2][0-9])([0-5][0-9])([0-5][0-9])([Z]?)$");
				DateTimeKind zone = matches.Groups[7].Value == "Z" ? DateTimeKind.Utc : DateTimeKind.Local;
				returns = new DateTime(
					Int32.Parse(matches.Groups[1].Value),
					Int32.Parse(matches.Groups[2].Value),
					Int32.Parse(matches.Groups[3].Value),
					Int32.Parse(matches.Groups[4].Value),
					Int32.Parse(matches.Groups[5].Value),
					Int32.Parse(matches.Groups[6].Value),
					zone
				);
				found = true;
			}
			if (Regex.IsMatch(_dt, "^([0-9]{4})-([0-1][0-9])-([0-3][0-9])$")) {
				Match matches = Regex.Match(_dt, "^([0-9]{4})-([0-1][0-9])-([0-3][0-9])$");
				returns = new DateTime(
					Int32.Parse(matches.Groups[1].Value),
					Int32.Parse(matches.Groups[2].Value),
					Int32.Parse(matches.Groups[3].Value)
				);
				found = true;
			}
			if (!found) {
				wpDebug.Write("Error parsing DateTime {0}", _dt);
			}
			return returns;
		}
	}

	public class c_weekday {
		public const int MO = 1;
		public const int TU = 2;
		public const int WE = 3;
		public const int TH = 4;
		public const int FR = 5;
		public const int SA = 6;
		public const int SU = 0;
		private int wd;
		public int get {
			get { return wd; }
		}
		public c_weekday(string _wd) {
			wd = fromString(_wd);
		}
		private int fromString(string i) {
			switch (i.ToLower()) {
				case "mo": return c_weekday.MO;
				case "tu": return c_weekday.TU;
				case "we": return c_weekday.WE;
				case "th": return c_weekday.TH;
				case "fr": return c_weekday.FR;
				case "sa": return c_weekday.SA;
				case "su": return c_weekday.SU;
				default: return 0;
			}
		}
		public override string ToString() {
			switch (wd) {
				case c_weekday.MO: return "MO";
				case c_weekday.TU: return "TU";
				case c_weekday.WE: return "WE";
				case c_weekday.TH: return "TH";
				case c_weekday.FR: return "FR";
				case c_weekday.SA: return "SA";
				case c_weekday.SU: return "SU";
				default: return "ERROR";
			}
		}
	}
	public class c_frequenz {
		public const int yearly = 1;
		public const int monthly = 2;
		public const int weekly = 3;
		public const int daily = 4;
		public const int hourly = 5;
		public const int minutly = 6;
		public const int secondly = 7;
		private int fq;
		public int get {
			get { return fq; }
		}
		public c_frequenz(string _fq) {
			fq = fromString(_fq);
		}
		private int fromString(string i) {
			switch (i.ToLower()) {
				case "yearly": return c_frequenz.yearly;
				case "monthly": return c_frequenz.monthly;
				case "weekly": return c_frequenz.weekly;
				case "daily": return c_frequenz.daily;
				case "hourly": return c_frequenz.hourly;
				case "minutly": return c_frequenz.minutly;
				case "secondly": return c_frequenz.secondly;
				default: return 0;
			}
		}
		public override string ToString() {
			switch (fq) {
				case c_frequenz.yearly: return "YEARLY";
				case c_frequenz.monthly: return "MONTHLY";
				case c_frequenz.weekly: return "WEEKLY";
				case c_frequenz.daily: return "DAILY";
				case c_frequenz.hourly: return "HOURLY";
				case c_frequenz.minutly: return "MINUTLY";
				case c_frequenz.secondly: return "SECONLY";
				default: return "ERROR";
			}
		}
	}
}
/** @} */
