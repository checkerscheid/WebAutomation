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
//# Revision     : $Rev:: 65                                                      $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: BeendenPW.Designer.cs 65 2023-12-21 22:58:05Z            $ #
//#                                                                                 #
//###################################################################################
using System.Windows.Forms;

namespace WebAutomation {
	partial class BeendenPW {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.btnok = new System.Windows.Forms.Button();
			this.mtbpw = new System.Windows.Forms.MaskedTextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.button1 = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// btnok
			// 
			this.btnok.Location = new System.Drawing.Point(148, 167);
			this.btnok.Name = "btnok";
			this.btnok.Size = new System.Drawing.Size(154, 23);
			this.btnok.TabIndex = 0;
			this.btnok.Text = "Server Beenden";
			this.btnok.UseVisualStyleBackColor = true;
			this.btnok.Click += new System.EventHandler(this.btnok_Click);
			// 
			// mtbpw
			// 
			this.mtbpw.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.mtbpw.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.mtbpw.Location = new System.Drawing.Point(12, 129);
			this.mtbpw.Name = "mtbpw";
			this.mtbpw.PasswordChar = '*';
			this.mtbpw.Size = new System.Drawing.Size(220, 23);
			this.mtbpw.TabIndex = 1;
			this.mtbpw.KeyDown += new System.Windows.Forms.KeyEventHandler(this.mtbpw_KeyDown);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 110);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(255, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "Bitte das Passwort zum Beenden eingeben:";
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(12, 167);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(130, 23);
			this.button1.TabIndex = 3;
			this.button1.Text = "nur Minimieren";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(12, 9);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(290, 86);
			this.label2.TabIndex = 4;
			this.label2.Text = "Möchten Sie den WebAutomationServer Server wirklich beenden?\r\n\r\nOhne diesen Serve" +
    "r werden auf der WEBvisuCS keine Daten mehr angezeigt!";
			// 
			// BeendenPW
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.AutoSize = true;
			this.ClientSize = new System.Drawing.Size(304, 191);
			this.ControlBox = false;
			this.Controls.Add(this.label2);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.mtbpw);
			this.Controls.Add(this.btnok);
			this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MaximumSize = new System.Drawing.Size(320, 230);
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(320, 230);
			this.Name = "BeendenPW";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "WebAutomation Beenden";
			this.TopMost = true;
			this.Load += new System.EventHandler(this.BeendenPW_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnok;
		private System.Windows.Forms.Label label1;
		public System.Windows.Forms.MaskedTextBox mtbpw;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Label label2;
	}
}