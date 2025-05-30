//###################################################################################
//#                                                                                 #
//#              (C) FreakaZone GmbH                                                #
//#              =======================                                            #
//#                                                                                 #
//###################################################################################
//#                                                                                 #
//# Author       : Christian Scheid                                                 #
//# Date         : 07.11.2019                                                       #
//#                                                                                 #
//# Revision     : $Rev:: 238                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: D1Mini.cs 238 2025-05-30 11:25:05Z                       $ #
//#                                                                                 #
//###################################################################################
using FreakaZone.Libraries.wpEventLog;
using FreakaZone.Libraries.wpSQL.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Timers;

namespace WebAutomation.Controller {

	public class D1Mini {
		private readonly int _id;
		public int Id {
			get { return _id; }
		}
		private readonly string _name;
		public string Name {
			get { return _name; }
		}
		private readonly IPAddress _ipAddress;
		public IPAddress IpAddress {
			get { return _ipAddress; }
		}
		private string _mac;
		private string _description;

		public string readDeviceName;
		public string readDeviceDescription;
		public string readVersion;
		public string readIp;
		public string readMac;
		public string readSsid;
		public string readUpdateMode;

		private bool _active;
		public bool Active {
			get { return _active; }
			set {
				Debug.Write(MethodBase.GetCurrentMethod(), $"D1Mini {nameof(Active)} changed from {_active} to {value}");
				_active = value;
			}
		}

		private int _id_onoff;
		public int Id_onoff {
			get { return _id_onoff; }
			set { _id_onoff = value; }
		}
		private int _id_temp;
		public int Id_temp {
			get { return _id_temp; }
			set { _id_temp = value; }
		}
		private int _id_hum;
		public int Id_hum {
			get { return _id_hum; }
			set { _id_hum = value; }
		}
		private int _id_ldr;
		public int Id_ldr {
			get { return _id_ldr; }
			set { _id_ldr = value; }
		}
		private int _id_light;
		public int Id_light {
			get { return _id_light; }
			set { _id_light = value; }
		}
		private int _id_relais;
		public int Id_relais {
			get { return _id_relais; }
			set { _id_relais = value; }
		}
		private int _id_rain;
		public int Id_rain {
			get { return _id_rain; }
			set { _id_rain = value; }
		}
		private int _id_moisture;
		public int Id_moisture {
			get { return _id_moisture; }
			set { _id_moisture = value; }
		}
		private int _id_vol;
		public int Id_vol {
			get { return _id_vol; }
			set { _id_vol = value; }
		}
		private int _id_window;
		public int Id_window {
			get { return _id_window; }
			set { _id_window = value; }
		}
		private int _id_analogout;
		public int Id_analogout {
			get { return _id_analogout; }
			set { _id_analogout = value; }
		}
		public bool Online {
			set {
				if(value) {
					if(Debug.debugD1Mini)
						Debug.Write(MethodInfo.GetCurrentMethod(), $"D1 Mini `recived Online`: {_name}/info/Online, 1");
					SetOnlineError(false);
					toreset?.Stop();
				} else {
					if(Debug.debugD1Mini)
						Debug.Write(MethodInfo.GetCurrentMethod(), $"D1 Mini `recived Online`: {_name}/info/Online, 0 - start resetTimer");
					toreset?.Start();
				}
			}
		}
		private Timer t;
		private Timer toreset;

		public class CmdList {
			public const string RestartDevice = "RestartDevice";
			public const string ForceMqttUpdate = "ForceMqttUpdate";
			public const string ForceRenewValue = "ForceRenewValue";
			public const string UpdateFW = "UpdateFW";
			public const string UnknownCmd = "UnknownCmd";
			private string _choosenCmd;
			public CmdList(string cmd) {
				switch(cmd) {
					case RestartDevice:
						_choosenCmd = RestartDevice;
						break;
					case ForceMqttUpdate:
						_choosenCmd = ForceMqttUpdate;
						break;
					case ForceRenewValue:
						_choosenCmd = ForceRenewValue;
						break;
					case UpdateFW:
						_choosenCmd = UpdateFW;
						break;
					default:
						_choosenCmd = UnknownCmd;
						break;
				}
			}
			public string Cmd {
				get { return _choosenCmd; }
			}
			public bool IsValid {
				get { return !(_choosenCmd == null || _choosenCmd == UnknownCmd); }
			}
		}
		private readonly List<string> subscribeList = [
			"info/DeviceName", "info/DeviceDescription",
			"info/Version", "info/wpFreakaZone",
			"info/WiFi/Ip", "info/WiFi/Mac", "info/WiFi/SSID",
			"UpdateMode", "info/Online" ];
		public D1Mini(int id, String name, IPAddress ip, String mac, String description) {
			_id = id;
			_name = name;
			_ipAddress = ip;
			_mac = mac;
			_description = description;
			_active = true;
		}
		public D1Mini(TableD1Mini td1m) {
			_id = td1m.id_d1mini;
			_name = td1m.name;
			_ipAddress = IPAddress.Parse(td1m.ip);
			_mac = td1m.mac;
			_description = td1m.description;
			_active = td1m.active;
			TableRest tr = (TableRest)td1m.SubValues.First();
			_id_onoff = tr.id_onoff;
			_id_temp = tr.id_temp;
			_id_hum = tr.id_hum;
			_id_ldr = tr.id_ldr;
			_id_light = tr.id_light;

			_id_relais = tr.id_relais;
			_id_rain = tr.id_rain;
			_id_moisture = tr.id_moisture;
			_id_vol = tr.id_vol;
			_id_window = tr.id_window;
			_id_analogout = tr.id_analogout;
		}
		public void Start() {
			t = new Timer(D1MiniServer.OnlineTogglerSendIntervall * 1000);
			t.Elapsed += OnlineCheck_Elapsed;
			if(D1MiniServer.OnlineTogglerSendIntervall > 0) {
				t.Start();
				SendOnlineQuestion();
			}
			toreset = new Timer(D1MiniServer.OnlineTogglerWait * 1000);
			toreset.AutoReset = false;
			toreset.Elapsed += Toreset_Elapsed;
		}

