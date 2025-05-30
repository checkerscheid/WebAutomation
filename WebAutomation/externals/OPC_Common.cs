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
//# File-ID      : $Id:: OPC_Common.cs 237 2025-05-30 11:23:27Z                   $ #
//#                                                                                 #
//###################################################################################
using System;
using System.Runtime.InteropServices;
using System.Text;
/**
 * @defgroup externals externals
 * @{
 */
namespace OPC.Common {
	/// <summary>
	/// Opc Server lister (OPCEnum)
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public class OpcServers {
		/// <summary></summary>
		public string ProgID;
		/// <summary></summary>
		public string ServerName;
		/// <summary></summary>
		public Guid ClsID;
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			StringBuilder sb = new StringBuilder("OPCServer: ", 300);
			sb.AppendFormat("'{0}' ID={1} [{2}]", ServerName, ProgID, ClsID);
			return sb.ToString();
		}
	}
	/// <summary>
	/// 
	/// </summary>
	[ComVisible(true)]
	public class OpcServerList {
		/// <summary></summary>
		private object OPCListObj;
		/// <summary></summary>
		private IOPCServerList ifList;
		/// <summary></summary>
		private object EnumObj;
		/// <summary></summary>
		private IEnumGUID ifEnum;
		/// <summary>
		/// 
		/// </summary>
		public OpcServerList() {
			OPCListObj = null;
			ifList = null;
			EnumObj = null;
			ifEnum = null;
		}
		/// <summary>
		/// 
		/// </summary>
		~OpcServerList() { Dispose(); }
		/// <summary>
		/// 
		/// </summary>
		/// <param name="serverslist"></param>
		/// <param name="remoteserver"></param>
		public void ListAllData20(out OpcServers[] serverslist, string remoteserver) {                  // CATID_OPCDAServer20
			ListAll(new Guid("63D5F432-CFE4-11d1-B2C8-0060083BA1FB"), out serverslist, remoteserver);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="serverslist"></param>
		public void ListAllData20(out OpcServers[] serverslist) {                   // CATID_OPCDAServer20
			ListAllData20(out serverslist, "localhost");
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="catid"></param>
		/// <param name="serverslist"></param>
		/// <param name="remoteserver"></param>
		public void ListAll(Guid catid, out OpcServers[] serverslist, string remoteserver) {
			serverslist = null;
			Dispose();
			Guid guid = new Guid("13486D51-4821-11D2-A494-3CB306C10000");
			Type typeoflist;
			if(remoteserver == "localhost" || remoteserver == "127.0.0.1") {
				typeoflist = Type.GetTypeFromCLSID(guid);
			} else {
				typeoflist = Type.GetTypeFromCLSID(guid, remoteserver);
			}
			OPCListObj = Activator.CreateInstance(typeoflist);

			ifList = (IOPCServerList)OPCListObj;
			if(ifList == null)
				Marshal.ThrowExceptionForHR(HRESULTS.E_ABORT);

			ifList.EnumClassesOfCategories(1, ref catid, 0, ref catid, out EnumObj);
			if(EnumObj == null)
				Marshal.ThrowExceptionForHR(HRESULTS.E_ABORT);

			ifEnum = (IEnumGUID)EnumObj;
			if(ifEnum == null)
				Marshal.ThrowExceptionForHR(HRESULTS.E_ABORT);

			int maxcount = 300;
			IntPtr ptrGuid = Marshal.AllocCoTaskMem(maxcount * 16);
			int count = 0;
			ifEnum.Next(maxcount, ptrGuid, out count);
			if(count < 1) { Marshal.FreeCoTaskMem(ptrGuid); return; }

			serverslist = new OpcServers[count];

			byte[] guidbin = new byte[16];
			int runGuid = (int)ptrGuid;
			for(int i = 0; i < count; i++) {
				serverslist[i] = new OpcServers();
				Marshal.Copy((IntPtr)runGuid, guidbin, 0, 16);
				serverslist[i].ClsID = new Guid(guidbin);
				ifList.GetClassDetails(ref serverslist[i].ClsID,
										out serverslist[i].ProgID, out serverslist[i].ServerName);
				runGuid += 16;
			}

			Marshal.FreeCoTaskMem(ptrGuid);
			Dispose();
		}
		/// <summary>
		/// 
		/// </summary>
		public void Dispose() {
			ifEnum = null;
			if(!(EnumObj == null)) {
				int rc = Marshal.FinalReleaseComObject(EnumObj);
				EnumObj = null;
			}
			ifList = null;
			if(!(OPCListObj == null)) {
				int rc = Marshal.FinalReleaseComObject(OPCListObj);
				OPCListObj = null;
			}
		}
	}
	/// <summary>
	/// 
	/// </summary>
	public class HRESULTS {
		/// <summary></summary>
		public const int S_OK = 0x00000000;
		/// <summary></summary>
		public const int S_FALSE = 0x00000001;
		/// <summary></summary>
		public const int E_NOTIMPL = unchecked((int)0x80004001);        // winerror.h
		/// <summary></summary>
		public const int E_NOINTERFACE = unchecked((int)0x80004002);
		/// <summary></summary>
		public const int E_ABORT = unchecked((int)0x80004004);
		/// <summary></summary>
		public const int E_FAIL = unchecked((int)0x80004005);
		/// <summary></summary>
		public const int E_OUTOFMEMORY = unchecked((int)0x8007000E);
		/// <summary></summary>
		public const int E_INVALIDARG = unchecked((int)0x80070057);
		/// <summary></summary>
		public const int CONNECT_E_NOCONNECTION = unchecked((int)0x80040200);       // olectl.h
		/// <summary></summary>
		public const int CONNECT_E_ADVISELIMIT = unchecked((int)0x80040201);
		/// <summary></summary>
		public const int OPC_E_INVALIDHANDLE = unchecked((int)0xC0040001);      // opcerror.h
		/// <summary></summary>
		public const int OPC_E_BADTYPE = unchecked((int)0xC0040004);
		/// <summary></summary>
		public const int OPC_E_PUBLIC = unchecked((int)0xC0040005);
		/// <summary></summary>
		public const int OPC_E_BADRIGHTS = unchecked((int)0xC0040006);
		/// <summary></summary>
		public const int OPC_E_UNKNOWNITEMID = unchecked((int)0xC0040007);
		/// <summary></summary>
		public const int OPC_E_INVALIDITEMID = unchecked((int)0xC0040008);
		/// <summary></summary>
		public const int OPC_E_INVALIDFILTER = unchecked((int)0xC0040009);
		/// <summary></summary>
		public const int OPC_E_UNKNOWNPATH = unchecked((int)0xC004000A);
		/// <summary></summary>
		public const int OPC_E_RANGE = unchecked((int)0xC004000B);
		/// <summary></summary>
		public const int OPC_E_DUPLICATENAME = unchecked((int)0xC004000C);
		/// <summary></summary>
		public const int OPC_S_UNSUPPORTEDRATE = unchecked((int)0x0004000D);
		/// <summary></summary>
		public const int OPC_S_CLAMP = unchecked((int)0x0004000E);
		/// <summary></summary>
		public const int OPC_S_INUSE = unchecked((int)0x0004000F);
		/// <summary></summary>
		public const int OPC_E_INVALIDCONFIGFILE = unchecked((int)0xC0040010);
		/// <summary></summary>
		public const int OPC_E_NOTFOUND = unchecked((int)0xC0040011);
		/// <summary></summary>
		public const int OPC_E_INVALID_PID = unchecked((int)0xC0040203);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="hresultcode"></param>
		/// <returns></returns>
		public static bool Failed(int hresultcode) { return (hresultcode < 0); }
		/// <summary>
		/// 
		/// </summary>
		/// <param name="hresultcode"></param>
		/// <returns></returns>
		public static bool Succeeded(int hresultcode) { return (hresultcode >= 0); }
		/// <summary>
		/// 
		/// </summary>
		/// <param name="hresultcode"></param>
		/// <returns></returns>
		public static string getError(int hresultcode) {
			string returns;
			switch(hresultcode) {
				case S_OK:
					returns = "S_OK";
					break;
				case S_FALSE:
					returns = "S_FALSE";
					break;
				case E_NOTIMPL:
					returns = "E_NOTIMPL";
					break;
				case E_NOINTERFACE:
					returns = "E_NOINTERFACE";
					break;
				case E_ABORT:
					returns = "E_ABORT";
					break;
				case E_FAIL:
					returns = "E_FAIL";
					break;
				case E_OUTOFMEMORY:
					returns = "E_OUTOFMEMORY";
					break;
				case E_INVALIDARG:
					returns = "E_INVALIDARG";
					break;
				case CONNECT_E_NOCONNECTION:
					returns = "CONNECT_E_NOCONNECTION";
					break;
				case CONNECT_E_ADVISELIMIT:
					returns = "CONNECT_E_ADVISELIMIT";
					break;
				case OPC_E_INVALIDHANDLE:
					returns = "OPC_E_INVALIDHANDLE";
					break;
				case OPC_E_BADTYPE:
					returns = "OPC_E_BADTYPE";
					break;
				case OPC_E_PUBLIC:
					returns = "OPC_E_PUBLIC";
					break;
				case OPC_E_BADRIGHTS:
					returns = "OPC_E_BADRIGHTS";
					break;
				case OPC_E_UNKNOWNITEMID:
					returns = "OPC_E_UNKNOWNITEMID";
					break;
				case OPC_E_INVALIDITEMID:
					returns = "OPC_E_INVALIDITEMID";
					break;
				case OPC_E_INVALIDFILTER:
					returns = "OPC_E_INVALIDFILTER";
					break;
				case OPC_E_UNKNOWNPATH:
					returns = "OPC_E_UNKNOWNPATH";
					break;
				case OPC_E_RANGE:
					returns = "OPC_E_RANGE";
					break;
				case OPC_E_DUPLICATENAME:
					returns = "OPC_E_DUPLICATENAME";
					break;
				case OPC_S_UNSUPPORTEDRATE:
					returns = "OPC_S_UNSUPPORTEDRATE";
					break;
				case OPC_S_CLAMP:
					returns = "OPC_S_CLAMP";
					break;
				case OPC_S_INUSE:
					returns = "OPC_S_INUSE";
					break;
				case OPC_E_INVALIDCONFIGFILE:
					returns = "OPC_E_INVALIDCONFIGFILE";
					break;
				case OPC_E_NOTFOUND:
					returns = "OPC_E_NOTFOUND";
					break;
				case OPC_E_INVALID_PID:
					returns = "OPC_E_INVALID_PID";
					break;
				default:
					returns = "UNKNOWN ERROR";
					break;
			}
			return returns;
		}
	}
	/// <summary>
	/// dummy VARIANT  (workaround)
	/// </summary>
	[ComVisible(true), StructLayout(LayoutKind.Sequential, Pack = 2)]
	public class DUMMY_VARIANT {
		/// <summary></summary>
		public static short VT_TYPEMASK = 0x0fff;
		/// <summary></summary>
		public static short VT_VECTOR = 0x1000;
		/// <summary></summary>
		public static short VT_ARRAY = 0x2000;
		/// <summary></summary>
		public static short VT_BYREF = 0x4000;
		/// <summary></summary>
		public static short VT_ILLEGAL = unchecked((short)0xffff);
		/// <summary></summary>
		public static int ConstSize = 16;
		/// <summary></summary>
		public short vt;
		/// <summary></summary>
		public short r1;
		/// <summary></summary>
		public short r2;
		/// <summary></summary>
		public short r3;
		/// <summary></summary>
		public int v1;
		/// <summary></summary>
		public int v2;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="addrofvariant"></param>
		[DllImport("oleaut32.dll")]
		public static extern void VariantInit(IntPtr addrofvariant);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="addrofvariant"></param>
		/// <returns></returns>
		[DllImport("oleaut32.dll")]
		public static extern int VariantClear(IntPtr addrofvariant);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="vevt"></param>
		/// <returns></returns>
		public static string VarEnumToString(VarEnum vevt) {
			string strvt = "";
			short vtshort = (short)vevt;
			if(vtshort == VT_ILLEGAL)
				return "VT_ILLEGAL";

			if((vtshort & VT_ARRAY) != 0)
				strvt += "VT_ARRAY | ";

			if((vtshort & VT_BYREF) != 0)
				strvt += "VT_BYREF | ";

			if((vtshort & VT_VECTOR) != 0)
				strvt += "VT_VECTOR | ";

			VarEnum vtbase = (VarEnum)(vtshort & VT_TYPEMASK);
			strvt += vtbase.ToString();
			return strvt;
		}
	}
	/// <summary>
	/// OPC common
	/// </summary>
	[ComVisible(true), ComImport,
	Guid("F31DFDE2-07B6-11d2-B2D8-0060083BA1FB"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IOPCCommon {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwLcid"></param>
		void SetLocaleID(
			[In] int dwLcid);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="pdwLcid"></param>
		void GetLocaleID(
			[Out] out int pdwLcid);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="pdwCount"></param>
		/// <param name="pdwLcid"></param>
		/// <returns></returns>
		[PreserveSig]
		int QueryAvailableLocaleIDs(
			[Out] out int pdwCount,
			[Out] out IntPtr pdwLcid);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwError"></param>
		/// <param name="ppString"></param>
		void GetErrorString(
			[In] int dwError,
			[Out, MarshalAs(UnmanagedType.LPWStr)] out string ppString);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="szName"></param>
		void SetClientName(
			[In, MarshalAs(UnmanagedType.LPWStr)] string szName);
	}
	/// <summary>
	/// Common callback
	/// </summary>
	[ComVisible(true), ComImport,
	Guid("F31DFDE1-07B6-11d2-B2D8-0060083BA1FB"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IOPCShutdown {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="szReason"></param>
		void ShutdownRequest(
			[In, MarshalAs(UnmanagedType.LPWStr)] string szReason);
	}
	/// <summary>
	/// Server List enum
	/// </summary>
	[ComVisible(true), ComImport,
	Guid("13486D50-4821-11D2-A494-3CB306C10000"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IOPCServerList {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="cImplemented"></param>
		/// <param name="catidImpl"></param>
		/// <param name="cRequired"></param>
		/// <param name="catidReq"></param>
		/// <param name="ppUnk"></param>
		void EnumClassesOfCategories(
			[In] int cImplemented,  // WARNING ONLY 1!!
			[In] ref Guid catidImpl,        // WARNING ONLY 1!!
			[In] int cRequired,     // WARNING ONLY 1!!
			[In] ref Guid catidReq,     // WARNING ONLY 1!!
			[Out, MarshalAs(UnmanagedType.IUnknown)] out object ppUnk);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="clsid"></param>
		/// <param name="ppszProgID"></param>
		/// <param name="ppszUserType"></param>
		void GetClassDetails(
			[In] ref Guid clsid,
			[Out, MarshalAs(UnmanagedType.LPWStr)] out string ppszProgID,
			[Out, MarshalAs(UnmanagedType.LPWStr)] out string ppszUserType);
		/// <summary>
		/// 
		/// </summary>
		/// <param name="szProgId"></param>
		/// <param name="clsid"></param>
		void CLSIDFromProgID(
			[In, MarshalAs(UnmanagedType.LPWStr)] string szProgId,
			[Out] out Guid clsid);
	}
	/// <summary>
	/// Enum GUIDs
	/// </summary>
	[ComVisible(true), ComImport,
	Guid("0002E000-0000-0000-C000-000000000046"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IEnumGUID {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="celt"></param>
		/// <param name="rgelt"></param>
		/// <param name="pceltFetched"></param>
		void Next(
			[In] int celt,
			[In] IntPtr rgelt,              // ptr to Out-Values!!
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
