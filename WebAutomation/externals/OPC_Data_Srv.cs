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
//# Revision     : $Rev:: 237                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: OPC_Data_Srv.cs 237 2025-05-30 11:23:27Z                 $ #
//#                                                                                 #
//###################################################################################
using FreakaZone.Libraries.wpEventLog;
using OPC.Common;
using OPC.Data.Interface;
using System;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using WebAutomation.externals;
/**
 * @addtogroup externals
 * @{
 */
namespace OPC.Data {
	// ------------- managed side only structs ----------------------
	/// <summary>
	/// QueryAvailableProperties
	/// </summary>
	public class OPCProperty {
		/// <summary></summary>
		public int PropertyID;
		/// <summary></summary>
		public string Description;
		/// <summary></summary>
		public VarEnum DataType;
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			return "ID:" + PropertyID + " '" + Description + "' T:" + DUMMY_VARIANT.VarEnumToString(DataType);
		}
	}
	/// <summary>
	/// GetItemProperties
	/// </summary>
	public class OPCPropertyData {
		/// <summary></summary>
		public int PropertyID;
		/// <summary></summary>
		public int Error;
		/// <summary></summary>
		public object Data;
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			if(Error == HRESULTS.S_OK)
				return "ID:" + PropertyID + " Data:" + Data.ToString();
			else
				return "ID:" + PropertyID + " Error:" + Error.ToString();
		}
	}
	/// <summary>
	/// LookupItemIDs
	/// </summary>
	public class OPCPropertyItem {
		/// <summary></summary>
		public int PropertyID;
		/// <summary></summary>
		public int Error;
		/// <summary></summary>
		public string newItemID;
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			if(Error == HRESULTS.S_OK)
				return "ID:" + PropertyID + " newID:" + newItemID;
			else
				return "ID:" + PropertyID + " Error:" + Error.ToString();
		}
	}

	// ----------------- event argument+handler ------------------------
	/// <summary>
	/// 
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	public delegate void ShutdownRequestEventHandler(object sender, ShutdownRequestEventArgs e);
	/// <summary>
	/// IOPCShutdown
	/// </summary>
	public class ShutdownRequestEventArgs: EventArgs {
		/// <summary></summary>
		public string shutdownReason;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="shutdownReasonp"></param>
		public ShutdownRequestEventArgs(string shutdownReasonp) {
			shutdownReason = shutdownReasonp;
		}
	}

	// --------------------------- OpcServer ------------------------
	/// <summary>
	/// OpcServer
	/// </summary>
	[ComVisible(true)]
	public class OpcServer: IOPCShutdown {
		/// <summary></summary>
		private Logger eventLog;
		/// <summary></summary>
		private string _name;
		/// <summary></summary>
		public string Name {
			get { return _name; }
			set { _name = value; }
		}
		/// <summary></summary>
		private object OPCserverObj = null;
		/// <summary></summary>
		private IOPCServer ifServer = null;
		/// <summary></summary>
		private IOPCCommon ifCommon = null;
		/// <summary></summary>
		private IOPCBrowseServerAddressSpace ifBrowse = null;
		/// <summary></summary>
		private IOPCItemProperties ifItmProps = null;
		/// <summary></summary>
		private IConnectionPointContainer cpointcontainer = null;
		/// <summary></summary>
		private IConnectionPoint shutdowncpoint = null;
		/// <summary></summary>
		private int shutdowncookie = 0;
		/// <summary>
		/// 
		/// </summary>
		public OpcServer(string name) {
			eventLog = new Logger(Logger.ESource.OPCDataServer);
			State = ServerState.notconnected;
			_name = name;
		}
		/// <summary>
		/// 
		/// </summary>
		public static class ServerState {
			/// <summary></summary>
			public const int connected = 0;
			/// <summary></summary>
			public const int notconnected = 1;
			/// <summary></summary>
			public const int failed = 2;
		}
		/// <summary></summary>
		private int _state;
		/// <summary>
		/// 
		/// </summary>
		public int State {
			get { return _state; }
			set { _state = value; }
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="clsidOPCserver"></param>
		public void Connect(string prgidOPCserver, string servername) {
			Disconnect();

			Type typeofOPCserver = Type.GetTypeFromProgID(prgidOPCserver);
			if(typeofOPCserver == null)
				Marshal.ThrowExceptionForHR(HRESULTS.OPC_E_NOTFOUND);

			// unmanaged Code
			OPCserverObj = Interop.CreateInstance(typeofOPCserver.GUID, servername);

			// managed Code
			// OPCserverObj = Activator.CreateInstance(typeofOPCserver);

			ifServer = (IOPCServer)OPCserverObj;
			if(ifServer == null)
				Marshal.ThrowExceptionForHR(HRESULTS.CONNECT_E_NOCONNECTION);

			// connect all interfaces
			ifCommon = (IOPCCommon)OPCserverObj;
			ifBrowse = (IOPCBrowseServerAddressSpace)ifServer;
			ifItmProps = (IOPCItemProperties)ifServer;
			cpointcontainer = (IConnectionPointContainer)OPCserverObj;
			AdviseIOPCShutdown();
			State = ServerState.connected;
		}
		/// <summary>
		/// 
		/// </summary>
		public void Disconnect() {
			if(!(shutdowncpoint == null)) {
				try {
					if(shutdowncookie != 0) {
						shutdowncpoint.Unadvise(shutdowncookie);
						shutdowncookie = 0;
					}
				} catch(Exception ex) {
					eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
				} finally {
					Debug.Write(MethodInfo.GetCurrentMethod(), "OPC Data Srv '{0}' - Marshal.ReleaseComObject shutdowncpoint", this._name);
					int rc = Marshal.FinalReleaseComObject(shutdowncpoint);
					shutdowncpoint = null;
				}
			}

			cpointcontainer = null;
			ifBrowse = null;
			ifItmProps = null;
			ifCommon = null;
			ifServer = null;
			if(!(OPCserverObj == null)) {
				Debug.Write(MethodInfo.GetCurrentMethod(), "OPC Data Srv '{0}' - Marshal.ReleaseComObject OPCserverObj", this._name);
				if(this._name != "CoDeSys.OPC.02")
					Interop.ReleaseServer(OPCserverObj);
				OPCserverObj = null;
			}
			Debug.Write(MethodInfo.GetCurrentMethod(), "OPC Data Srv '{0}' - Disconnected", this._name);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="serverStatus"></param>
		public void GetStatus(out SERVERSTATUS serverStatus) {
			try {
				if(ifServer == null) {
					serverStatus = null;
				} else {
					ifServer.GetStatus(out serverStatus);
				}
			} catch(Exception ex) {
				Debug.WriteError(MethodInfo.GetCurrentMethod(), ex);
				serverStatus = null;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="errorCode"></param>
		/// <param name="localeID"></param>
		/// <returns></returns>
		public string GetErrorString(int errorCode, int localeID) {
			string errorres;
			ifServer.GetErrorString(errorCode, localeID, out errorres);
			return errorres;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="groupName"></param>
		/// <param name="setActive"></param>
		/// <param name="requestedUpdateRate"></param>
		/// <returns></returns>
		public OpcGroup AddGroup(string groupName, bool setActive, int requestedUpdateRate) {
			return AddGroup(groupName, setActive, requestedUpdateRate, null, null, 0);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="groupName"></param>
		/// <param name="setActive"></param>
		/// <param name="requestedUpdateRate"></param>
		/// <param name="biasTime"></param>
		/// <param name="percentDeadband"></param>
		/// <param name="localeID"></param>
		/// <returns></returns>
		public OpcGroup AddGroup(string groupName, bool setActive, int requestedUpdateRate,
									int[] biasTime, float[] percentDeadband, int localeID) {
			if(ifServer == null)
				Marshal.ThrowExceptionForHR(HRESULTS.E_ABORT);

			OpcGroup grp = new OpcGroup(ref ifServer, false, groupName, setActive, requestedUpdateRate);
			grp.internalAdd(biasTime, percentDeadband, localeID);
			return grp;
		}
		/// <summary>
		/// IOPCServerPublicGroups (indirect)
		/// </summary>
		/// <param name="groupName"></param>
		/// <returns></returns>
		public OpcGroup GetPublicGroup(string groupName) {
			if(ifServer == null)
				Marshal.ThrowExceptionForHR(HRESULTS.E_ABORT);

			OpcGroup grp = new OpcGroup(ref ifServer, true, groupName, false, 1000);
			grp.internalAdd(null, null, 0);
			return grp;
		}
		/// <summary>
		/// IOPCCommon
		/// </summary>
		/// <param name="lcid"></param>
		public void SetLocaleID(int lcid) {
			ifCommon.SetLocaleID(lcid);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="lcid"></param>
		public void GetLocaleID(out int lcid) {
			ifCommon.GetLocaleID(out lcid);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="lcids"></param>
		public void QueryAvailableLocaleIDs(out int[] lcids) {
			lcids = null;
			int count;
			IntPtr ptrIds;
			int hresult = ifCommon.QueryAvailableLocaleIDs(out count, out ptrIds);
			if(HRESULTS.Failed(hresult))
				Marshal.ThrowExceptionForHR(hresult);
			if(((int)ptrIds) == 0)
				return;
			if(count < 1) { Marshal.FreeCoTaskMem(ptrIds); return; }

			lcids = new int[count];
			Marshal.Copy(ptrIds, lcids, 0, count);
			Marshal.FreeCoTaskMem(ptrIds);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		public void SetClientName(string name) {
			ifCommon.SetClientName(name);
		}
		/// <summary>
		/// IOPCBrowseServerAddressSpace
		/// </summary>
		/// <returns></returns>
		public OPCNAMESPACETYPE QueryOrganization() {
			OPCNAMESPACETYPE ns;
			ifBrowse.QueryOrganization(out ns);
			return ns;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="direction"></param>
		/// <param name="name"></param>
		public void ChangeBrowsePosition(OPCBROWSEDIRECTION direction, string name) {
			ifBrowse.ChangeBrowsePosition(direction, name);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="filterType"></param>
		/// <param name="filterCriteria"></param>
		/// <param name="dataTypeFilter"></param>
		/// <param name="accessRightsFilter"></param>
		/// <param name="stringEnumerator"></param>
		public void BrowseOPCItemIDs(OPCBROWSETYPE filterType, string filterCriteria,
										VarEnum dataTypeFilter, OPCACCESSRIGHTS accessRightsFilter,
										out IEnumString stringEnumerator) {
			stringEnumerator = null;
			object enumtemp;
			ifBrowse.BrowseOPCItemIDs(filterType, filterCriteria, (short)dataTypeFilter, accessRightsFilter, out enumtemp);
			stringEnumerator = (IEnumString)enumtemp;
			enumtemp = null;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="itemDataID"></param>
		/// <returns></returns>
		public string GetItemID(string itemDataID) {
			string itemid;
			ifBrowse.GetItemID(itemDataID, out itemid);
			return itemid;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="itemID"></param>
		/// <param name="stringEnumerator"></param>
		public void BrowseAccessPaths(string itemID, out IEnumString stringEnumerator) {
			stringEnumerator = null;
			object enumtemp;
			ifBrowse.BrowseAccessPaths(itemID, out enumtemp);
			stringEnumerator = (IEnumString)enumtemp;
			enumtemp = null;
		}
		/// <summary>
		/// extra helper
		/// </summary>
		/// <param name="typ"></param>
		/// <param name="lst"></param>
		unsafe public void Browse(OPCBROWSETYPE typ, out ArrayList lst) {
			lst = null;
			IEnumString enumerator;
			BrowseOPCItemIDs(typ, "", VarEnum.VT_EMPTY, 0, out enumerator);
			if(enumerator == null)
				return;

			lst = new ArrayList(500);
			int int2;
			IntPtr cft = new IntPtr(&int2);


			string[] strF = new string[100];
			int hresult;
			do {
				hresult = enumerator.Next(100, strF, cft);

				if(int2 > 0) {
					for(int i = 0; i < int2; i++)
						lst.Add(strF[i]);
				}
			}
			while(hresult == HRESULTS.S_OK);

			int rc = Marshal.FinalReleaseComObject(enumerator);
			enumerator = null;
			lst.TrimToSize();
		}
		/// <summary>
		/// IOPCItemProperties
		/// </summary>
		/// <param name="itemID"></param>
		/// <param name="opcProperties"></param>
		public void QueryAvailableProperties(string itemID, out OPCProperty[] opcProperties) {
			opcProperties = null;

			int count = 0;
			IntPtr ptrID;
			IntPtr ptrDesc;
			IntPtr ptrTyp;
			ifItmProps.QueryAvailableProperties(itemID, out count, out ptrID, out ptrDesc, out ptrTyp);
			if((count == 0) || (count > 10000))
				return;

			int runID = (int)ptrID;
			int runDesc = (int)ptrDesc;
			int runTyp = (int)ptrTyp;
			if((runID == 0) || (runDesc == 0) || (runTyp == 0))
				Marshal.ThrowExceptionForHR(HRESULTS.E_ABORT);

			opcProperties = new OPCProperty[count];

			IntPtr ptrString;
			for(int i = 0; i < count; i++) {
				opcProperties[i] = new OPCProperty();

				opcProperties[i].PropertyID = Marshal.ReadInt32((IntPtr)runID);
				runID += 4;

				ptrString = (IntPtr)Marshal.ReadInt32((IntPtr)runDesc);
				runDesc += 4;
				opcProperties[i].Description = Marshal.PtrToStringUni(ptrString);
				Marshal.FreeCoTaskMem(ptrString);

				opcProperties[i].DataType = (VarEnum)Marshal.ReadInt16((IntPtr)runTyp);
				runTyp += 2;
			}

			Marshal.FreeCoTaskMem(ptrID);
			Marshal.FreeCoTaskMem(ptrDesc);
			Marshal.FreeCoTaskMem(ptrTyp);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="itemID"></param>
		/// <param name="propertyIDs"></param>
		/// <param name="propertiesData"></param>
		/// <returns></returns>
		public bool GetItemProperties(string itemID, int[] propertyIDs, out OPCPropertyData[] propertiesData) {
			propertiesData = null;
			int count = propertyIDs.Length;
			if(count < 1)
				return false;

			IntPtr ptrDat;
			IntPtr ptrErr;
			int hresult = ifItmProps.GetItemProperties(itemID, count, propertyIDs, out ptrDat, out ptrErr);
			if(HRESULTS.Failed(hresult))
				Marshal.ThrowExceptionForHR(hresult);

			int runDat = (int)ptrDat;
			int runErr = (int)ptrErr;
			if((runDat == 0) || (runErr == 0))
				Marshal.ThrowExceptionForHR(HRESULTS.E_ABORT);

			propertiesData = new OPCPropertyData[count];

			for(int i = 0; i < count; i++) {
				propertiesData[i] = new OPCPropertyData();
				propertiesData[i].PropertyID = propertyIDs[i];

				propertiesData[i].Error = Marshal.ReadInt32((IntPtr)runErr);
				runErr += 4;

				if(propertiesData[i].Error == HRESULTS.S_OK) {
					propertiesData[i].Data = Marshal.GetObjectForNativeVariant((IntPtr)runDat);
					DUMMY_VARIANT.VariantClear((IntPtr)runDat);
				} else
					propertiesData[i].Data = null;

				runDat += DUMMY_VARIANT.ConstSize;
			}

			Marshal.FreeCoTaskMem(ptrDat);
			Marshal.FreeCoTaskMem(ptrErr);
			return hresult == HRESULTS.S_OK;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="itemID"></param>
		/// <param name="propertyIDs"></param>
		/// <param name="propertyItems"></param>
		/// <returns></returns>
		public bool LookupItemIDs(string itemID, int[] propertyIDs, out OPCPropertyItem[] propertyItems) {
			propertyItems = null;
			int count = propertyIDs.Length;
			if(count < 1)
				return false;

			IntPtr ptrErr;
			IntPtr ptrIds;
			int hresult = ifItmProps.LookupItemIDs(itemID, count, propertyIDs, out ptrIds, out ptrErr);
			if(HRESULTS.Failed(hresult))
				Marshal.ThrowExceptionForHR(hresult);

			int runIds = (int)ptrIds;
			int runErr = (int)ptrErr;
			if((runIds == 0) || (runErr == 0))
				Marshal.ThrowExceptionForHR(HRESULTS.E_ABORT);

			propertyItems = new OPCPropertyItem[count];

			IntPtr ptrString;
			for(int i = 0; i < count; i++) {
				propertyItems[i] = new OPCPropertyItem();
				propertyItems[i].PropertyID = propertyIDs[i];

				propertyItems[i].Error = Marshal.ReadInt32((IntPtr)runErr);
				runErr += 4;

				if(propertyItems[i].Error == HRESULTS.S_OK) {
					ptrString = (IntPtr)Marshal.ReadInt32((IntPtr)runIds);
					propertyItems[i].newItemID = Marshal.PtrToStringUni(ptrString);
					Marshal.FreeCoTaskMem(ptrString);
				} else
					propertyItems[i].newItemID = null;

				runIds += 4;
			}

			Marshal.FreeCoTaskMem(ptrIds);
			Marshal.FreeCoTaskMem(ptrErr);
			return hresult == HRESULTS.S_OK;
		}
		/// <summary>
		/// IOPCShutdown, COMMON CALLBACK
		/// </summary>
		/// <param name="shutdownReason"></param>
		void IOPCShutdown.ShutdownRequest(string shutdownReason) {
			ShutdownRequestEventArgs e = new ShutdownRequestEventArgs(shutdownReason);
			if(ShutdownRequested != null)
				ShutdownRequested(this, e);
		}
		/// <summary>
		/// event
		/// </summary>
		public event ShutdownRequestEventHandler ShutdownRequested;
		/// <summary>
		/// private
		/// </summary>
		private void AdviseIOPCShutdown() {
			Type sinktype = typeof(IOPCShutdown);
			Guid sinkguid = sinktype.GUID;

			cpointcontainer.FindConnectionPoint(ref sinkguid, out shutdowncpoint);
			if(shutdowncpoint == null)
				return;

			shutdowncpoint.Advise(this, out shutdowncookie);
		}
	}
}
/** @} */
