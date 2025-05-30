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
//# File-ID      : $Id:: OPC_Data.cs 237 2025-05-30 11:23:27Z                     $ #
//#                                                                                 #
//###################################################################################
using System;
using System.Runtime.InteropServices;
/**
 * @addtogroup externals
 * @{
 */
namespace OPC.Data.Interface {
	/// <summary>
	/// 
	/// </summary>
	public enum OPCDATASOURCE {
		OPC_DS_CACHE = 1,
		OPC_DS_DEVICE = 2
	}
	/// <summary>
	/// 
	/// </summary>
	public enum OPCBROWSETYPE {
		OPC_BRANCH = 1,
		OPC_LEAF = 2,
		OPC_FLAT = 3
	}
	/// <summary>
	/// 
	/// </summary>
	public enum OPCNAMESPACETYPE {
		OPC_NS_HIERARCHIAL = 1,
		OPC_NS_FLAT = 2
	}
	/// <summary>
	/// 
	/// </summary>
	public enum OPCBROWSEDIRECTION {
		OPC_BROWSE_UP = 1,
		OPC_BROWSE_DOWN = 2,
		OPC_BROWSE_TO = 3
	}
	/// <summary>
	/// 
	/// </summary>
	[Flags]
	public enum OPCACCESSRIGHTS {
		OPC_READABLE = 1,
		OPC_WRITEABLE = 2
	}
	/// <summary>
	/// 
	/// </summary>
	public enum OPCEUTYPE {
		OPC_NOENUM = 0,
		OPC_ANALOG = 1,
		OPC_ENUMERATED = 2
	}
	/// <summary>
	/// 
	/// </summary>
	public enum OPCSERVERSTATE {
		OPC_STATUS_RUNNING = 1,
		OPC_STATUS_FAILED = 2,
		OPC_STATUS_NOCONFIG = 3,
		OPC_STATUS_SUSPENDED = 4,
		OPC_STATUS_TEST = 5,
		OPC_STATUS_NOTCONNECTED = 6
	}
	/// <summary>
	/// 
	/// </summary>
	public enum OPCENUMSCOPE {
		OPC_ENUM_PRIVATE_CONNECTIONS = 1,
		OPC_ENUM_PUBLIC_CONNECTIONS = 2,
		OPC_ENUM_ALL_CONNECTIONS = 3,
		OPC_ENUM_PRIVATE = 4,
		OPC_ENUM_PUBLIC = 5,
		OPC_ENUM_ALL = 6
	}
	// OPC Quality flags
	/// <summary>
	/// OPC Quality flags
	/// </summary>
	[Flags]
	public enum OPC_QUALITY_MASKS: short {
		LIMIT_MASK = 0x0003,
		STATUS_MASK = 0x00FC,
		MASTER_MASK = 0x00C0,
	}
	/// <summary>
	/// 
	/// </summary>
	[Flags]
	public enum OPC_QUALITY_MASTER: short {
		QUALITY_BAD = 0x0000,
		QUALITY_UNCERTAIN = 0x0040,
		ERROR_QUALITY_VALUE = 0x0080,       // non standard!
		QUALITY_GOOD = 0x00C0,
	}
	/// <summary>
	/// 
	/// </summary>
	[Flags]
	public enum OPC_QUALITY_STATUS: short {
		BAD = 0x0000,   // STATUS_MASK Values for Quality = BAD
		CONFIG_ERROR = 0x0004,
		NOT_CONNECTED = 0x0008,
		DEVICE_FAILURE = 0x000c,
		SENSOR_FAILURE = 0x0010,
		LAST_KNOWN = 0x0014,
		COMM_FAILURE = 0x0018,
		OUT_OF_SERVICE = 0x001C,
		WAIT_FOR_INITIAL_DATA = 0x0020,

