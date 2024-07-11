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
//# Revision     : $Rev:: 118                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: WebAutomationServer.Designer.cs 118 2024-07-04 14:20:41Z#$ #
//#                                                                                 #
//###################################################################################
using System;
using System.Diagnostics;
using System.Windows.Forms;
using WebAutomation.Helper;
using WebAutomation.PlugIns;
/**
* @defgroup WEBAutomationWindow WEBAutomationWindow
* @{
*/
namespace WebAutomation {
	/// <summary>
	/// 
	/// </summary>
	partial class WebAutomationServer {
		/// <summary>
		/// Erforderliche Designervariable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Verwendete Ressourcen bereinigen.
		/// </summary>
		/// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
		protected override void Dispose(bool disposing) {
			if(disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
			Helper.wpDebug.Write("{0} - Disposed", Application.ProductName);
		}

		#region Vom Windows Form-Designer generierter Code

		/// <summary>
		/// Erforderliche Methode für die Designerunterstützung.
		/// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
		/// </summary>
		private void InitializeComponent() {
			this.components = new System.ComponentModel.Container();
			this.statusStrip = new System.Windows.Forms.StatusStrip();
			this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
			this.SystemIcon = new System.Windows.Forms.NotifyIcon(this.components);
			this.txt_lastchange = new System.Windows.Forms.Label();
			this.txt_msg = new System.Windows.Forms.Label();
			this.lbl_lastchange = new System.Windows.Forms.Label();
			this.lbl_msg = new System.Windows.Forms.TextBox();
			this.txt_db = new System.Windows.Forms.Label();
			this.lbl_db = new System.Windows.Forms.Label();
			this.txt_System = new System.Windows.Forms.Label();
			this.lbl_prozessor = new System.Windows.Forms.Label();
			this.lbl_memory = new System.Windows.Forms.Label();
			this.lbl_volumeinfo = new System.Windows.Forms.Label();
			this.nonsens = new System.Windows.Forms.TextBox();
			this.statusStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// statusStrip
			// 
			this.statusStrip.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
			this.statusStrip.Location = new System.Drawing.Point(0, 185);
			this.statusStrip.Name = "statusStrip";
			this.statusStrip.Padding = new System.Windows.Forms.Padding(1, 0, 17, 0);
			this.statusStrip.Size = new System.Drawing.Size(698, 22);
			this.statusStrip.TabIndex = 0;
			this.statusStrip.Text = "statusStrip1";
			// 
			// toolStripStatusLabel1
			// 
			this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
			this.toolStripStatusLabel1.Size = new System.Drawing.Size(12, 17);
			this.toolStripStatusLabel1.Text = "-";
			// 
			// SystemIcon
			// 
			this.SystemIcon.Icon = global::WebAutomation.Properties.Resources.wp;
			this.SystemIcon.Visible = true;
			this.SystemIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(this.SystemIcon_MouseClick);
			// 
			// txt_lastchange
			// 
			this.txt_lastchange.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.txt_lastchange.AutoSize = true;
			this.txt_lastchange.Location = new System.Drawing.Point(13, 131);
			this.txt_lastchange.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.txt_lastchange.Name = "txt_lastchange";
			this.txt_lastchange.Size = new System.Drawing.Size(83, 13);
			this.txt_lastchange.TabIndex = 1;
			this.txt_lastchange.Text = "Last Change:";
			// 
			// txt_msg
			// 
			this.txt_msg.AutoSize = true;
			this.txt_msg.Location = new System.Drawing.Point(13, 16);
			this.txt_msg.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.txt_msg.Name = "txt_msg";
			this.txt_msg.Size = new System.Drawing.Size(88, 13);
			this.txt_msg.TabIndex = 2;
			this.txt_msg.Text = "Last Message:";
			// 
			// lbl_lastchange
			// 
			this.lbl_lastchange.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.lbl_lastchange.AutoSize = true;
			this.lbl_lastchange.Location = new System.Drawing.Point(146, 131);
			this.lbl_lastchange.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.lbl_lastchange.Name = "lbl_lastchange";
			this.lbl_lastchange.Size = new System.Drawing.Size(12, 13);
			this.lbl_lastchange.TabIndex = 3;
			this.lbl_lastchange.Text = "-";
			// 
			// lbl_msg
			// 
			this.lbl_msg.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lbl_msg.Location = new System.Drawing.Point(149, 16);
			this.lbl_msg.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.lbl_msg.Multiline = true;
			this.lbl_msg.Name = "lbl_msg";
			this.lbl_msg.ReadOnly = true;
			this.lbl_msg.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.lbl_msg.Size = new System.Drawing.Size(535, 73);
			this.lbl_msg.TabIndex = 4;
			this.lbl_msg.TabStop = false;
			this.lbl_msg.Enter += new System.EventHandler(this.lbl_msg_Enter);
			// 
			// txt_db
			// 
			this.txt_db.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.txt_db.AutoSize = true;
			this.txt_db.Location = new System.Drawing.Point(13, 118);
			this.txt_db.Name = "txt_db";
			this.txt_db.Size = new System.Drawing.Size(130, 13);
			this.txt_db.TabIndex = 5;
			this.txt_db.Text = "geladene Datenbank:";
			// 
			// lbl_db
			// 
			this.lbl_db.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.lbl_db.AutoSize = true;
			this.lbl_db.Location = new System.Drawing.Point(146, 118);
			this.lbl_db.Name = "lbl_db";
			this.lbl_db.Size = new System.Drawing.Size(12, 13);
			this.lbl_db.TabIndex = 6;
			this.lbl_db.Text = "-";
			// 
			// txt_System
			// 
			this.txt_System.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.txt_System.AutoSize = true;
			this.txt_System.Location = new System.Drawing.Point(13, 92);
			this.txt_System.Name = "txt_System";
			this.txt_System.Size = new System.Drawing.Size(55, 13);
			this.txt_System.TabIndex = 7;
			this.txt_System.Text = "System:";
			// 
			// lbl_prozessor
			// 
			this.lbl_prozessor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.lbl_prozessor.AutoSize = true;
			this.lbl_prozessor.Location = new System.Drawing.Point(146, 92);
			this.lbl_prozessor.Name = "lbl_prozessor";
			this.lbl_prozessor.Size = new System.Drawing.Size(12, 13);
			this.lbl_prozessor.TabIndex = 8;
			this.lbl_prozessor.Text = "-";
			// 
			// lbl_memory
			// 
			this.lbl_memory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.lbl_memory.AutoSize = true;
			this.lbl_memory.Location = new System.Drawing.Point(146, 105);
			this.lbl_memory.Name = "lbl_memory";
			this.lbl_memory.Size = new System.Drawing.Size(12, 13);
			this.lbl_memory.TabIndex = 9;
			this.lbl_memory.Text = "-";
			// 
			// lbl_volumeinfo
			// 
			this.lbl_volumeinfo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.lbl_volumeinfo.AutoSize = true;
			this.lbl_volumeinfo.Location = new System.Drawing.Point(415, 92);
			this.lbl_volumeinfo.Name = "lbl_volumeinfo";
			this.lbl_volumeinfo.Size = new System.Drawing.Size(12, 13);
			this.lbl_volumeinfo.TabIndex = 10;
			this.lbl_volumeinfo.Text = "-";
			// 
			// nonsens
			// 
			this.nonsens.Location = new System.Drawing.Point(159, 26);
			this.nonsens.Name = "nonsens";
			this.nonsens.ReadOnly = true;
			this.nonsens.Size = new System.Drawing.Size(100, 21);
			this.nonsens.TabIndex = 0;
			// 
			// WebAutomationServer
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Control;
			this.ClientSize = new System.Drawing.Size(698, 207);
			this.Controls.Add(this.txt_msg);
			this.Controls.Add(this.lbl_msg);
			this.Controls.Add(this.txt_System);
			this.Controls.Add(this.lbl_prozessor);
			this.Controls.Add(this.lbl_memory);
			this.Controls.Add(this.lbl_volumeinfo);
			this.Controls.Add(this.txt_db);
			this.Controls.Add(this.lbl_db);
			this.Controls.Add(this.txt_lastchange);
			this.Controls.Add(this.lbl_lastchange);
			this.Controls.Add(this.nonsens);
			this.Controls.Add(this.statusStrip);
			this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Icon = global::WebAutomation.Properties.Resources.wp;
			this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.Name = "WebAutomationServer";
			this.Text = "WebAutomation Server";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.WebAutomationServer_FormClosing);
			this.Load += new System.EventHandler(this.WebAutomationServer_Load);
			this.Shown += new System.EventHandler(this.WebAutomationServer_Shown);
			this.ClientSizeChanged += new System.EventHandler(this.WebAutomationServer_ClientSizeChanged);
			this.statusStrip.ResumeLayout(false);
			this.statusStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

#endregion
		/// <summary></summary>
		private System.Windows.Forms.StatusStrip statusStrip;
		/// <summary></summary>
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
		/// <summary></summary>
		private System.Windows.Forms.NotifyIcon SystemIcon;
		/// <summary></summary>
		private System.Windows.Forms.Label txt_lastchange;
		/// <summary></summary>
		private System.Windows.Forms.Label txt_msg;
		/// <summary></summary>
		private System.Windows.Forms.Label lbl_lastchange;
		/// <summary></summary>
		private System.Windows.Forms.TextBox lbl_msg;
		private System.Windows.Forms.Label txt_db;
		private System.Windows.Forms.Label lbl_db;


#region UI

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void WebAutomationServer_Load(object sender, EventArgs e) {
			_ = initAsync();
			eventLog.Write(Application.ProductName + " Server gestartet");
			this.Text = Application.ProductName;
			this.StringChanged += new StringChangedEventHandler(WebAutomationServer_StringChanged);
		}

		private void WebAutomationServer_Shown(object sender, EventArgs e) {
			if(_wpStartMinimized) {
				this.WindowState = FormWindowState.Minimized;
				this.Hide();
				eventLog.Write(EventLogEntryType.Warning, Application.ProductName + " Server im 'Minimierten Modus' gestartet");
			}
		}

		private void WebAutomationServer_StringChanged(WebAutomationServer.StringChangedEventArgs e) {
			try {
				lbl_lastchange.Invoke((MethodInvoker)(() => lbl_lastchange.Text = e.newValue));
			} catch(Exception) { }
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SystemIcon_MouseClick(object sender, MouseEventArgs e) {
			if(this.WindowState == FormWindowState.Minimized) {
				this.Show();
				this.WindowState = lastState;
			} else {
				this.WindowState = FormWindowState.Minimized;
				this.Hide();
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void WebAutomationServer_ClientSizeChanged(object sender, EventArgs e) {
			if(this.WindowState == FormWindowState.Minimized) {
				this.Hide();
				SystemIcon.BalloonTipTitle = Application.ProductName + " - " + Ini.get("Projekt", "Nummer");
				SystemIcon.BalloonTipText = "wurde minimiert";
				SystemIcon.BalloonTipIcon = ToolTipIcon.Info;
				SystemIcon.ShowBalloonTip(1000);
			} else {
				this.Show();
				lastState = this.WindowState;
			}
		}
		public void finish() {
			Helper.wpDebug.Write(Application.ProductName + " Server - Beginn stop");
			isFinished = true;
			if(ApacheService != null)
				ApacheService.ServiceStatusChanged -= ApacheService_ServiceStatusChanged;
			if(MssqlService != null)
				MssqlService.ServiceStatusChanged -= MssqlService_ServiceStatusChanged;
			if(SystemStatus != null)
				SystemStatus.MemoryStatusChanged -= SystemStatus_MemoryStatusChanged;
			if(SystemStatus != null)
				SystemStatus.ProzessorStatusChanged -= SystemStatus_ProzessorStatusChanged;

			if(ThreadEmailSender != null)
				ThreadEmailSender.Join(1500);
			if(wpWebCom != null)
				wpWebCom.finished();
			if(wpWebSockets != null)
				wpWebSockets.finished();
			if(wpWatchdog != null)
				wpWatchdog.finished();
			if(wpOPCClient != null)
				wpOPCClient.finished();
			if(wpRest != null)
				wpRest.finished();
			D1MiniServer.Stop();
			ShellyServer.Stop();
			if(wpMQTTClient != null)
				wpMQTTClient.Stop();
			Trends.Stop();
			eventLog.Write(EventLogEntryType.Warning, Application.ProductName + " Server gestoppt");
		}

		private void WebAutomationServer_FormClosing(object sender, FormClosingEventArgs e) {
			string pw = Ini.get("Beenden", "PW");
			if(pw.Length > 0) {
				BeendenPW bpw = new BeendenPW();
				if(bpw.ShowDialog() == DialogResult.OK) {
					if(bpw.mtbpw.Text == pw) {
						finish();
					} else {
						MessageBox.Show("Das Passwort war nicht korrekt.",
							"Fehlerhafte Eingabe",
							MessageBoxButtons.OK,
							MessageBoxIcon.Error);
						e.Cancel = true;
					}
				} else {
					e.Cancel = true;
				}
			} else {
				finish();
			}
		}

#endregion

		private Label txt_System;
		private Label lbl_prozessor;
		private Label lbl_memory;
		private Label lbl_volumeinfo;
		private TextBox nonsens;
	}
}
/** @} */
