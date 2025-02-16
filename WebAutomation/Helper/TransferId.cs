//###################################################################################
//#                                                                                 #
//#              (C) FreakaZone GmbH                                                #
//#              =======================                                            #
//#                                                                                 #
//###################################################################################
//#                                                                                 #
//# Author       : Christian Scheid                                                 #
//# Date         : 23.12.2019                                                       #
//#                                                                                 #
//# Revision     : $Rev:: 171                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: TransferId.cs 171 2025-02-13 12:28:06Z                   $ #
//#                                                                                 #
//###################################################################################
using FreakaZone.Libraries.wpEventLog;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using static FreakaZone.Libraries.wpEventLog.Logger;

namespace WebAutomation.Helper {
	class TransferId {
		/// <summary>WebAutomationServer Event Log</summary>
		private static Logger eventLog = new Logger(Logger.ESource.TransferId);
		/// <summary>Transfer Grund: OpcRouter</summary>
		public const int TransferOpcRouter = 1;
		/// <summary>Transfer Grund: Watchdog</summary>
		public const int TransferWatchdog = 2;
		/// <summary>Transfer Grund: Taster Default</summary>
		public const int TransferTaster = 3;
		/// <summary>Transfer Grund: Force Read</summary>
		public const int TransferForceRead = 4;
		/// <summary>Transfer Grund: Zeitprogramm</summary>
		public const int TransferSchedule = 5;
		/// <summary>Transfer Grund: Szenen</summary>
		public const int TransferScene = 6;
		/// <summary>Transfer Grund: Szenen Zeitprogramm</summary>
		public const int TransferSceneSchedule = 7;
		/// <summary>Transfer Grund: Szenen Zeitprogramm</summary>
		public const int TransferShelly = 8;
        public const int TransferMQTT = 9;
        /// <summary>Transfer Grund: Normale Operation</summary>
        public const int TransferNormalOP = 0;
		/// <summary>Transfer Id</summary>
		private static int _tid = 0;
		/// <summary>Dictionary Transfer Id (id, forwhat)></summary>
		private static Dictionary<int, transferID> _dtid = new Dictionary<int, transferID>();
		private static Mutex locktid = new Mutex(false, Application.ProductName + ":LockTransferId");
		/// <summary>
		/// erzeugt eine neue Transfer Id
		/// </summary>
		/// <param name="forWhat">Der Grund fur OPC schreiben / lesen</param>
		/// <returns>Transfer Id</returns>
		public static int getNew(int forWhat) {
			int mytid = -1;
			bool entered = false;
			int notEntered = 0;

			if(locktid.WaitOne(1000)) {
				try {
					if(++_tid >= Int16.MaxValue) _tid = 0;
					mytid = _tid;
				} finally {
					locktid.ReleaseMutex();
				}

				while(!entered && notEntered < 10) {
					if(Monitor.TryEnter(_dtid, 5000)) {
						try {
							if(_dtid.ContainsKey(mytid)) {
								_dtid[mytid] = new transferID(mytid, forWhat);
							} else {
								_dtid.Add(mytid, new transferID(mytid, forWhat));
							}
						} catch(Exception ex) {
							eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
						} finally {
							Monitor.Exit(_dtid);
							entered = true;
						}
					} else {
						if(++notEntered >= 10) {
							eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Error,
								String.Format("Angeforderte Transfer Id konnte nicht angefügt werden: {0}", mytid));
						} else {
							Thread.Sleep(10);
						}
					}
				}
			} else {
				eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Error,
					String.Format("Deadlockopfer ☺: Mutextimeout für Transfer Id abgelaufen"));
			}
			return mytid;
		}
		public static int getNew() {
			return getNew(TransferNormalOP);
		}
		/// <summary>
		/// Transfer Id von Zwischenspeicher loeschen
		/// </summary>
		/// <param name="id"></param>
		public static void remove(int id) {
			bool entered = false;
			int notEntered = 0;
			while(!entered && notEntered < 10) {
				if(Monitor.TryEnter(_dtid, 5000)) {
					try {
						if(_dtid.ContainsKey(id)) {
							_dtid[id].Stop();
							_dtid.Remove(id);
						} else {
							Debug.Write(MethodInfo.GetCurrentMethod(), "Tranaction ID nicht gefunden: {0}", id);
						}
					} catch(Exception ex) {
						eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
					} finally {
						Monitor.Exit(_dtid);
						entered = true;
					}
				} else {
					if(++notEntered >= 10) {
						eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Error,
							String.Format("Transfer Id konnte nicht gelöscht werden: {0}", id));
					} else {
						Thread.Sleep(10);
					}
				}
			}
		}
		/// <summary>
		/// ermittelt anhand der Transfer Id den Grund fuer OPC schreiben / lesen
		/// </summary>
		/// <param name="id"></param>
		/// <returns>Grund fuer Transfer</returns>
		public static int getTransferReason(int id) {
			int returns = -1;
			bool entered = false;
			int notEntered = 0;
			while(!entered && notEntered < 10) {
				if(Monitor.TryEnter(_dtid, 5000)) {
					try {
						if(_dtid.ContainsKey(id)) returns = _dtid[id].ForWhat;
					} catch(Exception ex) {
						eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
					} finally {
						Monitor.Exit(_dtid);
						entered = true;
					}
				} else {
					if(++notEntered >= 10) {
						eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Error,
							String.Format("Angeforderte Transfer Id nicht verfügbar: {0}", id));
					} else {
						Thread.Sleep(10);
					}
				}
			}
			return returns;
		}
		public static int getTransferCounter() {
			int returns = -1;
			bool entered = false;
			int notEntered = 0;
			while(!entered && notEntered < 10) {
				if(Monitor.TryEnter(_dtid, 5000)) {
					try {
						returns = _dtid.Count;
					} catch(Exception ex) {
						eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
					} finally {
						Monitor.Exit(_dtid);
						entered = true;
					}
				} else {
					if(++notEntered >= 10) {
						eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Error, "Transfer Liste nicht verfügbar");
					} else {
						Thread.Sleep(10);
					}
				}
			}
			return returns;
		}
		/// <summary>
		/// 
		/// </summary>
		private class transferID : System.Timers.Timer {
			private Logger eventLog;
			private int _id;
			private int _forWhat;
			public int ForWhat {
				get { return _forWhat; }
			}
			public transferID(int id, int forWhat) {
				_id = id;
				_forWhat = forWhat;
				if(!WebAutomationServer.isInit) {
					if(Program.MainProg.wpBigProject) {
						if(Debug.debugTransferID)
							Debug.Write(MethodInfo.GetCurrentMethod(), "TA intervall (init): 240 s (TAID-{0})", id);
						this.Interval = 240 * 1000;
					} else {
						if(Debug.debugTransferID)
							Debug.Write(MethodInfo.GetCurrentMethod(), "TA intervall (init): 60 s (TAID-{0})", id);
						this.Interval = 60 * 1000;
					}
				} else {
					if(forWhat == TransferForceRead) {
						if(Debug.debugTransferID)
							Debug.Write(MethodInfo.GetCurrentMethod(), "TA intervall: 60 s (TAID-{0})", id);
						this.Interval = 60 * 1000;
					} else if(Program.MainProg.wpBigProject) {
						if(Debug.debugTransferID)
							Debug.Write(MethodInfo.GetCurrentMethod(), "TA intervall: 30 s (TAID-{0})", id);
						this.Interval = 30 * 1000;
					} else {
						if(Debug.debugTransferID)
							Debug.Write(MethodInfo.GetCurrentMethod(), "TA intervall: 10 s (TAID-{0})", id);
						this.Interval = 10 * 1000;
					}
				}
				this.AutoReset = false;
				this.Elapsed += new System.Timers.ElapsedEventHandler(ttaid_Elapsed);
				this.Enabled = true;
				eventLog = new Logger(Logger.ESource.TransferId);
			}
			private void ttaid_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
				string reason = "unbekannt";
				switch(_forWhat) {
					case TransferOpcRouter:
						reason = "Opc Router";
						break;
					case TransferId.TransferWatchdog:
						reason = "Watchdog";
						break;
					case TransferId.TransferTaster:
						reason = "Taster Default";
						break;
					case TransferForceRead:
						reason = "Force Read";
						break;
					case TransferSchedule:
						reason = "Zeitprogramm";
						break;
					case TransferScene:
						reason = "Szenen";
						break;
					case TransferSceneSchedule:
						reason = "Szenen Zeitprogramm";
						break;
					case TransferShelly:
						reason = "Shelly";
						break;
					case TransferMQTT:
						reason = "MQTT";
						break;
					default:
						reason = "Normale Operation";
						break;
				}
				eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Warning,
					String.Format("Server Antwortet nicht auf Anfrage. Transfer Id: {0}, Transfergrund: {1}, Zeit: {2} s",
						_id, reason, (this.Interval / 1000)));
				remove(_id);
			}
		}
	}
}