		public void Stop() {
			if(t != null)
				t.Stop();
			t = null;
			if(toreset != null)
				toreset.Stop();
			toreset = null;
			if(Debug.debugD1Mini)
				Debug.Write(MethodInfo.GetCurrentMethod(), $"D1 Mini stopped `{_name} sendOnlineQuestion`");
		}
		public void SetOnlineTogglerSendIntervall() {
			t.Interval = D1MiniServer.OnlineTogglerSendIntervall * 1000;
			t.Stop();
			if(D1MiniServer.OnlineTogglerSendIntervall > 0) {
				t.Start();
			}
		}
		public void SetOnlineTogglerWait() {
			toreset.Interval = D1MiniServer.OnlineTogglerWait * 1000;
			toreset.Stop();
		}
		private void OnlineCheck_Elapsed(object sender, ElapsedEventArgs e) {
			SendOnlineQuestion();
		}
		private void Toreset_Elapsed(object sender, ElapsedEventArgs e) {
			if(Debug.debugD1Mini)
				Debug.Write(MethodInfo.GetCurrentMethod(), $"D1 Mini `lastChancePing`: {_name} no response, send 'lastChance Ping'");
			//last chance
			Ping _ping = new Ping();
			if(_ping.Send(_ipAddress, 750).Status != IPStatus.Success) {
				SetOnlineError();
			} else {
				SetOnlineError(false);
				if(Debug.debugD1Mini)
					Debug.Write(MethodInfo.GetCurrentMethod(), $"D1 Mini OnlineToggler Script is missing?: {_name} MQTT no response, Ping OK");
			}
		}

		public List<string> GetSubscribtions() {
			List<string> returns = new List<string>();
			foreach(string topic in subscribeList) {
				returns.Add(_name + "/" + topic);
			}
			return returns;
		}
		public bool SendCmd(CmdList cmd) {
			bool returns = false;
			if(cmd.IsValid) {
				_ = Program.MainProg.wpMQTTClient.setValue(_name + "/" + cmd.Cmd, "1");
				if(Debug.debugD1Mini)
					Debug.Write(MethodInfo.GetCurrentMethod(), $"D1 Mini `sendCmd` success: {_name}, {cmd.Cmd}");
				returns = true;
			} else {
				Debug.Write(MethodInfo.GetCurrentMethod(), $"D1 Mini `sendCmd` ERROR: {_name}, {cmd.Cmd}");
			}
			return returns;
		}
		private void SendOnlineQuestion() {
			if(Debug.debugD1Mini)
				Debug.Write(MethodInfo.GetCurrentMethod(), $"D1 Mini `sendOnlineQuestion`: {_name}/info/Online, 0");
			_ = Program.MainProg.wpMQTTClient.setValue(_name + "/info/Online", "0", MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);
		}
		private void SetOnlineError(bool e) {
			if(Debug.debugD1Mini)
				Debug.Write(MethodInfo.GetCurrentMethod(), $"D1 Mini `setOnlineError`: {_name}/ERROR/Online, {(e ? "1" : "0")}");
			_ = Program.MainProg.wpMQTTClient.setValue(_name + "/ERROR/Online", e ? "1" : "0");
		}
		private void SetOnlineError() {
			SetOnlineError(true);
		}
	}
	class D1MiniBroadcast {
		public CIam Iam { get; set; }
		public class CIam {
			public string FreakaZoneClient { get; set; }
			public string IP { get; set; }
			public string MAC { get; set; }
			public string wpFreakaZoneVersion { get; set; }
			public string Version { get; set; }
		}
	}
}
