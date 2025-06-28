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
//# Revision     : $Rev:: 245                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: Watchdog.cs 245 2025-06-28 15:07:22Z                     $ #
//#                                                                                 #
//###################################################################################
using FreakaZone.Libraries.wpCommen;
using FreakaZone.Libraries.wpEventLog;
using FreakaZone.Libraries.wpIniFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using static FreakaZone.Libraries.wpEventLog.Logger;

namespace WebAutomation.Helper {
	public class Watchdog {
		private Logger eventLog;
		private System.Timers.Timer watchdogTimer;
		private int watchdogByte;
		private int watchdogByteLast;
		private int maxWatchdogByte;
		public Watchdog() {
			eventLog = new Logger(Logger.ESource.PlugInWatchdog);
			Debug.Write(MethodInfo.GetCurrentMethod(), "Watchdog Init");
			int watchdogVerz;
			if(Int32.TryParse(IniFile.Get("Watchdog", "Verz"), out watchdogVerz)) {
				if(watchdogVerz <= 0)
					watchdogVerz = 1;
				watchdogTimer = new System.Timers.Timer(1000 * 60 * watchdogVerz);
				watchdogTimer.Elapsed += watchdog_Tick;
				watchdogTimer.AutoReset = true;
				watchdogTimer.Enabled = true;
				if(!Int32.TryParse(IniFile.Get("Watchdog", "MaxInt"), out maxWatchdogByte)) {
					maxWatchdogByte = 255;
				}
				if(maxWatchdogByte < 2)
					maxWatchdogByte = 2;
				watchdogByte = maxWatchdogByte - 1;
				eventLog.Write(MethodInfo.GetCurrentMethod(), "PlugIn Watchdog initialisiert");
				// We are faster!!!
				Thread.Sleep(1000);
				setWDB();
				// We are faster!!!
				Thread.Sleep(1000);
				setWDB();
			}
			Debug.Write(MethodInfo.GetCurrentMethod(), "Watchdog Inited");
		}
		public void finished() {
			Debug.Write(MethodInfo.GetCurrentMethod(), "Watchdog Stop");
			if(watchdogTimer != null) {
				watchdogTimer.Stop();
				watchdogTimer.Enabled = false;
				watchdogTimer.Dispose();
			}
			Debug.Write(MethodInfo.GetCurrentMethod(), "Watchdog Stoped");
		}
		private void watchdog_Tick(object sender, EventArgs e) {
			//PDebug.Write("Nur zum Test Watchdog: Transfer Counter: {0}", Program.MainProg.getTransferCounter());
			setWDB();
		}
		private void setWDB() {
			watchdogByteLast = watchdogByte;
			watchdogByte++;
			if(watchdogByte > maxWatchdogByte || watchdogByte < 1)
				watchdogByte = 1;

			int watchdogId, MqttAlarm;
			bool MqttAlarmValid = false;
			List<int> OpcWatchdogs = new List<int>();
			List<int> MqttWatchdogs = new List<int>();
			string OpcWatchdog = IniFile.Get("Watchdog", "DpIdOpc");
			string MqttWatchdog = IniFile.Get("Watchdog", "DpIdMqtt");
			if(Int32.TryParse(IniFile.Get("Watchdog", "DpIdMqttAlarm"), out MqttAlarm)) {
				MqttAlarmValid = true;
			}
			if(OpcWatchdog.Contains(",")) {
				string[] ids = OpcWatchdog.Split(',');
				foreach(string id in ids) {
					if(Int32.TryParse(id.Trim(), out watchdogId)) {
						OpcWatchdogs.Add(watchdogId);
					}
				}
			} else {
				if(Int32.TryParse(OpcWatchdog.Trim(), out watchdogId)) {
					OpcWatchdogs.Add(watchdogId);
				}
			}
			if(MqttWatchdog.Contains(",")) {
				string[] ids = MqttWatchdog.Split(',');
				foreach(string id in ids) {
					if(Int32.TryParse(id.Trim(), out watchdogId)) {
						MqttWatchdogs.Add(watchdogId);
					}
				}
			} else {
				if(Int32.TryParse(MqttWatchdog.Trim(), out watchdogId)) {
					MqttWatchdogs.Add(watchdogId);
				}
			}
			foreach(int id in OpcWatchdogs) {
				Program.MainProg.wpOPCClient.setValue(id, watchdogByte.ToString(), TransferId.TransferWatchdog);
				if(Debug.debugTransferID)
					Debug.Write(MethodInfo.GetCurrentMethod(), "write OPC WatchdogByte: {0}", watchdogByte);
			}
			foreach(int id in MqttWatchdogs) {
				if(watchdogByte > 1 && !String.IsNullOrEmpty(Datapoints.Get(id).Value) && Datapoints.Get(id).Value != watchdogByteLast.ToString()) {
					Debug.Write(MethodBase.GetCurrentMethod(), $"MQTT Broker Offline? DP: {Datapoints.Get(id).Value}, WD: {watchdogByteLast}");
					if(MqttAlarmValid)
						Datapoints.Get(MqttAlarm).WriteValue("1");
				} else {
					if(MqttAlarmValid)
						Datapoints.Get(MqttAlarm).WriteValue("0");
				}
				Datapoints.Get(id).WriteValue(watchdogByte.ToString());
				if(Debug.debugTransferID)
					Debug.Write(MethodInfo.GetCurrentMethod(), "write MQTT WatchdogByte: {0}", watchdogByte);
			}
		}
	}
	public class wpServiceStatus {
		private Logger eventLog;
		public event ServiceStatusChangedEventHandler ServiceStatusChanged;
		private ServiceControllerStatus _serviceStatus;
		private Timer serviceTimer;
		private Timer serviceStart;
		private string _serviceName;
		public ServiceControllerStatus ServiceStatus {
			set {
				if(_serviceStatus != value) {
					_serviceStatus = value;
					if(_serviceStatus == ServiceControllerStatus.Stopped) {
						if(Common.IsAdmin()) {
							try {
								serviceStart.Change(2000, Timeout.Infinite);
							} catch(Exception ex) {
								eventLog.WriteError(MethodInfo.GetCurrentMethod(), ex);
							}
						}
					}
					if(ServiceStatusChanged != null)
						ServiceStatusChanged(new ServiceStatusChangedEventArgs(value));
				}
			}

		}
		public delegate void ServiceStatusChangedEventHandler(ServiceStatusChangedEventArgs e);
		public wpServiceStatus(string servicename) {
			eventLog = new Logger(Logger.ESource.PlugInServiceStatus);
			this._serviceName = servicename;
			if(_serviceName != "" && checkServiceInstalled()) {
				serviceTimer = new Timer(Timer_Tick, _serviceName, 0, 500);
				if(Common.IsAdmin()) {
					Debug.Write(MethodInfo.GetCurrentMethod(), "Autostart Services activated.");
					serviceStart = new Timer(new TimerCallback(Start_Tick));
				}
			}
			eventLog.Write(MethodInfo.GetCurrentMethod(), "PlugIn ServiceStatus initialisiert");
		}
		private void Timer_Tick(object stateInfo) {
			string _servicename = (string)stateInfo;
			ServiceController _service = new ServiceController(_servicename);
			ServiceStatus = _service.Status;
		}
		private void Start_Tick(object sn) {
			ServiceController _service = new ServiceController(_serviceName);
			eventLog.Write(MethodInfo.GetCurrentMethod(), "Service '{0}' send start", _serviceName);
			_service.Start();
		}
		private bool checkServiceInstalled() {
			foreach(ServiceController sc in ServiceController.GetServices()) {
				if(sc.ServiceName == _serviceName) {
					eventLog.Write(MethodInfo.GetCurrentMethod(), "Service '{0}' exists - start monitoring", _serviceName);
					return true;
				}
			}
			eventLog.Write(MethodInfo.GetCurrentMethod(), ELogEntryType.Warning, "Service '{0}' did not exists - monitoring faild", _serviceName);
			return false;
		}
		public class ServiceStatusChangedEventArgs: EventArgs {
			public ServiceControllerStatus newStatus;
			public ServiceStatusChangedEventArgs(ServiceControllerStatus _newStatus) {
				this.newStatus = _newStatus;
			}
		}
	}
	public class wpSystemStatus {
		private Logger eventLog;
		public event MemoryStatusChangedEventHandler MemoryStatusChanged;
		public event ProzessorStatusChangedEventHandler ProzessorStatusChanged;

