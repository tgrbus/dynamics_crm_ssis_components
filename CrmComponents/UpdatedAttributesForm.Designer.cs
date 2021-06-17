namespace CrmComponents
{
    partial class UpdatedAttributesForm
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
            this.dgv01 = new System.Windows.Forms.DataGridView();
            this.Changed = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.logicalNameDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Type = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.modifiedAttributeBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.lbl01 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dgv01)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.modifiedAttributeBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // dgv01
            // 
            this.dgv01.AllowUserToAddRows = false;
            this.dgv01.AllowUserToDeleteRows = false;
            this.dgv01.AutoGenerateColumns = false;
            this.dgv01.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv01.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Changed,
            this.logicalNameDataGridViewTextBoxColumn,
            this.Type});
            this.dgv01.DataSource = this.modifiedAttributeBindingSource;
            this.dgv01.Location = new System.Drawing.Point(12, 12);
            this.dgv01.Name = "dgv01";
            this.dgv01.ReadOnly = true;
            this.dgv01.Size = new System.Drawing.Size(347, 305);
            this.dgv01.TabIndex = 0;
            this.dgv01.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv01_CellContentClick);
            // 
            // Changed
            // 
            this.Changed.DataPropertyName = "Changed";
            this.Changed.HeaderText = "Changed";
            this.Changed.Name = "Changed";
            this.Changed.ReadOnly = true;
            // 
            // logicalNameDataGridViewTextBoxColumn
            // 
            this.logicalNameDataGridViewTextBoxColumn.DataPropertyName = "LogicalName";
            this.logicalNameDataGridViewTextBoxColumn.HeaderText = "LogicalName";
            this.logicalNameDataGridViewTextBoxColumn.Name = "logicalNameDataGridViewTextBoxColumn";
            this.logicalNameDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // Type
            // 
            this.Type.DataPropertyName = "Type";
            this.Type.HeaderText = "Type";
            this.Type.Name = "Type";
            this.Type.ReadOnly = true;
            // 
            // modifiedAttributeBindingSource
            // 
            this.modifiedAttributeBindingSource.DataSource = typeof(CrmComponents.Model.ModifiedAttribute);
            // 
            // lbl01
            // 
            this.lbl01.AutoSize = true;
            this.lbl01.Location = new System.Drawing.Point(60, 190);
            this.lbl01.Name = "lbl01";
            this.lbl01.Size = new System.Drawing.Size(35, 13);
            this.lbl01.TabIndex = 1;
            this.lbl01.Text = "label1";
            // 
            // UpdatedAttributesForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(366, 329);
            this.Controls.Add(this.lbl01);
            this.Controls.Add(this.dgv01);
            this.Name = "UpdatedAttributesForm";
            this.Text = "UpdatedAttributesForm";
            this.Load += new System.EventHandler(this.UpdatedAttributesForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgv01)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.modifiedAttributeBindingSource)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgv01;
        private System.Windows.Forms.DataGridViewTextBoxColumn Changed;
        private System.Windows.Forms.DataGridViewTextBoxColumn logicalNameDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn Type;
        private System.Windows.Forms.BindingSource modifiedAttributeBindingSource;
        private System.Windows.Forms.Label lbl01;
    }
}