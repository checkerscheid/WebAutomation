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
//# Revision     : $Rev:: 87                                                      $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: OPCClient.cs 87 2024-04-10 06:45:26Z                     $ #
//#                                                                                 #
//###################################################################################
using OPC.Common;
using OPC.Data;
using OPC.Data.Interface;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using WebAutomation.PlugIns;
/**
* @addtogroup WebAutomation
* @{
*/
namespace WebAutomation.Helper {
	/// <summary>
	/// 
	/// </summary>
	public class OPCClient {
		/// <summary></summary>
		private Logger eventLog;
		/// <summary></summary>
		private bool running;
		/// <summary></summary>
		private Dictionary<int, PGAOPCServer> TheServer;
		/// <summary>TheGroup[ServerID][GroupID] = OpcGroup</summary>
		private Dictionary<int, Dictionary<int, OpcGroup>> TheGroup;
		/// <summary>Hsrv[ServerID][GroupID] = List[Hsrv]</summary>
		private Dictionary<int, Dictionary<int, List<int>>> Hsrv;

		public event EventHandler<valueChangedEventArgs> valueChanged;
		public class valueChangedEventArgs: EventArgs {
			public int idOPCPoint { get; set; }
			public string name { get; set; }
			public string value { get; set; }
		}
		/// <summary></summary>
		private string OPCbrowsed;
		/// <summary></summary>
		private const int ISERROR = 0;
		/// <summary></summary>
		private const int ISGROUP = 1;
		/// <summary></summary>
		private const int ISITEM = 2;
		/// <summary></summary>
		//private System.Windows.Forms.Timer CheckOPCServerstateTimer;
		/// <summary>in Sekunden</summary>
		//private int trytorestart = 30;
		//private CheckOpcServerState OpcChecker;
		//private Thread ThreadOpcChecker;

