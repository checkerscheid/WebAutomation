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
//# Revision     : $Rev:: 204                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: WebCom.cs 204 2025-05-01 20:19:13Z                       $ #
//#                                                                                 #
//###################################################################################
using FreakaZone.Libraries.wpCommen;
using FreakaZone.Libraries.wpEventLog;
using FreakaZone.Libraries.wpIniFile;
using FreakaZone.Libraries.wpSamsungRemote;
using FreakaZone.Libraries.wpSQL;
using FreakaZone.Libraries.wpSQL.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebAutomation.PlugIns;
using static FreakaZone.Libraries.wpEventLog.Logger;
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
			Debug.Write(MethodInfo.GetCurrentMethod(), "WebCom init");
			isFinished = false;
			WatchDogByte = 1;
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls; // | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
			eventLog = new Logger(Logger.ESource.WEBcom);
			WebComListener = new TcpListener(IPAddress.Any, IniFile.getInt("TCP", "Port"));
			WebComServer = new Thread(new ThreadStart(TCP_Listener));
			WebComServer.Name = "WebComServer";
			Debug.Write(MethodInfo.GetCurrentMethod(), "WebCom gestartet, auf Port {0} gemappt", IniFile.getInt("TCP", "Port"));
			WebComServer.Start();
		}
		/// <summary>
		/// 
		/// </summary>
		public void finished() {
			if(WebComListener != null)
				WebComListener.Stop();
			WebComListener = null;
			isFinished = true;
			if(WebComServer != null)
				WebComServer.Join(1500);
			eventLog.Write(MethodInfo.GetCurrentMethod(), String.Format("{0} gestoppt", WebComServer.Name));
		}
		/// <summary>
		/// 
		/// </summary>
		private static class wpBefehl {
			/// <summary></summary>
			#region Server
			public const string cHello = "Hello Server";
			public const string cVersion = "getVersion";
			public const string cChangeWartung = "changeWartung";
			public const string cReloadSettings = "ReloadSettings"; // cfg from SQL
			public const string cReadEvent = "ReadEvent";
			public const string cGetDebug = "wpGetDebug";
			public const string cSetDebug = "wpSetDebug";
			public const string cHistoryCleaner = "HistoryCleaner";
			public const string cSetSummer = "SetSummer";
			// SQL TEST
			public const string cInsertDummy = "InsertDummy";
			public const string cSelectScene = "SelectScene";
			// SQL TEST END
			#endregion

			#region Datapoints
			public const string cActiveDP = "ActiveDP";
			public const string cActiveDPextended = "ActiveDPextended";
			public const string cSystem = "ActiveSystem";
			public const string cWrite = "WriteDP";
			public const string cWriteMulti = "WriteMultiDP";
			#endregion

			#region OPC
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
			public const string cReadItem = "ReadItem";
			#endregion

			#region Alarm
			public const string cAlarms = "ActiveAlarms";
			public const string cQuitAlarm = "QuitAlarm";
			public const string cQuitAlarms = "QuitAlarms";
			public const string cUpdateAlarm = "UpdateAlarm";
			public const string cUpdateAlarms = "UpdateAlarms";
			public const string cDeleteAlarm = "DeleteAlarm";
			public const string cDeleteAlarms = "DeleteAlarms";
			public const string cUpdateAlarmGroups = "UpdateAlarmGroups";
			public const string cUpdateMail = "UpdateMail";
			#endregion

			#region Trend
			public const string cSaveNewTrend = "SaveNewTrend";
			public const string cUpdateTrend = "UpdateTrend";
			public const string cDeleteTrend = "DeleteTrend";
			public const string cActivateTrend = "ActivateTrend";
			public const string cDeactivateTrend = "DeactivateTrend";
			public const string cUpdateTrendIntervall = "UpdateTrendIntervall";
			public const string cUpdateTrendMaxEntries = "UpdateTrendMaxEntries";
			public const string cUpdateTrendMaxDays = "UpdateTrendMaxDays";
			#endregion

			#region Calendar
			public const string cCalendarRenew = "CalendarRenew";
			#endregion

			#region Scene
			public const string cForceSceneRenew = "ForceSceneRenew";
			public const string cWriteScene = "WriteSceneDP";
			#endregion

			#region Router
			public const string cUpdateRouter = "UpdateRouter";
			#endregion

			#region MQTT
			public const string cPublishTopic = "publishTopic";
			public const string cSetBrowseMqtt = "setBrowseMqtt";
			public const string cUnsetBrowseMqtt = "unsetBrowseMqtt";
			public const string cGetBrowseMqtt = "getBrowseMqtt";
			#endregion

			#region ShellyAndD1Mini
			public const string cForceMqttUpdate = "ForceMqttUpdate";
			#endregion

			#region Shelly
			public const string cShellyMqttUpdate = "shellyMqttUpdate";
			public const string cGetShellyStatus = "GetShellyStatus";
			public const string cDeleteShelly = "DeleteShelly";
			#endregion

			#region D1Mini
			public const string cD1MiniMqttUpdate = "d1MiniMqttUpdate";
			public const string cGetAllD1MiniSettings = "getAllD1MiniSettings";
			public const string cGetD1MiniStatus = "getD1MiniStatus";
			public const string cGetAndSaveD1MiniStatus = "getAndSaveD1MiniStatus";
			public const string cGetD1MiniNeoPixelStatus = "getD1MiniNeoPixelStatus";
			public const string cSetD1MiniCmd = "SetD1MiniCmd";
			public const string cSetD1MiniUrlCmd = "SetD1MiniUrlCmd";
			public const string cStartD1MiniSearch = "StartD1MiniSearch";
			public const string cAddD1Mini = "AddD1Mini";
			public const string cRenewD1MiniActiveState = "RenewD1MiniActiveState";
			public const string cDeleteD1Mini = "DeleteD1Mini";
			public const string cGetD1MiniServer = "GetD1MiniServer";
			public const string cSetD1MiniServer = "SetD1MiniServer";
			#endregion

			#region Remote
			public const string cRemoteControl = "RemoteControl";
			#endregion

			#region Shopping
			public const string cSetProductChecked = "SetProductChecked";
			#endregion

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
				if(a_param.Length > 1)
					returns[1] = a_param[1];
				return returns;
			}
			/// <summary>
			/// 
			/// </summary>
			/// <param name="param"></param>
			/// <returns></returns>
			public static string[] getParam(string param) {
				Regex rParam = new Regex(@"%~%");
				if(param != null)
					return rParam.Split(param);
				else
					return null;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		private void TCP_Listener() {
			try {
				WebComListener.Start();
				eventLog.Write(MethodInfo.GetCurrentMethod(), String.Format("{0} gestartet", WebComServer.Name));
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
				eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="client"></param>
		private async void TCP_HandleClient(object client) {
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
					byte[] answer = await getAnswer(s_message);
					clientStream.Write(answer, 0, answer.Length);
					clientStream.Flush();
					clientStream.Close();
				}
			} catch (Exception ex) {
				eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex, tcpClient.ToString());
			} finally {
				tcpClient.Close();
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		private async Task<byte[]> getAnswer(string text) {
			string returns = new ret { erg = ret.ERROR, message = "undefined command" }.ToString();
			string[] s_befehl = wpBefehl.getBefehl(text);
			string[] param;
			int outint;
			int outint2;
			D1Mini d1md;
			switch (s_befehl[0]) {
				case wpBefehl.cHello:
					returns = new ret { erg = ret.OK, message = "Hello FreakaZone Client" }.ToString();
					break;
				case wpBefehl.cVersion:
					string[] pVersion = Application.ProductVersion.Split('.');
					returns = String.Format("{0}.{1} Build {2}", pVersion[0], pVersion[1], Program.subversion);
					break;
				case wpBefehl.cInsertDummy:
					using(Database Sql = new Database("Insert Test Dummy")) {
						returns = Sql.Insert<TableTv>(new TableTv("Dummy1", "12", 51, "h", "67", true)).ToString();
					}
					break;
				case wpBefehl.cSelectScene:
					using(Database Sql = new Database("Select Scene")) {
						returns = Sql.SelectJoin<TableScene, TableSceneValue>().ToString();
					}
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
						returns = new ret { erg = ret.ERROR, message = "Szene nicht gefunden" }.ToString();
					}
					break;
				case wpBefehl.cPublishTopic:
					param = wpBefehl.getParam(s_befehl[1]);
					returns = await Program.MainProg.wpMQTTClient.setValue(param[0], param[1]);
					break;
				case wpBefehl.cForceMqttUpdate:
					returns = ForceMqttUpdate();
					break;
				case wpBefehl.cShellyMqttUpdate:
					returns = ShellyMqttUpdate();
					break;
				case wpBefehl.cD1MiniMqttUpdate:
					returns = D1MiniMqttUpdate();
					break;
				case wpBefehl.cSetBrowseMqtt:
					returns = await Program.MainProg.wpMQTTClient.setBrowseTopics();
					break;
				case wpBefehl.cUnsetBrowseMqtt:
					_ = Program.MainProg.wpMQTTClient.unsetBrowseTopics();
					returns = new ret { erg = ret.OK }.ToString();
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
				case wpBefehl.cGetAndSaveD1MiniStatus:
					returns = D1MiniServer.getJsonStatus(s_befehl[1], true);
					break;
				case wpBefehl.cGetD1MiniNeoPixelStatus:
					returns = D1MiniServer.getJsonNeoPixel(s_befehl[1]);
					break;
				case wpBefehl.cSetD1MiniCmd:
					returns = new ret { erg = ret.ERROR }.ToString();
					param = wpBefehl.getParam(s_befehl[1]);
					d1md = D1MiniServer.get(param[0]);
					if(d1md != null) {
						D1Mini.cmdList cL = new D1Mini.cmdList(param[1]);
						if(d1md.sendCmd(cL))
							returns = new ret { erg = ret.OK }.ToString();
					}
					break;
				case wpBefehl.cSetD1MiniUrlCmd:
					param = wpBefehl.getParam(s_befehl[1]);
					returns = D1MiniServer.sendUrlCmd(param[0], param[1]);
					break;
				case wpBefehl.cStartD1MiniSearch:
					//returns = D1MiniServer.startSearch();
					break;
				case wpBefehl.cAddD1Mini:
					returns = new ret { erg = ret.ERROR }.ToString();
					param = wpBefehl.getParam(s_befehl[1]);
					if(Int32.TryParse(param[0], out outint)) {
						D1MiniServer.addD1Mini(outint);
						returns = new ret { erg = ret.OK }.ToString();
					}
					break;
				case wpBefehl.cRenewD1MiniActiveState:
					D1MiniServer.renewActiveState();
					returns = new ret { erg = ret.OK }.ToString();
					break;
				case wpBefehl.cDeleteD1Mini:
					param = wpBefehl.getParam(s_befehl[1]);
					for(int i = 0; i < param.Length; i++) {
						if(Int32.TryParse(param[i], out outint)) {
							D1MiniServer.removeD1Mini(outint);
						}
					}
					returns = new ret { erg = ret.OK }.ToString();
					break;
				case wpBefehl.cGetD1MiniServer:
					returns = D1MiniServer.getServerSettings();
					break;
				case wpBefehl.cSetD1MiniServer:
					param = wpBefehl.getParam(s_befehl[1]);
					returns = D1MiniServer.setServerSetting(param[0], param[1]);
					break;
				case wpBefehl.cDeleteShelly:
					param = wpBefehl.getParam(s_befehl[1]);
					for(int i = 0; i < param.Length; i++) {
						if(Int32.TryParse(param[i], out outint)) {
							ShellyServer.removeShelly(outint);
						}
					}
					returns = new ret { erg = ret.OK }.ToString();
					break;
				case wpBefehl.cGetShellyStatus:
					ShellyServer.getAllStatus();
					returns = new ret { erg = ret.OK }.ToString();
					break;
				case wpBefehl.cRemoteControl:
					param = wpBefehl.getParam(s_befehl[1]);
					Tvs tvs = new Tvs();
					Tv tv = tvs.Get(param[0]);
					TVParams tvp = new TVParams(
						einaus: param[1],
						tvbutton: param[2],
						dienst: param[3],
						richtung: param[4]);
					Debug.Write(MethodBase.GetCurrentMethod(), $"RemoteControl: {tvp.ToString()}");
					returns = new ret { erg = ret.OK, message = tv.Set(tvp) }.ToString();
					break;
				case wpBefehl.cSetProductChecked:
					param = wpBefehl.getParam(s_befehl[1]);
					Shopping.setProductChecked(param[0] == "1", Int32.Parse(param[1]), Int32.Parse(param[2]));
					returns = new ret { erg = ret.OK }.ToString();
					break;
				case wpBefehl.cReadItem:
					param = wpBefehl.getParam(s_befehl[1]);
					if(Int32.TryParse(param[1], out outint)) {
						Program.MainProg.wpOPCClient.ReadOPC(outint);
						returns = new ret { erg = ret.OK }.ToString();
					} else {
						returns = new ret { erg = ret.ERROR, message = "Szene nicht gefunden" }.ToString();
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
							returns = new ret { erg = ret.ERROR, message = "undefined command" }.ToString();
						}
					} catch(Exception ex) {
						returns = new ret { erg = ret.OK, message = ex.Message, trace = ex.StackTrace }.ToString();
					}
					break;
				case wpBefehl.cOPCSubGroup:
					param = wpBefehl.getParam(s_befehl[1]);
					try {
						if(param.Length == 3 && param[0] != null && param[1] != null) {
							Program.MainProg.wpOPCClient.DoBrowse(param[0], param[1], param[2]);
							returns = Program.MainProg.wpOPCClient.OPCgebrowsed;
						} else {
							returns = new ret { erg = ret.ERROR, message = "undefined command" }.ToString();
						}
					} catch(Exception ex) {
						returns = new ret { erg = ret.OK, message = ex.Message, trace = ex.StackTrace }.ToString();
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
						returns = new ret { erg = ret.ERROR, message = "OPC Server nicht gefunden" }.ToString();
					}
					break;
				case wpBefehl.cChangeOPCServerWriteLevel:
					param = wpBefehl.getParam(s_befehl[1]);
					returns = new ret { erg = ret.OK }.ToString();
					if(Int32.TryParse(param[0], out outint)) {
						WriteLevel.AddWriteLevel(outint);
					} else {
						returns = new ret { erg = ret.ERROR, message = "OPC Server nicht gefunden" }.ToString();
					}
					break;
				case wpBefehl.cOPCGroupDetails:
					if (Int32.TryParse(wpBefehl.getParam(s_befehl[1])[0], out outint)) {
						returns = Program.MainProg.wpOPCClient.GetOPCGroupDetails(outint);
					} else {
						returns = new ret { erg = ret.ERROR, message = "OPC Gruppe nicht gefunden" }.ToString();
					}
					break;
				case wpBefehl.cOPCGroupActive:
					if (Int32.TryParse(wpBefehl.getParam(s_befehl[1])[0], out outint)) {
						returns = Program.MainProg.wpOPCClient.ChangeOPCGroupState(outint);
					} else {
						returns = new ret { erg = ret.ERROR, message = "OPC Gruppe nicht gefunden" }.ToString();
					}
					break;
				case wpBefehl.cChangeOPCGroupWriteLevel:
					param = wpBefehl.getParam(s_befehl[1]);
					returns = new ret { erg = ret.OK }.ToString();
					if (Int32.TryParse(param[0], out outint)) {
						WriteLevel.AddGroupWriteLevel(outint);
					} else {
						returns = new ret { erg = ret.ERROR, message = "OPC Server nicht gefunden" }.ToString();
					}
					break;
				case wpBefehl.cChangeOPCItemType:
					param = wpBefehl.getParam(s_befehl[1]);
					returns = new ret { erg = ret.OK }.ToString();
					if (Int32.TryParse(param[0], out outint)) {
						if (Program.MainProg.wpOPCClient.ChangeOPCItemType(outint, PVTEnum.get(param[1])) != 0) {
							returns = new ret { erg = ret.ERROR, message = "Hat nicht geklappt" }.ToString();
						}
					}
					break;
				case wpBefehl.cChangeOPCItemWriteLevel:
					param = wpBefehl.getParam(s_befehl[1]);
					returns = new ret { erg = ret.OK }.ToString();
					if (Int32.TryParse(param[0], out outint)) {
						WriteLevel.AddItemWriteLevel(outint);
					} else {
						returns = new ret { erg = ret.ERROR, message = "OPC Server nicht gefunden" }.ToString();
					}
					break;
				case wpBefehl.cRenameOPCServer:
					param = wpBefehl.getParam(s_befehl[1]);
					if (Int32.TryParse(param[0], out outint)) {
						Program.MainProg.wpOPCClient.ChangeOPCServerName(outint, param[1]);
						returns = new ret { erg = ret.OK }.ToString();
					} else {
						returns = new ret { erg = ret.ERROR, message = "OPC Server nicht gefunden" }.ToString();
					}
					break;
				case wpBefehl.cRenameOPCGroup:
					param = wpBefehl.getParam(s_befehl[1]);
					if (Int32.TryParse(param[0], out outint)) {
						Program.MainProg.wpOPCClient.renameOPCGroup(outint, param[1]);
						returns = new ret { erg = ret.OK }.ToString();
					} else {
						returns = new ret { erg = ret.ERROR, message = "OPC Gruppe nicht gefunden" }.ToString();
					}
					break;
				case wpBefehl.cActivateServer:
					if (Int32.TryParse(wpBefehl.getParam(s_befehl[1])[0], out outint)) {
						returns = Program.MainProg.wpOPCClient.newOPCServer(outint);
					} else {
						returns = new ret { erg = ret.ERROR }.ToString();
					}
					break;
				case wpBefehl.cActivateGroup:
					if (Int32.TryParse(wpBefehl.getParam(s_befehl[1])[0], out outint)) {
						returns = Program.MainProg.wpOPCClient.newOPCGroup(outint);
					} else {
						returns = new ret { erg = ret.ERROR }.ToString();
					}
					break;
				case wpBefehl.cActivateItems:
					if (Int32.TryParse(wpBefehl.getParam(s_befehl[1])[0], out outint)) {
						returns = Program.MainProg.wpOPCClient.newOPCItems(outint);
					} else {
						returns = new ret { erg = ret.ERROR }.ToString();
					}
					break;
				case wpBefehl.cAddOPCGroup:
					if(Int32.TryParse(wpBefehl.getParam(s_befehl[1])[0], out outint)) {
						returns = Program.MainProg.wpOPCClient.newOPCGroup(outint);
					} else {
						returns = new ret { erg = ret.ERROR }.ToString();
					}
					break;
				case wpBefehl.cRemoveOPCServer:
					if (Int32.TryParse(wpBefehl.getParam(s_befehl[1])[0], out outint)) {
						returns = Program.MainProg.wpOPCClient.removeOPCServer(outint);
					} else {
						returns = new ret { erg = ret.ERROR }.ToString();
					}
					break;
				case wpBefehl.cRemoveOPCGroup:
					if (Int32.TryParse(wpBefehl.getParam(s_befehl[1])[0], out outint)) {
						returns = Program.MainProg.wpOPCClient.removeOPCGroup(outint);
					} else {
						returns = new ret { erg = ret.ERROR }.ToString();
					}
					break;
				case wpBefehl.cRemoveOPCItem:
					int[] Items = Arrays.StringArrayToIntArray(wpBefehl.getParam(s_befehl[1]));
					returns = Program.MainProg.wpOPCClient.removeOPCItems(Items);
					break;
				case wpBefehl.cMoveOPCItem:
					returns = new ret { erg = ret.ERROR, message = "keine gültigen Parameter" }.ToString();
					param = wpBefehl.getParam(s_befehl[1]);
					if (Int32.TryParse(param[0], out outint) &&
						Int32.TryParse(param[1], out outint2)) {
						OPCItem TheItem = OpcDatapoints.getItem(outint);
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
						returns = new ret { erg = ret.ERROR, message = $"ID: {param[1]} DOSNT EXISTS" }.ToString();
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
						returns = new ret { erg = ret.ERROR, message = $"ID: {param[1]} DOSNT EXISTS" }.ToString();
					}
					break;
				case wpBefehl.cUpdateAlarms:
					param = wpBefehl.getParam(s_befehl[1]);
					returns = UpdateAlarms(param);
					break;
				case wpBefehl.cDeleteAlarm:
					param = wpBefehl.getParam(s_befehl[1]);
					if(Int32.TryParse(param[0], out outint)) {
						using (Database Sql = new Database("Delete Alarm")) {
							Datapoint AlarmWeg = Datapoints.GetFromAlarmId(outint);
							if(AlarmWeg != null) {
								Alarms.RemoveAlarm(AlarmWeg.idAlarm);
								AlarmWeg.idAlarm = null;
								Sql.wpNonResponse("DELETE FROM [alarm] WHERE [id_alarm] = {0}", outint);
							}
						}
					}
					returns = new ret { erg = ret.OK }.ToString();
					break;
				case wpBefehl.cDeleteAlarms:
					param = wpBefehl.getParam(s_befehl[1]);
					for(int i = 0; i < param.Length; i++) {
						if(Int32.TryParse(param[i], out outint)) {
							using (Database Sql = new Database("Delete Alarm")) {
								Datapoint AlarmWeg = Datapoints.GetFromAlarmId(outint);
								if(AlarmWeg != null) {
									Alarms.RemoveAlarm(AlarmWeg.idAlarm);
									AlarmWeg.idAlarm = null;
									Sql.wpNonResponse("DELETE FROM [alarm] WHERE [id_alarm] = {0}", outint);
								}
							}
						}
					}
					returns = new ret { erg = ret.OK }.ToString();
					break;
				case wpBefehl.cUpdateAlarmGroups:
					returns = Alarms.FillAlarmGroups();
					break;
				case wpBefehl.cUpdateMail:
					returns = Program.MainProg.SetRecipientRequired();
					break;
				case wpBefehl.cCalendarRenew:
					param = wpBefehl.getParam(s_befehl[1]);
					if (Int32.TryParse(param[0], out outint)) {
						returns = Program.MainProg.CalDav.renewCalendar(outint);
					} else {
						returns = new ret { erg = ret.ERROR, message = "id not found" }.ToString();
					}
					break;
#region Trend
				case wpBefehl.cSaveNewTrend:
					param = wpBefehl.getParam(s_befehl[1]);
					if (Int32.TryParse(param[0], out outint)) {
						Trends.AddTrend(outint);
						returns = new ret { erg = ret.OK }.ToString();
					} else {
						returns = new ret { erg = ret.ERROR, message = "id not found" }.ToString();
					}
					break;
				case wpBefehl.cUpdateTrend:
					// Version3.0: UpdateTrend
					param = wpBefehl.getParam(s_befehl[1]);
					if(Int32.TryParse(param[0], out outint)) {
						int dp = Trends.Get(outint).IdDP;
						Trends.RemoveTrend(outint);
						Trends.AddTrend(dp);
						returns = new ret { erg = ret.OK }.ToString();
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
					returns = new ret { erg = ret.OK }.ToString();
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
					returns = new ret { erg = ret.OK }.ToString();
					break;
				case wpBefehl.cActivateTrend:
					param = wpBefehl.getParam(s_befehl[1]);
					for(int i = 0; i < param.Length; i++) {
						if(Int32.TryParse(param[i], out outint)) {
							Trends.Get(outint).Activate();
						}
					}
					returns = new ret { erg = ret.OK }.ToString();
					break;
				case wpBefehl.cDeactivateTrend:
					param = wpBefehl.getParam(s_befehl[1]);
					for(int i = 0; i < param.Length; i++) {
						if(Int32.TryParse(param[i], out outint)) {
							Trends.Get(outint).Deactivate();
						}
					}
					returns = new ret { erg = ret.OK }.ToString();
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
					returns = new ret { erg = ret.OK }.ToString();
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
					returns = new ret { erg = ret.OK }.ToString();
					break;
#endregion
				case wpBefehl.cUpdateRouter:
					param = wpBefehl.getParam(s_befehl[1]);
					if (Int32.TryParse(param[0], out outint)) {
						Router.UpdateRouter(outint);
					}
					returns = new ret { erg = ret.OK }.ToString();
					break;
				case wpBefehl.cReadEvent:
					param = wpBefehl.getParam(s_befehl[1]);
					try {
						if (param != null) returns = Logger.readLog(param[0]);
						else returns = Logger.readLog();
					} catch (Exception ex) {
						returns = new ret { erg = ret.ERROR, message = ex.Message, trace = ex.StackTrace }.ToString();
						eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
					}
					//PDebug.Write(returns);
					break;
				case wpBefehl.cReloadSettings:
					try {
						IniFile.read();
						finished();
						init();
						eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Warning, "Reload Settings");
						returns = new ret { erg = ret.OK }.ToString();
					} catch (Exception ex) {
						returns = new ret { erg = ret.ERROR, message = ex.Message, trace = ex.StackTrace }.ToString();
						eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
					}
					break;
				case wpBefehl.cChangeWartung:
					Program.MainProg.wpWartung = !Program.MainProg.wpWartung;
					if(Program.MainProg.wpWartung)
						eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Warning, "Wartung wurde aktiviert");
					else
						eventLog.Write(MethodInfo.GetCurrentMethod(), "Wartung deaktiviert");
					returns = new ret { erg = ret.OK }.ToString();
					break;
				case wpBefehl.cGetDebug:
					returns = Debug.getDebugJson();
					break;
				case wpBefehl.cSetDebug:
					param = wpBefehl.getParam(s_befehl[1]);
					returns = Debug.changeDebug(param);
					break;
				case wpBefehl.cHistoryCleaner:
					await Task.Run(() => {
						using(Database Sql = new Database("HistoryCleaner")) {
							Sql.HistoryCleaner();
						}
					});
					returns = new ret { erg = ret.OK }.ToString();
					break;
				case wpBefehl.cSetSummer:
					param = wpBefehl.getParam(s_befehl[1]);
					returns = Program.MainProg.wpSun.SetSummer(param[0] == "0" ? false : true);
					break;
				default:
					returns = new ret { erg = ret.ERROR, message = "undefined command" }.ToString();
					break;
			}
			//wpEventLog.Write(String.Format("{0} Server Antwort: {1}", WebComServer.Name, returns));
			if(returns == null)
				returns = new ret { erg = ret.ERROR, message = "undefined command" }.ToString();
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
			ret returns = new ret();
			try {
				if (Int32.TryParse(param[1], out idDp) &&
					Int32.TryParse(param[0], out writeLevel)) {
					Datapoint DP = Datapoints.Get(idDp);
					if(DP != null) {
						if(Datapoints.Get(idDp).WriteLevel <= writeLevel) {
							Datapoints.Get(idDp).writeValue(param[2]);
							returns.erg = ret.OK;
						} else {
							returns.erg = ret.ERROR;
							returns.message = "Ihnen fehlt die Berechtigung zum schreiben.";
						}
					} else {
						returns.erg = ret.ERROR;
						returns.message = $"ERROR: ID: {param[1]} DOSNT EXISTS";
					}
				} else {
					returns.erg = ret.ERROR;
					returns.message = $"ERROR: ID: {param[1]} NOT PARSEABLE";
				}
			} catch(Exception ex) {
				returns.erg = ret.ERROR;
				returns.message = ex.Message;
				returns.trace = ex.StackTrace;
			}
			return returns.ToString();
		}
		private string ForceMqttUpdate() {
			string returns = new ret { erg = ret.ERROR }.ToString();
			if(Program.MainProg.wpMQTTClient.forceMqttUpdate())
				returns = new ret { erg = ret.OK }.ToString();
			return returns;
		}
		private string ShellyMqttUpdate() {
			string returns = new ret { erg = ret.ERROR }.ToString();
			if(Program.MainProg.wpMQTTClient.shellyMqttUpdate())
				returns = new ret { erg = ret.OK }.ToString();
			return returns;
		}
		private string D1MiniMqttUpdate() {
			string returns = new ret { erg = ret.ERROR }.ToString();
			if(Program.MainProg.wpMQTTClient.d1MiniMqttUpdate())
				returns = new ret { erg = ret.OK }.ToString();
			return returns;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="param"></param>
		/// <returns></returns>
		private string writeMultiDP(string[] param) {
			int outint;
			ret returns = new ret { erg = ret.OK };
			try {
				Dictionary<int, string> toWrite = new Dictionary<int, string>();
				for(int i = 1; i < param.Length; i = i + 2) {
					if (Int32.TryParse(param[i], out outint)) {
						Datapoint dp = Datapoints.Get(outint);
						string korrekt = param[i + 1];
						toWrite.Add(outint, korrekt);
					} else {
						returns.erg = ret.ERROR;
						returns.message += $"ID: {param[i]} DOSNT EXISTS<br />";
					}
				}
				Datapoints.writeValues(toWrite);
			} catch (Exception ex) {
				returns.erg = ret.ERROR;
				returns.message = ex.Message;
				returns.trace = ex.StackTrace;
			}
			return returns.ToString();
		}
		private string writeSceneDP(int idscene, string user) {
			int level = 0;
			string[][] erg;
			ret returns = new ret { erg = ret.OK, message = "" };
			user = user.ToLower();
			using (Database Sql = new Database("get User Level")) {
				erg = Sql.wpQuery(@"SELECT TOP 1 [g].[order]
					FROM [user] [u]
					INNER JOIN [usergroup] [g] ON [u].[id_usergroup] = [g].[id_usergroup]
					WHERE [login] = '{0}'", user);
			}
			if (erg.Length == 1 && erg[0].Length == 1 &&
				Int32.TryParse(erg[0][0], out level)) {
				List<int> ids = new List<int>();
				List<string> values = new List<string>();
				Datapoint p;
				returns.message = $"WriteScene ({{idscene}}):";
				string sqlLog = "";
				Dictionary<int, string> d = Scene.getScene(idscene);
				foreach (KeyValuePair<int, string> kvp in d) {
					p = Datapoints.Get(kvp.Key);
					if (p != null && p.WriteLevel <= level) {
						p.writeValue(kvp.Value);
						returns.message += $"\r\n\tWrite DP Ok:\r\n\t\tuser: {user} ({level}), idDp: {kvp.Key}, Value: {kvp.Value}";
						sqlLog += String.Format("('{0}', '{1} (scene)', '{2:s}', '{3}', '{4}'),", user, p.Name, DateTime.Now, p.Value, kvp.Value);
					} else {
						returns.erg = ret.ERROR;
						returns.message += $"\r\n\tkeine Berechtigung zum schreiben\r\n\t\tuser: {user} ({level}), idDp: {kvp.Key}, Value: {kvp.Value}";
					}
				}
				if (sqlLog.Length > 0) {
					using (Database Sql = new Database("write Scene log")) {
						Sql.wpNonResponse("INSERT INTO [useractivity] ([username], [datapoint], [writetime], [oldvalue], [newvalue]) VALUES {0}", sqlLog.Substring(0, sqlLog.Length - 1));
					}
				}
				eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Warning, returns.message);
			} else {
				returns.erg = ret.ERROR;
				returns.message = "User nicht gefunden";
			}
			return returns.ToString();
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="idAlarm"></param>
		/// <returns></returns>
		private string UpdateAlarm(int idAlarm) {
			try {
				using(Database Sql = new Database("Update Alarm")) {
					List<int> Hsrv = new List<int>();

					string[][] DBAlarms = Sql.wpQuery(@"
						SELECT
							[dp].[id_dp], [a].[text], [a].[link], [t].[name], [t].[autoquit],
							[g].[name], [dp].[name], [c].[condition], [a].[min], [a].[max], [a].[delay],
							ISNULL([a].[id_alarmgroups1], 0), ISNULL([a].[id_alarmgroups2], 0),
							ISNULL([a].[id_alarmgroups3], 0), ISNULL([a].[id_alarmgroups4], 0),
							ISNULL([a].[id_alarmgroups5], 0)
						FROM [alarm] [a]
						INNER JOIN [alarmtype] [t] ON [a].[id_alarmtype] = [t].[id_alarmtype]
						INNER JOIN [alarmgroup] [g] ON [a].[id_alarmgroup] = [g].[id_alarmgroup]
						INNER JOIN [dp] ON [a].[id_dp] = [dp].[id_dp]
						INNER JOIN [alarmcondition] [c] ON [a].[id_alarmcondition] = [c].[id_alarmcondition]
						WHERE [a].[id_alarm] = " + idAlarm.ToString());
					Alarm TheAlarm = Alarms.Get(idAlarm);
					/// TODO: ohne Neustart neuen Alarm generieren
					if (TheAlarm != null) {
						TheAlarm.Alarmtext = DBAlarms[0][1];
						TheAlarm.Alarmlink = DBAlarms[0][2];
						TheAlarm.Alarmtype = DBAlarms[0][3];
						TheAlarm.Autoquit = DBAlarms[0][4] == "True";
						TheAlarm.Alarmgroup = DBAlarms[0][5];
						TheAlarm.Condition = DBAlarms[0][7];
						TheAlarm.Min = DBAlarms[0][8];
						if (TheAlarm.Condition == ">x<" || TheAlarm.Condition == "<x>")
							TheAlarm.Max = Int32.Parse(DBAlarms[0][9]);
						int delay;
						if (Int32.TryParse(DBAlarms[0][10], out delay)) {
							TheAlarm.UpdateDelay(delay);
						}
						TheAlarm.Alarmgroups1 = Int32.Parse(DBAlarms[0][11]);
						TheAlarm.Alarmgroups2 = Int32.Parse(DBAlarms[0][12]);
						TheAlarm.Alarmgroups3 = Int32.Parse(DBAlarms[0][13]);
						TheAlarm.Alarmgroups4 = Int32.Parse(DBAlarms[0][14]);
						TheAlarm.Alarmgroups5 = Int32.Parse(DBAlarms[0][15]);
						TheAlarm.InAlarm = false;
						TheAlarm.AlarmUpdate = DateTime.Now;
						eventLog.Write(MethodInfo.GetCurrentMethod(), String.Format("Alarm update: {0} ({1})", DBAlarms[0][1], DBAlarms[0][0]));
					} else {
						eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Warning,
							String.Format("Alarm wurde nicht gefunden: {0} ({1})", DBAlarms[0][1], DBAlarms[0][0]));
					}
				}
				return new ret { erg = ret.OK }.ToString();
			} catch(Exception ex) {
				eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Error,
					String.Format("{0}\r\nTrace:\r\n{1}", ex.Message, ex.StackTrace));
				return new ret { erg = ret.ERROR, message = ex.Message, trace = ex.StackTrace }.ToString();
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
			return succeed ? new ret { erg = ret.OK }.ToString() : new ret { erg = ret.ERROR }.ToString();
		}
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		private string getActiveAlarms() {
			WatchDogByte += 1;
			if(WatchDogByte > 255) WatchDogByte = 1;
			DateTime Now = DateTime.Now;
			string returns =
$"{{WatchDog={WatchDogByte}}}" +
$"{{DateTime={Now.ToString("yyyy-MM-ddTHH:mm:ss")}}}" +
$"{{Date={Now.ToString("dd.MM.yyyy")}}}" +
$"{{Time={Now.ToString("HH:mm:ss")}}}" +
$"{{Alarme=";
			foreach (Alarm TheAlarm in Alarms.getActiveAlarms()) {
				returns +=
	$"{{{TheAlarm.IdAlarm}={{" +
		$"{{id={TheAlarm.IdAlarm}}}" +
		$"{{DpName={TheAlarm.DpName}}}" +
		$"{{Come={TheAlarm.Come}}}" +
		$"{{Gone={(TheAlarm.Gone == Alarm.Default ? "-" : TheAlarm.Gone.ToString())}}}" +
		$"{{Quit={(TheAlarm.Quit == Alarm.Default ? "-" : TheAlarm.Quit.ToString())}}}" +
		$"{{Type={TheAlarm.Alarmtype}}}" +
		$"{{Group={TheAlarm.Alarmgroup}}}" +
		$"{{Text={TheAlarm.Alarmtext}}}" +
		$"{{Link={TheAlarm.Alarmlink}}}" +
		$"{{AlarmUpdate={TheAlarm.AlarmUpdate.ToString()}}}" +
		$"{(Alarms.UseAlarmGroup1 ?
			$"{{AlarmGroup1={TheAlarm.Alarmnames1}}}" : "")}" +
		$"{(Alarms.UseAlarmGroup2 ?
			$"{{AlarmGroup2={TheAlarm.Alarmnames2}}}" : "")}" +
		$"{(Alarms.UseAlarmGroup3 ?
			$"{{AlarmGroup3={TheAlarm.Alarmnames3}}}" : "")}" +
		$"{(Alarms.UseAlarmGroup4 ?
			$"{{AlarmGroup4={TheAlarm.Alarmnames4}}}" : "")}" +
		$"{(Alarms.UseAlarmGroup5 ?
			$"{{AlarmGroup5={TheAlarm.Alarmnames5}}}" : "")}" +
	$"}}";
			}
			returns +=