		private float _memory;
		private float _prozessor;

		private bool _firstMemory = true;
		private bool _firstProzessor = true;

		private Timer memoryTimer;
		private Timer prozessorTimer;

		DriveInfo[] Drives = DriveInfo.GetDrives();

		System.Diagnostics.PerformanceCounter cpuCounter;
		System.Diagnostics.PerformanceCounter ramCounter;

		public delegate void MemoryStatusChangedEventHandler(SystemStatusChangedEventArgs e);
		public delegate void ProzessorStatusChangedEventHandler(SystemStatusChangedEventArgs e);
		public float MemoryStatus {
			set {
				_memory = value;
				if(MemoryStatusChanged != null)
					MemoryStatusChanged(new SystemStatusChangedEventArgs(value));
			}

		}
		public float ProzessorStatus {
			set {
				_prozessor = value;
				if(ProzessorStatusChanged != null)
					ProzessorStatusChanged(new SystemStatusChangedEventArgs(value));
			}

		}

		public wpSystemStatus() {
			eventLog = new Logger(Logger.ESource.PlugInServiceStatus);
			ramCounter = new System.Diagnostics.PerformanceCounter("Memory", "Available MBytes");
			cpuCounter = new System.Diagnostics.PerformanceCounter("Process", "% Processor Time", System.Diagnostics.Process.GetCurrentProcess().ProcessName);

			memoryTimer = new Timer(memoryTimer_Tick, null, 100, 500);
			prozessorTimer = new Timer(prozessorTimer_Tick, null, 100, 500);

			eventLog.Write(MethodInfo.GetCurrentMethod(), "PlugIn ServiceStatus initialisiert");

		}
		private void memoryTimer_Tick(object stateInfo) {
			float fTemp = getMemory();
			if(fTemp != _memory || _firstMemory) {
				_firstMemory = false;
				MemoryStatus = fTemp;
			}
		}
		private void prozessorTimer_Tick(object stateInfo) {
			float fTemp = getProzessor();
			if(fTemp != _prozessor || _firstProzessor) {
				_firstProzessor = false;
				ProzessorStatus = fTemp;
			}
		}
		private float getMemory() {
			return ramCounter.NextValue();
		}
		private float getProzessor() {
			float _cpu = (float)Math.Round((double)cpuCounter.NextValue(), 0);
			if(_cpu < 0)
				_cpu = 0;
			if(_cpu > 100)
				_cpu = 100;
			return _cpu;
		}
		public class SystemStatusChangedEventArgs: EventArgs {
			public float newStatus;
			public SystemStatusChangedEventArgs(float _newStatus) {
				this.newStatus = _newStatus;
			}
		}
	}
}