		UNCERTAIN = 0x0040, // STATUS_MASK Values for Quality = UNCERTAIN
		LAST_USABLE = 0x0044,
		SENSOR_CAL = 0x0050,
		EGU_EXCEEDED = 0x0054,
		SUB_NORMAL = 0x0058,

		OK = 0x00C0,    // STATUS_MASK Value for Quality = GOOD
		LOCAL_OVERRIDE = 0x00D8
	}
	/// <summary>
	/// 
	/// </summary>
	[Flags]
	public enum OPC_QUALITY_LIMIT {
		LIMIT_OK = 0x0000,
		LIMIT_LOW = 0x0001,
		LIMIT_HIGH = 0x0002,
		LIMIT_CONST = 0x0003
	}
	/// <summary>
	/// 
	/// </summary>
	public enum OPC_PROPS {
		OPC_PROP_CDT = 1,
		OPC_PROP_VALUE = 2,
		OPC_PROP_QUALITY = 3,
		OPC_PROP_TIME = 4,
		OPC_PROP_RIGHTS = 5,
		OPC_PROP_SCANRATE = 6,

		OPC_PROP_UNIT = 100,
		OPC_PROP_DESC = 101,
		OPC_PROP_HIEU = 102,
		OPC_PROP_LOEU = 103,
		OPC_PROP_HIRANGE = 104,
		OPC_PROP_LORANGE = 105,
		OPC_PROP_CLOSE = 106,
		OPC_PROP_OPEN = 107,
		OPC_PROP_TIMEZONE = 108,

		OPC_PROP_FGC = 200,
		OPC_PROP_BGC = 201,
		OPC_PROP_BLINK = 202,
		OPC_PROP_BMP = 203,
		OPC_PROP_SND = 204,
		OPC_PROP_HTML = 205,
		OPC_PROP_AVI = 206,

