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
//# Revision     : $Rev:: 109                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: Watchdog.cs 109 2024-06-16 15:59:41Z                     $ #
//#                                                                                 #
//###################################################################################
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading;

namespace WebAutomation.Helper {
	public class Watchdog {
		private Logger eventLog;
		private System.Timers.Timer watchdogTimer;
		private int watchdogByte;
		private int maxWatchdogByte;
		public Watchdog() {
			eventLog = new Logger(wpEventLog.PlugInWatchdog);
			wpDebug.Write("Watchdog init");
			int watchdogVerz;
			if (Int32.TryParse(Ini.get("Watchdog", "Verz"), out watchdogVerz)) {
				if (watchdogVerz <= 0) watchdogVerz = 1;
				watchdogTimer = new System.Timers.Timer(1000 * 60 * watchdogVerz);
				watchdogTimer.Elapsed += watchdog_Tick;
				watchdogTimer.AutoReset = true;
				watchdogTimer.Enabled = true;
				if (!Int32.TryParse(Ini.get("Watchdog", "MaxInt"), out maxWatchdogByte)) {
					maxWatchdogByte = 255;
				}
				if (maxWatchdogByte < 2) maxWatchdogByte = 2;
				watchdogByte = maxWatchdogByte - 1;
				eventLog.Write("PlugIn Watchdog initialisiert");
				// We are faster!!!
				System.Threading.Thread.Sleep(1000);
				setWDB();
				// We are faster!!!
				System.Threading.Thread.Sleep(1000);
				setWDB();
			}
			eventLog.Write("Watchdog gestartet");
		}
		public void finished() {
			if (watchdogTimer != null) {
				watchdogTimer.Stop();
				watchdogTimer.Enabled = false;
				watchdogTimer.Dispose();
			}
		}
		private void watchdog_Tick(object sender, EventArgs e) {
			//PDebug.Write("Nur zum Test Watchdog: Transfer Counter: {0}", Program.MainProg.getTransferCounter());
			setWDB();
		}
		private void setWDB() {
			watchdogByte++;
			if (watchdogByte > maxWatchdogByte || watchdogByte < 1) watchdogByte = 1;

			int watchdogId;
			if (Int32.TryParse(Ini.get("Watchdog", "DpId"), out watchdogId)) {
				Program.MainProg.wpOPCClient.setValue(watchdogId, watchdogByte.ToString(),
					TransferId.TransferWatchdog);
				if(wpDebug.debugTransferID)
					wpDebug.Write("write WatchdogByte: {0}", watchdogByte);
			} else {
				string[] ids = Ini.get("Watchdog", "DpId").Split(',');

				if (ids.Length > 0) {
					List<int> idstowrite = new List<int>();
					foreach (string id in ids) {
						if (Int32.TryParse(id.Trim(), out watchdogId)) {
							Program.MainProg.wpOPCClient.setValue(watchdogId, watchdogByte.ToString(),
								TransferId.TransferWatchdog);
							if(wpDebug.debugWatchdog)
								wpDebug.Write("write WatchdogByte: {0}", watchdogByte);
						}
					}
				}
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
				if (_serviceStatus != value) {
					_serviceStatus = value;
					if (_serviceStatus == ServiceControllerStatus.Stopped) {
						if (wpHelp.isAdmin()) {
							try {
								serviceStart.Change(2000, Timeout.Infinite);
							} catch (Exception ex) {
								eventLog.WriteError(ex);
							}
						}
					}
					if (ServiceStatusChanged != null) ServiceStatusChanged(new ServiceStatusChangedEventArgs(value));
				}
			}

		}
		public delegate void ServiceStatusChangedEventHandler(ServiceStatusChangedEventArgs e);
		public wpServiceStatus(string servicename) {
			eventLog = new Logger(wpEventLog.PlugInServiceStatus);
			this._serviceName = servicename;
			if (_serviceName != "" && checkServiceInstalled()) {
				serviceTimer = new Timer(Timer_Tick, _serviceName, 0, 500);
				if (wpHelp.isAdmin()) {
					wpDebug.Write("Autostart Services activated.");
					serviceStart = new Timer(new TimerCallback(Start_Tick));
				}
			}
			eventLog.Write("PlugIn ServiceStatus initialisiert");
		}
		private void Timer_Tick(object stateInfo) {
			string _servicename = (string)stateInfo;
			ServiceController _service = new ServiceController(_servicename);
			ServiceStatus = _service.Status;
		}
		private void Start_Tick(object sn) {
			ServiceController _service = new ServiceController(_serviceName);
			eventLog.Write("Service '{0}' send start", _serviceName);
			_service.Start();
		}
		private bool checkServiceInstalled() {
			foreach (ServiceController sc in ServiceController.GetServices()) {
				if (sc.ServiceName == _serviceName) {
					eventLog.Write("Service '{0}' exists - start monitoring", _serviceName);
					return true;
				}
			}
			eventLog.Write(EventLogEntryType.Warning, "Service '{0}' did not exists - monitoring faild", _serviceName);
			return false;
		}
		public class ServiceStatusChangedEventArgs : EventArgs {
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

		PerformanceCounter cpuCounter;
		PerformanceCounter ramCounter;

		public delegate void MemoryStatusChangedEventHandler(SystemStatusChangedEventArgs e);
		public delegate void ProzessorStatusChangedEventHandler(SystemStatusChangedEventArgs e);
		public float MemoryStatus {
			set {
				_memory = value;
				if (MemoryStatusChanged != null) MemoryStatusChanged(new SystemStatusChangedEventArgs(value));
			}

		}
		public float ProzessorStatus {
			set {
				_prozessor = value;
				if (ProzessorStatusChanged != null) ProzessorStatusChanged(new SystemStatusChangedEventArgs(value));
			}

		}

		public wpSystemStatus() {
			eventLog = new Logger(wpEventLog.PlugInServiceStatus);
			ramCounter = new PerformanceCounter("Memory", "Available MBytes");
			cpuCounter = new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);

			memoryTimer = new Timer(memoryTimer_Tick, null, 100, 500);
			prozessorTimer = new Timer(prozessorTimer_Tick, null, 100, 500);

			eventLog.Write("PlugIn ServiceStatus initialisiert");

		}
		private void memoryTimer_Tick(object stateInfo) {
			float fTemp = getMemory();
			if (fTemp != _memory || _firstMemory) {
				_firstMemory = false;
				MemoryStatus = fTemp;
			}
		}
		private void prozessorTimer_Tick(object stateInfo) {
			float fTemp = getProzessor();
			if (fTemp != _prozessor || _firstProzessor) {
				_firstProzessor = false;
				ProzessorStatus = fTemp;
			}
		}
		private float getMemory() {
			return ramCounter.NextValue();
		}
		private float getProzessor() {
			float _cpu = (float)Math.Round((double)cpuCounter.NextValue(), 0);
			if (_cpu < 0) _cpu = 0;
			if (_cpu > 100) _cpu = 100;
			return _cpu;
		}
		public class SystemStatusChangedEventArgs : EventArgs {
			public float newStatus;
			public SystemStatusChangedEventArgs(float _newStatus) {
				this.newStatus = _newStatus;
			}
		}
	}
}
