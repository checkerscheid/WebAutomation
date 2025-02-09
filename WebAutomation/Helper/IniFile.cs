using FreakaZone.Libraries.wpEventLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace WebAutomation.Helper {
	class Ini {

		/// <summary></summary>
		private static Dictionary<string, Dictionary<string, string>> IniDic = new Dictionary<string, Dictionary<string, string>>();
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public static bool read() {
			IniDic.Clear();
			Regex Kommentar = new Regex(@"#.*$");
			Regex Group = new Regex(@"\[(.*)\]");
			Regex KeyValue = new Regex(@"(.*)=(.*)");
			string aktgroup = "";
			Match m;
			if(File.Exists(".\\settings.ini")) {
				string[] TheContent = File.ReadAllLines(".\\settings.ini");
				for(int i = 0; i < TheContent.Length; i++) {
					// Kommentar
					while(Kommentar.IsMatch(TheContent[i])) {
						TheContent[i] = Kommentar.Replace(TheContent[i], "");
					}
					// Gruppe
					m = Group.Match(TheContent[i]);
					if(m.Groups.Count > 1) {
						aktgroup = m.Groups[1].Value.Trim();
						if(!IniDic.ContainsKey(aktgroup)) {
							IniDic.Add(aktgroup, new Dictionary<string, string>());
						}
					}
					// Eigenschaft
					m = KeyValue.Match(TheContent[i]);
					if(m.Groups.Count > 2) {
						if(!IniDic[aktgroup].ContainsKey(m.Groups[1].Value)) {
							IniDic[aktgroup].Add(m.Groups[1].Value.Trim(), m.Groups[2].Value.Trim());
						}
					}
				}
				return true;
			} else {
				//MessageBox.Show("INI Datei wurde nicht gefunden");
				return false;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="group"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		public static int getInt(string group, string key) {
			if(Ini.IniDic.ContainsKey(group)) {
				if(IniDic[group].ContainsKey(key)) {
					int outint;
					if(Int32.TryParse(IniDic[group][key], out outint)) {
						return outint;
					}
				}
			}
			if(group == "Log") {
				//MessageBox.Show(String.Format("Fehlender Eintrag '{1}' in Gruppe '{0}'", group, key));
			} else {
				using(Logger INILog = new Logger(wpLog.ESource.WebAutomation)) {
					INILog.Write(MethodInfo.GetCurrentMethod(), EventLogEntryType.Error, String.Format("Fehlender Eintrag '{1}' in Gruppe '{0}'", group, key));
				}
			}
			return -1;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="group"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		public static string get(string group, string key) {
			if(IniDic.ContainsKey(group)) {
				if(IniDic[group].ContainsKey(key)) {
					return IniDic[group][key];
				}
			}
			if(group == "Log") {
				//MessageBox.Show(String.Format("Fehlender Eintrag '{1}' in Gruppe '{0}'", group, key));
			} else {
				using(Logger INILog = new Logger(wpLog.ESource.WebAutomation)) {
					INILog.Write(MethodInfo.GetCurrentMethod(), EventLogEntryType.Error, String.Format("Fehlender Eintrag '{1}' in Gruppe '{0}'", group, key));
				}
			}
			return "";
		}
	}
}
