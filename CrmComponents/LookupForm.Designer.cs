using CrmComponents.Model;

namespace CrmComponents
{
    partial class LookupForm
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
            this.components = new System.ComponentModel.Container();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCNC = new System.Windows.Forms.Button();
            this.cmbATTR = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.rb01 = new System.Windows.Forms.RadioButton();
            this.grp01 = new System.Windows.Forms.GroupBox();
            this.rb02 = new System.Windows.Forms.RadioButton();
            this.grp02 = new System.Windows.Forms.GroupBox();
            this.txt01 = new System.Windows.Forms.TextBox();
            this.rb04 = new System.Windows.Forms.RadioButton();
            this.rb03 = new System.Windows.Forms.RadioButton();
            this.cmbENT = new System.Windows.Forms.ComboBox();
            this.entitiesBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.cmbMethod = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.cmbAlternateKey = new System.Windows.Forms.ComboBox();
            this.alternateKeyBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.label4 = new System.Windows.Forms.Label();
            this.dgv = new System.Windows.Forms.DataGridView();
            this.LName = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.CrmType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SsisColumn = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.crmAttributeBindingSource1 = new System.Windows.Forms.BindingSource(this.components);
            this.btnAdd = new System.Windows.Forms.Button();
            this.grp01.SuspendLayout();
            this.grp02.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.entitiesBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.alternateKeyBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.crmAttributeBindingSource1)).BeginInit();
            this.SuspendLayout();
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(67, 536);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCNC
            // 
            this.btnCNC.Location = new System.Drawing.Point(180, 536);
            this.btnCNC.Name = "btnCNC";
            this.btnCNC.Size = new System.Drawing.Size(75, 23);
            this.btnCNC.TabIndex = 1;
            this.btnCNC.Text = "Cancel";
            this.btnCNC.UseVisualStyleBackColor = true;
            this.btnCNC.Click += new System.EventHandler(this.btnCNC_Click);
            // 
            // cmbATTR
            // 
            this.cmbATTR.FormattingEnabled = true;
            this.cmbATTR.Location = new System.Drawing.Point(102, 133);
            this.cmbATTR.Name = "cmbATTR";
            this.cmbATTR.Size = new System.Drawing.Size(151, 21);
            this.cmbATTR.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(18, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(69, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Target entity:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(18, 136);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(82, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "Target attribute:";
            // 
            // rb01
            // 
            this.rb01.AutoSize = true;
            this.rb01.Location = new System.Drawing.Point(6, 29);
            this.rb01.Name = "rb01";
            this.rb01.Size = new System.Drawing.Size(92, 17);
            this.rb01.TabIndex = 8;
            this.rb01.TabStop = true;
            this.rb01.Text = "Raise an Error";
            this.rb01.UseVisualStyleBackColor = true;
            this.rb01.CheckedChanged += new System.EventHandler(this.rb_CheckedChanged);
            // 
            // grp01
            // 
            this.grp01.Controls.Add(this.rb02);
            this.grp01.Controls.Add(this.rb01);
            this.grp01.Location = new System.Drawing.Point(18, 371);
            this.grp01.Name = "grp01";
            this.grp01.Size = new System.Drawing.Size(116, 100);
            this.grp01.TabIndex = 9;
            this.grp01.TabStop = false;
            this.grp01.Text = "Multiple Matches";
            // 
            // rb02
            // 
            this.rb02.AutoSize = true;
            this.rb02.Location = new System.Drawing.Point(6, 65);
            this.rb02.Name = "rb02";
            this.rb02.Size = new System.Drawing.Size(77, 17);
            this.rb02.TabIndex = 9;
            this.rb02.TabStop = true;
            this.rb02.Text = "Match First";
            this.rb02.UseVisualStyleBackColor = true;
            this.rb02.CheckedChanged += new System.EventHandler(this.rb_CheckedChanged);
            // 
            // grp02
            // 
            this.grp02.Controls.Add(this.txt01);
            this.grp02.Controls.Add(this.rb04);
            this.grp02.Controls.Add(this.rb03);
            this.grp02.Location = new System.Drawing.Point(159, 371);
            this.grp02.Name = "grp02";
            this.grp02.Size = new System.Drawing.Size(200, 130);
            this.grp02.TabIndex = 10;
            this.grp02.TabStop = false;
            this.grp02.Text = "When not Found";
            // 
            // txt01
            // 
            this.txt01.Location = new System.Drawing.Point(24, 89);
            this.txt01.Name = "txt01";
            this.txt01.Size = new System.Drawing.Size(170, 20);
            this.txt01.TabIndex = 2;
            // 
            // rb04
            // 
            this.rb04.AutoSize = true;
            this.rb04.Location = new System.Drawing.Point(8, 65);
            this.rb04.Name = "rb04";
            this.rb04.Size = new System.Drawing.Size(89, 17);
            this.rb04.TabIndex = 1;
            this.rb04.TabStop = true;
            this.rb04.Text = "Default Value";
            this.rb04.UseVisualStyleBackColor = true;
            this.rb04.CheckedChanged += new System.EventHandler(this.rb_CheckedChanged);
            // 
            // rb03
            // 
            this.rb03.AutoSize = true;
            this.rb03.Location = new System.Drawing.Point(8, 29);
            this.rb03.Name = "rb03";
            this.rb03.Size = new System.Drawing.Size(92, 17);
            this.rb03.TabIndex = 0;
            this.rb03.TabStop = true;
            this.rb03.Text = "Raise an Error";
            this.rb03.UseVisualStyleBackColor = true;
            this.rb03.CheckedChanged += new System.EventHandler(this.rb_CheckedChanged);
            // 
            // cmbENT
            // 
            this.cmbENT.FormattingEnabled = true;
            this.cmbENT.Location = new System.Drawing.Point(102, 10);
            this.cmbENT.Name = "cmbENT";
            this.cmbENT.Size = new System.Drawing.Size(151, 21);
            this.cmbENT.TabIndex = 11;
            this.cmbENT.SelectedIndexChanged += new System.EventHandler(this.cmbENT_SelectedIndexChanged);
            // 
            // entitiesBindingSource
            // 
            this.entitiesBindingSource.DataSource = typeof(string);
            // 
            // cmbMethod
            // 
            this.cmbMethod.FormattingEnabled = true;
            this.cmbMethod.Items.AddRange(new object[] {
            "Primary Key",
            "Alternate Key",
            "Manually Specify"});
            this.cmbMethod.Location = new System.Drawing.Point(102, 49);
            this.cmbMethod.Name = "cmbMethod";
            this.cmbMethod.Size = new System.Drawing.Size(151, 21);
            this.cmbMethod.TabIndex = 12;
            this.cmbMethod.SelectedIndexChanged += new System.EventHandler(this.cmbMethod_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(18, 52);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(43, 13);
            this.label3.TabIndex = 13;
            this.label3.Text = "Method";
            // 
            // cmbAlternateKey
            // 
            this.cmbAlternateKey.DataSource = this.alternateKeyBindingSource;
            this.cmbAlternateKey.DisplayMember = "LogicalName";
            this.cmbAlternateKey.FormattingEnabled = true;
            this.cmbAlternateKey.Location = new System.Drawing.Point(102, 90);
            this.cmbAlternateKey.Name = "cmbAlternateKey";
            this.cmbAlternateKey.Size = new System.Drawing.Size(151, 21);
            this.cmbAlternateKey.TabIndex = 14;
            this.cmbAlternateKey.ValueMember = "LogicalName";
            this.cmbAlternateKey.SelectedIndexChanged += new System.EventHandler(this.cmbAlternateKey_SelectedIndexChanged);
            // 
            // alternateKeyBindingSource
            // 
            this.alternateKeyBindingSource.DataSource = typeof(CrmComponents.Model.AlternateKey);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(18, 93);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(70, 13);
            this.label4.TabIndex = 15;
            this.label4.Text = "Alternate Key";
            // 
            // dgv
            // 
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.LName,
            this.CrmType,
            this.SsisColumn});
            this.dgv.Location = new System.Drawing.Point(18, 179);
            this.dgv.Name = "dgv";
            this.dgv.Size = new System.Drawing.Size(443, 150);
            this.dgv.TabIndex = 16;
            this.dgv.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_CellValueChanged);
            // 
            // LName
            // 
            this.LName.HeaderText = "LName";
            this.LName.Name = "LName";
            // 
            // CrmType
            // 
            this.CrmType.HeaderText = "Column1";
            this.CrmType.Name = "CrmType";
            // 
            // SsisColumn
            // 
            this.SsisColumn.HeaderText = "Input";
            this.SsisColumn.Name = "SsisColumn";
            // 
            // crmAttributeBindingSource1
            // 
            this.crmAttributeBindingSource1.DataSource = typeof(CrmComponents.Model.CrmAttribute);
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(285, 130);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(75, 23);
            this.btnAdd.TabIndex = 17;
            this.btnAdd.Text = "AddRow";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // LookupForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 582);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.dgv);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.cmbAlternateKey);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.cmbMethod);
            this.Controls.Add(this.cmbENT);
            this.Controls.Add(this.grp02);
            this.Controls.Add(this.grp01);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmbATTR);
            this.Controls.Add(this.btnCNC);
            this.Controls.Add(this.btnOK);
            this.Name = "LookupForm";
            this.Text = "LookupForm";
            this.Load += new System.EventHandler(this.LookupForm_Load);
            this.grp01.ResumeLayout(false);
            this.grp01.PerformLayout();
            this.grp02.ResumeLayout(false);
            this.grp02.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.entitiesBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.alternateKeyBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.crmAttributeBindingSource1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCNC;
        private System.Windows.Forms.ComboBox cmbATTR;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.RadioButton rb01;
        private System.Windows.Forms.GroupBox grp01;
        private System.Windows.Forms.RadioButton rb02;
        private System.Windows.Forms.GroupBox grp02;
        private System.Windows.Forms.TextBox txt01;
        private System.Windows.Forms.RadioButton rb04;
        private System.Windows.Forms.RadioButton rb03;
        private System.Windows.Forms.ComboBox cmbENT;
        private System.Windows.Forms.BindingSource entitiesBindingSource;
        private System.Windows.Forms.ComboBox cmbMethod;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cmbAlternateKey;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.BindingSource alternateKeyBindingSource;
        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.BindingSource crmAttributeBindingSource1;
        private System.Windows.Forms.DataGridViewComboBoxColumn LName;
        private System.Windows.Forms.DataGridViewTextBoxColumn CrmType;
        private System.Windows.Forms.DataGridViewComboBoxColumn SsisColumn;
        private System.Windows.Forms.Button btnAdd;
    }
}