		private Mutex lockopc = new Mutex(false, Program.myName + ":LockOpcClient");
		private int locktimeout = 5000;
		private int locksleep = 100;
		/// <summary>
		/// 
		/// </summary>
		public string OPCgebrowsed {
			get { return OPCbrowsed; }
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="globalServer"></param>
		public OPCClient() {
			running = false;
			init();
		}
		/// <summary>
		/// 
		/// </summary>
		private void init() {
			eventLog = new Logger(wpEventLog.OPC);
			eventLog.Write("OPC Client gestartet");
				
			TheServer = new Dictionary<int, PGAOPCServer>();
			TheGroup = new Dictionary<int, Dictionary<int, OpcGroup>>();
			Hsrv = new Dictionary<int, Dictionary<int, List<int>>>();

			int idserver;
			int idgroup;
			int duration;
			int idpoint;

			using (SQL SQL = new SQL("Add OPC Server")) {
				string[][] DBserver = SQL.wpQuery(@"SELECT
					[id_opcserver], [progid], [clsid], [name], [server], [active] FROM [opcserver]");
				for(int iserver = 0; iserver < DBserver.Length; iserver++) {
					try {
						if (Int32.TryParse(DBserver[iserver][0], out idserver)) {
							TheServerAdd(idserver, DBserver[iserver][1], DBserver[iserver][2], DBserver[iserver][3],
								DBserver[iserver][4], DBserver[iserver][5] == "True");
						}
					} catch(Exception ex) {
						eventLog.WriteError(ex, DBserver[iserver][3]);
					}
				}
			}
			using (SQL SQL = new SQL("Add OPC Groups")) {
				string tempLog, GroupLog = "";
				string tempErr, GroupErr = "";
				string[][] DBGroup = SQL.wpQuery(@"
					SELECT [s].[id_opcserver], [g].[id_opcgroup], [g].[name], [g].[duration], ([g].[active] & [s].[active]) AS [active]
					FROM [opcgroup] [g]
					INNER JOIN [opcserver] [s] ON [g].[id_opcserver] = [s].[id_opcserver]");
				for (int igroup = 0; igroup < DBGroup.Length; igroup++) {
					try {
						if (Int32.TryParse(DBGroup[igroup][0], out idserver) &&
							Int32.TryParse(DBGroup[igroup][1], out idgroup) &&
							Int32.TryParse(DBGroup[igroup][3], out duration)) {
								TheGroupAdd(idserver, idgroup, DBGroup[igroup][2],
									duration, DBGroup[igroup][4] == "True",
									out tempLog, out tempErr);
								GroupLog += tempLog;
								GroupErr += tempErr;
						}
					} catch(Exception ex) {
						eventLog.WriteError(ex, DBGroup[igroup][2]);
					}
				}
				if (GroupLog != "") {
					eventLog.Write("OPC Gruppen:" + GroupLog);
				}
				if (GroupErr.Length > 0) eventLog.Write(EventLogEntryType.Warning, GroupErr);
			}
			using (SQL SQL = new SQL("Add OPC Items")) {
				string[][] DBDatapoints = SQL.wpQuery(@"SELECT
					[s].[id_opcserver], [g].[id_opcgroup],
					[d].[id_opcdatapoint], [d].[opcname], [d].[name], [d].[forcetype], ([d].[active] & [g].[active] & [s].[active]) AS [active]
					FROM [opcdatapoint] [d]
					INNER JOIN [opcgroup] [g] ON [d].[id_opcgroup] = [g].[id_opcgroup]
					INNER JOIN [opcserver] [s] ON [g].[id_opcserver] = [s].[id_opcserver]");

				for (int idatapoint = 0; idatapoint < DBDatapoints.Length; idatapoint++) {
					try {
						if (Int32.TryParse(DBDatapoints[idatapoint][0], out idserver) &&
							Int32.TryParse(DBDatapoints[idatapoint][1], out idgroup) &&
							Int32.TryParse(DBDatapoints[idatapoint][2], out idpoint)) {
							if (TheServer[idserver].State == OpcServer.ServerState.connected) {
								TheItemAdd(idserver, idgroup, idpoint, DBDatapoints[idatapoint][3], DBDatapoints[idatapoint][4],
									DBDatapoints[idatapoint][6] == "True",
									PVTEnum.get(DBDatapoints[idatapoint][5]));
							}
						}
					} catch (Exception ex) {
						eventLog.WriteError(ex, DBDatapoints[idatapoint][3]);
					}
				}
			}
			Alarm.AddAlarms();
			//WriteLevel.AddWriteLevel();
			//Router.AddRouter();

			running = true;
			activate();

			eventLog.Write("OPC Client initialisiert");

			//OpcChecker = new CheckOpcServerState();
			//ThreadOpcChecker = new Thread(OpcChecker.doWork);
			//ThreadOpcChecker.Start(TheServer);

			//if (Program.MainProg.BigProject) trytorestart = 5 * 60;

			//CheckOPCServerstateTimer = new System.Windows.Forms.Timer();
			//CheckOPCServerstateTimer.Interval = trytorestart * 1000;
			//CheckOPCServerstateTimer.Tick += new EventHandler(CheckOPCServerstate_Tick);
			//CheckOPCServerstateTimer.Start();
		}
		public void connect(int aktServerid) {
			try {
				int idgroup;
				int duration;
				int idpoint;

				using (SQL SQL = new SQL("Add OPC Server after shutdown")) {
					string[][] DBserver = SQL.wpQuery(@$"
						SELECT TOP 1 [id_opcserver], [progid], [clsid], [name], [server], [active]
						FROM [opcserver] WHERE [id_opcserver] = {aktServerid}");
					TheServerAdd(aktServerid, DBserver[0][1], DBserver[0][2], DBserver[0][3],
						DBserver[0][4], DBserver[0][5] == "True");
				}
				using (SQL SQL = new SQL("Add OPC Groups")) {
					string tempLog, GroupLog = "";
					string tempErr, GroupErr = "";
					string[][] DBGroup = SQL.wpQuery(@$"
						SELECT [g].[id_opcgroup], [g].[name], [g].[duration], [g].[active]
						FROM [opcgroup] [g]
						INNER JOIN [opcserver] [s] ON [g].[id_opcserver] = [s].[id_opcserver]
						WHERE [s].[id_opcserver] = {aktServerid}");
					for (int igroup = 0; igroup < DBGroup.Length; igroup++) {
						if (Int32.TryParse(DBGroup[igroup][0], out idgroup) &&
							Int32.TryParse(DBGroup[igroup][2], out duration)) {
							TheGroupAdd(aktServerid, idgroup, DBGroup[igroup][1], duration,
								DBGroup[igroup][3] == "True", out tempLog, out tempErr);
							GroupLog += tempLog;
							GroupErr += tempErr;
						}
					}
					eventLog.Write(GroupLog);
					if (GroupErr.Length > 0) eventLog.Write(EventLogEntryType.Warning, GroupErr);
				}
				using (SQL SQL = new SQL("Add OPC Items")) {
					string[][] DBDatapoints = SQL.wpQuery(@$"
						SELECT [g].[id_opcgroup], [d].[id_opcdatapoint], [d].[opcname], [d].[name], [d].[active]
						FROM [opcdatapoint] [d]
						INNER JOIN [opcgroup] [g] ON [d].[id_opcgroup] = [g].[id_opcgroup]
						INNER JOIN [opcserver] [s] ON [g].[id_opcserver] = [s].[id_opcserver]
						WHERE [s].[id_opcserver] = {aktServerid}");
					for (int idatapoint = 0; idatapoint < DBDatapoints.Length; idatapoint++) {
						if (Int32.TryParse(DBDatapoints[idatapoint][0], out idgroup) &&
							Int32.TryParse(DBDatapoints[idatapoint][1], out idpoint)) {
							TheItemAdd(aktServerid, idgroup, idpoint, DBDatapoints[idatapoint][2], DBDatapoints[idatapoint][3],
								DBDatapoints[idatapoint][4] == "True");
						}
					}
				}
				Alarm.AddAlarms(aktServerid);
				//WriteLevel.AddWriteLevel(aktServerid);
				//Router.AddRouter();
				activate(aktServerid);
			} catch (Exception ex) {
				eventLog.WriteError(ex);
			}
		}
		public void disconnect(int aktServerid) {
			try {
				string TheLog = "";
				foreach (KeyValuePair<int, OpcGroup> opcgroup in TheGroup[aktServerid]) {
					try {
						removeEvents(opcgroup.Value, aktServerid, opcgroup.Key);
						//opcgroup.Value.RemoveItems(Hsrv[aktServerid][opcgroup.Key].ToArray(), out aErr);
						//opcgroup.Value.Remove(true);
						TheLog += String.Format("\r\n\tGroup '{0}' Removed from '{1}'",
							opcgroup.Value.Name, TheServer[aktServerid].Name);
					} catch (Exception ex) {
						eventLog.Write(EventLogEntryType.Warning,
							String.Format("OPC Server ERROR: \r\n{0}", ex.Message));
					}
				}
				if (TheLog != "") eventLog.Write($"disconnect OPC Server {TheServer[aktServerid].Name} {TheLog}");
				try {
					TheServer[aktServerid].ShutdownRequested -=
						new ShutdownRequestEventHandler(TheServer_ShutdownRequested);
					// TheServer[aktServerid].Disconnect();

					eventLog.Write(String.Format("OPC Server '{0}' disconnect", TheServer[aktServerid].Name));
				} catch (Exception ex) {
					eventLog.WriteError(ex);
				} finally {
					TheServer[aktServerid].Dispose();
					TheServer[aktServerid] = null;
				}
				TheServer.Remove(aktServerid);
				TheGroup.Remove(aktServerid);
				Hsrv.Remove(aktServerid);
				using (SQL SQL = new SQL("Remove OPC Server")) {
					string[][] DBDatapoints = SQL.wpQuery(@$"
						SELECT [d].[id_opcdatapoint] FROM [opcdatapoint] [d]
						INNER JOIN [opcgroup] [g] ON [d].[id_opcgroup] = [g].[id_opcgroup]
						INNER JOIN [opcserver] [s] ON [g].[id_opcserver] = [s].[id_opcserver]
						WHERE [s].[id_opcserver] = {aktServerid}");
					List<int> toDelete = new List<int>();
					int intout;
					for (int i = 0; i < DBDatapoints.Length; i++) {
						if (Int32.TryParse(DBDatapoints[i][0], out intout)) toDelete.Add(intout);
					}
					Server.Dictionaries.deleteItems(toDelete.ToArray());
				}
			} catch (Exception ex) {
				eventLog.WriteError(ex);
			}
		}

#region addandconnect

		/// <summary>
		/// Fuegt einen OPC Server dem lokalen Cache hinzu.
		/// </summary>
		/// <param name="ServerID"></param>
		private void TheServerAdd(int ServerID, string progid, string clsid, string ServerName,
			string externServer, bool active) {
			// local add
			if (!TheServer.ContainsKey(ServerID))
				TheServer.Add(ServerID, new PGAOPCServer(ServerID, ServerName, progid, clsid));
			if (!TheGroup.ContainsKey(ServerID))
				TheGroup.Add(ServerID, new Dictionary<int, OpcGroup>());
			if (!Hsrv.ContainsKey(ServerID))
				Hsrv.Add(ServerID, new Dictionary<int, List<int>>());

			//TheServer[ServerID].ProgID = progid;
			//TheServer[ServerID].ClsID = clsid;
			//TheServer[ServerID].PGAid = ServerID;
			//TheServer[ServerID].PGAname = ServerName;
			TheServer[ServerID].Active = active;
			// opc connect
			if (TheServer[ServerID].Active) {
				TheServerConnect(ServerID, progid, clsid, externServer);
			}
		}

		/// <summary>
		/// Etabliert eine Verbindung zu einem OPC Server
		/// </summary>
		/// <param name="ServerID"></param>
		/// <param name="progid"></param>
		/// <param name="externServer"></param>
		private void TheServerConnect(int ServerID, string progid, string clsid, string externServer) {
			SERVERSTATUS RunningTest = null;
			TheServer[ServerID].GetStatus(out RunningTest);
			if (RunningTest == null || RunningTest.eServerState != OPCSERVERSTATE.OPC_STATUS_RUNNING) {
				try {
					if (externServer != "") {
						TheServer[ServerID].Connect(progid, externServer);
					} else {
						TheServer[ServerID].Connect(progid);
					}
					TheServer[ServerID].ShutdownRequested +=
						new ShutdownRequestEventHandler(TheServer_ShutdownRequested);
					TheServer[ServerID].GetStatus(out RunningTest);
					TheServer[ServerID].SetClientName(Application.ProductName);
					eventLog.Write(String.Format($"OPC Server connected - {progid}\r\n" +
						$"\tVendor: {RunningTest.szVendorInfo}\r\n" +
						$"\tMajor Version: {RunningTest.wMajorVersion}\r\n" +
						$"\tMinor Version: {RunningTest.wMinorVersion}\r\n" +
						$"\tBuild Number: {RunningTest.wBuildNumber}"));
				} catch (Exception ex) {
					TheServer[ServerID].State = OpcServer.ServerState.failed;
					eventLog.WriteError(ex, TheServer[ServerID].Name);
				}
			} else {
				eventLog.Write(String.Format("OPC Server '{0}' ist bereits aktiv", progid));
			}
		}
		/// <summary>
		/// Fuegt eine OPC Gruppe dem lokalen Cache hinzu.
		/// </summary>
		/// <param name="ServerID"></param>
		/// <param name="GroupID"></param>
		/// <param name="GroupName"></param>
		/// <param name="Duration"></param>
		private void TheGroupAdd(int ServerID, int GroupID, string GroupName, int Duration, bool active,
			out string GroupLog, out string GroupErr) {
			// local add
			GroupLog = "";
			GroupErr = "";
			if (!TheGroup[ServerID].ContainsKey(GroupID)) {
				TheGroup[ServerID].Add(GroupID, null);
			}
			if (!Hsrv[ServerID].ContainsKey(GroupID)) {
				Hsrv[ServerID].Add(GroupID, new List<int>());
			}
			// opc connect
			TheGroupConnect(ServerID, GroupID, GroupName, Duration, active, out GroupLog, out GroupErr);
		}
		/// <summary>
		/// Etabliert eine Verbindung zu einer OPC Gruppe
		/// </summary>
		/// <param name="ServerID"></param>
		/// <param name="GroupID"></param>
		/// <param name="GroupName"></param>
		/// <param name="Duration"></param>
		/// <param name="active"></param>
		/// <param name="GroupLog"></param>
		/// <param name="GroupErr"></param>
		private void TheGroupConnect(int ServerID, int GroupID, string GroupName, int Duration, bool active,
			out string GroupLog, out string GroupErr) {
			GroupLog = "";
			GroupErr = "";
			if (TheServer[ServerID].Active) {
				SERVERSTATUS RunningTest = null;
				TheServer[ServerID].GetStatus(out RunningTest);
				if (RunningTest != null && RunningTest.eServerState == OPCSERVERSTATE.OPC_STATUS_RUNNING) {
					try {
						TheGroup[ServerID][GroupID] = TheServer[ServerID].AddGroup(GroupName, false, Duration);
						TheGroup[ServerID][GroupID].PGAactive = active;
						if (Program.MainProg.wpForceRead) {
#if DEBUGFORCEREAD
							TheGroup[ServerID][GroupID].forceRead = new System.Timers.Timer(Duration * 5);
#else
							TheGroup[ServerID][GroupID].forceRead = new System.Timers.Timer(Duration * 120);
#endif
						}
						GroupLog += String.Format("\r\n\tOPC Group '{0}' created in '{1}'",
							TheGroup[ServerID][GroupID].Name,
							TheServer[ServerID].Name);
					} catch(Exception ex) {
						eventLog.WriteError(ex);
					}
				} else {
					GroupErr += String.Format("\r\n\tOPC Server '{0}' nicht kontaktiert.\r\n" +
						"\tOPC Gruppe nicht erzeugt: '{1}'",
						TheServer[ServerID].Name,
						GroupName);
				}
			}
		}
		/// <summary>
		/// Fuegt einen OPC Datenpunkt mit Datentyp des Servers dem lokalen Cache hinzu.
		/// </summary>
		/// <param name="ServerID"></param>
		/// <param name="GroupID"></param>
		/// <param name="PointID"></param>
		/// <param name="OPCPath"></param>
		private void TheItemAdd(int ServerID, int GroupID, int PointID, string OPCPath, string PointName, bool active) {
			TheItemAdd(ServerID, GroupID, PointID, OPCPath, PointName, active, VarEnum.VT_EMPTY);
		}
		/// <summary>
		/// Fuegt mehrere OPC Datenpunkte mit Datentyp des Servers dem lokalen Cache hinzu.
		/// </summary>
		/// <param name="ServerID"></param>
		/// <param name="GroupID"></param>
		/// <param name="PointID"></param>
		/// <param name="OPCPath"></param>
		/// <param name="active"></param>
		private void TheItemAdd(int ServerID, int GroupID, int[] PointID, string[] OPCPath, string[] PointName, bool[] active) {
			if (PointID.Length == OPCPath.Length && PointID.Length == active.Length) {
				VarEnum[] OPCType = new VarEnum[PointID.Length];
				for (int i = 0; i <= OPCType.Length; i++) OPCType[i] = VarEnum.VT_EMPTY;
				TheItemAdd(ServerID, GroupID, PointID, OPCPath, PointName, active, OPCType);
			}
		}
		/// <summary>
		/// Fuegt einen OPC Datenpunkt mit eigenen Datentyp dem lokalen Cache hinzu.
		/// </summary>
		/// <param name="ServerID"></param>
		/// <param name="GroupID"></param>
		/// <param name="PointID"></param>
		/// <param name="OPCPath"></param>
		private void TheItemAdd(int ServerID, int GroupID, int PointID, string OPCPath, string PointName,
			bool active, VarEnum OPCType) {
			if (!Server.Dictionaries.checkItem(PointID)) {
				// local add
				Server.Dictionaries.addItem(PointID, new OPCItem(PointID, OPCPath, PointName, GroupID, ServerID));
				// opc add
				if (active) TheItemConnect(ServerID, GroupID, PointID, OPCPath, OPCType);
			}
		}
		/// <summary>
		/// Fuegt mehrere OPC Datenpunkte mit eigenen Datentypen dem lokalen Cache hinzu.
		/// </summary>
		/// <param name="ServerID"></param>
		/// <param name="GroupID"></param>
		/// <param name="PointID"></param>
		/// <param name="OPCPath"></param>
		/// <param name="active"></param>
		/// <param name="OPCType"></param>
		private void TheItemAdd(int ServerID, int GroupID, int[] PointID, string[] OPCPath, string[] PointName,
			bool[] active, VarEnum[] OPCType) {
			List<int> PointIDs = new List<int>();
			List<string> OPCPaths = new List<string>();
			List<VarEnum> OPCTypes = new List<VarEnum>();
			if (PointID.Length == OPCPath.Length && PointID.Length  == OPCType.Length) {
				for (int i = 0; i <= PointID.Length; i++) {
					if (Server.Dictionaries.getItem(PointID[i]) == null) {
						Server.Dictionaries.addItem(PointID[i],
							new OPCItem(PointID[i], OPCPath[i], PointName[i], GroupID, ServerID));
						if (active[i]) {
							PointIDs.Add(PointID[i]);
							OPCPaths.Add(OPCPath[i]);
							OPCTypes.Add(OPCType[i]);
						}
					}
				}
			}
			TheItemConnect(ServerID, GroupID, PointIDs.ToArray(), OPCPaths.ToArray(), OPCTypes.ToArray());
		}
		/// <summary>
		/// Etabliert eine Verbindung zu einem OPC Datenpunkt
		/// </summary>
		/// <param name="ServerID"></param>
		/// <param name="GroupID"></param>
		/// <param name="PointID"></param>
		/// <param name="OPCPath"></param>
		/// <param name="TheType"></param>
		private void TheItemConnect(int ServerID, int GroupID, int PointID, string OPCPath, VarEnum OPCType) {
			OPCItemDef[] OPCItems = new OPCItemDef[1];
			OPCItemResult[] TestItem = new OPCItemResult[1];
			int[] arrErr;
			SERVERSTATUS ss;
			TheServer[ServerID].GetStatus(out ss);
			if(ss != null && ss.eServerState == OPCSERVERSTATE.OPC_STATUS_RUNNING) {
				OPCItems[0] = new OPCItemDef(OPCPath, false, PointID, OPCType);
				TheGroup[ServerID][GroupID].ValidateItems(OPCItems, true, out TestItem);
				if (TestItem[0].Error < 0) {
					eventLog.Write(EventLogEntryType.Warning,
						String.Format("Gruppe '{0}' - Item nicht aktiv: '{1}'\r\n\t{2}",
						TheGroup[ServerID][GroupID].Name,
						Server.Dictionaries.getItem(PointID).OpcItemName,
						HRESULTS.getError(TestItem[0].Error)));
					if (TestItem[0].HandleServer != 0) {
						TheGroup[ServerID][GroupID].RemoveItems(new int[] { TestItem[0].HandleServer }, out arrErr);
					}
				} else {
					OPCItems[0].Active = true;
					TheGroup[ServerID][GroupID].AddItems(OPCItems, out TestItem);
					if (TestItem[0].Error < 0) {
						eventLog.Write("Item konnte nicht aktiviert werden: '{0}'", OPCPath);
					}
					// local store information from opc server
					Server.Dictionaries.getItem(PointID).Hsrv = TestItem[0].HandleServer;
					Server.Dictionaries.getItem(PointID).DBType = TestItem[0].CanonicalDataType;
					Hsrv[ServerID][GroupID].Add(TestItem[0].HandleServer);
					TheGroup[ServerID][GroupID].PGAactive = true;
				}
			}
		}
		/// <summary>
		/// Etabliert eine Verbindung zu mehreren OPC Datenpunkten in einer OPC Gruppe
		/// </summary>
		/// <param name="ServerID"></param>
		/// <param name="GroupID"></param>
		/// <param name="PointID"></param>
		/// <param name="OPCPath"></param>
		/// <param name="OPCType"></param>
		private void TheItemConnect(int ServerID, int GroupID, int[] PointID, string[] OPCPath, VarEnum[] OPCType) {
			if (PointID.Length == OPCPath.Length && PointID.Length == OPCType.Length) {
				SERVERSTATUS ss;
				TheServer[ServerID].GetStatus(out ss);
				if (ss.eServerState == OPCSERVERSTATE.OPC_STATUS_RUNNING) {
					OPCItemDef[] OPCItems = new OPCItemDef[PointID.Length];
					OPCItemResult[] TestItem = new OPCItemResult[PointID.Length];
					for (int i = 0; i < PointID.Length; i++) {
						OPCItems[i] = new OPCItemDef(OPCPath[i], true, PointID[i], OPCType[i]);
					}
					TheGroup[ServerID][GroupID].AddItems(OPCItems, out TestItem);
				
					for (int i = 0; i < PointID.Length; i++) {
						if (TestItem[i].Error < 0) {
							eventLog.Write(EventLogEntryType.Warning,
								String.Format("Gruppe '{0}' - Item nicht aktiv: '{1}'\r\n\t{2}",
								TheGroup[ServerID][GroupID].Name,
								Server.Dictionaries.getItem(PointID[i]).OpcItemName,
								HRESULTS.getError(TestItem[i].Error)));
						} else {
							// local store information from opc server
							Server.Dictionaries.getItem(PointID[i]).Hsrv = TestItem[i].HandleServer;
							Server.Dictionaries.getItem(PointID[i]).DBType = TestItem[i].CanonicalDataType;
							Hsrv[ServerID][GroupID].Add(TestItem[i].HandleServer);
						}
					}
				}
			}
		}

#endregion

		private void forceRead_Elapsed(object sender, System.Timers.ElapsedEventArgs e,
			int _serverid, int _groupid) {
			forceRead(_serverid, _groupid);
		}
		public void forceRead(int _serverid, int _groupid) {
			if(!running) return;
			DateTime Now = DateTime.Now;
			if(TheGroup.ContainsKey(_serverid) &&
				TheGroup[_serverid].ContainsKey(_groupid) && !(TheGroup[_serverid][_groupid] == null)) {
				OpcGroup _Group = TheGroup[_serverid][_groupid];
				DateTime Calc = _Group.LastChange.AddMilliseconds(_Group.RequestedUpdateRate * 30 + 1000);
				if(Calc <= Now) {
					int cI;
					int[] arrErr;
					int tid = TransferId.getNew(TransferId.TransferForceRead);
					try {
						if(Hsrv[_serverid][_groupid].Count > 0) {
							if(lockopc.WaitOne(locktimeout)) {
								try {
									if(TheGroup[_serverid][_groupid].Read(
										Hsrv[_serverid][_groupid].ToArray(),
										tid,
										out cI, out arrErr)) {
										wpDebug.Write("Force Read (TAID-{1}): Group {0} ", _Group.Name, tid);
									} else {
										wpDebug.Write("Force Read (TAID-{1}): Group {0} faild: {2}",
											_Group.Name, tid, String.Join(",\r\n ", arrErr));
									}
									if(Program.MainProg.wpPSOPC) {
										Thread.Sleep(locksleep);
									}
								} finally {
									lockopc.ReleaseMutex();
								}
							} else {
								eventLog.Write(EventLogEntryType.Error, "Deadlockopfer ☺: Mutextimeout für Force Read abgelaufen (TAID-{0})", tid);
							}
						} else {
							wpDebug.Write("Force Read '{0}' - Keine Items vorhanden",
								TheGroup[_serverid][_groupid].Name);
						}
					} catch(Exception ex) {
						eventLog.WriteError(ex, _Group.Name);
					}
				}
			}
		}
		private void addEvents(OpcGroup og, int serverid, int groupid) {
			if(!og.hasReadCompleted()) og.ReadCompleted += new ReadCompleteEventHandler(TheGroup_ReadCompleted);
			if(!og.hasDataChanged()) og.DataChanged += new DataChangeEventHandler(TheGroup_DataChanged);
			if(!og.hasWriteCompleted()) og.WriteCompleted += new WriteCompleteEventHandler(TheGroup_WriteCompleted);
			if(!og.hasCancelCompleted()) og.CancelCompleted += new CancelCompleteEventHandler(TheGroup_CancelCompleted);
			if (Program.MainProg.wpForceRead) {
				og.forceRead.Elapsed +=
					new System.Timers.ElapsedEventHandler((sender, e) =>
						forceRead_Elapsed(sender, e, serverid, groupid));
			}
			wpDebug.Write("Add Events to {0} ({1})", TheGroup[serverid][groupid].Name, TheServer[serverid].Name);
		}
		private void removeEvents(OpcGroup og, int serverid, int groupid) {
			if(TheServer[serverid].Active && TheGroup[serverid][groupid].Active) {
				og.ReadCompleted -= new ReadCompleteEventHandler(TheGroup_ReadCompleted);
				og.DataChanged -= new DataChangeEventHandler(TheGroup_DataChanged);
				og.WriteCompleted -= new WriteCompleteEventHandler(TheGroup_WriteCompleted);
				og.CancelCompleted -= new CancelCompleteEventHandler(TheGroup_CancelCompleted);
				if(Program.MainProg.wpForceRead) {
					og.forceRead.Elapsed -=
						new System.Timers.ElapsedEventHandler((sender, e) =>
							forceRead_Elapsed(sender, e, serverid, groupid));
				}
				wpDebug.Write("Remove Events from {0} ({1})", TheGroup[serverid][groupid].Name, TheServer[serverid].Name);
			} else {
				wpDebug.Write("no Events removed from {0} ({1}) Disabled", TheGroup[serverid][groupid].Name, TheServer[serverid].Name);
			}
		}

#region dynamicOPC

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ServerID"></param>
		/// <returns></returns>
		public string newOPCServer(int ServerID) {
			using (SQL SQL = new SQL("Add OPC Server")) {
				string[][] DBserver = SQL.wpQuery(@"SELECT [progid], [clsid], [name], [server], [active]
					FROM [opcserver] WHERE [id_opcserver] = {0}", ServerID);
				TheServerAdd(ServerID, DBserver[0][0], DBserver[0][1], DBserver[0][2],
					DBserver[0][3], DBserver[0][4] == "True");
			}
			return "S_OK";
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="ServerID"></param>
		/// <returns></returns>
		public string removeOPCServer(int ServerID) {
			string TheLog = "";
			try {
				foreach (KeyValuePair<int, OpcGroup> opcgroup in TheGroup[ServerID]) {
					try {
						int[] aErr;
						removeEvents(opcgroup.Value, ServerID, opcgroup.Key);
						opcgroup.Value.RemoveItems(Hsrv[ServerID][opcgroup.Key].ToArray(), out aErr);
						opcgroup.Value.Remove(true);
						TheLog += String.Format("\r\n\tGroup '{0}' Removed from '{1}'",
							opcgroup.Value.Name, TheServer[ServerID].Name);
					} catch (Exception ex) {
						eventLog.Write(EventLogEntryType.Warning,
							String.Format("OPC Server ERROR: \n{0}", ex.Message));
					}
				}
				if(TheLog != "")
					eventLog.Write($"remove OPC Server {TheServer[ServerID].Name} {TheLog}");
				try {
					TheServer[ServerID].Disconnect();
					eventLog.Write(String.Format("OPC Server '{0}' disconnect", TheServer[ServerID].Name));
				} catch(Exception ex) {
					eventLog.WriteError(ex);
				}
				TheServer.Remove(ServerID);
				TheGroup.Remove(ServerID);
				Hsrv.Remove(ServerID);
				using(SQL SQL = new SQL("Remove OPC Server")) {
					string[][] DBDatapoints = SQL.wpQuery(@"
						SELECT [d].[id_opcdatapoint] FROM [opcdatapoint] [d]
						INNER JOIN [opcgroup] [g] ON [d].[id_opcgroup] = [g].[id_opcgroup]
						INNER JOIN [opcserver] [s] ON [g].[id_opcserver] = [s].[id_opcserver]
						WHERE [s].[id_opcserver] = " + ServerID);
					List<int> toDelete = new List<int>();
					int intout;
					for(int i = 0; i < DBDatapoints.Length; i++) {
						if (Int32.TryParse(DBDatapoints[i][0], out intout)) toDelete.Add(intout);
					}
					Server.Dictionaries.deleteItems(toDelete.ToArray());
					SQL.wpNonResponse("DELETE FROM [opcserver] WHERE [id_opcserver] = {0}", ServerID);
				}
			} catch (Exception ex) {
				eventLog.WriteError(ex);
			}
			return "S_OK";
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="GroupID"></param>
		/// <returns></returns>
		public string newOPCGroup(int GroupID) {
			int idserver;
			int duration;
			using (SQL SQL = new SQL("Add OPC Groups")) {
				string tempLog, GroupLog = "";
				string tempErr, GroupErr = "";
				string[][] DBGroup = SQL.wpQuery(@"
					SELECT [s].[id_opcserver], [g].[name], [g].[duration], [g].[active]
					FROM [opcgroup] [g]
					INNER JOIN [opcserver] [s] ON [g].[id_opcserver] = [s].[id_opcserver]
					WHERE [s].[active] = 1 AND [g].[active] = 1 AND [g].[id_opcgroup] = " + GroupID);
				if (Int32.TryParse(DBGroup[0][0], out idserver) &&
					Int32.TryParse(DBGroup[0][2], out duration)) {
						TheGroupAdd(idserver, GroupID, DBGroup[0][1], duration, DBGroup[0][3] == "True",
							out tempLog, out tempErr);
						GroupLog += tempLog;
						GroupErr += tempErr;
				}
				eventLog.Write(GroupLog);
				if (GroupErr.Length > 0) eventLog.Write(EventLogEntryType.Warning, GroupErr);
				activate(idserver);
			}
			return "S_OK";
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="GroupID"></param>
		/// <returns></returns>
		public string removeOPCGroup(int GroupID) {
			try {
				using(SQL SQL = new SQL("Remove OPC Group")) {
					string[][] OPCServerID = SQL.wpQuery(@"SELECT [id_opcserver]
						FROM [opcgroup] WHERE [id_opcgroup] = {0}", GroupID);
					int ServerID;
					if (Int32.TryParse(OPCServerID[0][0], out ServerID)) {
						try {
							int[] aErr;
							removeEvents(TheGroup[ServerID][GroupID], ServerID, GroupID);
							TheGroup[ServerID][GroupID].RemoveItems(Hsrv[ServerID][GroupID].ToArray(), out aErr);
							TheGroup[ServerID][GroupID].Remove(true);
							eventLog.Write(String.Format("Group '{0}' Removed from '{1}'\r\n",
								TheGroup[ServerID][GroupID].Name, TheServer[ServerID].Name));
						} catch(Exception ex) {
							eventLog.WriteError(ex);
						}
				
						TheGroup[ServerID].Remove(GroupID);
						Hsrv[ServerID].Remove(GroupID);
						string[][] DBDatapoints = SQL.wpQuery(@"
							SELECT [d].[id_opcdatapoint] FROM [opcdatapoint] [d]
							INNER JOIN [opcgroup] [g] ON [d].[id_opcgroup] = [g].[id_opcgroup]
							WHERE [g].[id_opcgroup] = " + GroupID);
						List<int> toDelete = new List<int>();
						int intout;
						for (int i = 0; i < DBDatapoints.Length; i++) {
							if (Int32.TryParse(DBDatapoints[i][0], out intout)) toDelete.Add(intout);
						}
						Server.Dictionaries.deleteItems(toDelete.ToArray());
						SQL.wpNonResponse("DELETE FROM [opcgroup] WHERE [id_opcgroup] = {0}", GroupID);
					}
				}
			} catch (Exception ex) {
				eventLog.WriteError(ex);
			}
			/// TODO: Server entfernen, wenn keine OPC Gruppen mehr vorhanden sind
			return "S_OK";
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="GroupID"></param>
		/// <returns></returns>
		public string newOPCItems(int GroupID) {
			int idserver;
			int idpoint;
			using (SQL SQL = new SQL("Add OPC Items")) {
				string[][] DBDatapoints = SQL.wpQuery(@"
					SELECT [s].[id_opcserver], [d].[id_opcdatapoint], [d].[opcname], [d].[name]
					FROM [opcdatapoint] [d]
					INNER JOIN [opcgroup] [g] ON [d].[id_opcgroup] = [g].[id_opcgroup]
					INNER JOIN [opcserver] [s] ON [g].[id_opcserver] = [s].[id_opcserver]
					WHERE [s].[active] = 1 AND [g].[active] = 1 AND [d].[active] = 1 AND [g].[id_opcgroup] = {0}",
					GroupID);

				for (int idatapoint = 0; idatapoint < DBDatapoints.Length; idatapoint++) {
					if (Int32.TryParse(DBDatapoints[idatapoint][0], out idserver) &&
						Int32.TryParse(DBDatapoints[idatapoint][1], out idpoint)) {
						TheItemAdd(idserver, GroupID, idpoint, DBDatapoints[idatapoint][2], DBDatapoints[idatapoint][3], true);
						if(idatapoint == DBDatapoints.Length - 1) activate(idserver, GroupID);
					}
				}
			}
			return "S_OK";
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="PointID"></param>
		/// <returns></returns>
		public string removeOPCItems(int[] PointID) {
			string where = "DELETE FROM [opcdatapoint] WHERE ";
			for(int i = 0; i < PointID.Length; i++) {
				where += String.Format("[id_opcdatapoint] = {0} OR ", PointID[i]);
			}
			using(SQL SQL = new SQL("Delete OPC Items")) {
				SQL.wpNonResponse(where.Substring(0, where.Length - 4));
			}
			Server.Dictionaries.deleteItems(PointID);
			/// TODO: Gruppe entfernen, wenn keine Datenpunkte mehr vorhanden sind
			return "S_OK";
		}
		/// <summary>
		/// 
		/// </summary>
		private void activate() {
			foreach(KeyValuePair<int, PGAOPCServer> pServer in TheServer) {
				activate(pServer.Key);
			}
		}
		/// <summary>
		/// 
		/// </summary>
		private void activate(int ServerID) {
			foreach (KeyValuePair<int, OpcGroup> pGroup in TheGroup[ServerID]) {
				activate(ServerID, pGroup.Key);
			}
		}
		/// <summary>
		/// 
		/// </summary>
		private void activate(int ServerID, int GroupID) {
			PGAOPCServer pServer = TheServer[ServerID];
			OpcGroup pGroup = TheGroup[ServerID][GroupID];
			if (pGroup != null) {
				try {
					if (pGroup.Active) {
						// pGroup.SetEnable(false);
						pGroup.Active = false;
					}
					if (pGroup.PGAactive) {
						if (Program.MainProg.wpForceRead) {
							// pGroup.forceRead.Enabled = true;
							pGroup.forceRead.Start();
						}
						addEvents(pGroup, ServerID, GroupID);
						// pGroup.SetEnable(true);
						pGroup.Active = true;
						if (Program.MainProg.wpPSOPC) {
							Thread.Sleep(locksleep);
						}
						int cI;
						int[] aE;
						int[] ArrayHsrv = Hsrv[ServerID][GroupID].ToArray();
						if(ArrayHsrv.Length > 0) {
							int taid = TransferId.getNew();
							if(!pGroup.Read(
								ArrayHsrv, taid,
								out cI, out aE)) {
								string l = String.Format("TAID-{0} faild", taid);
								for(int i = 0; i < aE.Length; i++) {
									l += String.Format("\r\nError: {0}", HRESULTS.getError(aE[i]));
								}
								eventLog.Write(l);
							}
						}
					}
				} catch (Exception ex) {
					eventLog.WriteError(ex);
				}
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="idgroup"></param>
		/// <param name="newname"></param>
		public void renameOPCGroup(int idgroup, string newname) {
			int idserver;
			using (SQL SQL = new SQL("Rename OPCGroup")) {
				string[][] DBServer = SQL.wpQuery(@"
					SELECT TOP 1 [id_opcserver] FROM [opcgroup] WHERE [id_opcgroup] = {0}", idgroup);
				Int32.TryParse(DBServer[0][0], out idserver);
			}
			renameOPCGroup(idserver, idgroup, newname);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="idserver"></param>
		/// <param name="idgroup"></param>
		/// <param name="newname"></param>
		public void renameOPCGroup(int idserver, int idgroup, string newname) {
			TheGroup[idserver][idgroup].SetName(newname);
		}

#endregion

		/// <summary>
		/// 
		/// </summary>
		public void finished() {
			running = false;
			//OpcChecker.doStop();
			//ThreadOpcChecker.Join(1500);
			//CheckOPCServerstateTimer.Tick -= new EventHandler(CheckOPCServerstate_Tick);
			//CheckOPCServerstateTimer.Stop();
			foreach(KeyValuePair<int, PGAOPCServer> opcserver in TheServer) {
				try {
					string TheLog = "";
					foreach(KeyValuePair<int, OpcGroup> opcgroup in TheGroup[opcserver.Key]) {
						try {
							if (opcgroup.Value != null) {
								int[] aErr;
								removeEvents(opcgroup.Value, opcserver.Key, opcgroup.Key);
								try {
									if (opcgroup.Value.RemoveItems(Hsrv[opcserver.Key][opcgroup.Key].ToArray(),
										out aErr)) {
										opcgroup.Value.Remove(true);
									}
								} catch (Exception ex) {
									eventLog.WriteError(ex);
								}
								TheLog += String.Format("\r\n\tGroup '{0}' Removed from '{1}'",
									opcgroup.Value.Name,
									opcserver.Value.Name);
							}
						} catch (Exception ex) {
							eventLog.WriteError(ex);
						}
					}
					if(TheLog != "")
						eventLog.Write($"finished OPC Server {opcserver.Value.Name} {TheLog}");
					try {
						opcserver.Value.ShutdownRequested -=
							new ShutdownRequestEventHandler(TheServer_ShutdownRequested);
						opcserver.Value.Disconnect();
						eventLog.Write(String.Format("OPC Server '{0}' disconnect", opcserver.Value.Name));
					} catch(Exception ex) {
						eventLog.WriteError(ex);
					}
				} catch (Exception ex) {
					eventLog.WriteError(ex);
				}
			}
			eventLog.Write("OPC Client gestoppt");
		}

#region OPCasync

		//private System.Timers.Timer t_shutdown;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void TheServer_ShutdownRequested(object sender, ShutdownRequestEventArgs e) {
			PGAOPCServer Server = (PGAOPCServer)sender;
			eventLog.Write(EventLogEntryType.Error, "OPC Server '{0}' Shutdown: {1}",
				Server.Name,
				e.shutdownReason);
			//t_shutdown = new System.Timers.Timer(10000);
			//t_shutdown.AutoReset = false;
			//t_shutdown.Elapsed += new System.Timers.ElapsedEventHandler(
			//	(_sender, _e) => t_shutdown_Elapsed(_sender, _e, Server.Id));

			//t_shutdown.Enabled = true;
		}

		//private void t_shutdown_Elapsed(object sender, System.Timers.ElapsedEventArgs e, int serverid) {
		//	reconnect(serverid);
		//}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void TheGroup_ReadCompleted(object sender, ReadCompleteEventArgs e) {
			OpcGroup RCGroup = (OpcGroup)sender;
			DateTime DateTimeNow = DateTime.Now;
			string lastchange = "";
			foreach (OPCItemState s in e.sts) {
				OPCItem ItemChanged = Server.Dictionaries.getItem(s.HandleClient);
				if (ItemChanged == null) {
					eventLog.Write(EventLogEntryType.Warning, "[ReadCompleted] Item momentan in Gebrauch id: '{0}'",
						s.HandleClient);
				} else if (!HRESULTS.Succeeded(s.Error)) {
					eventLog.Write(EventLogEntryType.Error, "Fehler beim lesen des Items '{0}'\r\n\tERROR: {1}",
						ItemChanged.OpcItemName,
						HRESULTS.getError(s.Error));
				} else {
					if (s.DataValue != null) {
						ItemChanged.Quality = s.Quality;
						TimeSpan ts = DateTimeNow - ItemChanged.Lastupdate;
						if (ts > new TimeSpan(0, 0, 0, 0, 900)) {
							ItemChanged.Lastupdate = DateTimeNow;
						}
						object erg = s.DataValue;

						if (ItemChanged.Value != erg.ToString()) {
							ItemChanged.Value = erg.ToString();
						}
						ItemChanged.DBType = s.CanonicalDataType;
						if(running) {
							lastchange += String.Format($"(read) - {ItemChanged.OpcItemName} {ItemChanged.Value}\r\n");
							valueChangedEventArgs vcea = new valueChangedEventArgs();
							vcea.idOPCPoint = ItemChanged.Hclt;
							vcea.name = ItemChanged.ItemName;
							vcea.value = ItemChanged.Value;
							if(valueChanged != null) valueChanged.Invoke(this, vcea);
						}
						if (!ItemChanged.hasFirstValue) ItemChanged.hasFirstValue = true;
					}
				}
			}
			if (lastchange != "") Program.MainProg.lastchange = lastchange;
			TransferId.remove(e.transactionID);
			wpDebug.Write("Read Completed (TAID-{0})", e.transactionID);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void TheGroup_DataChanged(object sender, DataChangeEventArgs e) {
			OpcGroup RCGroup = (OpcGroup)sender;
			string lastchange = "";
			if (running) {
				DateTime DateTimeNow = DateTime.Now;
				foreach (OPCItemState s in e.sts) {
					OPCItem ItemChanged = Server.Dictionaries.getItem(s.HandleClient);
					if (ItemChanged == null) {
						eventLog.Write(EventLogEntryType.Warning,
							"[DataChanged] Item momentan in Gebrauch id: '{0}'", s.HandleClient);
					} else if (!HRESULTS.Succeeded(s.Error)) {
						//if(ItemChanged.hasFirstValue) {
						//	eventLog.Write(EventLogEntryType.Error,
						//		"Fehler beim datachange des Items '{0}'\r\nERROR: {1}",
						//		ItemChanged.OpcItemName, HRESULTS.getError(s.Error));
						//} else {
						//	PDebug.Write("seltsames Softing-Verhalten: Fehler beim datachange des Items '{0}'\r\n\tERROR: {1}",
						//		ItemChanged.OpcItemName, HRESULTS.getError(s.Error));
						//}
					} else {
						if (s.DataValue != null) {
							ItemChanged.Quality = s.Quality;
							TimeSpan ts = DateTimeNow - ItemChanged.Lastupdate;
							if (ts > new TimeSpan(0, 0, 0, 0, 900)) {
								ItemChanged.Lastupdate = DateTimeNow;
							}

							object erg = s.DataValue;
							if (ItemChanged.Value != erg.ToString()) {
								ItemChanged.Value = erg.ToString();
							}
							ItemChanged.DBType = s.CanonicalDataType;
							lastchange += String.Format($"(change) - {ItemChanged.OpcItemName} {ItemChanged.Value}\r\n");
							valueChangedEventArgs vcea = new valueChangedEventArgs();
							vcea.idOPCPoint = ItemChanged.Hclt;
							vcea.name = ItemChanged.ItemName;
							vcea.value = ItemChanged.Value;
							if(valueChanged != null) valueChanged.Invoke(ItemChanged, vcea);
							if(!ItemChanged.hasFirstValue) ItemChanged.hasFirstValue = true;
						}
					}
				}
			}
			if (e.transactionID > 0) {
				TransferId.remove(e.transactionID);
				wpDebug.Write("Data Changed (TAID-{0})", e.transactionID);
			}
			if (lastchange != "") Program.MainProg.lastchange = lastchange;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void TheGroup_WriteCompleted(object sender, WriteCompleteEventArgs e) {
			string strevlog = "";
			for(int i = 0; i < e.res.Length; i++) {
				OPCItem TheItem = Server.Dictionaries.getItem(e.res[i].HandleClient);
				if (TheItem != null) {
					// Fuehrt zu einer Schleife!!!
					// if (Program.MainProg.IsTaster(e.res[i].HandleClient))
					//	setTasterDefault(e.res[i].HandleClient, TheItem.Value);
					if (e.masterError != HRESULTS.S_OK) {
						if(TransferId.getTransferReason(e.transactionID) !=
							TransferId.TransferOpcRouter &&
							TransferId.getTransferReason(e.transactionID) !=
							TransferId.TransferWatchdog) {
							strevlog += String.Format("{3}Item: {0} ({1}), Error:{2}",
								TheItem.OpcItemName,
								e.res[i].HandleClient,
								HRESULTS.getError(e.masterError),
								(e.res.Length == 1 ? ": " : "\r\n\t"));
						}
					}
				}
			}
			TransferId.remove(e.transactionID);
			if (strevlog.Length > 0) {
				eventLog.Write("Group Write Completed (TAID-{0}): {1}", e.transactionID, strevlog);
			}
		}
		public void TheGroup_CancelCompleted(object sender, CancelCompleteEventArgs e) {
			eventLog.Write(EventLogEntryType.Warning, "not implemented");
		}

#endregion

#region OPCsetter

		/// <summary>
		/// 
		/// </summary>
		/// <param name="opcid"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public string setValue(int opcid, string value) {
			return setValue(opcid, value, TransferId.TransferNormalOP);
		}
		public string setValue(int opcid, string value, int transferreason) {
			if (!running) return "";
			try {
				OPCItem Item = Server.Dictionaries.getItem(opcid);
				if (Item != null) {
					int[] aE = new int[] { 1 };
					int cI;
					int[] HSrv = new int[1] { Item.Hsrv };
					object[] Val = new object[1] { setTypeForSetValue(Item.DBType, value) };
					int taid = TransferId.getNew(transferreason);
					if (transferreason == TransferId.TransferOpcRouter ||
						transferreason == TransferId.TransferWatchdog ||
						transferreason == TransferId.TransferMQTT) {
						if(transferreason == TransferId.TransferOpcRouter && Program.MainProg.wpDebugOpcRouter) {
							wpDebug.Write("Write (TAID-{0}): Item '{1}': old: '{2}', new: '{3}'",
								taid, Item.OpcItemName, Item.Value, value);
						}
						if(transferreason == TransferId.TransferWatchdog && Program.MainProg.wpDebugWatchdog) {
							wpDebug.Write("Write (TAID-{0}): Item '{1}': old: '{2}', new: '{3}'",
								taid, Item.OpcItemName, Item.Value, value);
						}
						if(transferreason == TransferId.TransferMQTT && Program.MainProg.wpDebugMQTT) {
							wpDebug.Write("Write (TAID-{0}): Item '{1}': old: '{2}', new: '{3}'",
								taid, Item.OpcItemName, Item.Value, value);
						}
					} else {
						eventLog.Write("Write (TAID-{0}): Item '{1}': old: '{2}', new: '{3}'",
							taid, Item.OpcItemName, Item.Value, value);
					}
					if (TheGroup[Item.Server][Item.Group].Active) {
						if (lockopc.WaitOne(locktimeout)) {
							try {
								if (!TheGroup[Item.Server][Item.Group].Write(
									HSrv, Val,
									taid,
									out cI, out aE)) {
									eventLog.Write("Write (TAID-{0}): {1} konnte nicht geschrieben werden:\r\n{2}",
										taid, Item.OpcItemName, HRESULTS.getError(aE[0]));
								}

								if (Program.MainProg.wpPSOPC) {
									Thread.Sleep(locksleep);
								}
							} catch (Exception ex) {
								eventLog.WriteError(ex);
								return String.Format("OPC Error: {0}", ex.Message);
							} finally {
								lockopc.ReleaseMutex();
							}
						} else {
							eventLog.Write(EventLogEntryType.Error, "Deadlockopfer ☺: Mutextimeout für SetValue abgelaufen (TAID-{0})", taid);
						}
						return HRESULTS.getError(aE[0]);
					} else {
						return "Gruppe nicht aktiv";
					}
				} else {
					return "Item nicht aktiv";
				}
			} catch (FormatException) {
				return "Invalid data format!";
			} catch (OverflowException) {
				return "Invalid data range/overflow!";
			} catch (COMException) {
				return "OPC Write Item error!";
			} catch (Exception ex) {
				eventLog.WriteError(ex);
				return String.Format("OPC Error: {0}", ex.Message);
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="opcid"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		//public string setValues(int[] opcid, string[] value) {
		//    return setValues(opcid, value, WebAutomationServer.NormalOP);
		//}
		public string setValues(int[] opcid, string[] value, int transferreason) {
			if (!running) return "";
			string returns = "";
			Dictionary<int, Dictionary<int, string>> log = new Dictionary<int,Dictionary<int,string>>();
			try {
				int[] aE;
				int cI;

				Dictionary<int, Dictionary<int, Dictionary<int, object>>> Items =
					new Dictionary<int, Dictionary<int, Dictionary<int, object>>>();
				for (int i = 0; i < opcid.Length; i++) {
					OPCItem TheItem = Server.Dictionaries.getItem(opcid[i]);
					if (TheItem != null) {
						if (Items.ContainsKey(TheItem.Server)) {
							if (Items[TheItem.Server].ContainsKey(TheItem.Group)) {
								Items[TheItem.Server][TheItem.Group].Add(TheItem.Hsrv,
									setTypeForSetValue(TheItem.DBType, value[i]));
								log[TheItem.Server][TheItem.Group] +=
									String.Format("\r\n\tWrite Item '{0}': old: '{1}', new: '{2}'",
										TheItem.OpcItemName, TheItem.Value, value[i]);
							} else {
								Items[TheItem.Server].Add(TheItem.Group, new Dictionary<int, object>());
								log[TheItem.Server].Add(TheItem.Group,
									String.Format("\r\n\tWrite Item '{0}': old: '{1}', new: '{2}'",
										TheItem.OpcItemName, TheItem.Value, value[i]));
								Items[TheItem.Server][TheItem.Group].Add(TheItem.Hsrv,
									setTypeForSetValue(TheItem.DBType, value[i]));
							}
						} else {
							Items.Add(TheItem.Server, new Dictionary<int, Dictionary<int, object>>());
							log.Add(TheItem.Server, new Dictionary<int,string>());
							if (Items[TheItem.Server].ContainsKey(TheItem.Group)) {
								Items[TheItem.Server][TheItem.Group].Add(TheItem.Hsrv,
									setTypeForSetValue(TheItem.DBType, value[i]));
								log[TheItem.Server][TheItem.Group] +=
									String.Format("\r\n\tWrite Item '{0}': old: '{1}', new: '{2}'",
										TheItem.OpcItemName, TheItem.Value, value[i]);
							} else {
								Items[TheItem.Server].Add(TheItem.Group, new Dictionary<int, object>());
								log[TheItem.Server].Add(TheItem.Group,
									String.Format("\r\n\tWrite Item '{0}': old: '{1}', new: '{2}'",
									TheItem.OpcItemName, TheItem.Value, value[i]));
								Items[TheItem.Server][TheItem.Group].Add(TheItem.Hsrv,
									setTypeForSetValue(TheItem.DBType, value[i]));
							}
						}
					} else {
						returns += "Item nicht aktiv";
					}
				}

				int[] HSrvToWrite;
				//string[] StringValToWrite;
				object[] ValToWrite;

				foreach (KeyValuePair<int, Dictionary<int, Dictionary<int, object>>> _Server in Items) {
					foreach (KeyValuePair<int, Dictionary<int, object>> _Group in _Server.Value) {
						HSrvToWrite = new int[_Group.Value.Count];
						//StringValToWrite = new string[_Group.Value.Count];
						ValToWrite = new object[_Group.Value.Count];
						_Group.Value.Keys.CopyTo(HSrvToWrite, 0);
						_Group.Value.Values.CopyTo(ValToWrite, 0);
						int taid = TransferId.getNew(transferreason);
						if (log[_Server.Key][_Group.Key] != "") {
							if (transferreason == TransferId.TransferOpcRouter ||
								transferreason == TransferId.TransferWatchdog) {
								wpDebug.Write("setValue (TAID-{0}): {1}", taid, log[_Server.Key][_Group.Key]);
							} else {
								eventLog.Write("setValue (TAID-{0}): {1}", taid, log[_Server.Key][_Group.Key]);
							}
						}
						if (TheGroup[_Server.Key][_Group.Key].Active) {
							if (lockopc.WaitOne(locktimeout)) {
								try {
									if (!TheGroup[_Server.Key][_Group.Key].Write(HSrvToWrite, ValToWrite,
										taid, out cI, out aE)) {
										string l = String.Format("TAID-{0} faild", taid);
										for (int i = 0; i < aE.Length; i++) {
											l += String.Format("\r\nError: {0}", HRESULTS.getError(aE[i]));
										}
										eventLog.Write(l);
									}
									if (Program.MainProg.wpPSOPC) {
										Thread.Sleep(locksleep);
									}
								} catch (Exception ex) {
									eventLog.WriteError(ex);
									return String.Format("OPC Error: {0}", ex.Message);
								} finally {
									lockopc.ReleaseMutex();
								}
							} else {
								eventLog.Write(EventLogEntryType.Error, "Deadlockopfer ☺: Mutextimeout für SetValues abgelaufen (TAID-{0})", taid);
							}
						} else {
							returns += "Gruppe nicht aktiv";
						}
					}
				}
			} catch (FormatException) {
				returns += "Invalid data format!";
			} catch (OverflowException) {
				returns += "Invalid data range/overflow!";
			} catch (COMException) {
				returns += "OPC Write Item error!";
			} catch (Exception ex) {
				eventLog.WriteError(ex);
				returns += String.Format("OPC Error: {0}", ex.Message);
			}
			if (returns == "") returns = "S_OK";
			return returns;
		}
		private object setTypeForSetValue(VarEnum t, string v) {
			object returns = v;
			bool found = false;
			// Bool
			if (t == VarEnum.VT_BOOL) {
				bool parsed;
				if (Boolean.TryParse(v, out parsed)) {
					returns = parsed;
					found = true;
				}
			}
			// Int
			if (t == VarEnum.VT_I1) {
				char parsed;
				if (Char.TryParse(v, out parsed)) {
					returns = parsed;
					found = true;
				}
			}
			if (t == VarEnum.VT_I2) {
				short parsed;
				if (Int16.TryParse(v, out parsed)) {
					returns = parsed;
					found = true;
				}
			}
			if (t == VarEnum.VT_I4) {
				int parsed;
				if (Int32.TryParse(v, out parsed)) {
					returns = parsed;
					found = true;
				}
			}
			if (t == VarEnum.VT_I8 || t == VarEnum.VT_INT) {
				long parsed;
				if (Int64.TryParse(v, out parsed)) {
					returns = parsed;
					found = true;
				}
			}
			// UInt
			if (t == VarEnum.VT_UI1) {
				byte parsed;
				if (Byte.TryParse(v, out parsed)) {
					returns = parsed;
					found = true;
				}
			}
			if (t == VarEnum.VT_UI2) {
				ushort parsed;
				if (UInt16.TryParse(v, out parsed)) {
					returns = parsed;
					found = true;
				}
			}
			if (t == VarEnum.VT_UI4) {
				uint parsed;
				if (UInt32.TryParse(v, out parsed)) {
					returns = parsed;
					found = true;
				}
			}
			if (t == VarEnum.VT_UI8 || t == VarEnum.VT_UINT) {
				ulong parsed;
				if (UInt64.TryParse(v, out parsed)) {
					returns = parsed;
					found = true;
				}
			}
			// Real
			if (t == VarEnum.VT_R4) {
				float parsed;
				if (Single.TryParse(v, out parsed)) {
					returns = parsed;
					found = true;
				}
			}
			if (t == VarEnum.VT_R8 || t == VarEnum.VT_DECIMAL) {
				double parsed;
				if (Double.TryParse(v, out parsed)) {
					returns = parsed;
					found = true;
				}
			}
			// String
			if (t == VarEnum.VT_BSTR || t == VarEnum.VT_DATE) {
				returns = v;
			}
			// Fallback
			if (!(found == true)) {
				returns = (object)v;
			}
			return returns;
		}

#endregion


#region BrowseOPC

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public static string getAllOPCServer(string remoteserver) {
			string OPCServer = "{\"OPCServer\"=";
			OpcServers[] m_Servers;
			new OpcServerList().ListAllData20(out m_Servers, remoteserver);

			foreach (OpcServers m_Server in m_Servers) {
				OPCServer += String.Format("{{\"{0}\"={{\"ProgID\"=\"{1}\"}}{{\"ClsID\"=\"{2}\"}}}}",
					m_Server.ServerName,
					m_Server.ProgID,
					m_Server.ClsID
				);
			}
			return OPCServer + "}";
		}
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public static string getAllOPCServer() {
			return getAllOPCServer("localhost");
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="theSrvId"></param>
		/// <param name="remoteserver"></param>
		/// <returns></returns>
		public bool DoBrowse(string theSrvId, string remoteserver) {
			OPCbrowsed = "";
			OpcServer theSrv = null;
			bool connected = false;
			foreach(KeyValuePair<int, PGAOPCServer> available in TheServer) {
				if (available.Value.Progid == theSrvId && available.Value.Remoteserver == remoteserver) {
					theSrv = available.Value;
					connected = true;
					wpDebug.Write("{0} ist bereits kontaktiert", theSrvId);
					break;
				}
			}
			if (theSrv == null) {
				theSrv = new OpcServer(theSrvId);
			}

			try {
				if (!connected) {
					if (remoteserver == "localhost" || remoteserver == "127.0.0.1") {
						theSrv.Connect(theSrvId, "localhost");
					} else {
						theSrv.Connect(theSrvId, remoteserver);
					}
					theSrv.SetClientName(Application.ProductName);
					wpDebug.Write("{0} wurde kontaktiert", theSrvId);
				}

				OPCNAMESPACETYPE opcorgi = theSrv.QueryOrganization();

				if (opcorgi == OPCNAMESPACETYPE.OPC_NS_HIERARCHIAL) {
					eventLog.Write("OPC_NS_HIERARCHIAL");
					OPCbrowsed += "{\"Root\"=";
					theSrv.ChangeBrowsePosition(OPCBROWSEDIRECTION.OPC_BROWSE_TO, "");
					RecurBrowse(theSrv);
				}
				if (opcorgi == OPCNAMESPACETYPE.OPC_NS_FLAT) {
					eventLog.Write("OPC_NS_FLAT");
					OPCbrowsed += "{\"FLAT\"=";
					OPCbrowsed += "{\"OPCITEMS\"=";
					ArrayList lst;
					theSrv.Browse(OPCBROWSETYPE.OPC_LEAF, out lst);

					string[] itemstrings = new string[2];
					foreach (string item in lst) {
						itemstrings[0] = item;
						itemstrings[1] = theSrv.GetItemID(item);
						OPCbrowsed += "{\"" + theSrv.GetItemID(item) + "\"=\"" + item + "\"}";
					}
					OPCbrowsed += "}";
				}
				OPCbrowsed.Substring(0, OPCbrowsed.Length - 1);
				if (!connected) theSrv.Disconnect();
				eventLog.Write(OPCbrowsed);
			} catch (COMException ex) {
				if (!connected) theSrv.Disconnect();
				eventLog.WriteError(ex);
				return false;
			} catch (Exception ex) {
				if (!connected) theSrv.Disconnect();
				eventLog.WriteError(ex);
				return false;
			}
			return true;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="theSrvId"></param>
		/// <returns></returns>
		public bool DoBrowse(string theSrvId) {
			return DoBrowse(theSrvId, "localhost");
		}
		public bool DoBrowse(string theSrvId, string remoteserver, string path) {
			string[] pathes = path.Split(new char[1] { '\\' });
			OPCbrowsed = "";
			OpcServer theSrv = null;
			bool connected = false;
			foreach(KeyValuePair<int, PGAOPCServer> available in TheServer) {
				if(available.Value.Progid == theSrvId && available.Value.Remoteserver == remoteserver) {
					theSrv = available.Value;
					connected = true;
					wpDebug.Write("{0} ist bereits kontaktiert", theSrvId);
					break;
				}
			}
			if(theSrv == null) {
				theSrv = new OpcServer(theSrvId);
			}

			try {
				if(!connected) {
					if(remoteserver == "localhost" || remoteserver == "127.0.0.1") {
						theSrv.Connect(theSrvId, "localhost");
					} else {
						theSrv.Connect(theSrvId, remoteserver);
					}
					theSrv.SetClientName(Application.ProductName);
					wpDebug.Write("{0} wurde kontaktiert", theSrvId);
				}

				OPCNAMESPACETYPE opcorgi = theSrv.QueryOrganization();
				if(opcorgi == OPCNAMESPACETYPE.OPC_NS_HIERARCHIAL) {
					Stopwatch sw = new Stopwatch();
					sw.Start();
					string akt = "";
					theSrv.ChangeBrowsePosition(OPCBROWSEDIRECTION.OPC_BROWSE_TO, "");
					foreach(string level in pathes) {
						ArrayList lst;
						theSrv.Browse(OPCBROWSETYPE.OPC_BRANCH, out lst);
						foreach(string s in lst) {
							if(s == level) {
								akt = s;
								theSrv.ChangeBrowsePosition(OPCBROWSEDIRECTION.OPC_BROWSE_DOWN, s);
								break;
							}
						}
					}
					wpDebug.Write("gebrowsed bis Ziel ({0} ms)", sw.ElapsedMilliseconds);

					ArrayList lst2;
					theSrv.Browse(OPCBROWSETYPE.OPC_BRANCH, out lst2);
					foreach(string s in lst2) {
						OPCbrowsed += "{\"" + s + "\"=";
						theSrv.ChangeBrowsePosition(OPCBROWSEDIRECTION.OPC_BROWSE_DOWN, s);
						ArrayList lst3;
						theSrv.Browse(OPCBROWSETYPE.OPC_BRANCH, out lst3);
						if(lst3.Count < 1)
							OPCbrowsed += "\"" + s + "\"}";
						else
							OPCbrowsed += "{}}";
						theSrv.ChangeBrowsePosition(OPCBROWSEDIRECTION.OPC_BROWSE_UP, "");
					}
					wpDebug.Write("Ziel gebrowsed ({0} ms)", sw.ElapsedMilliseconds);
					sw.Stop();
				}
				if(!connected) theSrv.Disconnect();
				eventLog.Write(OPCbrowsed);
			} catch(COMException ex) {
				if(!connected) theSrv.Disconnect();
				eventLog.WriteError(ex);
				return false;
			} catch(Exception ex) {
				if(!connected) theSrv.Disconnect();
				eventLog.WriteError(ex);
				return false;
			}
			return true;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="theSrv"></param>
		/// <param name="depth"></param>
		/// <returns></returns>
		private int RecurBrowse(OpcServer theSrv) {
			try {
				ArrayList lst;
				theSrv.Browse(OPCBROWSETYPE.OPC_BRANCH, out lst);
				if (lst == null)
					return ISERROR;
				if (lst.Count < 1)
					return ISITEM;

				foreach (string s in lst) {
					OPCbrowsed += "{\"" + s + "\"=";
					theSrv.ChangeBrowsePosition(OPCBROWSEDIRECTION.OPC_BROWSE_DOWN, s);

					ArrayList lst2;
					theSrv.Browse(OPCBROWSETYPE.OPC_BRANCH, out lst2);
					if(lst2.Count < 1)
						OPCbrowsed += "\"" + s + "\"}";
					else
						OPCbrowsed += "{}}";
					theSrv.ChangeBrowsePosition(OPCBROWSEDIRECTION.OPC_BROWSE_UP, "");
				}
				OPCbrowsed += "}";
			} catch (COMException ex) {
				eventLog.WriteError(ex);
				return ISERROR;
			}
			return ISGROUP;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="theSrvId"></param>
		/// <param name="FullPath"></param>
		/// <param name="remoteserver"></param>
		/// <returns></returns>
		public string getItems(string theSrvId, string FullPath, string remoteserver) {
			string returns = "{\"OPCITEMS\"=";
			string rootname = "Root";
			string selectednode;
			OpcServer theSrv = null;
			bool connected = false;
			foreach (KeyValuePair<int, PGAOPCServer> available in TheServer) {
				if (available.Value.Progid == theSrvId && available.Value.Remoteserver == remoteserver) {
					theSrv = available.Value;
					connected = true;
					wpDebug.Write("{0} ist bereits kontaktiert", theSrvId);
					break;
				}
			}
			if (theSrv == null) {
				theSrv = new OpcServer(theSrvId);
			}

			try {
				if (!connected) {
					if (remoteserver == "localhost" || remoteserver == "127.0.0.1") {
						theSrv.Connect(theSrvId, "localhost");
					} else {
						theSrv.Connect(theSrvId, remoteserver);
					}
					theSrv.SetClientName(Application.ProductName);
					wpDebug.Write("{0} wurde kontaktiert", theSrvId);
				}

				theSrv.ChangeBrowsePosition(OPCBROWSEDIRECTION.OPC_BROWSE_TO, "");

				if (FullPath.Length > rootname.Length) {
					selectednode = FullPath.Substring(rootname.Length + 1);
					string[] splitpath = selectednode.Split(new char[] { '\\' });

					foreach (string n in splitpath)
						theSrv.ChangeBrowsePosition(OPCBROWSEDIRECTION.OPC_BROWSE_DOWN, n);
				} else
					selectednode = "";

				ArrayList lst;
				theSrv.Browse(OPCBROWSETYPE.OPC_LEAF, out lst);
				if (lst == null)
					return null;
				if (lst.Count < 1)
					return null;

				string[] itemstrings = new string[2];
				foreach (string item in lst) {
					itemstrings[0] = item;
					itemstrings[1] = theSrv.GetItemID(item);
					returns += "{\"" + theSrv.GetItemID(item) + "\"=\"" + item + "\"}";
				}
				if (!connected) theSrv.Disconnect();
				return returns + "}";
			} catch (Exception ex) {
				if (!connected) theSrv.Disconnect();
				eventLog.WriteError(ex);
				return null;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="theSrvId"></param>
		/// <param name="FullPath"></param>
		/// <returns></returns>
		public string getItems(string theSrvId, string FullPath) {
			return getItems(theSrvId, FullPath, "localhost");
		}

#endregion

		/// <summary>
		/// 
		/// </summary>
		/// <param name="idserver"></param>
		/// <returns></returns>
		public string GetOPCServerDetails(int idserver) {
			string returns = String.Format("{{OPCServerName={0}}}{{OPCServerID={1}}}{{ProgID={2}}}{{ClsID={3}}}",
				TheServer[idserver].Name,
				TheServer[idserver].Id,
				TheServer[idserver].Progid,
				TheServer[idserver].Clsid);
			SERVERSTATUS ss;
			TheServer[idserver].GetStatus(out ss);
			returns += String.Format(@"{{State={0}}}{{Version={1}.{2}.{3}}}{{Vendor={4}}}{{OPCGruppen={5}}}" +
				"{{Start={6}}}{{Update={7}}}{{Time={8}}}",
				ss.eServerState,
				ss.wMajorVersion,
				ss.wMinorVersion,
				ss.wBuildNumber,
				ss.szVendorInfo,
				ss.dwGroupCount,
				DateTime.FromFileTime(ss.ftStartTime),
				DateTime.FromFileTime(ss.ftLastUpdateTime),
				DateTime.FromFileTime(ss.ftCurrentTime));
			returns += "{OpcGroupDetails=";
			foreach (KeyValuePair<int, OpcGroup> kvp in TheGroup[idserver]) {
				returns += String.Format(@"{{{0}=", kvp.Key);
				OpcGroup _group = kvp.Value;
				returns += String.Format(@"{{Active={0}}}{{Name={1}}}{{ItemCount={2}}}",
					_group.OPCState.Active,
					_group.OPCState.Name,
					kvp.Value.countItem.Count);

				string sGood = "{itemstates=";
				int iGood = 0;
				foreach (KeyValuePair<int, int> TheItemStates in TheGroup[idserver][kvp.Key].countItem) {
					if (TheItemStates.Value == HRESULTS.S_OK) {
						iGood++;
					}
				}
				sGood += String.Format("{{S_OK={0}}}}}", iGood);
				returns += sGood;


				sGood = "{itemquality=";
				iGood = 0;
				foreach (KeyValuePair<int, short> TheItemQuality in TheGroup[idserver][kvp.Key].countItemState) {
					if (TheItemQuality.Value == (short)OPC_QUALITY_STATUS.OK) {
						iGood++;
					}
				}
				sGood += String.Format("{{S_OK={0}}}}}", iGood);
				returns += sGood + "}";
			}
			return returns + "}";
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="serverid"></param>
		/// <param name="writelevel"></param>
		/// <param name="defaultvalue"></param>
		/// <param name="forcewritelevel"></param>
		/// <returns></returns>
		public string ChangeOPCServerWriteLevel(int serverid) {
			string returns = "S_OK";
			WriteLevel.AddWriteLevel(serverid);
			return returns;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="idserver"></param>
		/// <returns></returns>
		public string GetOPCGroupDetails(int idgroup) {
			int outint;
			using(SQL SQL = new SQL("OPC Group Details")) {
				string[][] DBServer = SQL.wpQuery(@"SELECT TOP 1 [id_opcserver]
					FROM [opcgroup] WHERE [id_opcgroup] = {0}", idgroup);
				Int32.TryParse(DBServer[0][0], out outint);
			}
			return GetOPCGroupDetails(outint, idgroup);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="idserver"></param>
		/// <returns></returns>
		public string GetOPCGroupDetails(int idserver, int idgroup) {
			OpcGroup _group = TheGroup[idserver][idgroup];
			string returns = String.Format(@"{{Active={0}}}{{Name={1}}}{{Update={2}}}{{RequestUpdate={3}}}" +
				"{{ItemCount={4}}}",
				_group.OPCState.Active,
				_group.OPCState.Name,
				_group.OPCState.UpdateRate,
				_group.RequestedUpdateRate,
				TheGroup[idserver][idgroup].countItem.Count);

			string sGood = "{itemstates=";
			int iGood = 0;
			foreach(KeyValuePair<int, int> TheItemStates in TheGroup[idserver][idgroup].countItem) {
				if (TheItemStates.Value == HRESULTS.S_OK) {
					iGood++;
				} else {
					sGood += String.Format("{{{0}={1}}}", Server.Dictionaries.getItem(TheItemStates.Key).OpcItemName,
						HRESULTS.getError(TheItemStates.Value));
				}
			}
			sGood += String.Format("{{S_OK={0}}}}}", iGood);
			returns += sGood;


			sGood = "{itemquality=";
			iGood = 0;
			foreach (KeyValuePair<int, short> TheItemQuality in TheGroup[idserver][idgroup].countItemState) {
				if (TheItemQuality.Value == (short)OPC_QUALITY_STATUS.OK) {
					iGood++;
				} else {
					sGood += String.Format("{{{0}={1}}}", Server.Dictionaries.getItem(TheItemQuality.Key).OpcItemName,
						OpcGroup.QualityToString(TheItemQuality.Value));
				}
			}
			sGood += String.Format("{{S_OK={0}}}}}", iGood);
			returns += sGood;
			return returns;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="idgroup"></param>
		/// <returns></returns>
		public string ChangeOPCGroupState(int idgroup) {
			int outint;
			using (SQL SQL = new SQL("OPC Group Details")) {
				string[][] DBServer = SQL.wpQuery(@"SELECT TOP 1 [id_opcserver]
					FROM [opcgroup] WHERE [id_opcgroup] = {0}", idgroup);
				Int32.TryParse(DBServer[0][0], out outint);
			}
			return ChangeOPCGroupState(outint, idgroup);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="idserver"></param>
		/// <param name="idgroup"></param>
		/// <returns></returns>
		public string ChangeOPCGroupState(int idserver, int idgroup) {
			if (TheGroup[idserver][idgroup].Active) {
				TheGroup[idserver][idgroup].SetEnable(false);
				TheGroup[idserver][idgroup].Active = false;
				TheGroup[idserver][idgroup].PGAactive = false;
				removeEvents(TheGroup[idserver][idgroup], idserver, idgroup);
				using (SQL SQL = new SQL("OPC Group deactivate")) {
					SQL.wpNonResponse("UPDATE [opcgroup] SET [active] = 0 WHERE [id_opcgroup] = {0}", idgroup);
				}
				return "False";
			} else {
				TheGroup[idserver][idgroup].SetEnable(true);
				TheGroup[idserver][idgroup].Active = true;
				TheGroup[idserver][idgroup].PGAactive = true;
				addEvents(TheGroup[idserver][idgroup], idserver, idgroup);
				using (SQL SQL = new SQL("OPC Group activate")) {
					SQL.wpNonResponse("UPDATE [opcgroup] SET [active] = 1 WHERE [id_opcgroup] = {0}", idgroup);
				}

				WriteLevel.AddGroupWriteLevel(idgroup);

				int cI;
				int[] aE;
				int[] ArrayHsrv = Hsrv[idserver][idgroup].ToArray();
				if (ArrayHsrv.Length > 0) {
					TheGroup[idserver][idgroup].Read(Hsrv[idserver][idgroup].ToArray(),
						TransferId.getNew(), out cI, out aE);
				}

				return "True";
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		//private void CheckOPCServerstate_Tick(object sender, EventArgs e) {
		//	CheckOPCServerstate();
		//}
		//private void CheckOPCServerstate() {
		//	string aktServername = "";
		//	int aktServerid = 0;
		//	try {
		//		SERVERSTATUS ss;
		//		foreach (KeyValuePair<int, PGAOPCServer> os in TheServer) {
		//			aktServername = os.Value.Name;
		//			aktServerid = os.Value.Id;
		//			PDebug.Write("Try to Connect Server {0}", aktServername);
		//			if (os.Value.Active) {
		//				os.Value.GetStatus(out ss);
		//				if (ss == null || ss.eServerState != OPCSERVERSTATE.OPC_STATUS_RUNNING) {
		//					wpEventLog.Write(String.Format("{0}: connection lost???", os.Value.Name));
		//				}
		//			} else {
		//				PDebug.Write("Server {0} deactivated", aktServername);
		//			}
		//		}
		//	} catch (Exception) {
		//		wpEventLog.Write(EventLogEntryType.Warning,
		//			String.Format("OPC Server '{0}': lost Connection\r\nRetry to connect evry {1} Seconds",
		//			aktServername,
		//			trytorestart));
		//		reconnect(aktServerid);
		//	}
		//}
		//private void reconnect(int aktServerid) {
		//	try {
		//		string TheLog = "";
		//		PTaster.RemoveTaster(aktServerid);
		//		PTrend.removeTrend(aktServerid);
		//		foreach (KeyValuePair<int, OpcGroup> opcgroup in TheGroup[aktServerid]) {
		//			try {
		//				removeEvents(opcgroup.Value, aktServerid, opcgroup.Key);
		//				//opcgroup.Value.RemoveItems(Hsrv[aktServerid][opcgroup.Key].ToArray(), out aErr);
		//				//opcgroup.Value.Remove(true);
		//				TheLog += String.Format("Group '{0}' Removed from '{1}'\r\n",
		//					opcgroup.Value.Name, TheServer[aktServerid].Name);
		//			} catch (Exception ex) {
		//				wpEventLog.Write(EventLogEntryType.Warning,
		//					String.Format("OPC Server ERROR: \n{0}", ex.Message));
		//			}
		//		}
		//		if (TheLog != "") wpEventLog.Write(TheLog);
		//		try {
		//			TheServer[aktServerid].ShutdownRequested -=
		//				new ShutdownRequestEventHandler(TheServer_ShutdownRequested);
		//			// TheServer[aktServerid].Disconnect();

		//			wpEventLog.Write(String.Format("OPC Server '{0}' disconnect", TheServer[aktServerid].Name));
		//		} catch (Exception ex) {
		//			wpEventLog.WriteError(ex);
		//		} finally {
		//			TheServer[aktServerid].Dispose();
		//			TheServer[aktServerid] = null;
		//		}
		//		TheServer.Remove(aktServerid);
		//		TheGroup.Remove(aktServerid);
		//		Hsrv.Remove(aktServerid);
		//		using (PSQL SQL = new PSQL("Remove OPC Server")) {
		//			string[][] DBDatapoints = SQL.wpQuery(@"
		//					SELECT [d].[id_opcdatapoint] FROM [opcdatapoint] [d]
		//					INNER JOIN [opcgroup] [g] ON [d].[id_opcgroup] = [g].[id_opcgroup]
		//					INNER JOIN [opcserver] [s] ON [g].[id_opcserver] = [s].[id_opcserver]
		//					WHERE [s].[id_opcserver] = {0}",
		//				aktServerid);
		//			List<int> toDelete = new List<int>();
		//			int intout;
		//			for (int i = 0; i < DBDatapoints.Length; i++) {
		//				if (Int32.TryParse(DBDatapoints[i][0], out intout)) toDelete.Add(intout);
		//			}
		//			Program.MainProg.DeleteItems(toDelete.ToArray());
		//		}
		//	} catch (Exception ex) {
		//		wpEventLog.WriteError(ex);
		//	}

		//	// reconnect

		//	try {
		//		int idgroup;
		//		int duration;
		//		int idpoint;

		//		using (PSQL SQL = new PSQL("Add OPC Server after shutdown")) {
		//			string[][] DBserver = SQL.wpQuery(@"
		//					SELECT TOP 1 [id_opcserver], [progid], [clsid], [name], [server], [active]
		//					FROM [opcserver] WHERE [id_opcserver] = {0}",
		//				aktServerid);
		//			TheServerAdd(aktServerid, DBserver[0][1], DBserver[0][2], DBserver[0][3],
		//				DBserver[0][4], DBserver[0][5] == "True");
		//		}
		//		using (PSQL SQL = new PSQL("Add OPC Groups")) {
		//			string tempLog, GroupLog = "";
		//			string tempErr, GroupErr = "";
		//			string[][] DBGroup = SQL.wpQuery(@"
		//						SELECT [g].[id_opcgroup], [g].[name], [g].[duration], [g].[active]
		//						FROM [opcgroup] [g]
		//						INNER JOIN [opcserver] [s] ON [g].[id_opcserver] = [s].[id_opcserver]
		//						WHERE [s].[id_opcserver] = {0}",
		//					aktServerid);
		//			for (int igroup = 0; igroup < DBGroup.Length; igroup++) {
		//				if (Int32.TryParse(DBGroup[igroup][0], out idgroup) &&
		//					Int32.TryParse(DBGroup[igroup][2], out duration)) {
		//					TheGroupAdd(aktServerid, idgroup, DBGroup[igroup][1], duration,
		//						DBGroup[igroup][3] == "True", out tempLog, out tempErr);
		//					GroupLog += tempLog;
		//					GroupErr += tempErr;
		//				}
		//			}
		//			wpEventLog.Write(GroupLog);
		//			if (GroupErr.Length > 0) wpEventLog.Write(EventLogEntryType.Warning, GroupErr);
		//		}
		//		using (PSQL SQL = new PSQL("Add OPC Items")) {
		//			string[][] DBDatapoints = SQL.wpQuery(@"
		//						SELECT [g].[id_opcgroup], [d].[id_opcdatapoint], [d].[opcname], [d].[active]
		//						FROM [opcdatapoint] [d]
		//						INNER JOIN [opcgroup] [g] ON [d].[id_opcgroup] = [g].[id_opcgroup]
		//						INNER JOIN [opcserver] [s] ON [g].[id_opcserver] = [s].[id_opcserver]
		//						WHERE [s].[id_opcserver] = {0}",
		//					aktServerid);


		//			for (int idatapoint = 0; idatapoint < DBDatapoints.Length; idatapoint++) {
		//				if (Int32.TryParse(DBDatapoints[idatapoint][0], out idgroup) &&
		//					Int32.TryParse(DBDatapoints[idatapoint][1], out idpoint)) {
		//					TheItemAdd(aktServerid, idgroup, idpoint, DBDatapoints[idatapoint][2],
		//						DBDatapoints[idatapoint][3] == "True");
		//				}
		//			}
		//		}
		//		PAlarm.AddAlarms(aktServerid);
		//		PTrend.AddTrends(aktServerid);
		//		PSchedule.AddSchedules(aktServerid);
		//		PTaster.AddTaster(aktServerid);
		//		PWriteLevel.AddWriteLevel(aktServerid);
		//		PRouter.AddRouter();
		//		activate(aktServerid);
		//	} catch (Exception ex) {
		//		wpEventLog.WriteError(ex);
		//	}
		//}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="idserver"></param>
		/// <param name="newname"></param>
		public void ChangeOPCServerName(int idserver, string newname) {
			TheServer[idserver].Name = newname;
		}

		public int ChangeOPCItemType(int iditem, VarEnum newtype) {
			OPCItem TheItem = Server.Dictionaries.getItem(iditem);
			int[] arrErr;
			int cI;
			if (!TheGroup[TheItem.Server][TheItem.Group].SetDatatypes(
				new int[] { TheItem.Hsrv }, new VarEnum[] { newtype }, out arrErr)) {
				eventLog.Write("Datentyp konnte nicht umbenannt werden");
			}
			using(SQL SQL = new SQL("Change DataType")) {
				if (newtype == VarEnum.VT_EMPTY) SQL.wpNonResponse(@"UPDATE [opcdatapoint] SET
					[forcetype] = NULL WHERE [id_opcdatapoint] = {0}", iditem);
				else SQL.wpNonResponse(@"UPDATE [opcdatapoint] SET
					[forcetype] = ""{0}"" WHERE [id_opcdatapoint] = {1}", PVTEnum.ToString(newtype), iditem);
			}
			if (!TheGroup[TheItem.Server][TheItem.Group].Read(new int[] { TheItem.Hsrv },
				TransferId.getNew(), out cI, out arrErr)) {
				eventLog.Write("Datentyp konnte nicht gelesen werden");
			}
			return arrErr[0];
		}
		public void ReadOPC(int iditem) {
			if (!running) return;
			OPCItem TheItem = Server.Dictionaries.getItem(iditem);
			int[] arrErr;
			int cI;
			if (TheItem != null) {
				int taid = TransferId.getNew();
				wpDebug.Write("Read (TAID-{0}): Item '{1}'",
					taid, TheItem.OpcItemName);
				TheGroup[TheItem.Server][TheItem.Group].Read(new int[] { TheItem.Hsrv },
					taid, out cI, out arrErr);
			}
		}

		internal string moveOPCItem(OPCItem TheItem, int idnewgroup) {
			int[] arrErr;
			TheGroup[TheItem.Server][TheItem.Group].RemoveItems(new int[] {TheItem.Hsrv}, out arrErr);
			Hsrv[TheItem.Server][TheItem.Group].Remove(TheItem.Hclt);
			TheItemConnect(TheItem.Server, idnewgroup, TheItem.Hclt, TheItem.OpcItemName, TheItem.DBType);
			using(SQL SQL = new SQL("Move Item to Group")) {
				SQL.wpNonResponse(@"UPDATE [opcdatapoint] SET
					[id_opcgroup] = {0} WHERE [id_opcdatapoint] = {1}", idnewgroup, TheItem.Hclt);
			}
			return "S_OK";
		}
		private class CheckOpcServerState {
			private volatile bool _doStop;
			private int _counter;
			private int _maxCounter;
			private static Logger eventLog = new Logger(wpEventLog.OPCDataServer);
			public CheckOpcServerState() {
				_doStop = false;
				_counter = 0;
				_maxCounter = 600;
			}
			public void doWork(object ServerList) {
				Dictionary<int, PGAOPCServer> TheServer = (Dictionary<int, PGAOPCServer>)ServerList;
				while (!_doStop) {
					if (++_counter > _maxCounter) {
						CheckServer(TheServer);
						_counter = 0;
					} else {
						Thread.Sleep(1000);
					}
				}
			}
			public void doStop() {
				_doStop = true;
			}
			private void CheckServer(Dictionary<int, PGAOPCServer> TheServer) {
				string aktServername = "";
				int aktServerid = 0;
				Stopwatch watch = new Stopwatch();
				watch.Start();
				try {
					SERVERSTATUS ss;
					foreach (KeyValuePair<int, PGAOPCServer> os in TheServer) {
						aktServername = os.Value.Name;
						aktServerid = os.Value.Id;
						wpDebug.Write("Try to Connect Server {0}", aktServername);
						if (os.Value.Active) {
							os.Value.GetStatus(out ss);
							if (ss == null || ss.eServerState != OPCSERVERSTATE.OPC_STATUS_RUNNING) {
								eventLog.Write(String.Format("{0}: connection lost???", os.Value.Name));
								if (ss == null) {
									//Program.MainProg.POPCClient.disconnect(aktServerid);
									Thread.Sleep(2000);
									Program.MainProg.wpOPCClient.connect(aktServerid);
								}
							}
						} else {
							wpDebug.Write("Server {0} deactivated", aktServername);
						}
					}
				} catch (Exception) {
					eventLog.Write(EventLogEntryType.Warning,
						String.Format("OPC Server '{0}': lost Connection\r\nRetry to connect evry {1} Seconds",
						aktServername,
						_maxCounter));
					//Program.MainProg.POPCClient.disconnect(aktServerid);
					Thread.Sleep(2000);
					Program.MainProg.wpOPCClient.connect(aktServerid);
				}
				watch.Stop();
				wpDebug.Write("Dauer: {0}, getSERVERSTATUS", watch.Elapsed);
			}
		}
	}
}
/** @} */
