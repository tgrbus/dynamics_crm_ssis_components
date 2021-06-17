namespace CrmComponents
{
    partial class CrmConnectionEditor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnOK = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txtUN = new System.Windows.Forms.TextBox();
            this.txtPSW = new System.Windows.Forms.TextBox();
            this.cmbORG = new System.Windows.Forms.ComboBox();
            this.btnCancel = new System.Windows.Forms.Button();
            this.cmbCONN = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.cmbSERV = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txtDMN = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.cmbDisc = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.nudTimeout = new System.Windows.Forms.NumericUpDown();
            this.txtCrmSrv = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.txtAppId = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.txtAuthSrv = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.txtPass2 = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.txtUsr2 = new System.Windows.Forms.TextBox();
            this.txtCert = new System.Windows.Forms.TextBox();
            this.label16 = new System.Windows.Forms.Label();
            this.txtSecrt = new System.Windows.Forms.TextBox();
            this.label17 = new System.Windows.Forms.Label();
            this.btnTest = new System.Windows.Forms.Button();
            this.pnlRest = new System.Windows.Forms.Panel();
            this.pnlSoap = new System.Windows.Forms.Panel();
            this.pnlPass = new System.Windows.Forms.Panel();
            this.pnlClient = new System.Windows.Forms.Panel();
            this.pnlCert = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.nudTimeout)).BeginInit();
            this.pnlRest.SuspendLayout();
            this.pnlSoap.SuspendLayout();
            this.pnlPass.SuspendLayout();
            this.pnlClient.SuspendLayout();
            this.pnlCert.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(251, 357);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(11, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(79, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Discovery URL";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(11, 54);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(55, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Username";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(11, 86);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(53, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Password";
            // 
            // txtUN
            // 
            this.txtUN.Location = new System.Drawing.Point(145, 51);
            this.txtUN.Name = "txtUN";
            this.txtUN.Size = new System.Drawing.Size(279, 20);
            this.txtUN.TabIndex = 5;
            this.txtUN.TextChanged += new System.EventHandler(this.txtUN_TextChanged);
            // 
            // txtPSW
            // 
            this.txtPSW.Location = new System.Drawing.Point(145, 83);
            this.txtPSW.Name = "txtPSW";
            this.txtPSW.PasswordChar = '*';
            this.txtPSW.Size = new System.Drawing.Size(279, 20);
            this.txtPSW.TabIndex = 6;
            this.txtPSW.UseSystemPasswordChar = true;
            // 
            // cmbORG
            // 
            this.cmbORG.FormattingEnabled = true;
            this.cmbORG.Location = new System.Drawing.Point(145, 147);
            this.cmbORG.Name = "cmbORG";
            this.cmbORG.Size = new System.Drawing.Size(279, 21);
            this.cmbORG.TabIndex = 7;
            this.cmbORG.SelectedIndexChanged += new System.EventHandler(this.cmbORG_SelectedIndexChanged);
            this.cmbORG.Click += new System.EventHandler(this.cmbORG_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(349, 357);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 9;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // cmbCONN
            // 
            this.cmbCONN.FormattingEnabled = true;
            this.cmbCONN.Items.AddRange(new object[] {
            "Online",
            "OAuth",
            "OnPremise"});
            this.cmbCONN.Location = new System.Drawing.Point(145, 67);
            this.cmbCONN.Name = "cmbCONN";
            this.cmbCONN.Size = new System.Drawing.Size(279, 21);
            this.cmbCONN.TabIndex = 10;
            this.cmbCONN.SelectedIndexChanged += new System.EventHandler(this.cmbCONN_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(11, 70);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(88, 13);
            this.label4.TabIndex = 11;
            this.label4.Text = "Connection Type";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(11, 29);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(70, 13);
            this.label5.TabIndex = 12;
            this.label5.Text = "Service Type";
            // 
            // cmbSERV
            // 
            this.cmbSERV.FormattingEnabled = true;
            this.cmbSERV.Location = new System.Drawing.Point(145, 26);
            this.cmbSERV.Name = "cmbSERV";
            this.cmbSERV.Size = new System.Drawing.Size(279, 21);
            this.cmbSERV.TabIndex = 13;
            this.cmbSERV.SelectedIndexChanged += new System.EventHandler(this.cmbSERV_SelectedIndexChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(11, 118);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(43, 13);
            this.label6.TabIndex = 14;
            this.label6.Text = "Domain";
            // 
            // txtDMN
            // 
            this.txtDMN.Location = new System.Drawing.Point(145, 115);
            this.txtDMN.Name = "txtDMN";
            this.txtDMN.Size = new System.Drawing.Size(279, 20);
            this.txtDMN.TabIndex = 15;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(11, 150);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(66, 13);
            this.label7.TabIndex = 16;
            this.label7.Text = "Organization";
            // 
            // cmbDisc
            // 
            this.cmbDisc.FormattingEnabled = true;
            this.cmbDisc.Location = new System.Drawing.Point(145, 19);
            this.cmbDisc.Name = "cmbDisc";
            this.cmbDisc.Size = new System.Drawing.Size(279, 21);
            this.cmbDisc.TabIndex = 17;
            this.cmbDisc.SelectedIndexChanged += new System.EventHandler(this.cmbDisc_SelectedIndexChanged);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(11, 319);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(68, 13);
            this.label8.TabIndex = 18;
            this.label8.Text = "Timeout(sec)";
            this.label8.Click += new System.EventHandler(this.label8_Click);
            // 
            // nudTimeout
            // 
            this.nudTimeout.Location = new System.Drawing.Point(145, 317);
            this.nudTimeout.Maximum = new decimal(new int[] {
            1800,
            0,
            0,
            0});
            this.nudTimeout.Minimum = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.nudTimeout.Name = "nudTimeout";
            this.nudTimeout.Size = new System.Drawing.Size(63, 20);
            this.nudTimeout.TabIndex = 19;
            this.nudTimeout.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.nudTimeout.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            // 
            // txtCrmSrv
            // 
            this.txtCrmSrv.Location = new System.Drawing.Point(145, 88);
            this.txtCrmSrv.Name = "txtCrmSrv";
            this.txtCrmSrv.Size = new System.Drawing.Size(279, 20);
            this.txtCrmSrv.TabIndex = 5;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(11, 91);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(90, 13);
            this.label11.TabIndex = 4;
            this.label11.Text = "CRM Server URL";
            // 
            // txtAppId
            // 
            this.txtAppId.Location = new System.Drawing.Point(145, 54);
            this.txtAppId.Name = "txtAppId";
            this.txtAppId.Size = new System.Drawing.Size(279, 20);
            this.txtAppId.TabIndex = 3;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(11, 57);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(69, 13);
            this.label10.TabIndex = 2;
            this.label10.Text = "Client App ID";
            // 
            // txtAuthSrv
            // 
            this.txtAuthSrv.Location = new System.Drawing.Point(145, 19);
            this.txtAuthSrv.Name = "txtAuthSrv";
            this.txtAuthSrv.Size = new System.Drawing.Size(279, 20);
            this.txtAuthSrv.TabIndex = 1;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(11, 22);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(127, 13);
            this.label9.TabIndex = 0;
            this.label9.Text = "Authorization Server URL";
            // 
            // txtPass2
            // 
            this.txtPass2.Location = new System.Drawing.Point(145, 52);
            this.txtPass2.Name = "txtPass2";
            this.txtPass2.Size = new System.Drawing.Size(279, 20);
            this.txtPass2.TabIndex = 6;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(11, 56);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(53, 13);
            this.label14.TabIndex = 4;
            this.label14.Text = "Password";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(11, 22);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(55, 13);
            this.label13.TabIndex = 3;
            this.label13.Text = "Username";
            // 
            // txtUsr2
            // 
            this.txtUsr2.Location = new System.Drawing.Point(145, 15);
            this.txtUsr2.Name = "txtUsr2";
            this.txtUsr2.Size = new System.Drawing.Size(279, 20);
            this.txtUsr2.TabIndex = 2;
            // 
            // txtCert
            // 
            this.txtCert.Location = new System.Drawing.Point(145, 8);
            this.txtCert.Name = "txtCert";
            this.txtCert.Size = new System.Drawing.Size(279, 20);
            this.txtCert.TabIndex = 1;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(11, 11);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(110, 13);
            this.label16.TabIndex = 0;
            this.label16.Text = "Certificate Thumbprint";
            // 
            // txtSecrt
            // 
            this.txtSecrt.Location = new System.Drawing.Point(145, 8);
            this.txtSecrt.Name = "txtSecrt";
            this.txtSecrt.Size = new System.Drawing.Size(279, 20);
            this.txtSecrt.TabIndex = 1;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(11, 11);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(67, 13);
            this.label17.TabIndex = 0;
            this.label17.Text = "Client Secret";
            // 
            // btnTest
            // 
            this.btnTest.Location = new System.Drawing.Point(14, 357);
            this.btnTest.Name = "btnTest";
            this.btnTest.Size = new System.Drawing.Size(104, 23);
            this.btnTest.TabIndex = 25;
            this.btnTest.Text = "Test Connection";
            this.btnTest.UseVisualStyleBackColor = true;
            this.btnTest.Click += new System.EventHandler(this.btnTest_Click);
            // 
            // pnlRest
            // 
            this.pnlRest.Controls.Add(this.txtCrmSrv);
            this.pnlRest.Controls.Add(this.label9);
            this.pnlRest.Controls.Add(this.txtAppId);
            this.pnlRest.Controls.Add(this.label11);
            this.pnlRest.Controls.Add(this.txtAuthSrv);
            this.pnlRest.Controls.Add(this.label10);
            this.pnlRest.Location = new System.Drawing.Point(0, 94);
            this.pnlRest.Name = "pnlRest";
            this.pnlRest.Size = new System.Drawing.Size(434, 111);
            this.pnlRest.TabIndex = 26;
            // 
            // pnlSoap
            // 
            this.pnlSoap.Controls.Add(this.cmbORG);
            this.pnlSoap.Controls.Add(this.txtDMN);
            this.pnlSoap.Controls.Add(this.txtUN);
            this.pnlSoap.Controls.Add(this.txtPSW);
            this.pnlSoap.Controls.Add(this.cmbDisc);
            this.pnlSoap.Controls.Add(this.label1);
            this.pnlSoap.Controls.Add(this.label7);
            this.pnlSoap.Controls.Add(this.label3);
            this.pnlSoap.Controls.Add(this.label2);
            this.pnlSoap.Controls.Add(this.label6);
            this.pnlSoap.Location = new System.Drawing.Point(0, 94);
            this.pnlSoap.Name = "pnlSoap";
            this.pnlSoap.Size = new System.Drawing.Size(434, 205);
            this.pnlSoap.TabIndex = 27;
            // 
            // pnlPass
            // 
            this.pnlPass.Controls.Add(this.txtPass2);
            this.pnlPass.Controls.Add(this.label13);
            this.pnlPass.Controls.Add(this.txtUsr2);
            this.pnlPass.Controls.Add(this.label14);
            this.pnlPass.Location = new System.Drawing.Point(0, 204);
            this.pnlPass.Name = "pnlPass";
            this.pnlPass.Size = new System.Drawing.Size(434, 87);
            this.pnlPass.TabIndex = 28;
            // 
            // pnlClient
            // 
            this.pnlClient.Controls.Add(this.txtSecrt);
            this.pnlClient.Controls.Add(this.label17);
            this.pnlClient.Location = new System.Drawing.Point(0, 204);
            this.pnlClient.Name = "pnlClient";
            this.pnlClient.Size = new System.Drawing.Size(434, 37);
            this.pnlClient.TabIndex = 29;
            // 
            // pnlCert
            // 
            this.pnlCert.Controls.Add(this.txtCert);
            this.pnlCert.Controls.Add(this.label16);
            this.pnlCert.Location = new System.Drawing.Point(0, 204);
            this.pnlCert.Name = "pnlCert";
            this.pnlCert.Size = new System.Drawing.Size(434, 37);
            this.pnlCert.TabIndex = 30;
            // 
            // CrmConnectionEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(442, 400);
            this.Controls.Add(this.pnlCert);
            this.Controls.Add(this.pnlClient);
            this.Controls.Add(this.pnlPass);
            this.Controls.Add(this.pnlSoap);
            this.Controls.Add(this.pnlRest);
            this.Controls.Add(this.btnTest);
            this.Controls.Add(this.nudTimeout);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.cmbSERV);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.cmbCONN);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Name = "CrmConnectionEditor";
            this.Text = "CrmConnectionEditor";
            this.Load += new System.EventHandler(this.CrmConnectionEditor_Load);
            ((System.ComponentModel.ISupportInitialize)(this.nudTimeout)).EndInit();
            this.pnlRest.ResumeLayout(false);
            this.pnlRest.PerformLayout();
            this.pnlSoap.ResumeLayout(false);
            this.pnlSoap.PerformLayout();
            this.pnlPass.ResumeLayout(false);
            this.pnlPass.PerformLayout();
            this.pnlClient.ResumeLayout(false);
            this.pnlClient.PerformLayout();
            this.pnlCert.ResumeLayout(false);
            this.pnlCert.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtUN;
        private System.Windows.Forms.TextBox txtPSW;
        private System.Windows.Forms.ComboBox cmbORG;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.ComboBox cmbCONN;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox cmbSERV;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtDMN;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox cmbDisc;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.NumericUpDown nudTimeout;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox txtAppId;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox txtAuthSrv;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox txtCrmSrv;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox txtUsr2;
        private System.Windows.Forms.TextBox txtPass2;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.TextBox txtCert;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.TextBox txtSecrt;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Button btnTest;
        private System.Windows.Forms.Panel pnlRest;
        private System.Windows.Forms.Panel pnlSoap;
        private System.Windows.Forms.Panel pnlPass;
        private System.Windows.Forms.Panel pnlClient;
        private System.Windows.Forms.Panel pnlCert;
    }
}