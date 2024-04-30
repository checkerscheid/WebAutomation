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
//# Revision     : $Rev:: 94                                                      $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: WebCom.cs 94 2024-04-30 05:57:33Z                        $ #
//#                                                                                 #
//###################################################################################
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
	public class WebCom {
		/// <summary></summary>
		private Logger eventLog;
		/// <summary></summary>
		private TcpListener WebComListener;
		/// <summary></summary>
		private Thread WebComServer;
		/// <summary></summary>
		private UTF8Encoding encoder = new UTF8Encoding();
		/// <summary></summary>
		private bool isFinished;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="globalServer"></param>
		public WebCom() {
			init();
		}
		/// <summary></summary>
		private int WatchDogByte;
		/// <summary>
		/// 
		/// </summary>
		private void init() {
			isFinished = false;
			WatchDogByte = 1;
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls; // | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
			eventLog = new Logger(wpEventLog.Com);
			WebComListener = new TcpListener(IPAddress.Any, Ini.getInt("TCP", "Port"));
			WebComServer = new Thread(new ThreadStart(TCP_Listener));
			WebComServer.Name = "WebComServer";
			WebComServer.Start();
			wpDebug.Write("WebCom auf Port {0} gemappt", Ini.getInt("TCP", "Port"));
		}
		/// <summary>
		/// 
		/// </summary>
		public void finished() {
			WebComListener.Stop();
			WebComListener = null;
			isFinished = true;
			WebComServer.Join(1500);
			eventLog.Write(String.Format("{0} gestoppt", WebComServer.Name));
		}
		/// <summary>
		/// 
		/// </summary>
		private void TCP_Listener() {
			try {
				WebComListener.Start();
				eventLog.Write(String.Format("{0} gestartet", WebComServer.Name));
				do {
					if (!WebComListener.Pending()) {
						Thread.Sleep(250);
						continue;
					}
					TcpClient Pclient = WebComListener.AcceptTcpClient();
					Thread ClientThread = new Thread(new ParameterizedThreadStart(TCP_HandleClient));
					ClientThread.Name = "WebcomHandleClient";
					ClientThread.Start(Pclient);
				} while (!isFinished);
			} catch(Exception ex) {
				eventLog.WriteError(ex);
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="client"></param>
		private void TCP_HandleClient(object client) {
			TcpClient tcpClient = (TcpClient)client;
			try {
				string s_message = "";
				NetworkStream clientStream = tcpClient.GetStream();
				byte[] message = new byte[tcpClient.ReceiveBufferSize];
				int bytesRead = 0;
				do {
					bytesRead = clientStream.Read(message, bytesRead, (int)tcpClient.ReceiveBufferSize);
					s_message += encoder.GetString(message, 0, bytesRead);
				} while (clientStream.DataAvailable);
				if (!isFinished) {
					byte[] answer = getAnswer(s_message);
					clientStream.Write(answer, 0, answer.Length);
					clientStream.Flush();
					clientStream.Close();
				}
			} catch (Exception ex) {
				eventLog.WriteError(ex, tcpClient.ToString());
			} finally {
				tcpClient.Close();
			}
		}
		/// <summary>
		/// 
		/// </summary>
		private static class wpBefehl {
			/// <summary></summary>
			public const string cHello = "Hello Server";
			public const string cVersion = "getVersion";

			public const string cActiveDP = "ActiveDP";
			public const string cActiveDPextended = "ActiveDPextended";
			public const string cSystem = "ActiveSystem";

			public const string cOPCServer = "BrowseOPCServer";
			public const string cOPCGroup = "BrowseOPCGroup";
			public const string cOPCSubGroup = "BrowseOPCSubGroup";
			public const string cOPCItem = "BrowseOPCItem";

			public const string cOPCServerDetails = "OPCServerDetails";
			public const string cChangeOPCServerWriteLevel = "ChangeOPCServerWriteLevel";
			public const string cOPCGroupDetails = "OPCGroupDetails";
			public const string cOPCGroupActive = "OPCGroupActive";
			public const string cChangeOPCGroupWriteLevel = "ChangeOPCGroupWriteLevel";
			public const string cChangeOPCItemType = "ChangeOPCItemType";
			public const string cChangeOPCItemWriteLevel = "ChangeOPCItemWriteLevel";
			public const string cRenameOPCServer = "RenameOPCServer";
			public const string cRenameOPCGroup = "RenameOPCGroup";
			public const string cActivateServer = "ActivateServer";
			public const string cActivateGroup = "ActivateGroup";
			public const string cActivateItems = "ActivateItems";
			public const string cAddOPCGroup = "AddGroup";
			public const string cRemoveOPCServer = "RemoveOPCServer";
			public const string cRemoveOPCGroup = "RemoveOPCGroup";
			public const string cRemoveOPCItem = "RemoveOPCItem";
			public const string cMoveOPCItem = "MoveOPCItem";

			public const string cAlarms = "ActiveAlarms";
			public const string cQuitAlarm = "QuitAlarm";
			public const string cQuitAlarms = "QuitAlarms";
			public const string cUpdateAlarm = "UpdateAlarm";
			public const string cUpdateAlarms = "UpdateAlarms";
			public const string cDeleteAlarm = "DeleteAlarm";
			public const string cDeleteAlarms = "DeleteAlarms";
			public const string cUpdateAlarmGroups = "UpdateAlarmGroups";

			public const string cUpdateMail = "UpdateMail";
			public const string cCalendarRenew = "CalendarRenew";
			public const string cForceSceneRenew = "ForceSceneRenew";
			public const string cSaveNewTrend = "SaveNewTrend";
			public const string cUpdateTrend = "UpdateTrend";
			public const string cDeleteTrend = "DeleteTrend";
			public const string cUpdateTrendIntervall = "UpdateTrendIntervall";
			public const string cActivateTrend = "ActivateTrend";
			public const string cDeactivateTrend = "DeactivateTrend";
			public const string cUpdateTrendMaxEntries = "UpdateTrendMaxEntries";
			public const string cUpdateTrendMaxDays = "UpdateTrendMaxDays";
			public const string cUpdateRouter = "UpdateRouter";

			public const string cWrite = "WriteDP";
			public const string cWriteMulti = "WriteMultiDP";
			public const string cWriteScene = "WriteSceneDP";

			public const string cForceMqttUpdate = "ForceMqttUpdate";
			public const string cSetBrowseMqtt = "setBrowseMqtt";
			public const string cUnsetBrowseMqtt = "unsetBrowseMqtt";
			public const string cGetBrowseMqtt = "getBrowseMqtt";
			public const string cGetAllD1MiniSettings = "getAllD1MiniSettings";
			public const string cGetD1MiniStatus = "getD1MiniStatus";
			public const string cSetD1MiniCmd = "SetD1MiniCmd";
			public const string cStartD1MiniSearch = "StartD1MiniSearch";
			public const string cAddD1Mini = "AddD1Mini";
			public const string cDeleteD1Mini = "DeleteD1Mini";
			public const string cGetShellyStatus = "GetShellyStatus";

			public const string cReadItem = "ReadItem";
			public const string cReadEvent = "ReadEvent";

			public const string cChangeWartung = "changeWartung";
			public const string cReloadSettings = "ReloadSettings"; // cfg from SQL
			public const string cGetDebug = "wpGetDebug";
			public const string cSetDebug = "wpSetDebug";
			/// <summary>
			/// 
			/// </summary>
			/// <param name="text"></param>
			/// <returns></returns>
			public static string[] getBefehl(string text) {
				string[] returns = new string[2];
				Regex rBefehl = new Regex(@"^\{(.*)\}");
				Regex rParam = new Regex(@"<(.*)>");
				string[] a_befehl = rBefehl.Split(text);
				string[] a_param = rParam.Split(text);
				returns[0] = a_befehl.Length > 1 ? a_befehl[1] : "undefined";
				if (a_param.Length > 1) returns[1] = a_param[1];
				return returns;
			}
			/// <summary>
			/// 
			/// </summary>
			/// <param name="param"></param>
			/// <returns></returns>
			public static string[] getParam(string param) {
				Regex rParam = new Regex(@"%~%");
				if (param != null)
					return rParam.Split(param);
				else
					return null;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		private byte[] getAnswer(string text) {
			string returns = "{ERROR=undefined command}";
			string[] s_befehl = wpBefehl.getBefehl(text);
			string[] param;
			int outint;
			int outint2;
			switch (s_befehl[0]) {
				case wpBefehl.cHello:
					returns = "{Message=Hello PGA Client}";
					break;
				case wpBefehl.cVersion:
					string[] pVersion = Application.ProductVersion.Split('.');
					returns = String.Format("{0}.{1} Build {2}", pVersion[0], pVersion[1], Program.subversion);
					break;
				case wpBefehl.cActiveDP:
					returns = getActiveDP(wpBefehl.getParam(s_befehl[1]));
					break;
				case wpBefehl.cActiveDPextended:
					returns = getActiveDPextended(wpBefehl.getParam(s_befehl[1]));
					break;
				case wpBefehl.cSystem:
					returns = getActiveSystem();
					break;
				case wpBefehl.cWrite:
					returns = writeDP(wpBefehl.getParam(s_befehl[1]));
					break;
				case wpBefehl.cWriteMulti:
					returns = writeMultiDP(wpBefehl.getParam(s_befehl[1]));
					break;
				case wpBefehl.cWriteScene:
					param = wpBefehl.getParam(s_befehl[1]);
					if (Int32.TryParse(param[1], out outint)) {
						returns = writeSceneDP(outint, param[0]);
					} else {
						returns = "{ERROR=Szene nicht gefunden}";
					}
					break;
				case wpBefehl.cForceMqttUpdate:
					returns = ForceMqttUpdate();
					break;
				case wpBefehl.cSetBrowseMqtt:
					_ = Program.MainProg.wpMQTTClient.setBrowseTopics();
					returns = "S_OK";
					break;
				case wpBefehl.cUnsetBrowseMqtt:
					_ = Program.MainProg.wpMQTTClient.unsetBrowseTopics();
					returns = "S_OK";
					break;
				case wpBefehl.cGetBrowseMqtt:
					returns = JsonConvert.SerializeObject(Program.MainProg.wpMQTTClient.ServerTopics);
					break;
				case wpBefehl.cGetAllD1MiniSettings:
					returns = D1MiniServer.getJson();
					break;
				case wpBefehl.cGetD1MiniStatus:
					returns = D1MiniServer.getJsonStatus(s_befehl[1]);
					break;
				case wpBefehl.cSetD1MiniCmd:
					returns = "S_ERROR";
					param = wpBefehl.getParam(s_befehl[1]);
					D1MiniDevice d1md = D1MiniServer.get(param[0]);
					if(d1md != null) {
						D1MiniDevice.cmdList cL = new D1MiniDevice.cmdList(param[1]);
						if(d1md.sendCmd(cL)) returns = "S_OK";
					}
					break;
				case wpBefehl.cStartD1MiniSearch:
					returns = D1MiniServer.startSearch();
					break;
				case wpBefehl.cAddD1Mini:
					param = wpBefehl.getParam(s_befehl[1]);
					if(Int32.TryParse(param[0], out outint)) {
						D1MiniServer.addD1Mini(outint);
						returns = "S_OK";
					}
					break;
				case wpBefehl.cDeleteD1Mini:
					param = wpBefehl.getParam(s_befehl[1]);
					for(int i = 0; i < param.Length; i++) {
						if(Int32.TryParse(param[i], out outint)) {
							D1MiniServer.removeD1Mini(outint);
						}
					}
					returns = "{\"erg\":\"S_OK\"}";
					break;
				case wpBefehl.cGetShellyStatus:
					returns = ShellyServer.getAllStatus();
					break;
				case wpBefehl.cReadItem:
					param = wpBefehl.getParam(s_befehl[1]);
					if(Int32.TryParse(param[1], out outint)) {
						Program.MainProg.wpOPCClient.ReadOPC(outint);
						returns = "S_OK";
					} else {
						returns = "{ERROR=Szene nicht gefunden}";
					}
					break;
				case wpBefehl.cOPCServer:
					if (s_befehl[1] != null) {
						returns = OPCClient.getAllOPCServer(s_befehl[1]);
					} else {
						returns = OPCClient.getAllOPCServer();
					}
					break;
				case wpBefehl.cOPCGroup:
					param = wpBefehl.getParam(s_befehl[1]);
					try {
						if (param.Length == 2 && param[0] != null && param[1] != null) {
							Program.MainProg.wpOPCClient.DoBrowse(param[0], param[1]);
							returns = Program.MainProg.wpOPCClient.OPCgebrowsed;
						} else if (param.Length == 1 && param[0] != null) {
							Program.MainProg.wpOPCClient.DoBrowse(param[0]);
							returns = Program.MainProg.wpOPCClient.OPCgebrowsed;
						} else {
							returns = "{ERROR=undefined command}";
						}
					} catch(Exception ex) {
						returns = String.Format("ERROR={0}", ex.Message);
					}
					break;
				case wpBefehl.cOPCSubGroup:
					param = wpBefehl.getParam(s_befehl[1]);
					try {
						if(param.Length == 3 && param[0] != null && param[1] != null) {
							Program.MainProg.wpOPCClient.DoBrowse(param[0], param[1], param[2]);
							returns = Program.MainProg.wpOPCClient.OPCgebrowsed;
						} else {
							returns = "{ERROR=undefined command}";
						}
					} catch(Exception ex) {
						returns = String.Format("ERROR={0}", ex.Message);
					}
					break;
				case wpBefehl.cOPCItem:
					param = wpBefehl.getParam(s_befehl[1]);
					if (param.Length == 3 && param[0] != null && param[1] != null && param[2] != null) {
						returns = Program.MainProg.wpOPCClient.getItems(param[0], param[1], param[2]);
					} else {
						returns = Program.MainProg.wpOPCClient.getItems(param[0], param[1]);
					}
					break;
				case wpBefehl.cOPCServerDetails:
					if (Int32.TryParse(wpBefehl.getParam(s_befehl[1])[0], out outint)) {
						returns = Program.MainProg.wpOPCClient.GetOPCServerDetails(outint);
					} else {
						returns = "{ERROR=OPC Server nicht gefunden}";
					}
					break;
				case wpBefehl.cChangeOPCServerWriteLevel:
					param = wpBefehl.getParam(s_befehl[1]);
					returns = "S_OK";
					if(Int32.TryParse(param[0], out outint)) {
						WriteLevel.AddWriteLevel(outint);
					} else {
						returns = "{ERROR=OPC Server nicht gefunden}";
					}
					break;
				case wpBefehl.cOPCGroupDetails:
					if (Int32.TryParse(wpBefehl.getParam(s_befehl[1])[0], out outint)) {
						returns = Program.MainProg.wpOPCClient.GetOPCGroupDetails(outint);
					} else {
						returns = "{ERROR=OPC Gruppe nicht gefunden}";
					}
					break;
				case wpBefehl.cOPCGroupActive:
					if (Int32.TryParse(wpBefehl.getParam(s_befehl[1])[0], out outint)) {
						returns = Program.MainProg.wpOPCClient.ChangeOPCGroupState(outint);
					} else {
						returns = "{ERROR=OPC Gruppe nicht gefunden}";
					}
					break;
				case wpBefehl.cChangeOPCGroupWriteLevel:
					param = wpBefehl.getParam(s_befehl[1]);
					returns = "S_OK";
					if (Int32.TryParse(param[0], out outint)) {
						WriteLevel.AddGroupWriteLevel(outint);
					} else {
						returns = "{ERROR=OPC Server nicht gefunden}";
					}
					break;
				case wpBefehl.cChangeOPCItemType:
					param = wpBefehl.getParam(s_befehl[1]);
					returns = "S_OK";
					if (Int32.TryParse(param[0], out outint)) {
						if (Program.MainProg.wpOPCClient.ChangeOPCItemType(outint, PVTEnum.get(param[1])) != 0) {
							returns = "Hat nicht geklappt";
						}
					}
					break;
				case wpBefehl.cChangeOPCItemWriteLevel:
					param = wpBefehl.getParam(s_befehl[1]);
					returns = "S_OK";
					if (Int32.TryParse(param[0], out outint)) {
						WriteLevel.AddItemWriteLevel(outint);
					} else {
						returns = "{ERROR=OPC Server nicht gefunden}";
					}
					break;
				case wpBefehl.cRenameOPCServer:
					param = wpBefehl.getParam(s_befehl[1]);
					if (Int32.TryParse(param[0], out outint)) {
						Program.MainProg.wpOPCClient.ChangeOPCServerName(outint, param[1]);
						returns = "S_OK";
					} else {
						returns = "{ERROR=OPC Server nicht gefunden}";
					}
					break;
				case wpBefehl.cRenameOPCGroup:
					param = wpBefehl.getParam(s_befehl[1]);
					if (Int32.TryParse(param[0], out outint)) {
						Program.MainProg.wpOPCClient.renameOPCGroup(outint, param[1]);
						returns = "S_OK";
					} else {
						returns = "{ERROR=OPC Gruppe nicht gefunden}";
					}
					break;
				case wpBefehl.cActivateServer:
					if (Int32.TryParse(wpBefehl.getParam(s_befehl[1])[0], out outint)) {
						returns = Program.MainProg.wpOPCClient.newOPCServer(outint);
					} else {
						returns = "Fehler";
					}
					break;
				case wpBefehl.cActivateGroup:
					if (Int32.TryParse(wpBefehl.getParam(s_befehl[1])[0], out outint)) {
						returns = Program.MainProg.wpOPCClient.newOPCGroup(outint);
					} else {
						returns = "Fehler";
					}
					break;
				case wpBefehl.cActivateItems:
					if (Int32.TryParse(wpBefehl.getParam(s_befehl[1])[0], out outint)) {
						returns = Program.MainProg.wpOPCClient.newOPCItems(outint);
					} else {
						returns = "Fehler";
					}
					break;
				case wpBefehl.cAddOPCGroup:
					if(Int32.TryParse(wpBefehl.getParam(s_befehl[1])[0], out outint)) {
						returns = Program.MainProg.wpOPCClient.newOPCGroup(outint);
					} else {
						returns = "Fehler";
					}
					break;
				case wpBefehl.cRemoveOPCServer:
					if (Int32.TryParse(wpBefehl.getParam(s_befehl[1])[0], out outint)) {
						returns = Program.MainProg.wpOPCClient.removeOPCServer(outint);
					} else {
						returns = "Fehler";
					}
					break;
				case wpBefehl.cRemoveOPCGroup:
					if (Int32.TryParse(wpBefehl.getParam(s_befehl[1])[0], out outint)) {
						returns = Program.MainProg.wpOPCClient.removeOPCGroup(outint);
					} else {
						returns = "Fehler";
					}
					break;
				case wpBefehl.cRemoveOPCItem:
					int[] Items = Arrays.StringArrayToIntArray(wpBefehl.getParam(s_befehl[1]));
					returns = Program.MainProg.wpOPCClient.removeOPCItems(Items);
					break;
				case wpBefehl.cMoveOPCItem:
					returns = "keine gültigen Parameter";
					param = wpBefehl.getParam(s_befehl[1]);
					if (Int32.TryParse(param[0], out outint) &&
						Int32.TryParse(param[1], out outint2)) {
						OPCItem TheItem = Server.Dictionaries.getItem(outint);
						if(TheItem != null) {
							returns = Program.MainProg.wpOPCClient.moveOPCItem(TheItem, outint2);
						}
					}
					// @ToDo
					//returns = "keine gültigen Parameter";
					break;
				case wpBefehl.cAlarms:
					returns = getActiveAlarms();
					break;
				case wpBefehl.cQuitAlarm:
					param = wpBefehl.getParam(s_befehl[1]);
					if (Int32.TryParse(param[1], out outint)) {
						returns = quitAlarm(param[0], outint, param[2]);
					} else {
						returns = "{ERROR: ID: " + param[1] + " DOSNT EXISTS}";
					}
					break;
				case wpBefehl.cQuitAlarms:
					param = wpBefehl.getParam(s_befehl[1]);
					returns = quitAlarms(param);
					break;
				case wpBefehl.cUpdateAlarm:
					param = wpBefehl.getParam(s_befehl[1]);
					if (Int32.TryParse(param[0], out outint)) {
						returns = UpdateAlarm(outint);
					} else {
						returns = "{ERROR: ID: " + param[1] + " DOSNT EXISTS}";
					}
					break;
				case wpBefehl.cUpdateAlarms:
					param = wpBefehl.getParam(s_befehl[1]);
					returns = UpdateAlarms(param);
					break;
				case wpBefehl.cDeleteAlarm:
					param = wpBefehl.getParam(s_befehl[1]);
					if(Int32.TryParse(param[0], out outint)) {
						using (SQL SQL = new SQL("Delete Alarm")) {
							wpAlarm tmpAlarm = Program.MainProg.getAlarmFromAlarmid(outint);
							// in keiner Gruppe
							if(tmpAlarm != null) {
								int TheItemid = tmpAlarm.DpID;
								Program.MainProg.setAlarm(TheItemid, null);
							}
							SQL.wpNonResponse("DELETE FROM [alarm] WHERE [id_alarm] = {0}", outint);
						}
					}
					returns = "S_OK";
					break;
				case wpBefehl.cDeleteAlarms:
					param = wpBefehl.getParam(s_befehl[1]);
					for(int i = 0; i < param.Length; i++) {
						if(Int32.TryParse(param[i], out outint)) {
							using (SQL SQL = new SQL("Delete Alarm")) {
								wpAlarm tmpAlarm = Program.MainProg.getAlarmFromAlarmid(outint);
								// in keiner Gruppe
								if (tmpAlarm != null) {
									int TheItemid = tmpAlarm.DpID;
									Program.MainProg.setAlarm(TheItemid, null);
								}
								SQL.wpNonResponse("DELETE FROM [alarm] WHERE [id_alarm] = {0}", outint);
							}
						}
					}
					returns = "S_OK";
					break;
				case wpBefehl.cUpdateAlarmGroups:
					returns = Alarm.updateAlarmGroups();
					break;
				case wpBefehl.cUpdateMail:
					returns = Program.MainProg.SetRecipientRequired();
					break;
				case wpBefehl.cCalendarRenew:
					param = wpBefehl.getParam(s_befehl[1]);
					if (Int32.TryParse(param[0], out outint)) {
						returns = Program.MainProg.CalDav.renewCalendar(outint);
					} else {
						returns = "{ERROR:id not found}";
					}
					break;
#region Trend
				case wpBefehl.cSaveNewTrend:
					param = wpBefehl.getParam(s_befehl[1]);
					if (Int32.TryParse(param[0], out outint)) {
						Trends.AddTrend(outint);
						returns = "{\"erg\":\"S_OK\"}";
					} else {
						returns = "{\"erg\":\"ERROR\",\"message\":\"id not found\"}";
					}
					break;
				case wpBefehl.cUpdateTrend:
					// Version3.0: UpdateTrend
					param = wpBefehl.getParam(s_befehl[1]);
					if(Int32.TryParse(param[0], out outint)) {
						int dp = Trends.Get(outint).IdDP;
						Trends.RemoveTrend(outint);
						Trends.AddTrend(dp);
						returns = "{\"erg\":\"S_OK\"}";
					}
					break;
				case wpBefehl.cDeleteTrend:
					// Version3.0Test: DeleteTrend
					param = wpBefehl.getParam(s_befehl[1]);
					for (int i = 0; i < param.Length; i++) {
						if (Int32.TryParse(param[i], out outint)) {
							Trends.RemoveTrend(outint);
						}
					}
					returns = "{\"erg\":\"S_OK\"}";
					break;
				case wpBefehl.cUpdateTrendIntervall:
					param = wpBefehl.getParam(s_befehl[1]);
					if(Int32.TryParse(param[0], out outint2)) {
						for(int i = 1; i < param.Length; i++) {
							if(Int32.TryParse(param[i], out outint)) {
								Trends.Get(outint).Intervall = outint2;
							}
						}
					}
					returns = "{\"erg\":\"S_OK\"}";
					break;
				case wpBefehl.cActivateTrend:
					param = wpBefehl.getParam(s_befehl[1]);
					for(int i = 0; i < param.Length; i++) {
						if(Int32.TryParse(param[i], out outint)) {
							Trends.Get(outint).Activate();
						}
					}
					returns = "{\"erg\":\"S_OK\"}";
					break;
				case wpBefehl.cDeactivateTrend:
					param = wpBefehl.getParam(s_befehl[1]);
					for(int i = 0; i < param.Length; i++) {
						if(Int32.TryParse(param[i], out outint)) {
							Trends.Get(outint).Deactivate();
						}
					}
					returns = "{\"erg\":\"S_OK\"}";
					break;
				case wpBefehl.cUpdateTrendMaxEntries:
					param = wpBefehl.getParam(s_befehl[1]);
					if(Int32.TryParse(param[0], out outint2)) {
						for(int i = 1; i < param.Length; i++) {
							if(Int32.TryParse(param[i], out outint)) {
								Trends.Get(outint).MaxEntries = outint2;
							}
						}
					}
					returns = "{\"erg\":\"S_OK\"}";
					break;
				case wpBefehl.cUpdateTrendMaxDays:
					param = wpBefehl.getParam(s_befehl[1]);
					if(Int32.TryParse(param[0], out outint2)) {
						for(int i = 1; i < param.Length; i++) {
							if(Int32.TryParse(param[i], out outint)) {
								Trends.Get(outint).MaxDays = outint2;
							}
						}
					}
					returns = "{\"erg\":\"S_OK\"}";
					break;
#endregion
				case wpBefehl.cUpdateRouter:
					param = wpBefehl.getParam(s_befehl[1]);
					if (Int32.TryParse(param[0], out outint)) {
						Router.UpdateRouter(outint);
					}
					returns = "{\"erg\":\"S_OK\"}";
					break;
				case wpBefehl.cReadEvent:
					param = wpBefehl.getParam(s_befehl[1]);
					try {
						if (param != null) returns = wpEventLog.readLog(param[0]);
						else returns = wpEventLog.readLog();
					} catch (Exception ex) {
						returns = "{ERROR=" + ex.Message + "}{TRACE=" + ex.StackTrace + "}";
						eventLog.WriteError(ex);
					}
					//PDebug.Write(returns);
					break;
				case wpBefehl.cReloadSettings:
					try {
						Ini.read();
						finished();
						init();
						eventLog.Write(EventLogEntryType.Warning, "Reload Settings");
						returns = "{ERROR=S_OK}";
					} catch (Exception ex) {
						returns = "{ERROR=" + ex.Message + "}{TRACE=" + ex.StackTrace + "}";
						eventLog.WriteError(ex);
					}
					break;
				case wpBefehl.cChangeWartung:
					Program.MainProg.wpWartung = !Program.MainProg.wpWartung;
					if(Program.MainProg.wpWartung)
						eventLog.Write(EventLogEntryType.Warning, "Wartung wurde aktiviert");
					else
						eventLog.Write("Wartung deaktiviert");
					returns = "{\"erg\":\"S_OK\"}";
					break;
				case wpBefehl.cGetDebug:
					returns = Program.MainProg.getDebugJson();
					break;
				case wpBefehl.cSetDebug:
					param = wpBefehl.getParam(s_befehl[1]);
					returns = Program.MainProg.setDebug(param);
					break;
				default:
					returns = "{ERROR=undefined command}";
					break;
			}
			//wpEventLog.Write(String.Format("{0} Server Antwort: {1}", WebComServer.Name, returns));
			if(returns == null)
				returns = "{ERROR=undefined command}";
			return encoder.GetBytes(returns);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="param"></param>
		/// <returns></returns>
		private string writeDP(string[] param) {
			int idDp;
			int writeLevel;
			string returns = "";
			try {
				if (Int32.TryParse(param[1], out idDp) &&
					Int32.TryParse(param[0], out writeLevel)) {
					Datapoint DP = Datapoints.Get(idDp);
					if(DP != null) {
						if(Datapoints.Get(idDp).WriteLevel <= writeLevel) {
							Datapoints.Get(idDp).writeValue(param[2]);
							returns = "S_OK";
						} else {
							returns = "Ihnen fehlt die Berechtigung zum schreiben.";
						}
					} else {
						returns = "ERROR: ID: " + param[1] + " DOSNT EXISTS";
					}
				} else {
					returns = "ERROR: ID: " + param[1] + " NOT PARSEABLE";
				}
			} catch(Exception ex) {
				returns = ex.Message;
			}
			return returns;
		}
		private string ForceMqttUpdate() {
			string returns = "S_ERROR";
			if(Program.MainProg.wpMQTTClient.forceMqttUpdate())
				returns = "S_OK";
			return returns;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="param"></param>
		/// <returns></returns>
		private string writeMultiDP(string[] param) {
			int outint;
			string returns = "";
			try {
				List<int> IDsToWrite = new List<int>();
				List<string> ValuesToWrite = new List<string>();
				for(int i = 1; i < param.Length; i = i + 2) {
					if (Int32.TryParse(param[i], out outint)) {
						OPCItem TheItem = Server.Dictionaries.getItem(outint);
						string korrekt = param[i + 1];
						IDsToWrite.Add(outint);
						ValuesToWrite.Add(korrekt);
					} else {
						returns += "ERROR: ID: " + param[i] + " DOSNT EXISTS<br />";
					}
				}
				returns += Program.MainProg.wpOPCClient.setValues(IDsToWrite.ToArray(), ValuesToWrite.ToArray(),
					TransferId.TransferNormalOP);
			} catch (Exception ex) {
				returns += ex.Message;
			}
			return returns;
		}
		private string writeSceneDP(int idscene, string user) {
			int level = 0;
			string[][] erg;
			string returns = "";
			user = user.ToLower();
			using (SQL sql = new SQL("get User Level")) {
				erg = sql.wpQuery(@"SELECT TOP 1 [g].[order]
					FROM [user] [u]
					INNER JOIN [usergroup] [g] ON [u].[id_usergroup] = [g].[id_usergroup]
					WHERE [login] = '{0}'", user);
			}
			if (erg.Length == 1 && erg[0].Length == 1 &&
				Int32.TryParse(erg[0][0], out level)) {
				List<int> ids = new List<int>();
				List<string> values = new List<string>();
				string log = "";
				OPCItem p;
				Dictionary<int, string> d = Scene.getScene(idscene);
				foreach (KeyValuePair<int, string> kvp in d) {
					p = Server.Dictionaries.getItem(kvp.Key);
					if (p != null /*&& p.WriteLevel <= level*/) {
						ids.Add(kvp.Key);
						values.Add(kvp.Value);
						log += String.Format("('{0}', '{1} (scene)', '{2:s}', '{3}', '{4}'),", user, p.OpcItemName, DateTime.Now, p.Value, kvp.Value);
					} else {
						eventLog.Write(EventLogEntryType.Warning, "keine Berechtigung zum schreiben");
					}
				}
				if (ids.Count > 0) {
					returns = Program.MainProg.wpOPCClient.setValues(ids.ToArray(),
						values.ToArray(), TransferId.TransferScene);
				}
				if (log.Length > 0) {
					using (SQL sql = new SQL("write Scene log")) {
						sql.wpNonResponse("INSERT INTO [useractivity] ([username], [datapoint], [writetime], [oldvalue], [newvalue]) VALUES {0}", log.Substring(0, log.Length - 1));
					}
				}
			}
			return returns;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="idAlarm"></param>
		/// <returns></returns>
		private string UpdateAlarm(int idAlarm) {
			try {
				using(SQL SQL = new SQL("Update Alarm")) {
					List<int> Hsrv = new List<int>();

					string[][] DBAlarms = SQL.wpQuery(@"
						SELECT
							[d].[id_opcdatapoint], [a].[name], [a].[text], [a].[link], [t].[name], [t].[autoquit],
							[g].[name], [d].[opcname], [c].[condition], [a].[min], [a].[max], [a].[delay],
							[ag1].[name], [ag2].[name], [ag3].[name], [ag4].[name], [ag5].[name]
						FROM [alarm] [a]
						INNER JOIN [alarmtype] [t] ON [a].[id_alarmtype] = [t].[id_alarmtype]
						INNER JOIN [alarmgroup] [g] ON [a].[id_alarmgroup] = [g].[id_alarmgroup]
						INNER JOIN [opcdatapoint] [d] ON [a].[id_opcdatapoint] = [d].[id_opcdatapoint]
						INNER JOIN [alarmcondition] [c] ON [a].[id_alarmcondition] = [c].[id_alarmcondition]
						LEFT JOIN [alarmgroups1] [ag1] ON [a].[id_alarmgroups1] = [ag1].[id_alarmgroups1]
						LEFT JOIN [alarmgroups2] [ag2] ON [a].[id_alarmgroups2] = [ag2].[id_alarmgroups2]
						LEFT JOIN [alarmgroups3] [ag3] ON [a].[id_alarmgroups3] = [ag3].[id_alarmgroups3]
						LEFT JOIN [alarmgroups4] [ag4] ON [a].[id_alarmgroups4] = [ag4].[id_alarmgroups4]
						LEFT JOIN [alarmgroups5] [ag5] ON [a].[id_alarmgroups5] = [ag5].[id_alarmgroups5]
						WHERE [a].[id_alarm] = " + idAlarm.ToString());
					wpAlarm TheAlarm = Program.MainProg.getAlarm(Int32.Parse(DBAlarms[0][0]));
					/// TODO: ohne Neustart neuen Alarm generieren
					if (TheAlarm != null) {
						TheAlarm.Alarmname = DBAlarms[0][1];
						TheAlarm.Alarmtext = DBAlarms[0][2];
						TheAlarm.Alarmlink = DBAlarms[0][3];
						TheAlarm.Alarmtype = DBAlarms[0][4];
						TheAlarm.Autoquit = DBAlarms[0][5] == "True";
						TheAlarm.Alarmgroup = DBAlarms[0][6];
						TheAlarm.Condition = DBAlarms[0][8];
						TheAlarm.Min = DBAlarms[0][9];
						if (TheAlarm.Condition == ">x<" || TheAlarm.Condition == "<x>")
							TheAlarm.Max = Int32.Parse(DBAlarms[0][10]);
						int delay;
						if (Int32.TryParse(DBAlarms[0][11], out delay)) {
							TheAlarm.updateDelay(delay);
						}
						TheAlarm.Alarmgroups1 = DBAlarms[0][12];
						TheAlarm.Alarmgroups2 = DBAlarms[0][13];
						TheAlarm.Alarmgroups3 = DBAlarms[0][14];
						TheAlarm.Alarmgroups4 = DBAlarms[0][15];
						TheAlarm.Alarmgroups5 = DBAlarms[0][16];
						TheAlarm.Inalarm = false;
						TheAlarm.AlarmUpdate = DateTime.Now;
						eventLog.Write(String.Format("Alarm update: {0} ({1})", DBAlarms[0][1], DBAlarms[0][0]));
					} else {
						eventLog.Write(EventLogEntryType.Warning,
							String.Format("Alarm wurde nicht gefunden: {0} ({1})", DBAlarms[0][1], DBAlarms[0][0]));
					}
				}
				return "S_OK";
			} catch(Exception ex) {
				eventLog.Write(EventLogEntryType.Error,
					String.Format("{0}\r\nTrace:\r\n{1}", ex.Message, ex.StackTrace));
				return ex.Message;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="idAlarms"></param>
		/// <returns></returns>
		private string UpdateAlarms(string[] idAlarms) {
			bool succeed = true;
			int outint;
			for(int i = 0; i < idAlarms.Length; i++) {
				if (Int32.TryParse(idAlarms[i], out outint)) {
					if(UpdateAlarm(outint) != "S_OK") succeed = false;
				} else {
					succeed = false;
				}
			}
			return succeed ? "S_OK" : "Fehler!";
		}
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		private string getActiveAlarms() {
			WatchDogByte += 1;
			if(WatchDogByte > 255) WatchDogByte = 1;
			DateTime Now = DateTime.Now;
			string returns = String.Format(
				"{{WatchDog={0}}}{{DateTime={1}}}{{Date={2}}}{{Time={3}}}{{Alarme=",
				WatchDogByte,
				Now.ToString("yyyy, M-1, d, H, m, s"),
				Now.ToString("dd.MM.yyyy"),
				Now.ToString("HH:mm:ss"));
			foreach (KeyValuePair<int, wpAlarm> TheAlarm in Program.MainProg.getActiveAlarms()) {
				returns += String.Format("{{{0}=", TheAlarm.Value.Idalarm);
				returns += String.Format(
					@"{{id={0}}}{{OpcName={1}}}{{Come={2}}}{{Gone={3}}}{{Quit={4}}}{{Type={5}}}
					{{Group={6}}}{{Name={7}}}{{Text={8}}}{{Link={9}}}{{AlarmUpdate={10}}}{11}{12}{13}{14}{15}}}",
					TheAlarm.Value.Idalarm,
					TheAlarm.Value.DpName,
					TheAlarm.Value.Come == wpAlarm.Default ? "-" : TheAlarm.Value.Come.ToString(),
					TheAlarm.Value.Gone == wpAlarm.Default ? "-" : TheAlarm.Value.Gone.ToString(),
					TheAlarm.Value.Quit == wpAlarm.Default ? "-" : TheAlarm.Value.Quit.ToString(),
					TheAlarm.Value.Alarmtype,
					TheAlarm.Value.Alarmgroup,
					TheAlarm.Value.Alarmname,
					TheAlarm.Value.Alarmtext,
					TheAlarm.Value.Alarmlink,
					TheAlarm.Value.AlarmUpdate.ToString(),
					Alarm.useAlarmGroup1 ? String.Format("{{AlarmGroup1={0}}}", TheAlarm.Value.Alarmgroups1) : "",
					Alarm.useAlarmGroup2 ? String.Format("{{AlarmGroup2={0}}}", TheAlarm.Value.Alarmgroups2) : "",
					Alarm.useAlarmGroup3 ? String.Format("{{AlarmGroup3={0}}}", TheAlarm.Value.Alarmgroups3) : "",
					Alarm.useAlarmGroup4 ? String.Format("{{AlarmGroup4={0}}}", TheAlarm.Value.Alarmgroups4) : "",
					Alarm.useAlarmGroup5 ? String.Format("{{AlarmGroup5={0}}}", TheAlarm.Value.Alarmgroups5) : "");
			}
			return String.Format("{0}}}{{Wartung={1}}}",
				returns,
				Program.MainProg.wpWartung ? "True" : "False");
			//return returns + "}";
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="user"></param>
		/// <param name="id"></param>
		/// <param name="quittext"></param>
		/// <returns></returns>
		private string quitAlarm(string user, int id, string quittext) {
			DateTime DateTimeNow = DateTime.Now;
			wpAlarm TheAlarm = Program.MainProg.getAlarmFromAlarmid(id);
			TheAlarm.AlarmUpdate = DateTimeNow;
			TheAlarm.Quit = DateTimeNow;
			TheAlarm.QuitFrom = user;
			TheAlarm.QuitText = quittext;
			TheAlarm.Isquit = true;
			using(SQL SQL = new SQL("Quit Alarm")) {
				SQL.wpNonResponse(@"UPDATE [alarmhistoric]
					SET [quit] = ""{0}"", [quitfrom] = ""{1}"", [quittext] = ""{2}""
					WHERE [id_alarm] = {3} AND [quit] IS NULL",
					DateTimeNow.ToString(SQL.DateTimeFormat),
					user,
					quittext,
					TheAlarm.Idalarm);
			}
			Program.MainProg.QuitToMail(TheAlarm);
			return "S_OK";
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="param"></param>
		/// <returns></returns>
		private string quitAlarms(string[] param) {
			// hier weiter machen
			DateTime DateTimeNow = DateTime.Now;
			wpAlarm TheAlarm;
			List<wpAlarm> TheAlarmList = new List<wpAlarm>();
			string toinsert = "";
			for(int i = 1; i < param.Length - 1; i++) {
				int alarmid;
				if (Int32.TryParse(param[i], out alarmid)) {
					TheAlarm = Program.MainProg.getAlarmFromAlarmid(alarmid);
					TheAlarmList.Add(TheAlarm);
					TheAlarm.AlarmUpdate = DateTimeNow;
					TheAlarm.Quit = DateTimeNow;
					TheAlarm.QuitFrom = param[0];
					TheAlarm.QuitText = param[param.Length - 1];
					TheAlarm.Isquit = true;
					toinsert += "[id_alarm] = " + alarmid + " OR ";
				}
			}
			using (SQL SQL = new SQL("Quit Alarm")) {
				SQL.wpNonResponse(@"UPDATE [alarmhistoric]
					SET [quit] = '{0}', [quitfrom] = '{1}', [quittext] = '{2}'
					WHERE ({3}) AND [quit] IS NULL",
					DateTimeNow.ToString(SQL.DateTimeFormat),
					param[0],
					param[param.Length - 1],
					toinsert.Substring(0, toinsert.Length - 4));
			}
			Program.MainProg.QuitsToMail(TheAlarmList);
			return "S_OK";
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="param"></param>
		/// <returns></returns>
		private string getActiveDP(string[] param) {
			if (param == null) return null;

			string returns = "{Datenpunkte=";
			for (int i = 0; i < param.Length; i++) {
				int parsedint;
				if(Int32.TryParse(param[i], out parsedint)) {
					OPCItem TheItem = Server.Dictionaries.getItem(parsedint);
					if (TheItem != null) {
						returns += String.Format("{{{0}={{Value={1}}}}}",
							param[i],
							(TheItem.DBType == VarEnum.VT_BSTR) ? "\"" + TheItem.Value + "\"" : TheItem.Value
						);
					} else {
						returns += String.Format("{{{0}={{Value={1}}}}}",
							param[i],
							"error"
						);
					}
				}
			}
			//return String.Format("{0}}}{{Wartung={1}}}", returns, Me.Wartung ? "True" : "False");
			return returns + "}";
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="param"></param>
		/// <returns></returns>
		private string getActiveDPextended(string[] param) {
			if (param == null) return null;

			string returns = "{Datenpunkte=";
			int DPid;
			for (int i = 0; i < param.Length; i++) {
				if (Int32.TryParse(param[i], out DPid)) {
					if (Server.Dictionaries.checkItem(DPid)) {
						OPCItem TheItem = Server.Dictionaries.getItem(DPid);
						returns += String.Format(@"{{{0}={{Value={1}}}{{TimeStamp={2}}}{{Quality={3}}}
							{{QualityString={4}}}{{Type={5}}}}}",
							param[i],
							(TheItem.DBType == VarEnum.VT_BSTR) ? "\"" + TheItem.Value + "\"" : TheItem.Value,
							TheItem.Lastupdate,
							TheItem.Quality,
							OPCQuality.get(TheItem.Quality),
							TheItem.DBType);
					}
				} else {
					eventLog.Write(EventLogEntryType.Warning,
						String.Format("OPC Item ID nicht korrekt: {0}", param[i]));
				}
			}
			return returns + "}";
		}
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		private string getActiveSystem() {
			WatchDogByte += 1;
			if (WatchDogByte > 255) WatchDogByte = 1;
			DateTime Now = DateTime.Now;
			int AlarmsCome = 0;
			int AlarmsGone = 0;
			int AlarmsQuit = 0;
			foreach (KeyValuePair<int, wpAlarm> TheAlarm in Program.MainProg.getActiveAlarms()) {
				if (TheAlarm.Value.Come != wpAlarm.Default && TheAlarm.Value.Gone == wpAlarm.Default)
					AlarmsCome++;
				if (TheAlarm.Value.Gone != wpAlarm.Default) AlarmsGone++;
				if (TheAlarm.Value.Quit == wpAlarm.Default) AlarmsQuit++;
			}
			return String.Format(@"{{WatchDog={0}}}{{DateTime={1}}}{{Date={2}}}{{Time={3}}}
				{{AlarmsCome={4}}}{{AlarmsGone={5}}}{{AlarmsQuit={6}}}",
				WatchDogByte,
				Now.ToString("MM, dd, yyyy HH:mm:ss"),
				Now.ToString("dd.MM.yyyy"),
				Now.ToString("HH:mm:ss"),
				AlarmsCome,
				AlarmsGone,
				AlarmsQuit);
		}
	}
}
/** @} */
