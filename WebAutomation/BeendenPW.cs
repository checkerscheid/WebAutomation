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
//# Revision     : $Rev:: 213                                                     $ #
//# Author       : $Author::                                                      $ #
//# File-ID      : $Id:: BeendenPW.cs 213 2025-05-15 14:50:57Z                    $ #
//#                                                                                 #
//###################################################################################
using System;
using System.Windows.Forms;

namespace WebAutomation {

	/// <summary>
	/// Represents a form that prompts the user for a password before closing the application.
	/// </summary>
	/// <remarks>This form is typically used to confirm the user's intent to exit the application by requiring a
	/// password. The form sets its <see cref="Form.DialogResult"/> to <see cref="DialogResult.OK"/> if the user confirms
	/// the action, or closes without setting the result otherwise.</remarks>
	public partial class BeendenPW: Form {

		/// <summary>
		/// Initializes a new instance of the <see cref="BeendenPW"/> class.
		/// </summary>
		/// <remarks>This constructor sets up the <see cref="BeendenPW"/> component by initializing its user interface
		/// elements.</remarks>
		public BeendenPW() {
			InitializeComponent();
		}

		/// <summary>
		/// Handles the Load event of the form, initializing the form's title and setting the input focus.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">An <see cref="EventArgs"/> instance containing the event data.</param>
		private void BeendenPW_Load(object sender, EventArgs e) {
			this.Text = Application.ProductName;
			mtbPw.Select();
		}

		/// <summary>
		/// Handles the Click event of the button1 control and closes the current form.
		/// </summary>
		/// <param name="sender">The source of the event, typically the button that was clicked.</param>
		/// <param name="e">An <see cref="EventArgs"/> instance containing the event data.</param>
		private void btnMinimieren_Click(object sender, EventArgs e) {
			this.Close();
		}

		/// <summary>
		/// Handles the click event of the OK button.
		/// </summary>
		/// <remarks>Sets the dialog result to <see cref="DialogResult.OK"/> and closes the dialog.</remarks>
		/// <param name="sender">The source of the event, typically the OK button.</param>
		/// <param name="e">An <see cref="EventArgs"/> instance containing the event data.</param>
		private void btnOk_Click(object sender, EventArgs e) {
			this.DialogResult = DialogResult.OK;
			this.Close();
		}
	}
}
