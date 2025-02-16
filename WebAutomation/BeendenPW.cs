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
//# File-ID      : $Id:: BeendenPW.cs 65 2023-12-21 22:58:05Z                     $ #
//#                                                                                 #
//###################################################################################
using System;
using System.Windows.Forms;

namespace WebAutomation {
	public partial class BeendenPW: Form {
		public BeendenPW() {
			InitializeComponent();
		}

		private void btnok_Click(object sender, EventArgs e) {
			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		private void BeendenPW_Load(object sender, EventArgs e) {
			this.Text = Application.ProductName;
			mtbpw.Select();
		}

		private void mtbpw_KeyDown(object sender, KeyEventArgs e) {
			if (e.KeyData == Keys.Enter) {
				this.DialogResult = DialogResult.OK;
				this.Close();
			}
		}

		private void button1_Click(object sender, EventArgs e) {
			this.Close();
		}
	}
}
