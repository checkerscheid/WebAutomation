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
//# Revision     : $Rev:: 115                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: OPC_Data_Grp.cs 115 2024-07-04 00:02:57Z                 $ #
//#                                                                                 #
//###################################################################################
using OPC.Common;
using OPC.Data.Interface;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using WebAutomation;
using WebAutomation.Helper;
/**
* @addtogroup externals
* @{
*/
namespace OPC.Data {

	/// <summary>
	/// managed side only structs
	/// </summary>
	public class OPCItemDef {
		/// <summary>
		/// 
		/// </summary>
		public OPCItemDef() { }
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="activ"></param>
		/// <param name="hclt"></param>
		/// <param name="vt"></param>
		public OPCItemDef(string id, bool activ, int hclt, VarEnum vt) { ItemID = id; Active = activ; HandleClient = hclt; RequestedDataType = vt; }
		/// <summary></summary>
		public string AccessPath = "";
		/// <summary></summary>
		public string ItemID;
		/// <summary></summary>
		public bool Active;
		/// <summary></summary>
		public int HandleClient;
		/// <summary></summary>
		public byte[] Blob = null;
		/// <summary></summary>
		public VarEnum RequestedDataType;
	};
	/// <summary>
	/// 
	/// </summary>
	public class OPCItemResult {
		/// <summary></summary>
		public int Error;			// content below only valid if Error=S_OK
		/// <summary></summary>
		public int HandleServer;
		/// <summary></summary>
		public VarEnum CanonicalDataType;
		/// <summary></summary>
		public OPCACCESSRIGHTS AccessRights;
		/// <summary></summary>
		public byte[] Blob;
	}
	/// <summary>
	/// 
	/// </summary>
	public class OPCItemState {
		/// <summary></summary>
		public int Error;			// content below only valid if Error=S_OK
		/// <summary></summary>
		public int HandleClient;	// always valid for callbacks
		/// <summary></summary>
		public object DataValue;
		/// <summary></summary>
		public long TimeStamp;
		/// <summary></summary>
		public short Quality;
		/// <summary></summary>
		public VarEnum CanonicalDataType;
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			StringBuilder sb = new StringBuilder("OPCIST: ", 256);
			sb.AppendFormat("error=0x{0:x} hclt=0x{1:x}", Error, HandleClient);
			if (Error == HRESULTS.S_OK) {
				sb.AppendFormat(" val={0} time={1} qual=", DataValue, TimeStamp);
				sb.Append(OpcGroup.QualityToString(Quality));
			}