		OPC_PROP_ALMSTAT = 300,
		OPC_PROP_ALMHELP = 301,
		OPC_PROP_ALMAREAS = 302,
		OPC_PROP_ALMPRIMARYAREA = 303,
		OPC_PROP_ALMCONDITION = 304,
		OPC_PROP_ALMLIMIT = 305,
		OPC_PROP_ALMDB = 306,
		OPC_PROP_ALMHH = 307,
		OPC_PROP_ALMH = 308,
		OPC_PROP_ALML = 309,
		OPC_PROP_ALMLL = 310,
		OPC_PROP_ALMROC = 311,
		OPC_PROP_ALMDEV = 312
	}
	/// <summary>
	/// SERVER level structs
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Pack = 2, CharSet = CharSet.Unicode)]
	public class SERVERSTATUS {
		/// <summary></summary>
		public long ftStartTime;
		/// <summary></summary>
		public long ftCurrentTime;
		/// <summary></summary>
		public long ftLastUpdateTime;
		/// <summary></summary>
		[MarshalAs(UnmanagedType.U4)]
		public OPCSERVERSTATE eServerState;
		/// <summary></summary>
		public int dwGroupCount;
		/// <summary></summary>
		public int dwBandWidth;
		/// <summary></summary>
		public short wMajorVersion;
		/// <summary></summary>
		public short wMinorVersion;
		/// <summary></summary>
		public short wBuildNumber;
		/// <summary></summary>
		public short wReserved;
		/// <summary></summary>
		[MarshalAs(UnmanagedType.LPWStr)]
		public string szVendorInfo;
	};
	/// <summary>
	/// INTERNAL item level structs
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Pack = 2, CharSet = CharSet.Unicode)]
	internal class OPCITEMDEFintern {
		/// <summary></summary>
		[MarshalAs(UnmanagedType.LPWStr)]
		public string szAccessPath;
		/// <summary></summary>
		[MarshalAs(UnmanagedType.LPWStr)]
		public string szItemID;
		/// <summary></summary>
		[MarshalAs(UnmanagedType.Bool)]
		public bool bActive;
		/// <summary></summary>
		public int hClient;
		/// <summary></summary>
		public int dwBlobSize;
		/// <summary></summary>
		public IntPtr pBlob;
		/// <summary></summary>
		public short vtRequestedDataType;
		/// <summary></summary>
		public short wReserved;
	};
	/// <summary>
	/// 
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal class OPCITEMRESULTintern {
		/// <summary></summary>
		public int hServer = 0;
		/// <summary></summary>
		public short vtCanonicalDataType = 0;
		/// <summary></summary>
		public short wReserved = 0;
		/// <summary></summary>
		[MarshalAs(UnmanagedType.U4)]
		public OPCACCESSRIGHTS dwAccessRights = 0;
		/// <summary></summary>
		public int dwBlobSize = 0;
		/// <summary></summary>
		public int pBlob = 0;
	};
	/// <summary>
	/// SERVER
	/// </summary>
	[ComVisible(true), ComImport,
	Guid("39c13a4d-011e-11d0-9675-0020afd8adb3"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IOPCServer {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="szName"></param>
		/// <param name="bActive"></param>
		/// <param name="dwRequestedUpdateRate"></param>
		/// <param name="hClientGroup"></param>
		/// <param name="pTimeBias"></param>
		/// <param name="pPercentDeadband"></param>
		/// <param name="dwLCID"></param>
		/// <param name="phServerGroup"></param>
		/// <param name="pRevisedUpdateRate"></param>
		/// <param name="riid"></param>
		/// <param name="ppUnk"></param>
		void AddGroup(
			[In, MarshalAs(UnmanagedType.LPWStr)] string szName,
			[In, MarshalAs(UnmanagedType.Bool)] bool bActive,
			[In] int dwRequestedUpdateRate,
			[In] int hClientGroup,
			[In, MarshalAs(UnmanagedType.LPArray, SizeConst = 1)] int[] pTimeBias,
			[In, MarshalAs(UnmanagedType.LPArray, SizeConst = 1)] float[] pPercentDeadband,
			[In] int dwLCID,
			[Out] out int phServerGroup,
			[Out] out int pRevisedUpdateRate,
			[In] ref Guid riid,
			[Out, MarshalAs(UnmanagedType.IUnknown)] out object ppUnk);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwError"></param>
		/// <param name="dwLocale"></param>
		/// <param name="ppString"></param>
		void GetErrorString(
			[In] int dwError,
			[In] int dwLocale,
			[Out, MarshalAs(UnmanagedType.LPWStr)] out string ppString);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="szName"></param>
		/// <param name="riid"></param>
		/// <param name="ppUnk"></param>
		void GetGroupByName(
			[In, MarshalAs(UnmanagedType.LPWStr)] string szName,
			[In] ref Guid riid,
			[Out, MarshalAs(UnmanagedType.IUnknown)] out object ppUnk);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="ppServerStatus"></param>
		void GetStatus(
			[Out, MarshalAs(UnmanagedType.LPStruct)] out SERVERSTATUS ppServerStatus);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="hServerGroup"></param>
		/// <param name="bForce"></param>
		void RemoveGroup(
			[In] int hServerGroup,
			[In, MarshalAs(UnmanagedType.Bool)] bool bForce);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwScope"></param>
		/// <param name="riid"></param>
		/// <param name="ppUnk"></param>
		/// <returns></returns>
		[PreserveSig]
		int CreateGroupEnumerator(                                      // may return S_FALSE
			[In] int dwScope,
			[In] ref Guid riid,
			[Out, MarshalAs(UnmanagedType.IUnknown)] out object ppUnk);

	}
	/// <summary>
	/// Public Groups
	/// </summary>
	[ComVisible(true), ComImport,
	Guid("39c13a4e-011e-11d0-9675-0020afd8adb3"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IOPCServerPublicGroups {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="szName"></param>
		/// <param name="riid"></param>
		/// <param name="ppUnk"></param>
		void GetPublicGroupByName(
			[In, MarshalAs(UnmanagedType.LPWStr)] string szName,
			[In] ref Guid riid,
			[Out, MarshalAs(UnmanagedType.IUnknown)] out object ppUnk);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="hServerGroup"></param>
		/// <param name="bForce"></param>
		void RemovePublicGroup(
			[In] int hServerGroup,
			[In, MarshalAs(UnmanagedType.Bool)] bool bForce);

	}
	/// <summary>
	/// ServerAddressSpace Browsing
	/// </summary>
	[ComVisible(true), ComImport,
	Guid("39c13a4f-011e-11d0-9675-0020afd8adb3"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IOPCBrowseServerAddressSpace {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="pNameSpaceType"></param>
		void QueryOrganization(
			[Out, MarshalAs(UnmanagedType.U4)] out OPCNAMESPACETYPE pNameSpaceType);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwBrowseDirection"></param>
		/// <param name="szName"></param>
		void ChangeBrowsePosition(
			[In, MarshalAs(UnmanagedType.U4)] OPCBROWSEDIRECTION dwBrowseDirection,
			[In, MarshalAs(UnmanagedType.LPWStr)] string szName);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwBrowseFilterType"></param>
		/// <param name="szFilterCriteria"></param>
		/// <param name="vtDataTypeFilter"></param>
		/// <param name="dwAccessRightsFilter"></param>
		/// <param name="ppUnk"></param>
		/// <returns></returns>
		[PreserveSig]
		int BrowseOPCItemIDs(
			[In, MarshalAs(UnmanagedType.U4)] OPCBROWSETYPE dwBrowseFilterType,
			[In, MarshalAs(UnmanagedType.LPWStr)] string szFilterCriteria,
			[In, MarshalAs(UnmanagedType.U2)] short vtDataTypeFilter,
			[In, MarshalAs(UnmanagedType.U4)] OPCACCESSRIGHTS dwAccessRightsFilter,
			[Out, MarshalAs(UnmanagedType.IUnknown)] out object ppUnk);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="szItemDataID"></param>
		/// <param name="szItemID"></param>
		void GetItemID(
			[In, MarshalAs(UnmanagedType.LPWStr)] string szItemDataID,
			[Out, MarshalAs(UnmanagedType.LPWStr)] out string szItemID);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="szItemID"></param>
		/// <param name="ppUnk"></param>
		/// <returns></returns>
		[PreserveSig]
		int BrowseAccessPaths(
			[In, MarshalAs(UnmanagedType.LPWStr)] string szItemID,
			[Out, MarshalAs(UnmanagedType.IUnknown)] out object ppUnk);
	}
	/// <summary>
	/// Item Properties
	/// </summary>
	[ComVisible(true), ComImport,
	Guid("39c13a72-011e-11d0-9675-0020afd8adb3"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IOPCItemProperties {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="szItemID"></param>
		/// <param name="dwCount"></param>
		/// <param name="ppPropertyIDs"></param>
		/// <param name="ppDescriptions"></param>
		/// <param name="ppvtDataTypes"></param>
		void QueryAvailableProperties(
			[In, MarshalAs(UnmanagedType.LPWStr)] string szItemID,
			[Out] out int dwCount,
			[Out] out IntPtr ppPropertyIDs,
			[Out] out IntPtr ppDescriptions,
			[Out] out IntPtr ppvtDataTypes);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="szItemID"></param>
		/// <param name="dwCount"></param>
		/// <param name="pdwPropertyIDs"></param>
		/// <param name="ppvData"></param>
		/// <param name="ppErrors"></param>
		/// <returns></returns>
		[PreserveSig]
		int GetItemProperties(
			[In, MarshalAs(UnmanagedType.LPWStr)] string szItemID,
			[In] int dwCount,
			[In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] pdwPropertyIDs,
			[Out] out IntPtr ppvData,
			[Out] out IntPtr ppErrors);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="szItemID"></param>
		/// <param name="dwCount"></param>
		/// <param name="pdwPropertyIDs"></param>
		/// <param name="ppszNewItemIDs"></param>
		/// <param name="ppErrors"></param>
		/// <returns></returns>
		[PreserveSig]
		int LookupItemIDs(
			[In, MarshalAs(UnmanagedType.LPWStr)] string szItemID,
			[In] int dwCount,
			[In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] pdwPropertyIDs,
			[Out] out IntPtr ppszNewItemIDs,
			[Out] out IntPtr ppErrors);
	}
	/// <summary>
	/// GroupStateMgt
	/// </summary>
	[ComVisible(true),
	Guid("39c13a50-011e-11d0-9675-0020afd8adb3"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IOPCGroupStateMgt {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="pUpdateRate"></param>
		/// <param name="pActive"></param>
		/// <param name="ppName"></param>
		/// <param name="pTimeBias"></param>
		/// <param name="pPercentDeadband"></param>
		/// <param name="pLCID"></param>
		/// <param name="phClientGroup"></param>
		/// <param name="phServerGroup"></param>
		void GetState(
			[Out] out int pUpdateRate,
			[Out, MarshalAs(UnmanagedType.Bool)] out bool pActive,
			[Out, MarshalAs(UnmanagedType.LPWStr)] out string ppName,
			[Out] out int pTimeBias,
			[Out] out float pPercentDeadband,
			[Out] out int pLCID,
			[Out] out int phClientGroup,
			[Out] out int phServerGroup);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="pRequestedUpdateRate"></param>
		/// <param name="pRevisedUpdateRate"></param>
		/// <param name="pActive"></param>
		/// <param name="pTimeBias"></param>
		/// <param name="pPercentDeadband"></param>
		/// <param name="pLCID"></param>
		/// <param name="phClientGroup"></param>
		void SetState(
			[In, MarshalAs(UnmanagedType.LPArray, SizeConst = 1)] int[] pRequestedUpdateRate,
			[Out] out int pRevisedUpdateRate,
			[In, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.Bool, SizeConst = 1)] bool[] pActive,
			[In, MarshalAs(UnmanagedType.LPArray, SizeConst = 1)] int[] pTimeBias,
			[In, MarshalAs(UnmanagedType.LPArray, SizeConst = 1)] float[] pPercentDeadband,
			[In, MarshalAs(UnmanagedType.LPArray, SizeConst = 1)] int[] pLCID,
			[In, MarshalAs(UnmanagedType.LPArray, SizeConst = 1)] int[] phClientGroup);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="szName"></param>
		void SetName(
			[In, MarshalAs(UnmanagedType.LPWStr)] string szName);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="szName"></param>
		/// <param name="riid"></param>
		/// <param name="ppUnk"></param>
		void CloneGroup(
			[In, MarshalAs(UnmanagedType.LPWStr)] string szName,
			[In] ref Guid riid,
			[Out, MarshalAs(UnmanagedType.IUnknown)] out object ppUnk);

	}
	/// <summary>
	/// 
	/// </summary>
	[ComVisible(true),
	Guid("39c13a51-011e-11d0-9675-0020afd8adb3"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IOPCPublicGroupStateMgt {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="pPublic"></param>
		void GetState(
			[Out, MarshalAs(UnmanagedType.Bool)] out bool pPublic);
		/// <summary>
		/// 
		/// </summary>
		void MoveToPublic();
	}
	/// <summary>
	/// Item Mgmt
	/// </summary>
	[ComVisible(true), ComImport,
	Guid("39c13a54-011e-11d0-9675-0020afd8adb3"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IOPCItemMgt {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwCount"></param>
		/// <param name="pItemArray"></param>
		/// <param name="ppAddResults"></param>
		/// <param name="ppErrors"></param>
		/// <returns></returns>
		[PreserveSig]
		int AddItems(
			[In] int dwCount,
			[In] IntPtr pItemArray,
			[Out] out IntPtr ppAddResults,
			[Out] out IntPtr ppErrors);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwCount"></param>
		/// <param name="pItemArray"></param>
		/// <param name="bBlobUpdate"></param>
		/// <param name="ppValidationResults"></param>
		/// <param name="ppErrors"></param>
		/// <returns></returns>
		[PreserveSig]
		int ValidateItems(
			[In] int dwCount,
			[In] IntPtr pItemArray,
			[In, MarshalAs(UnmanagedType.Bool)] bool bBlobUpdate,
			[Out] out IntPtr ppValidationResults,
			[Out] out IntPtr ppErrors);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwCount"></param>
		/// <param name="phServer"></param>
		/// <param name="ppErrors"></param>
		/// <returns></returns>
		[PreserveSig]
		int RemoveItems(
			[In] int dwCount,
			[In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] phServer,
			[Out] out IntPtr ppErrors);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwCount"></param>
		/// <param name="phServer"></param>
		/// <param name="bActive"></param>
		/// <param name="ppErrors"></param>
		/// <returns></returns>
		[PreserveSig]
		int SetActiveState(
			[In] int dwCount,
			[In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] phServer,
			[In, MarshalAs(UnmanagedType.Bool)] bool bActive,
			[Out] out IntPtr ppErrors);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwCount"></param>
		/// <param name="phServer"></param>
		/// <param name="phClient"></param>
		/// <param name="ppErrors"></param>
		/// <returns></returns>
		[PreserveSig]
		int SetClientHandles(
			[In] int dwCount,
			[In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] phServer,
			[In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] phClient,
			[Out] out IntPtr ppErrors);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwCount"></param>
		/// <param name="phServer"></param>
		/// <param name="pRequestedDatatypes"></param>
		/// <param name="ppErrors"></param>
		/// <returns></returns>
		[PreserveSig]
		int SetDatatypes(
			[In] int dwCount,
			[In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] phServer,
			[In] IntPtr pRequestedDatatypes,
			[Out] out IntPtr ppErrors);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="riid"></param>
		/// <param name="ppUnk"></param>
		/// <returns></returns>
		[PreserveSig]
		int CreateEnumerator(
			[In] ref Guid riid,
			[Out, MarshalAs(UnmanagedType.IUnknown)] out object ppUnk);

	}
	/// <summary>
	/// Sync IO
	/// </summary>
	[ComVisible(true), ComImport,
	Guid("39c13a52-011e-11d0-9675-0020afd8adb3"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IOPCSyncIO {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwSource"></param>
		/// <param name="dwCount"></param>
		/// <param name="phServer"></param>
		/// <param name="ppItemValues"></param>
		/// <param name="ppErrors"></param>
		/// <returns></returns>
		[PreserveSig]
		int Read(
			[In, MarshalAs(UnmanagedType.U4)] OPCDATASOURCE dwSource,
			[In] int dwCount,
			[In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] phServer,
			[Out] out IntPtr ppItemValues,
			[Out] out IntPtr ppErrors);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwCount"></param>
		/// <param name="phServer"></param>
		/// <param name="pItemValues"></param>
		/// <param name="ppErrors"></param>
		/// <returns></returns>
		[PreserveSig]
		int Write(
			[In] int dwCount,
			[In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] phServer,
			[In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] object[] pItemValues,
			[Out] out IntPtr ppErrors);

	}
	/// <summary>
	/// Async IO
	/// </summary>
	[ComVisible(true), ComImport,
	Guid("39c13a71-011e-11d0-9675-0020afd8adb3"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IOPCAsyncIO2 {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwCount"></param>
		/// <param name="phServer"></param>
		/// <param name="dwTransactionID"></param>
		/// <param name="pdwCancelID"></param>
		/// <param name="ppErrors"></param>
		/// <returns></returns>
		[PreserveSig]
		int Read(
			[In] int dwCount,
			[In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] phServer,
			[In] int dwTransactionID,
			[Out] out int pdwCancelID,
			[Out] out IntPtr ppErrors);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwCount"></param>
		/// <param name="phServer"></param>
		/// <param name="pItemValues"></param>
		/// <param name="dwTransactionID"></param>
		/// <param name="pdwCancelID"></param>
		/// <param name="ppErrors"></param>
		/// <returns></returns>
		[PreserveSig]
		int Write(
			[In] int dwCount,
			[In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] phServer,
			[In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] object[] pItemValues,
			[In] int dwTransactionID,
			[Out] out int pdwCancelID,
			[Out] out IntPtr ppErrors);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwSource"></param>
		/// <param name="dwTransactionID"></param>
		/// <param name="pdwCancelID"></param>
		void Refresh2(
			[In, MarshalAs(UnmanagedType.U4)] OPCDATASOURCE dwSource,
			[In] int dwTransactionID,
			[Out] out int pdwCancelID);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwCancelID"></param>
		void Cancel2(
			[In] int dwCancelID);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="bEnable"></param>
		void SetEnable(
			[In, MarshalAs(UnmanagedType.Bool)] bool bEnable);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="pbEnable"></param>
		void GetEnable(
			[Out, MarshalAs(UnmanagedType.Bool)] out bool pbEnable);

	}
	/// <summary>
	/// Async Callback
	/// </summary>
	[ComVisible(true), ComImport,
	Guid("39c13a70-011e-11d0-9675-0020afd8adb3"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IOPCDataCallback {
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
		void OnDataChange(
			[In] int dwTransid,
			[In] int hGroup,
			[In] int hrMasterquality,
			[In] int hrMastererror,
			[In] int dwCount,
			[In] IntPtr phClientItems,
			[In] IntPtr pvValues,
			[In] IntPtr pwQualities,
			[In] IntPtr pftTimeStamps,
			[In] IntPtr ppErrors);
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
		void OnReadComplete(
			[In] int dwTransid,
			[In] int hGroup,
			[In] int hrMasterquality,
			[In] int hrMastererror,
			[In] int dwCount,
			[In] IntPtr phClientItems,
			[In] IntPtr pvValues,
			[In] IntPtr pwQualities,
			[In] IntPtr pftTimeStamps,
			[In] IntPtr ppErrors);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwTransid"></param>
		/// <param name="hGroup"></param>
		/// <param name="hrMastererr"></param>
		/// <param name="dwCount"></param>
		/// <param name="pClienthandles"></param>
		/// <param name="ppErrors"></param>
		void OnWriteComplete(
			[In] int dwTransid,
			[In] int hGroup,
			[In] int hrMastererr,
			[In] int dwCount,
			[In] IntPtr pClienthandles,
			[In] IntPtr ppErrors);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwTransid"></param>
		/// <param name="hGroup"></param>
		void OnCancelComplete(
			[In] int dwTransid,
			[In] int hGroup);

	}
	/// <summary>
	/// Enum Item Attributes
	/// </summary>
	[ComVisible(true), ComImport,
	Guid("39c13a55-011e-11d0-9675-0020afd8adb3"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IEnumOPCItemAttributes {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="celt"></param>
		/// <param name="ppItemArray"></param>
		/// <param name="pceltFetched"></param>
		void Next(
			[In] int celt,
			[Out] out IntPtr ppItemArray,
			[Out] out int pceltFetched);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="celt"></param>
		void Skip(
			[In] int celt);
		/// <summary>
		/// 
		/// </summary>
		void Reset();
		/// <summary>
		/// 
		/// </summary>
		/// <param name="ppUnk"></param>
		void Clone(
			[Out, MarshalAs(UnmanagedType.IUnknown)] out object ppUnk);
	}
}
/** @} */
