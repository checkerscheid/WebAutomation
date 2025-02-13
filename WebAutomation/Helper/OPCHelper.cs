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
//# Revision     : $Rev:: 171                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: OPCHelper.cs 171 2025-02-13 12:28:06Z                    $ #
//#                                                                                 #
//###################################################################################
using FreakaZone.Libraries.wpEventLog;
using FreakaZone.Libraries.wpSQL;
using OPC.Data;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
/**
* @addtogroup WebAutomation
* @{
*/
namespace WebAutomation.Helper {
	/// <summary>
	/// 
	/// </summary>
	public class PGAOPCServer:OpcServer {
		/// <summary></summary>
		private bool disposed = false;
		/// <summary></summary>
		private int _id;
		/// <summary></summary>
		public int Id {
			get { return _id; }
			set { _id = value; }
		}
		/// <summary></summary>
		private string _progid;
		/// <summary></summary>
		public string Progid {
			get { return _progid; }
			set { _progid = value; }
		}
		/// <summary></summary>
		private string _clsid;
		/// <summary></summary>
		public string Clsid {
			get { return _clsid; }
			set { _clsid = value; }
		}
		/// <summary></summary>
		private bool _active;
		/// <summary></summary>
		public bool Active {
			set { this._active = value; }
			get { return this._active; }
		}
		private string _remoteserver;
		public string Remoteserver {
			set { this._remoteserver = value; }
			get { return this._remoteserver; }
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="server"></param>
		/// <param name="name"></param>
		/// <param name="progid"></param>
		/// <param name="clsid"></param>
		public PGAOPCServer(int id, string name, string progid, string clsid):base(name) {
			_id = id;
			_progid = progid;
			_clsid = clsid;
			Name = name;
		}

		~PGAOPCServer() {
			Debug.Write(MethodInfo.GetCurrentMethod(), "OPC DATA SRV '{0}' - Finalize", Name);
			Disconnect();
			Dispose();
		}
		public void Dispose() {
			Debug.Write(MethodInfo.GetCurrentMethod(), "OPC DATA SRV '{0}' - Dispose", Name);
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing) {
			if (disposed)
				return;
			if (disposing) {
				// Free any other managed objects here.
			}
			// Free any unmanaged objects here.
			disposed = true;
		}
		public void Connect(string prgidOPCserver) {
			this._remoteserver = "localhost";
			base.Connect(prgidOPCserver, "localhost");
			Debug.Write(MethodInfo.GetCurrentMethod(), "OPC DATA SRV '{0}' - Connected", Name);
		}
		public new void Connect(string prgidOPCserver, string computername) {
			base.Connect(prgidOPCserver, computername);
			this._remoteserver = computername;
			Debug.Write(MethodInfo.GetCurrentMethod(), "OPC DATA SRV '{0}:{1}' - Connected", computername, Name);
		}
	}
	/// <summary>
	/// 
	/// </summary>
	/*public class PGAOPCGroup {
		private DateTime _lastChange;
		public DateTime LastChange {
			get { return _lastChange; }
			set { _lastChange = value; }
		}
		/// <summary>
		/// 
		/// </summary>
		public PGAOPCGroup() {
		}
	}*/
	/// <summary>
	/// 
	/// </summary>
	public class OPCItem {
		/// <summary></summary>
		private bool _active;
		/// <summary></summary>
		public bool Active {
			get { return _active; }
			set { _active = value; }
		}
		/// <summary></summary>
		private int _hclt;
		/// <summary></summary>
		public int Hclt {
			get { return _hclt; }
			set { _hclt = value; }
		}
		/// <summary></summary>
		private int _hsrv;
		/// <summary></summary>
		public int Hsrv {
			get { return _hsrv; }
			set { _hsrv = value; }
		}
		/// <summary></summary>
		private string _opcitemname;
		/// <summary></summary>
		public string OpcItemName {
			get { return _opcitemname; }
			set { _opcitemname = value; }
		}
		private string _itemname;
		public string ItemName {
			get { return _itemname; }
			set { _itemname = value; }
		}
		/// <summary></summary>
		private string _value;
		/// <summary></summary>
		public string Value {
			get { return _value; }
			set { _value = value; }
		}
		/// <summary></summary>
		private DateTime _lastupdate;
		/// <summary></summary>
		public DateTime Lastupdate {
			get { return _lastupdate; }
			set { _lastupdate = value; }
		}
		/// <summary></summary>
		private short _quality;
		/// <summary></summary>
		public short Quality {
			get { return _quality; }
			set { _quality = value; }
		}
		/// <summary></summary>
		private int _server;
		/// <summary></summary>
		public int Server {
			get { return _server; }
			set { _server = value; }
		}
		/// <summary></summary>
		private int _group;
		/// <summary></summary>
		public int Group {
			get { return _group; }
			set { _group = value; }
		}
		/// <summary></summary>
		private VarEnum _dbtype;
		/// <summary></summary>
		public VarEnum DBType {
			get { return _dbtype; }
			set { _dbtype = value; }
		}
		private bool _hasFirstValue;
		public bool hasFirstValue {
			get { return _hasFirstValue; }
			set {
				if (!hasFirstValue) {
					using (Database Sql = new Database("startup for Datapoint")) {
						Sql.wpNonResponse("UPDATE [opcdatapoint] SET [startuptype] = '{1}', [startupquality] = '{2}' WHERE [id_opcdatapoint] = {0}", _hclt, _dbtype, OPCQuality.get(_quality));
					}
				}
				_hasFirstValue = true;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="hclt"></param>
		/// <param name="opcitemname"></param>
		/// <param name="group"></param>
		/// <param name="server"></param>
		public OPCItem(int hclt, string opcitemname, string itemname, int group, int server) {
			_hclt = hclt;
			_opcitemname = opcitemname;
			_itemname = itemname;
			_server = server;
			_group = group;
			_active = true;
			_hasFirstValue = false;
		}
	}
	/// <summary>
	/// 
	/// </summary>
	internal static class OPCQuality {
		/// <summary>
		/// 
		/// </summary>
		private static Dictionary<short, string> Quality = new Dictionary<short, string>() {
				{0, "Bad"},
				{4, "Bad, Configuration Error"},
				{8, "Bad, Not Connected"},
				{12, "Bad, Device Failure"},
				{16, "Bad, Sensor Failure"},
				{20, "Bad, Last Known Value"},
				{24, "Bad, Comm Failure"},
				{28, "Bad, Out of Service"},
				{32, "Bad, Wait for initial Data"},
				{64, "Uncertain"},
				{68, "Uncertain, Last Usable Value"},
				{80, "Uncertain, Sensor Not Accurate"},
				{84, "Uncertain, EU Units Exceeded"},
				{88, "Uncertain, Sub Normal"},
				{192, "Good"},
				{216, "Good, Local Overrride"},
				{255, "No Value"}
			};
		/// <summary>
		/// 
		/// </summary>
		/// <param name="quali"></param>
		/// <returns></returns>
		public static string get(short quali) {
			if (OPCQuality.Quality.ContainsKey(quali)) {
				return OPCQuality.Quality[quali];
			}
			return quali.ToString();
		}
	}
	internal static class PVTEnum {
		private static Dictionary<string, VarEnum> VT_Enum = new Dictionary<string, VarEnum>() {
				{"VT_BOOL", VarEnum.VT_BOOL},
				{"VT_I1", VarEnum.VT_I1},
				{"VT_I2", VarEnum.VT_I2},
				{"VT_I4", VarEnum.VT_I4},
				{"VT_I8", VarEnum.VT_I8},
				{"VT_INT", VarEnum.VT_INT},
				{"VT_UI1", VarEnum.VT_UI1},
				{"VT_UI2", VarEnum.VT_UI2},
				{"VT_UI4", VarEnum.VT_UI4},
				{"VT_UI8", VarEnum.VT_UI8},
				{"VT_UINT", VarEnum.VT_UINT},
				{"VT_R4", VarEnum.VT_R4},
				{"VT_R8", VarEnum.VT_R8},
				{"VT_DECIMAL", VarEnum.VT_DECIMAL},
				{"VT_BSTR", VarEnum.VT_BSTR},
				{"VT_DATE", VarEnum.VT_DATE},
				{"VT_VARIANT", VarEnum.VT_VARIANT}
			};
		public static VarEnum get(string sVTEnum) {
			if (PVTEnum.VT_Enum.ContainsKey(sVTEnum)) return PVTEnum.VT_Enum[sVTEnum];
			else return VarEnum.VT_EMPTY;
		}
		public static string ToString(VarEnum TheType) {
			foreach (KeyValuePair<string, VarEnum> TheEntry in PVTEnum.VT_Enum) {
				if (TheEntry.Value == TheType) return TheEntry.Key;
			}
			return "VT_EMPTY";
		}
	}
}
/** @} */