			return sb.ToString();
		}
	}
	/// <summary>
	/// 
	/// </summary>
	public class OPCWriteResult {
		/// <summary></summary>
		public int Error;
		/// <summary></summary>
		public int HandleClient;
		/// <summary></summary>
		public object DataValue;
	}
	/// <summary>
	/// 
	/// </summary>
	public class OPCItemAttributes {
		/// <summary></summary>
		public string AccessPath;
		/// <summary></summary>
		public string ItemID;
		/// <summary></summary>
		public bool Active;
		/// <summary></summary>
		public int HandleClient;
		/// <summary></summary>
		public int HandleServer;
		/// <summary></summary>
		public OPCACCESSRIGHTS AccessRights;
		/// <summary></summary>
		public VarEnum RequestedDataType;
		/// <summary></summary>
		public VarEnum CanonicalDataType;
		/// <summary></summary>
		public OPCEUTYPE EUType;
		/// <summary></summary>
		public object EUInfo;
		/// <summary></summary>
		public byte[] Blob;
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			StringBuilder sb = new StringBuilder("OPCIAT: '", 512);
			sb.Append(ItemID); sb.Append("' ('"); sb.Append(AccessPath);
			sb.AppendFormat("') hc=0x{0:x} hs=0x{1:x} act={2}", HandleClient, HandleServer, Active);
			sb.AppendFormat("\r\n\tacc={0} typr={1} typc={2}", AccessRights, RequestedDataType, CanonicalDataType);
			sb.AppendFormat("\r\n\teut={0} eui={1}", EUType, EUInfo);
			if (!(Blob == null))
				sb.AppendFormat(" blob size={0}", Blob.Length);

			return sb.ToString();
		}
	}
	/// <summary>
	/// 
	/// </summary>
	public struct OPCGroupState {
		/// <summary></summary>
		public string Name;
		/// <summary></summary>
		public bool Public;
		/// <summary></summary>
		public int UpdateRate;
		/// <summary></summary>
		public bool Active;
		/// <summary></summary>
		public int TimeBias;
		/// <summary></summary>
		public float PercentDeadband;
		/// <summary></summary>
		public int LocaleID;
		/// <summary></summary>
		public int HandleClient;
		/// <summary></summary>
		public int HandleServer;
	}
	// ----------------- event arguments + handlers ------------------------
	/// <summary>
	/// IOPCAsyncIO2
	/// </summary>
	public class DataChangeEventArgs: EventArgs {
		/// <summary></summary>
		public int transactionID;
		/// <summary></summary>
		public int groupHandleClient;
		/// <summary></summary>
		public int masterQuality;
		/// <summary></summary>
		public int masterError;
		public DateTime lastChange;
		/// <summary></summary>
		public OPCItemState[] sts;
		/// <summary>
		/// 
		/// </summary>
		public DataChangeEventArgs() {
		}
	}
	/// <summary>
	/// 
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	public delegate void DataChangeEventHandler(object sender, DataChangeEventArgs e);
	/// <summary>
	/// 
	/// </summary>
	public class ReadCompleteEventArgs: EventArgs {
		/// <summary></summary>
		public int transactionID;
		/// <summary></summary>
		public int groupHandleClient;
		/// <summary></summary>
		public int masterQuality;
		/// <summary></summary>
		public int masterError;
		public DateTime lastChange;
		/// <summary></summary>
		public OPCItemState[] sts;
		/// <summary>
		/// 
		/// </summary>
		public ReadCompleteEventArgs() {
		}
	}
	/// <summary>
	/// 
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	public delegate void ReadCompleteEventHandler(object sender, ReadCompleteEventArgs e);
	/// <summary>
	/// 
	/// </summary>
	public class WriteCompleteEventArgs: EventArgs {
		/// <summary></summary>
		public int transactionID;
		/// <summary></summary>
		public int groupHandleClient;
		/// <summary></summary>
		public int masterError;
		/// <summary></summary>
		public OPCWriteResult[] res;
		/// <summary>
		/// 
		/// </summary>
		public WriteCompleteEventArgs() {
		}
	}
	/// <summary>
	/// 
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	public delegate void WriteCompleteEventHandler(object sender, WriteCompleteEventArgs e);
	/// <summary>
	/// 
	/// </summary>
	public class CancelCompleteEventArgs: EventArgs {
		/// <summary></summary>
		public int transactionID;
		/// <summary></summary>
		public int groupHandleClient;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="transactionIDp"></param>
		/// <param name="groupHandleClientp"></param>
		public CancelCompleteEventArgs(int transactionIDp, int groupHandleClientp) {
			transactionID = transactionIDp;
			groupHandleClient = groupHandleClientp;
		}
	}
	/// <summary>
	/// 
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	public delegate void CancelCompleteEventHandler(object sender, CancelCompleteEventArgs e);
	/// <summary>
	/// class OpcGroup
	/// </summary>
	public class OpcGroup: IOPCDataCallback {
		/// <summary></summary>
		private Logger EventLog;
		/// <summary>itemid, state</summary>
		private Dictionary<int, int> _countItem;
		/// <summary>itemid, quality</summary>
		private Dictionary<int, short> _countItemState;
		/// <summary></summary>
		private int _requestedUpdateRate;
		/// <summary></summary>
		private OPCGroupState state;
		/// <summary></summary>
		private IOPCServer ifServer = null;
		/// <summary></summary>
		private IOPCGroupStateMgt ifMgt = null;
		/// <summary></summary>
		private IOPCItemMgt ifItems = null;
		/// <summary></summary>
		private IOPCSyncIO ifSync = null;
		/// <summary></summary>
		private IOPCAsyncIO2 ifAsync = null;
		/// <summary></summary>
		private IConnectionPointContainer cpointcontainer = null;
		/// <summary></summary>
		private IConnectionPoint callbackcpoint = null;
		/// <summary></summary>
		private int callbackcookie = 0;
		private DateTime lastchange;
		public System.Timers.Timer forceRead;
		/// <summary>
		/// marshaling helpers:
		/// </summary>
		private readonly Type typeOPCITEMDEF;
		/// <summary></summary>
		private readonly int sizeOPCITEMDEF;
		/// <summary></summary>
		private readonly Type typeOPCITEMRESULT;
		/// <summary></summary>
		private readonly int sizeOPCITEMRESULT;
		/// <summary></summary>
		private bool _wpActive;
		/// <summary>
		/// 
		/// </summary>
		public bool wpActive {
			set { this._wpActive = value; }
			get { return this._wpActive; }
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="ifServerLink"></param>
		/// <param name="isPublic"></param>
		/// <param name="groupName"></param>
		/// <param name="setActive"></param>
		/// <param name="requestedUpdateRate"></param>
		internal OpcGroup(ref IOPCServer ifServerLink, bool isPublic, string groupName, bool setActive, int requestedUpdateRate) {
			EventLog = new Logger(wpEventLog.OPCDataGroup);
			ifServer = ifServerLink;

			state.Name = groupName;
			state.Public = isPublic;
			state.UpdateRate = requestedUpdateRate;
			_requestedUpdateRate = requestedUpdateRate;
			state.Active = setActive;
			state.TimeBias = 0;
			state.PercentDeadband = 0.0f;
			state.LocaleID = 0;
			state.HandleClient = this.GetHashCode();
			state.HandleServer = 0;
			_countItem = new Dictionary<int, int>();
			_countItemState = new Dictionary<int, short>();

			// marshaling helpers:
			typeOPCITEMDEF = typeof(OPCITEMDEFintern);
			sizeOPCITEMDEF = Marshal.SizeOf(typeOPCITEMDEF);
			typeOPCITEMRESULT = typeof(OPCITEMRESULTintern);
			sizeOPCITEMRESULT = Marshal.SizeOf(typeOPCITEMRESULT);
		}
		/// <summary>
		/// 
		/// </summary>
		/*
		~OpcGroup() {
			PDebug.Write(String.Format("OPC DATA GRP '{0}' - Finalize", this.Name);
			Remove(false);
		}
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="biasTime"></param>
		/// <param name="percentDeadband"></param>
		/// <param name="localeID"></param>
		internal void internalAdd(int[] biasTime, float[] percentDeadband, int localeID) {
			Type typGrpMgt = typeof(IOPCGroupStateMgt);
			Guid guidGrpTst = typGrpMgt.GUID;

			object objtemp;
			if (state.Public) {
				IOPCServerPublicGroups ifPubGrps = null;
				ifPubGrps = (IOPCServerPublicGroups)ifServer;
				if (ifPubGrps == null)
					Marshal.ThrowExceptionForHR(HRESULTS.E_NOINTERFACE);

				ifPubGrps.GetPublicGroupByName(state.Name, ref guidGrpTst, out objtemp);
				ifPubGrps = null;
			} else {
				ifServer.AddGroup(state.Name, state.Active, state.UpdateRate, state.HandleClient, biasTime, percentDeadband, state.LocaleID,
									out state.HandleServer, out state.UpdateRate, ref guidGrpTst, out objtemp);
			}
			if (objtemp == null)
				Marshal.ThrowExceptionForHR(HRESULTS.E_NOINTERFACE);

			ifMgt = (IOPCGroupStateMgt)objtemp;
			objtemp = null;
			GetStates();

			getinterfaces();
			AdviseIOPCDataCallback();
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="bForce"></param>
		public void Remove(bool bForce) {
			string myname = this.Name;
			//if(wpDebug.debugOPC)
			//	wpDebug.Write("OPC Data Grp '{0}' - Beginn Remove", myname);
			EventLog = null;
			if (!(_countItem == null)) _countItem.Clear();
			_countItem = null;
			if(!(_countItemState == null)) _countItemState.Clear();
			_countItemState = null;
			if(!(ifAsync == null)) SetEnable(false);
			if(!(ifMgt == null)) Active = false;
			//if(wpDebug.debugOPC)
			//	wpDebug.Write("OPC Data Grp '{0}' - Unadvice Callbackcookie", myname);
			if(!(callbackcpoint == null)) {
				try {
					if (callbackcookie != 0) {
						callbackcpoint.Unadvise(callbackcookie);
						callbackcookie = 0;
					}
				} catch(Exception ex) {
					EventLog.WriteError(ex, myname);
				} finally {
					wpDebug.Write("OPC Data Grp '{0}' - Marshal.ReleaseComObject shutdowncpoint", myname);
					int rc = Marshal.FinalReleaseComObject(callbackcpoint);
					callbackcpoint = null;
				}
			}

			cpointcontainer = null;
			ifItems = null;
			ifSync = null;
			ifAsync = null;

			//if(wpDebug.debugOPC)
			//	wpDebug.Write("OPC Data Grp '{0}' - ReleaseComObject", myname);
			if(!(ifMgt == null)) {
				wpDebug.Write("OPC Data Grp '{0}' - Marshal.ReleaseComObject ifMgt", myname);
				int rc = Marshal.FinalReleaseComObject(ifMgt);
				ifMgt = null;
			}
			//if(wpDebug.debugOPC)
			//	wpDebug.Write("OPC DATA GRP '{0}' - ReleasedComObject", myname);

			//if(wpDebug.debugOPC)
			//	wpDebug.Write("OPC DATA GRP '{0}' - RemoveGroup", myname);
			if (!(ifServer == null)) {
				if (!state.Public) {
					try {
						ifServer.RemoveGroup(state.HandleServer, bForce);
					} catch (Exception ex) {
						EventLog.WriteError(ex);
					}
				}
				ifServer = null;
			}
			wpDebug.Write("OPC DATA GRP '{0}' - RemovedGroup", myname);

			state.HandleServer = 0;
		}
		/// <summary>
		/// IOPCServerPublicGroups + IOPCPublicGroupStateMgt
		/// </summary>
		/// <param name="bForce"></param>
		public void DeletePublic(bool bForce) {
			if (!state.Public)
				Marshal.ThrowExceptionForHR(HRESULTS.E_FAIL);

			IOPCServerPublicGroups ifPubGrps = null;
			ifPubGrps = (IOPCServerPublicGroups)ifServer;
			if (ifPubGrps == null)
				Marshal.ThrowExceptionForHR(HRESULTS.E_NOINTERFACE);
			int serverhandle = state.HandleServer;
			Remove(false);
			ifPubGrps.RemovePublicGroup(serverhandle, bForce);
			ifPubGrps = null;
		}
		/// <summary>
		/// 
		/// </summary>
		public void MoveToPublic() {
			if (state.Public)
				Marshal.ThrowExceptionForHR(HRESULTS.E_FAIL);

			IOPCPublicGroupStateMgt ifPubMgt = null;
			ifPubMgt = (IOPCPublicGroupStateMgt)ifMgt;
			if (ifPubMgt == null)
				Marshal.ThrowExceptionForHR(HRESULTS.E_NOINTERFACE);
			ifPubMgt.MoveToPublic();
			ifPubMgt.GetState(out state.Public);
			ifPubMgt = null;
		}
		/// <summary>
		/// IOPCGroupStateMgt
		/// </summary>
		/// <param name="newName"></param>
		public void SetName(string newName) {
			ifMgt.SetName(newName);
			state.Name = newName;
		}
		/// <summary>
		/// 
		/// </summary>
		public void GetStates()	{ // like a refresh
			ifMgt.GetState(out	state.UpdateRate, out state.Active, out state.Name, out state.TimeBias, out state.PercentDeadband,
							out state.LocaleID, out state.HandleClient, out state.HandleServer);
		}
		/// <summary>
		/// 
		/// </summary>
		public OPCGroupState OPCState {
			get { return state; }
		}
		/// <summary>
		/// 
		/// </summary>
		public string Name {
			get { return state.Name; }
			set {
				SetName(value);
			}
		}
		/// <summary>
		/// 
		/// </summary>
		public bool Active {
			get { return state.Active; }
			set {
				ifMgt.SetState(null, out state.UpdateRate, new bool[1] { value }, null, null, null, null);
				state.Active = value;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		public bool Public {
			get { return state.Public; }
		}
		/// <summary>
		/// 
		/// </summary>
		public int UpdateRate {
			get { return state.UpdateRate; }
			set {
				ifMgt.SetState(new int[1] { value }, out state.UpdateRate, null, null, null, null, null);
				_requestedUpdateRate = value;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		public int TimeBias {
			get { return state.TimeBias; }
			set {
				ifMgt.SetState(null, out state.UpdateRate, null, new int[1] { value }, null, null, null);
				state.TimeBias = value;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		public float PercentDeadband {
			get { return state.PercentDeadband; }
			set {
				ifMgt.SetState(null, out state.UpdateRate, null, null, new float[1] { value }, null, null);
				state.PercentDeadband = value;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		public int LocaleID {
			get { return state.LocaleID; }
			set {
				ifMgt.SetState(null, out state.UpdateRate, null, null, null, new int[1] { value }, null);
				state.LocaleID = value;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		public int HandleClient {
			get { return state.HandleClient; }
			set {
				ifMgt.SetState(null, out state.UpdateRate, null, null, null, null, new int[1] { value });
				state.HandleClient = value;
			}
		}
		public DateTime LastChange {
			get { return lastchange; }
		}
		public int RequestedUpdateRate {
			get { return _requestedUpdateRate; }
		}
		/// <summary>
		/// 
		/// </summary>
		public int HandleServer {
			get { return state.HandleServer; }
		}
		public Dictionary<int, int> countItem {
			get { return _countItem; }
		}
		public Dictionary<int, short> countItemState {
			get { return _countItemState; }
		}
		/// <summary>
		/// IOPCItemMgt
		/// </summary>
		/// <param name="arrDef"></param>
		/// <param name="arrRes"></param>
		/// <returns></returns>
		public bool AddItems(OPCItemDef[] arrDef, out OPCItemResult[] arrRes) {
			arrRes = null;
			bool hasblobs = false;
			int count = arrDef.Length;

			IntPtr ptrDef = Marshal.AllocCoTaskMem(count * sizeOPCITEMDEF);
			int runDef = (int)ptrDef;
			OPCITEMDEFintern idf = new OPCITEMDEFintern();
			idf.wReserved = 0;
			foreach (OPCItemDef d in arrDef) {
				idf.szAccessPath = d.AccessPath;
				idf.szItemID = d.ItemID;
				idf.bActive = d.Active;
				idf.hClient = d.HandleClient;

				if (_countItem.ContainsKey(d.HandleClient)) _countItem[d.HandleClient] = HRESULTS.CONNECT_E_NOCONNECTION;
				else _countItem.Add(d.HandleClient, HRESULTS.CONNECT_E_NOCONNECTION);
				if (_countItemState.ContainsKey(d.HandleClient)) _countItemState[d.HandleClient] = (short)OPC_QUALITY_STATUS.NOT_CONNECTED;
				else _countItemState.Add(d.HandleClient, (short)OPC_QUALITY_STATUS.NOT_CONNECTED);

				idf.vtRequestedDataType = (short)d.RequestedDataType;
				idf.dwBlobSize = 0; idf.pBlob = IntPtr.Zero;
				if (d.Blob != null) {
					idf.dwBlobSize = d.Blob.Length;
					if (idf.dwBlobSize > 0) {
						hasblobs = true;
						idf.pBlob = Marshal.AllocCoTaskMem(idf.dwBlobSize);
						Marshal.Copy(d.Blob, 0, idf.pBlob, idf.dwBlobSize);
					}
				}

				Marshal.StructureToPtr(idf, (IntPtr)runDef, false);
				runDef += sizeOPCITEMDEF;
			}

			IntPtr ptrRes;
			IntPtr ptrErr;
			int hresult = ifItems.AddItems(count, ptrDef, out ptrRes, out ptrErr);

			runDef = (int)ptrDef;
			if (hasblobs) {
				for (int i = 0; i < count; i++) {
					IntPtr blob = (IntPtr)Marshal.ReadInt32((IntPtr)(runDef + 20));
					if (blob != IntPtr.Zero)
						Marshal.FreeCoTaskMem(blob);
					Marshal.DestroyStructure((IntPtr)runDef, typeOPCITEMDEF);
					runDef += sizeOPCITEMDEF;
				}
			} else {
				for (int i = 0; i < count; i++) {
					Marshal.DestroyStructure((IntPtr)runDef, typeOPCITEMDEF);
					runDef += sizeOPCITEMDEF;
				}
			}
			Marshal.FreeCoTaskMem(ptrDef);

			if (HRESULTS.Failed(hresult))
				Marshal.ThrowExceptionForHR(hresult);

			int runRes = (int)ptrRes;
			int runErr = (int)ptrErr;
			if ((runRes == 0) || (runErr == 0))
				Marshal.ThrowExceptionForHR(HRESULTS.E_ABORT);

			arrRes = new OPCItemResult[count];
			for (int i = 0; i < count; i++) {
				arrRes[i] = new OPCItemResult();
				arrRes[i].Error = Marshal.ReadInt32((IntPtr)runErr);

				if (_countItem.ContainsKey(arrDef[i].HandleClient)) _countItem[arrDef[i].HandleClient] = arrRes[i].Error;
				else _countItem.Add(arrDef[i].HandleClient, arrRes[i].Error);

				if (HRESULTS.Failed(arrRes[i].Error)) {
					runRes += sizeOPCITEMRESULT;
					runErr += 4;
					continue;
				}

				arrRes[i].HandleServer = Marshal.ReadInt32((IntPtr)runRes);
				arrRes[i].CanonicalDataType = (VarEnum)(int)Marshal.ReadInt16((IntPtr)(runRes + 4));
				arrRes[i].AccessRights = (OPCACCESSRIGHTS)Marshal.ReadInt32((IntPtr)(runRes + 8));

				int ptrblob = Marshal.ReadInt32((IntPtr)(runRes + 16));
				if ((ptrblob != 0)) {
					int blobsize = Marshal.ReadInt32((IntPtr)(runRes + 12));
					if (blobsize > 0) {
						arrRes[i].Blob = new byte[blobsize];
						Marshal.Copy((IntPtr)ptrblob, arrRes[i].Blob, 0, blobsize);
					}
					Marshal.FreeCoTaskMem((IntPtr)ptrblob);
				}

				runRes += sizeOPCITEMRESULT;
				runErr += 4;
			}

			Marshal.FreeCoTaskMem(ptrRes);
			Marshal.FreeCoTaskMem(ptrErr);
			return hresult == HRESULTS.S_OK;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="arrDef"></param>
		/// <param name="blobUpd"></param>
		/// <param name="arrRes"></param>
		/// <returns></returns>
		public bool ValidateItems(OPCItemDef[] arrDef, bool blobUpd,
									out OPCItemResult[] arrRes) {
			arrRes = null;
			bool hasblobs = false;
			int count = arrDef.Length;

			IntPtr ptrDef = Marshal.AllocCoTaskMem(count * sizeOPCITEMDEF);
			int runDef = (int)ptrDef;
			OPCITEMDEFintern idf = new OPCITEMDEFintern();
			idf.wReserved = 0;
			foreach (OPCItemDef d in arrDef) {
				idf.szAccessPath = d.AccessPath;
				idf.szItemID = d.ItemID;
				idf.bActive = d.Active;
				idf.hClient = d.HandleClient;

				if (_countItem.ContainsKey(d.HandleClient)) _countItem[d.HandleClient] = HRESULTS.CONNECT_E_NOCONNECTION;
				else _countItem.Add(d.HandleClient, HRESULTS.CONNECT_E_NOCONNECTION);
				if (_countItemState.ContainsKey(d.HandleClient)) _countItemState[d.HandleClient] = (short)OPC_QUALITY_STATUS.NOT_CONNECTED;
				else _countItemState.Add(d.HandleClient, (short)OPC_QUALITY_STATUS.NOT_CONNECTED);

				idf.vtRequestedDataType = (short)d.RequestedDataType;
				idf.dwBlobSize = 0; idf.pBlob = IntPtr.Zero;
				if (d.Blob != null) {
					idf.dwBlobSize = d.Blob.Length;
					if (idf.dwBlobSize > 0) {
						hasblobs = true;
						idf.pBlob = Marshal.AllocCoTaskMem(idf.dwBlobSize);
						Marshal.Copy(d.Blob, 0, idf.pBlob, idf.dwBlobSize);
					}
				}

				Marshal.StructureToPtr(idf, (IntPtr)runDef, false);
				runDef += sizeOPCITEMDEF;
			}

			IntPtr ptrRes;
			IntPtr ptrErr;
			int hresult = ifItems.ValidateItems(count, ptrDef, blobUpd, out ptrRes, out ptrErr);

			runDef = (int)ptrDef;
			if (hasblobs) {
				for (int i = 0; i < count; i++) {
					IntPtr blob = (IntPtr)Marshal.ReadInt32((IntPtr)(runDef + 20));
					if (blob != IntPtr.Zero)
						Marshal.FreeCoTaskMem(blob);
					Marshal.DestroyStructure((IntPtr)runDef, typeOPCITEMDEF);
					runDef += sizeOPCITEMDEF;
				}
			} else {
				for (int i = 0; i < count; i++) {
					Marshal.DestroyStructure((IntPtr)runDef, typeOPCITEMDEF);
					runDef += sizeOPCITEMDEF;
				}
			}
			Marshal.FreeCoTaskMem(ptrDef);

			if (HRESULTS.Failed(hresult))
				Marshal.ThrowExceptionForHR(hresult);

			int runRes = (int)ptrRes;
			int runErr = (int)ptrErr;
			if ((runRes == 0) || (runErr == 0))
				Marshal.ThrowExceptionForHR(HRESULTS.E_ABORT);

			arrRes = new OPCItemResult[count];
			for (int i = 0; i < count; i++) {
				arrRes[i] = new OPCItemResult();
				arrRes[i].Error = Marshal.ReadInt32((IntPtr)runErr);

				if (HRESULTS.Failed(arrRes[i].Error)) {
					runRes += sizeOPCITEMRESULT;
					runErr += 4;
					continue;
				}

				arrRes[i].HandleServer = Marshal.ReadInt32((IntPtr)runRes);
				arrRes[i].CanonicalDataType = (VarEnum)(int)Marshal.ReadInt16((IntPtr)(runRes + 4));
				arrRes[i].AccessRights = (OPCACCESSRIGHTS)Marshal.ReadInt32((IntPtr)(runRes + 8));

				int ptrblob = Marshal.ReadInt32((IntPtr)(runRes + 16));
				if ((ptrblob != 0)) {
					int blobsize = Marshal.ReadInt32((IntPtr)(runRes + 12));
					if (blobsize > 0) {
						arrRes[i].Blob = new byte[blobsize];
						Marshal.Copy((IntPtr)ptrblob, arrRes[i].Blob, 0, blobsize);
					}
					Marshal.FreeCoTaskMem((IntPtr)ptrblob);
				}

				runRes += sizeOPCITEMRESULT;
				runErr += 4;
			}

			Marshal.FreeCoTaskMem(ptrRes);
			Marshal.FreeCoTaskMem(ptrErr);
			return hresult == HRESULTS.S_OK;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="arrHSrv"></param>
		/// <param name="arrErr"></param>
		/// <returns></returns>
		public bool RemoveItems(int[] arrHSrv, out int[] arrErr) {
			arrErr = null;
			int count = arrHSrv.Length;
			IntPtr ptrErr;
			int hresult = HRESULTS.S_OK;
			if(count > 0) {
				hresult = ifItems.RemoveItems(count, arrHSrv, out ptrErr);
				if (HRESULTS.Failed(hresult))
					Marshal.ThrowExceptionForHR(hresult);
				arrErr = new int[count];
				Marshal.Copy(ptrErr, arrErr, 0, count);
				Marshal.FreeCoTaskMem(ptrErr);
			}
			return hresult == HRESULTS.S_OK;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="arrHSrv"></param>
		/// <param name="activate"></param>
		/// <param name="arrErr"></param>
		/// <returns></returns>
		public bool SetActiveState(int[] arrHSrv, bool activate, out int[] arrErr) {
			arrErr = null;
			int count = arrHSrv.Length;
			IntPtr ptrErr;
			int hresult = ifItems.SetActiveState(count, arrHSrv, activate, out ptrErr);
			if (HRESULTS.Failed(hresult))
				Marshal.ThrowExceptionForHR(hresult);

			arrErr = new int[count];
			Marshal.Copy(ptrErr, arrErr, 0, count);
			Marshal.FreeCoTaskMem(ptrErr);
			return hresult == HRESULTS.S_OK;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="arrHSrv"></param>
		/// <param name="arrHClt"></param>
		/// <param name="arrErr"></param>
		/// <returns></returns>
		public bool SetClientHandles(int[] arrHSrv, int[] arrHClt, out int[] arrErr) {
			arrErr = null;
			int count = arrHSrv.Length;
			if (count != arrHClt.Length)
				Marshal.ThrowExceptionForHR(HRESULTS.E_ABORT);

			IntPtr ptrErr;
			int hresult = ifItems.SetClientHandles(count, arrHSrv, arrHClt, out ptrErr);
			if (HRESULTS.Failed(hresult))
				Marshal.ThrowExceptionForHR(hresult);

			arrErr = new int[count];
			Marshal.Copy(ptrErr, arrErr, 0, count);
			Marshal.FreeCoTaskMem(ptrErr);
			return hresult == HRESULTS.S_OK;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="arrHSrv"></param>
		/// <param name="arrVT"></param>
		/// <param name="arrErr"></param>
		/// <returns></returns>
		public bool SetDatatypes(int[] arrHSrv, VarEnum[] arrVT, out int[] arrErr) {
			arrErr = null;
			int count = arrHSrv.Length;
			if (count != arrVT.Length)
				Marshal.ThrowExceptionForHR(HRESULTS.E_ABORT);

			IntPtr ptrVT = Marshal.AllocCoTaskMem(count * 2);
			int runVT = (int)ptrVT;
			foreach (VarEnum v in arrVT) {
				Marshal.WriteInt16((IntPtr)runVT, (short)v);
				runVT += 2;
			}

			IntPtr ptrErr;
			int hresult = ifItems.SetDatatypes(count, arrHSrv, ptrVT, out ptrErr);

			Marshal.FreeCoTaskMem(ptrVT);

			if (HRESULTS.Failed(hresult))
				Marshal.ThrowExceptionForHR(hresult);

			arrErr = new int[count];
			Marshal.Copy(ptrErr, arrErr, 0, count);
			Marshal.FreeCoTaskMem(ptrErr);
			return hresult == HRESULTS.S_OK;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public OpcEnumItemAttributes CreateAttrEnumerator() {
			Type typEnuAtt = typeof(IEnumOPCItemAttributes);
			Guid guidEnuAtt = typEnuAtt.GUID;
			object objtemp;

			int hresult = ifItems.CreateEnumerator(ref guidEnuAtt, out objtemp);
			if (HRESULTS.Failed(hresult))
				Marshal.ThrowExceptionForHR(hresult);
			if ((hresult == HRESULTS.S_FALSE) || (objtemp == null))
				return null;

			IEnumOPCItemAttributes ifenu = (IEnumOPCItemAttributes)objtemp;
			objtemp = null;

			OpcEnumItemAttributes enu = new OpcEnumItemAttributes(ifenu);
			return enu;
		}
		/// <summary>
		/// IOPCSyncIO
		/// </summary>
		/// <param name="src"></param>
		/// <param name="arrHSrv"></param>
		/// <param name="arrStat"></param>
		/// <returns></returns>
		public bool Read(OPCDATASOURCE src, int[] arrHSrv, out OPCItemState[] arrStat) {
			arrStat = null;
			int count = arrHSrv.Length;
			IntPtr ptrStat;
			IntPtr ptrErr;
			lastchange = DateTime.Now;
			int hresult = ifSync.Read(src, count, arrHSrv, out ptrStat, out ptrErr);
			if (HRESULTS.Failed(hresult))
				Marshal.ThrowExceptionForHR(hresult);

			int runErr = (int)ptrErr;
			int runStat = (int)ptrStat;
			if ((runErr == 0) || (runStat == 0))
				Marshal.ThrowExceptionForHR(HRESULTS.E_ABORT);

			arrStat = new OPCItemState[count];
			for (int i = 0; i < count; i++) {														// WORKAROUND !!!
				arrStat[i] = new OPCItemState();

				arrStat[i].Error = Marshal.ReadInt32((IntPtr)runErr);
				runErr += 4;

				arrStat[i].HandleClient = Marshal.ReadInt32((IntPtr)runStat);

				if (HRESULTS.Succeeded(arrStat[i].Error)) {
					short vt = Marshal.ReadInt16((IntPtr)(runStat + 16));
					if (vt == (short)VarEnum.VT_ERROR)
						arrStat[i].Error = Marshal.ReadInt32((IntPtr)(runStat + 24));

					arrStat[i].TimeStamp = Marshal.ReadInt64((IntPtr)(runStat + 4));
					arrStat[i].Quality = Marshal.ReadInt16((IntPtr)(runStat + 12));
					arrStat[i].DataValue = Marshal.GetObjectForNativeVariant((IntPtr)(runStat + 16));

					if (_countItemState.ContainsKey(arrStat[i].HandleClient))
						_countItemState[arrStat[i].HandleClient] = arrStat[i].Quality;
					else _countItemState.Add(arrStat[i].HandleClient, arrStat[i].Quality);

					DUMMY_VARIANT.VariantClear((IntPtr)(runStat + 16));
				} else
					arrStat[i].DataValue = null;

				runStat += 32;
			}

			Marshal.FreeCoTaskMem(ptrStat);
			Marshal.FreeCoTaskMem(ptrErr);
			return hresult == HRESULTS.S_OK;
		}
		/// <summary>
		/// IOPCSyncIO
		/// </summary>
		/// <param name="arrHSrv"></param>
		/// <param name="arrVal"></param>
		/// <param name="arrErr"></param>
		/// <returns></returns>
		public bool Write(int[] arrHSrv, object[] arrVal, out int[] arrErr) {
			arrErr = null;
			int count = arrHSrv.Length;
			if (count != arrVal.Length)
				Marshal.ThrowExceptionForHR(HRESULTS.E_ABORT);

			IntPtr ptrErr;
			int hresult = ifSync.Write(count, arrHSrv, arrVal, out ptrErr);
			if (HRESULTS.Failed(hresult))
				Marshal.ThrowExceptionForHR(hresult);

			arrErr = new int[count];
			Marshal.Copy(ptrErr, arrErr, 0, count);
			Marshal.FreeCoTaskMem(ptrErr);
			return hresult == HRESULTS.S_OK;
		}
		/// <summary>
		/// IOPCAsyncIO2
		/// </summary>
		/// <param name="arrHSrv"></param>
		/// <param name="transactionID"></param>
		/// <param name="cancelID"></param>
		/// <param name="arrErr"></param>
		/// <returns></returns>
		public bool Read(int[] arrHSrv, int transactionID, out int cancelID, out int[] arrErr) {
			arrErr = null;
			cancelID = 0;
			int count = arrHSrv.Length;

			IntPtr ptrErr;
			int hresult = ifAsync.Read(count, arrHSrv, transactionID, out cancelID, out ptrErr);
			if (HRESULTS.Failed(hresult))
				Marshal.ThrowExceptionForHR(hresult);

			arrErr = new int[count];
			Marshal.Copy(ptrErr, arrErr, 0, count);
			Marshal.FreeCoTaskMem(ptrErr);
			if(wpDebug.debugTransferID)
				wpDebug.Write("Async Read (TAID-{0})", transactionID);
			return hresult == HRESULTS.S_OK;
		}
		/// <summary>
		/// IOPCAsyncIO2
		/// </summary>
		/// <param name="arrHSrv"></param>
		/// <param name="arrVal"></param>
		/// <param name="transactionID"></param>
		/// <param name="cancelID"></param>
		/// <param name="arrErr"></param>
		/// <returns></returns>
		public bool Write(int[] arrHSrv, object[] arrVal, int transactionID,
							out int cancelID, out int[] arrErr) {
			arrErr = null;
			cancelID = 0;
			int count = arrHSrv.Length;
			if (count != arrVal.Length)
				Marshal.ThrowExceptionForHR(HRESULTS.E_ABORT);

			IntPtr ptrErr;
			int hresult = ifAsync.Write(count, arrHSrv, arrVal, transactionID, out cancelID, out ptrErr);
			if (HRESULTS.Failed(hresult))
				Marshal.ThrowExceptionForHR(hresult);

			arrErr = new int[count];
			Marshal.Copy(ptrErr, arrErr, 0, count);
			Marshal.FreeCoTaskMem(ptrErr);
			if(wpDebug.debugTransferID)
				wpDebug.Write("Async Write (TAID-{0})", transactionID);
			return hresult == HRESULTS.S_OK;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sourceMode"></param>
		/// <param name="transactionID"></param>
		/// <param name="cancelID"></param>
		public void Refresh2(OPCDATASOURCE sourceMode, int transactionID, out int cancelID) {
			ifAsync.Refresh2(sourceMode, transactionID, out cancelID);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="cancelID"></param>
		public void Cancel2(int cancelID) {
			ifAsync.Cancel2(cancelID);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="doEnable"></param>
		public void SetEnable(bool doEnable) {
			ifAsync.SetEnable(doEnable);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="isEnabled"></param>
		public void GetEnable(out bool isEnabled) {
			ifAsync.GetEnable(out isEnabled);
		}
		/// <summary>
		/// IOPCDataCallback
		/// </summary>
		/// <param name="dwTransid"></param>
		/// <param name="hGroup"></param>
		/// <param name="hrMasterquality"></param>
		/// <param name="hrMastererror"></param>
		/// <param name="dwCount"></param>
		/// <param name="phClientItems"></param>
		/// <param name="pvValues"></param>
		/// <param name="pwQualities"></param>
		/// <param name="pftTimeStamps"></param>
		/// <param name="ppErrors"></param>
		void IOPCDataCallback.OnDataChange(
				int dwTransid, int hGroup, int hrMasterquality, int hrMastererror, int dwCount,
				IntPtr phClientItems, IntPtr pvValues, IntPtr pwQualities, IntPtr pftTimeStamps, IntPtr ppErrors) {
			if(wpDebug.debugOPC) {
				wpDebug.Write("OpcGroup.OnDataChange");
			}
			if ((dwCount == 0) || (hGroup != state.HandleClient))
				return;
			int count = (int)dwCount;

			int runh = (int)phClientItems;
			int runv = (int)pvValues;
			int runq = (int)pwQualities;
			int runt = (int)pftTimeStamps;
			int rune = (int)ppErrors;
			lastchange = DateTime.Now;

			DataChangeEventArgs e = new DataChangeEventArgs();
			e.transactionID = dwTransid;
			e.groupHandleClient = hGroup;
			e.masterQuality = hrMasterquality;
			e.masterError = hrMastererror;
			e.lastChange = lastchange;
			e.sts = new OPCItemState[count];

			for (int i = 0; i < count; i++) {
				e.sts[i] = new OPCItemState();
				e.sts[i].Error = Marshal.ReadInt32((IntPtr)rune);
				rune += 4;

				e.sts[i].HandleClient = Marshal.ReadInt32((IntPtr)runh);
				runh += 4;

				if (HRESULTS.Succeeded(e.sts[i].Error)) {
					short vt = Marshal.ReadInt16((IntPtr)runv);
					if (vt == (short)VarEnum.VT_ERROR)
						e.sts[i].Error = Marshal.ReadInt32((IntPtr)(runv + 8));

					e.sts[i].CanonicalDataType = (VarEnum)vt;
					e.sts[i].DataValue = Marshal.GetObjectForNativeVariant((IntPtr)runv);
					e.sts[i].Quality = Marshal.ReadInt16((IntPtr)runq);
					e.sts[i].TimeStamp = Marshal.ReadInt64((IntPtr)runt);

					if (_countItemState.ContainsKey(e.sts[i].HandleClient)) _countItemState[e.sts[i].HandleClient] = e.sts[i].Quality;
					else _countItemState.Add(e.sts[i].HandleClient, e.sts[i].Quality);
				}

				runv += DUMMY_VARIANT.ConstSize;
				runq += 2;
				runt += 8;
			}

			if (DataChanged != null)
				DataChanged(this, e);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwTransid"></param>
		/// <param name="hGroup"></param>
		/// <param name="hrMasterquality"></param>
		/// <param name="hrMastererror"></param>
		/// <param name="dwCount"></param>
		/// <param name="phClientItems"></param>
		/// <param name="pvValues"></param>
		/// <param name="pwQualities"></param>
		/// <param name="pftTimeStamps"></param>
		/// <param name="ppErrors"></param>
		void IOPCDataCallback.OnReadComplete(
				int dwTransid, int hGroup, int hrMasterquality, int hrMastererror, int dwCount,
				IntPtr phClientItems, IntPtr pvValues, IntPtr pwQualities, IntPtr pftTimeStamps, IntPtr ppErrors) {
			if(wpDebug.debugOPC) {
				wpDebug.Write("OpcGroup.OnReadComplete");
			}
			if ((dwCount == 0) || (hGroup != state.HandleClient))
				return;
			int count = (int)dwCount;

			int runh = (int)phClientItems;
			int runv = (int)pvValues;
			int runq = (int)pwQualities;
			int runt = (int)pftTimeStamps;
			int rune = (int)ppErrors;
			lastchange = DateTime.Now;

			ReadCompleteEventArgs e = new ReadCompleteEventArgs();
			e.transactionID = dwTransid;
			e.groupHandleClient = hGroup;
			e.masterQuality = hrMasterquality;
			e.masterError = hrMastererror;
			e.lastChange = lastchange;
			e.sts = new OPCItemState[count];

			for (int i = 0; i < count; i++) {
				e.sts[i] = new OPCItemState();
				e.sts[i].Error = Marshal.ReadInt32((IntPtr)rune);
				rune += 4;

				e.sts[i].HandleClient = Marshal.ReadInt32((IntPtr)runh);
				runh += 4;

				if (HRESULTS.Succeeded(e.sts[i].Error)) {
					short vt = Marshal.ReadInt16((IntPtr)runv);
					if (vt == (short)VarEnum.VT_ERROR)
						e.sts[i].Error = Marshal.ReadInt32((IntPtr)(runv + 8));

					e.sts[i].CanonicalDataType = (VarEnum)vt;
					e.sts[i].DataValue = Marshal.GetObjectForNativeVariant((IntPtr)runv);
					e.sts[i].Quality = Marshal.ReadInt16((IntPtr)runq);
					e.sts[i].TimeStamp = Marshal.ReadInt64((IntPtr)runt);

					if (_countItemState.ContainsKey(e.sts[i].HandleClient)) _countItemState[e.sts[i].HandleClient] = e.sts[i].Quality;
					else _countItemState.Add(e.sts[i].HandleClient, e.sts[i].Quality);
				}

				runv += DUMMY_VARIANT.ConstSize;
				runq += 2;
				runt += 8;
			}

			if (ReadCompleted != null)
				ReadCompleted(this, e);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwTransid"></param>
		/// <param name="hGroup"></param>
		/// <param name="hrMastererr"></param>
		/// <param name="dwCount"></param>
		/// <param name="pClienthandles"></param>
		/// <param name="ppErrors"></param>
		void IOPCDataCallback.OnWriteComplete(
				int dwTransid, int hGroup, int hrMastererr, int dwCount,
				IntPtr pClienthandles, IntPtr ppErrors) {
			if(wpDebug.debugOPC) {
				wpDebug.Write("OpcGroup.OnWriteComplete");
			}
			if ((dwCount == 0) || (hGroup != state.HandleClient))
				return;
			int count = (int)dwCount;

			int runh = (int)pClienthandles;
			int rune = (int)ppErrors;

			WriteCompleteEventArgs e = new WriteCompleteEventArgs();
			e.transactionID = dwTransid;
			e.groupHandleClient = hGroup;
			e.masterError = hrMastererr;
			e.res = new OPCWriteResult[count];

			for (int i = 0; i < count; i++) {
				e.res[i] = new OPCWriteResult();

				e.res[i].Error = Marshal.ReadInt32((IntPtr)rune);
				rune += 4;

				e.res[i].HandleClient = Marshal.ReadInt32((IntPtr)runh);
				runh += 4;
			}

			if (WriteCompleted != null)
				WriteCompleted(this, e);
		}

		void IOPCDataCallback.OnCancelComplete(int dwTransid, int hGroup) {
			if(wpDebug.debugOPC) {
				wpDebug.Write("OpcGroup.OnCancelComplete");
			}
			if (hGroup != state.HandleClient)
				return;

			CancelCompleteEventArgs e = new CancelCompleteEventArgs(dwTransid, hGroup);
			if (CancelCompleted != null)
				CancelCompleted(this, e);
		}
		/// <summary>
		/// events
		/// </summary>
		public event DataChangeEventHandler DataChanged;
		public bool hasDataChanged() {
			if(DataChanged != null && DataChanged.GetInvocationList().Length > 0) return true;
			else return false;
		}
		/// <summary></summary>
		public event ReadCompleteEventHandler ReadCompleted;
		public bool hasReadCompleted() {
			if(ReadCompleted != null && ReadCompleted.GetInvocationList().Length > 0) return true;
			else return false;
		}
		/// <summary></summary>
		public event WriteCompleteEventHandler WriteCompleted;
		public bool hasWriteCompleted() {
			if(WriteCompleted != null && WriteCompleted.GetInvocationList().Length > 0) return true;
			else return false;
		}
		/// <summary></summary>
		public event CancelCompleteEventHandler CancelCompleted;
		public bool hasCancelCompleted() {
			if(CancelCompleted != null && CancelCompleted.GetInvocationList().Length > 0) return true;
			else return false;
		}
		/// <summary>
		/// helper
		/// </summary>
		/// <param name="Quality"></param>
		/// <returns></returns>
		public static string QualityToString(short Quality) {
			StringBuilder sb = new StringBuilder(256);
			OPC_QUALITY_MASTER oqm = (OPC_QUALITY_MASTER)(Quality & (short)OPC_QUALITY_MASKS.MASTER_MASK);
			OPC_QUALITY_STATUS oqs = (OPC_QUALITY_STATUS)(Quality & (short)OPC_QUALITY_MASKS.STATUS_MASK);
			OPC_QUALITY_LIMIT oql = (OPC_QUALITY_LIMIT)(Quality & (short)OPC_QUALITY_MASKS.LIMIT_MASK);
			sb.AppendFormat("{0} - {1} - {2}", oqm, oqs, oql);
			return sb.ToString();
		}
		/// <summary>
		/// private
		/// </summary>
		private void getinterfaces() {
			ifItems = (IOPCItemMgt)ifMgt;
			ifSync = (IOPCSyncIO)ifMgt;
			ifAsync = (IOPCAsyncIO2)ifMgt;

			cpointcontainer = (IConnectionPointContainer)ifMgt;
		}
		/// <summary>
		/// 
		/// </summary>
		private void AdviseIOPCDataCallback() {
			Type sinktype = typeof(IOPCDataCallback);
			Guid sinkguid = sinktype.GUID;

			cpointcontainer.FindConnectionPoint(ref sinkguid, out callbackcpoint);
			if (callbackcpoint == null)
				return;

			callbackcpoint.Advise(this, out callbackcookie);
		}
	}
	/// <summary>
	/// class OpcEnumItemAttributes
	/// </summary>
	public class OpcEnumItemAttributes {
		/// <summary></summary>
		private IEnumOPCItemAttributes ifEnum;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="ifEnump"></param>
		internal OpcEnumItemAttributes(IEnumOPCItemAttributes ifEnump) {
			ifEnum = ifEnump;
		}
		/// <summary>
		/// 
		/// </summary>
		~OpcEnumItemAttributes() { Dispose(); }
		/// <summary>
		/// 
		/// </summary>
		public void Dispose() {
			if (!(ifEnum == null)) {
				int rc = Marshal.ReleaseComObject(ifEnum);
				ifEnum = null;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="enumcountmax"></param>
		/// <param name="attributes"></param>
		public void Next(int enumcountmax, out OPCItemAttributes[] attributes) {
			attributes = null;

			IntPtr ptrAtt;
			int count;
			ifEnum.Next(enumcountmax, out ptrAtt, out count);
			int runatt = (int)ptrAtt;
			if ((runatt == 0) || (count <= 0) || (count > enumcountmax))
				return;

			attributes = new OPCItemAttributes[count];
			IntPtr ptrString;

			for (int i = 0; i < count; i++) {
				attributes[i] = new OPCItemAttributes();

				ptrString = (IntPtr)Marshal.ReadInt32((IntPtr)runatt);
				attributes[i].AccessPath = Marshal.PtrToStringUni(ptrString);
				Marshal.FreeCoTaskMem(ptrString);

				ptrString = (IntPtr)Marshal.ReadInt32((IntPtr)(runatt + 4));
				attributes[i].ItemID = Marshal.PtrToStringUni(ptrString);
				Marshal.FreeCoTaskMem(ptrString);

				attributes[i].Active = (Marshal.ReadInt32((IntPtr)(runatt + 8))) != 0;
				attributes[i].HandleClient = Marshal.ReadInt32((IntPtr)(runatt + 12));
				attributes[i].HandleServer = Marshal.ReadInt32((IntPtr)(runatt + 16));
				attributes[i].AccessRights = (OPCACCESSRIGHTS)Marshal.ReadInt32((IntPtr)(runatt + 20));
				attributes[i].RequestedDataType = (VarEnum)Marshal.ReadInt16((IntPtr)(runatt + 32));
				attributes[i].CanonicalDataType = (VarEnum)Marshal.ReadInt16((IntPtr)(runatt + 34));

				attributes[i].EUType = (OPCEUTYPE)Marshal.ReadInt32((IntPtr)(runatt + 36));
				attributes[i].EUInfo = Marshal.GetObjectForNativeVariant((IntPtr)(runatt + 40));
				DUMMY_VARIANT.VariantClear((IntPtr)(runatt + 40));

				int ptrblob = Marshal.ReadInt32((IntPtr)(runatt + 28));
				if ((ptrblob != 0)) {
					int blobsize = Marshal.ReadInt32((IntPtr)(runatt + 24));
					if (blobsize > 0) {
						attributes[i].Blob = new byte[blobsize];
						Marshal.Copy((IntPtr)ptrblob, attributes[i].Blob, 0, blobsize);
					}
					Marshal.FreeCoTaskMem((IntPtr)ptrblob);
				}

				runatt += 56;
			}

			Marshal.FreeCoTaskMem(ptrAtt);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="celt"></param>
		public void Skip(int celt) { ifEnum.Skip(celt); }
		/// <summary>
		/// 
		/// </summary>
		public void Reset() { ifEnum.Reset(); }
	}
}
/** @} */