$"}}" +
$"{{Wartung={(Program.MainProg.wpWartung ? "True" : "False")}}}";
				
			return returns;
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
			Alarm TheAlarm = Program.MainProg.getAlarmFromAlarmid(id);
			TheAlarm.AlarmUpdate = DateTimeNow;
			TheAlarm.Quit = DateTimeNow;
			TheAlarm.QuitFrom = user;
			TheAlarm.QuitText = quittext;
			TheAlarm.NeedQuit = true;
			using(Database Sql = new Database("Quit Alarm")) {
				Sql.wpNonResponse(@"UPDATE [alarmhistoric]
					SET [quit] = ""{0}"", [quitfrom] = ""{1}"", [quittext] = ""{2}""
					WHERE [id_alarm] = {3} AND [quit] IS NULL",
					DateTimeNow.ToString(Database.DateTimeFormat),
					user,
					quittext,
					TheAlarm.IdAlarm);
			}
			Program.MainProg.QuitToMail(TheAlarm);
			return new ret { erg = ret.OK }.ToString();
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="param"></param>
		/// <returns></returns>
		private string quitAlarms(string[] param) {
			// hier weiter machen
			DateTime DateTimeNow = DateTime.Now;
			Alarm TheAlarm;
			List<Alarm> TheAlarmList = new List<Alarm>();
			string toinsert = "";
			for(int i = 1; i < param.Length - 1; i++) {
				int alarmid;
				if (Int32.TryParse(param[i], out alarmid)) {
					TheAlarm = Alarms.Get(alarmid);
					TheAlarmList.Add(TheAlarm);
					TheAlarm.AlarmUpdate = DateTimeNow;
					TheAlarm.Quit = DateTimeNow;
					TheAlarm.QuitFrom = param[0];
					TheAlarm.QuitText = param[param.Length - 1];
					TheAlarm.NeedQuit = true;
					toinsert += "[id_alarm] = " + alarmid + " OR ";
				}
			}
			using (Database Sql = new Database("Quit Alarm")) {
				Sql.wpNonResponse(@"UPDATE [alarmhistoric]
					SET [quit] = '{0}', [quitfrom] = '{1}', [quittext] = '{2}'
					WHERE ({3}) AND [quit] IS NULL",
					DateTimeNow.ToString(Database.DateTimeFormat),
					param[0],
					param[param.Length - 1],
					toinsert.Substring(0, toinsert.Length - 4));
			}
			Program.MainProg.QuitsToMail(TheAlarmList);
			return new ret { erg = ret.OK }.ToString();
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="param"></param>
		/// <returns></returns>
		private string getActiveDP(string[] param) {
			if (param == null) return String.Empty;

			string returns = "{\"Datenpunkte\":{";
			for (int i = 0; i < param.Length; i++) {
				int parsedint;
				if(Int32.TryParse(param[i], out parsedint)) {
					Datapoint TheItem = Datapoints.Get(parsedint);
					if (TheItem != null) {
						returns += $"\"{parsedint}\":{{" +
							"\"erg\":\"S_OK\"," +
							$"\"Value\":\"{TheItem.Value}\"," +
							$"\"ValueString\":\"{TheItem.ValueString}\"," +
							$"\"LastChange\":\"{TheItem.LastChange.ToString("yyyy-MM-ddTHH:mm:ss")}\"" +
						"},";
					} else {
						returns += $"\"{parsedint}\":{{" +
							"\"erg\"=\"S_ERROR\"" +
						"},";
					}
				}
			}
			if(returns.EndsWith(",")) returns = returns.Remove(returns.Length - 1);
			return returns + "}}";
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
					try {
						Datapoint TheItem = Datapoints.Get(DPid);
						returns += $"{{{param[i]}={{Value=\"{TheItem.Value}\"}}{{TimeStamp={TheItem.LastChange.ToString("yyyy-MM-ddTHH:mm:ss")}}}}}";
					} catch(Exception ex) {
						Debug.WriteError(MethodInfo.GetCurrentMethod(), ex, DPid.ToString());
					}
				} else {
					eventLog.Write(MethodInfo.GetCurrentMethod(),ELogEntryType.Warning,
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
			foreach (Alarm TheAlarm in Alarms.getActiveAlarms()) {
				if (TheAlarm.Come != Alarm.Default && TheAlarm.Gone == Alarm.Default)
					AlarmsCome++;
				if (TheAlarm.Gone != Alarm.Default) AlarmsGone++;
				if (TheAlarm.Quit == Alarm.Default) AlarmsQuit++;
			}
			return String.Format(@"{{WatchDog={0}}}{{DateTime={1}}}{{Date={2}}}{{Time={3}}}
				{{AlarmsCome={4}}}{{AlarmsGone={5}}}{{AlarmsQuit={6}}}",
				WatchDogByte,
				Now.ToString("yyyy-MM-ddTHH:mm:ss"),
				Now.ToString("dd.MM.yyyy"),
				Now.ToString("HH:mm:ss"),
				AlarmsCome,
				AlarmsGone,
				AlarmsQuit);
		}
	}
	public class ret {
		public const string OK = "S_OK";
		public const string ERROR = "S_ERROR";
		private string _erg = string.Empty;
		public string erg {
			get { return _erg; }
			set { _erg = value; }
		}
		private string _message = string.Empty;
		public string message {
			get { return _message; }
			set { _message = value; }
		}
		private string _trace = string.Empty;
		public string trace {
			get { return _trace; }
			set { _trace = value; }
		}
		public override string ToString() {
			string msg = (_message != string.Empty) ? $",\"message\":\"{jsonEscape(_message)}\"" : "";
			string trc = (_trace != string.Empty) ? $",\"trace\":\"{jsonEscape(_trace)}\"" : "";
			return $"{{\"erg\":\"{erg}\"{msg}{trc}}}";
		}
		private string jsonEscape(string str) {
			return str.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
		}
	}
}
/** @} */
