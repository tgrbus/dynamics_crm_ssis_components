using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CrmComponents.Model;
using CrmComponents.Soap;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Microsoft.SqlServer.Dts.Runtime;
using Microsoft.SqlServer.Dts.Runtime.Design;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using Variable = Microsoft.SqlServer.Dts.Runtime.Variable;

namespace CrmComponents
{
    public partial class SourceComponentEditor : Form
    {
        class Connection {
            public string Name { get; set; }
            public string Id { get; set; }
        }

        class Col {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        private Connections connections;
        private Variables variables;
        private IDTSComponentMetaData100 metaData;
        private IDTSDesigntimeComponent100 designTimeComponent;
        private CManagedComponentWrapper designTimeInstance;
        private EntityModel model;
        private List<CrmAttribute> tempAttributes;
        private CrmCommands crmComm;
        private string sourceType;
        private string fetchXml;
        private FetchXmlHelpers fetchHelpers;

        private bool orderBySeleceted = false;
        private bool orderByName = false;
        private bool orderByType = false;

        private bool formLoaded = false;
        private DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(EntityModel));

        private Button btnOK;
        private Button btnCancel;
        private IDTSOutput100 output;
        private BindingSource crmAttributeBindingSource;
        private System.ComponentModel.IContainer components;
        private BindingSource entityBindingSource;
        private Button btnRFH;
        private List<Connection> conns;
        private List<SsisVariable> ssisVariables = new List<SsisVariable>();
        private TabPage tabPage2;
        private GroupBox groupBox1;
        private CheckBox chk02;
        private CheckBox chk01;
        private DataGridView dataGridView1;
        private DataGridViewCheckBoxColumn selectedDataGridViewCheckBoxColumn;
        private DataGridViewTextBoxColumn logicalNameDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn crmAttributeTypeDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn lengthDataGridViewTextBoxColumn;
        private TabPage tabPage1;
        private GroupBox grpBox;
        private RichTextBox txtFTCH;
        private ComboBox cmbVariables;
        private TextBox txtERR;
        private Label label4;
        private TextBox txtMR;
        private TextBox txtBS;
        private Label label3;
        private ComboBox cmbSRC;
        private Label label1;
        private Label lblConn;
        private ComboBox cmbConn;
        private Label lbl02;
        private ComboBox cmbEntities;
        private TabControl tabControl1;
        private string variablesComboText = "Select variable..";
        private IServiceProvider sp;
        private string newConnectionlabel = "New connection...";

        public SourceComponentEditor(Connections cons, Variables vars, IDTSComponentMetaData100 md, IServiceProvider serviceProvider) {
            InitializeComponent();
            
            variables = vars;
            connections = cons;
            metaData = md;
            sp = serviceProvider;

            ssisVariables.Add(new SsisVariable {Name = variablesComboText});
            List<SsisVariable> userVars = new List<SsisVariable>();
            List<SsisVariable> systemVars = new List<SsisVariable>();
            foreach(Variable variable in variables) {
                var x1 = (int)variable.DataType;
                var x2 = variable.QualifiedName;
                var x3 = variable.Value;
                SsisVariable v = new SsisVariable { DataType = x1, Name = x2, Value = x3 };
                if(v.Name.Contains("User::")) {
                    userVars.Add(v);
                } else {
                    systemVars.Add(v);
                }
            }

            userVars = userVars.OrderBy(n => n.Name).ToList();
            systemVars = systemVars.OrderBy(n => n.Name).ToList();

            ssisVariables.AddRange(userVars);
            ssisVariables.AddRange(systemVars);
            
            designTimeInstance = metaData.Instantiate();
            output = metaData.OutputCollection[0];

            foreach (IDTSCustomProperty100 property in metaData.CustomPropertyCollection) {
                if (property.Name == "State") {
                    model = property.Value as EntityModel;
                }
                if(property.Name == "Source") {
                    sourceType = property.Value.ToString();
                }
                if(property.Name == "FetchXML") {
                    fetchXml = property.Value.ToString();
                }
                if(property.Name == "Batch Size" && property.Value != null) {
                    int bs = (int)property.Value;
                    txtBS.Text = bs.ToString();
                }
                if(property.Name == "Max Rows Count" && property.Value != null) {
                    int mr = (int)property.Value;
                    txtMR.Text = mr.ToString();
                }
            }

            model = model ?? new EntityModel();
            conns = new List<Connection>();
            if (connections != null) {
                foreach (ConnectionManager connectionManager in connections) {
                    if(connectionManager.InnerObject is CrmConnection) {
                        conns.Add(new Connection { Name = connectionManager.Name, Id = connectionManager.ID });
                    }
                }
            }
            conns.Add(new Connection { Name = newConnectionlabel, Id = "-1" });

            cmbConn.DataSource = conns;
            cmbConn.DisplayMember = "Name";
            cmbConn.ValueMember = "Id";

            entityBindingSource.DataSource = model.EntityList;

            model.Attributes = tempAttributes = model.Attributes;
            crmAttributeBindingSource.DataSource = tempAttributes;
            
            IDTSRuntimeConnectionCollection100 x = metaData.RuntimeConnectionCollection;
            if(x != null && x[0] != null && x[0].ConnectionManagerID != null) {
                string connString = x[0]?.ConnectionManager?.ConnectionString ?? model.ConnectionString;
                if(!string.IsNullOrEmpty(connString)) {
                    crmComm = Model.Connection.CrmCommandsFactory(connString);
                }
            }
            fetchHelpers = new FetchXmlHelpers();

            txtERR.Visible = false;
        }

        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.entityBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.crmAttributeBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.btnRFH = new System.Windows.Forms.Button();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.chk02 = new System.Windows.Forms.CheckBox();
            this.chk01 = new System.Windows.Forms.CheckBox();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.selectedDataGridViewCheckBoxColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.logicalNameDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.crmAttributeTypeDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lengthDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.grpBox = new System.Windows.Forms.GroupBox();
            this.txtFTCH = new System.Windows.Forms.RichTextBox();
            this.cmbVariables = new System.Windows.Forms.ComboBox();
            this.txtERR = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtMR = new System.Windows.Forms.TextBox();
            this.txtBS = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.cmbSRC = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.lblConn = new System.Windows.Forms.Label();
            this.cmbConn = new System.Windows.Forms.ComboBox();
            this.lbl02 = new System.Windows.Forms.Label();
            this.cmbEntities = new System.Windows.Forms.ComboBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            ((System.ComponentModel.ISupportInitialize)(this.entityBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.crmAttributeBindingSource)).BeginInit();
            this.tabPage2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.tabPage1.SuspendLayout();
            this.grpBox.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(335, 665);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(437, 665);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // entityBindingSource
            // 
            this.entityBindingSource.DataSource = typeof(CrmComponents.Model.Entity);
            // 
            // crmAttributeBindingSource
            // 
            this.crmAttributeBindingSource.DataSource = typeof(CrmComponents.Model.CrmAttribute);
            this.crmAttributeBindingSource.CurrentChanged += new System.EventHandler(this.crmAttributeBindingSource_CurrentChanged);
            // 
            // btnRFH
            // 
            this.btnRFH.Location = new System.Drawing.Point(15, 665);
            this.btnRFH.Name = "btnRFH";
            this.btnRFH.Size = new System.Drawing.Size(143, 23);
            this.btnRFH.TabIndex = 11;
            this.btnRFH.Text = "Refresh CRM Metadata";
            this.btnRFH.UseVisualStyleBackColor = true;
            this.btnRFH.Click += new System.EventHandler(this.btnRFH_Click);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.groupBox1);
            this.tabPage2.Controls.Add(this.dataGridView1);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(534, 631);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Columns";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.chk02);
            this.groupBox1.Controls.Add(this.chk01);
            this.groupBox1.Location = new System.Drawing.Point(3, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(503, 49);
            this.groupBox1.TabIndex = 9;
            this.groupBox1.TabStop = false;
            this.groupBox1.Enter += new System.EventHandler(this.groupBox1_Enter);
            // 
            // chk02
            // 
            this.chk02.AutoSize = true;
            this.chk02.Location = new System.Drawing.Point(109, 18);
            this.chk02.Name = "chk02";
            this.chk02.Size = new System.Drawing.Size(103, 17);
            this.chk02.TabIndex = 1;
            this.chk02.Text = "Hide Unmapped";
            this.chk02.UseVisualStyleBackColor = true;
            this.chk02.CheckedChanged += new System.EventHandler(this.chkShow_CheckedChanged);
            // 
            // chk01
            // 
            this.chk01.AutoSize = true;
            this.chk01.Location = new System.Drawing.Point(12, 18);
            this.chk01.Name = "chk01";
            this.chk01.Size = new System.Drawing.Size(90, 17);
            this.chk01.TabIndex = 0;
            this.chk01.Text = "Hide Mapped";
            this.chk01.UseVisualStyleBackColor = true;
            this.chk01.CheckedChanged += new System.EventHandler(this.chkShow_CheckedChanged);
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.AutoGenerateColumns = false;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.selectedDataGridViewCheckBoxColumn,
            this.logicalNameDataGridViewTextBoxColumn,
            this.crmAttributeTypeDataGridViewTextBoxColumn,
            this.lengthDataGridViewTextBoxColumn});
            this.dataGridView1.DataSource = this.crmAttributeBindingSource;
            this.dataGridView1.Location = new System.Drawing.Point(3, 58);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(525, 567);
            this.dataGridView1.TabIndex = 5;
            this.dataGridView1.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellClick);
            this.dataGridView1.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellContentClick);
            this.dataGridView1.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView1_ColumnHeaderMouseClick);
            // 
            // selectedDataGridViewCheckBoxColumn
            // 
            this.selectedDataGridViewCheckBoxColumn.DataPropertyName = "Selected";
            this.selectedDataGridViewCheckBoxColumn.HeaderText = "Selected";
            this.selectedDataGridViewCheckBoxColumn.Name = "selectedDataGridViewCheckBoxColumn";
            this.selectedDataGridViewCheckBoxColumn.Width = 80;
            // 
            // logicalNameDataGridViewTextBoxColumn
            // 
            this.logicalNameDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.logicalNameDataGridViewTextBoxColumn.DataPropertyName = "LogicalName";
            this.logicalNameDataGridViewTextBoxColumn.HeaderText = "LogicalName";
            this.logicalNameDataGridViewTextBoxColumn.Name = "logicalNameDataGridViewTextBoxColumn";
            this.logicalNameDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // crmAttributeTypeDataGridViewTextBoxColumn
            // 
            this.crmAttributeTypeDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.crmAttributeTypeDataGridViewTextBoxColumn.DataPropertyName = "CrmAttributeType";
            this.crmAttributeTypeDataGridViewTextBoxColumn.HeaderText = "CrmAttributeType";
            this.crmAttributeTypeDataGridViewTextBoxColumn.Name = "crmAttributeTypeDataGridViewTextBoxColumn";
            this.crmAttributeTypeDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // lengthDataGridViewTextBoxColumn
            // 
            this.lengthDataGridViewTextBoxColumn.DataPropertyName = "Length";
            this.lengthDataGridViewTextBoxColumn.HeaderText = "Length";
            this.lengthDataGridViewTextBoxColumn.Name = "lengthDataGridViewTextBoxColumn";
            this.lengthDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // tabPage1
            // 
            this.tabPage1.BackColor = System.Drawing.SystemColors.Control;
            this.tabPage1.Controls.Add(this.grpBox);
            this.tabPage1.Controls.Add(this.label4);
            this.tabPage1.Controls.Add(this.txtMR);
            this.tabPage1.Controls.Add(this.txtBS);
            this.tabPage1.Controls.Add(this.label3);
            this.tabPage1.Controls.Add(this.cmbSRC);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.lblConn);
            this.tabPage1.Controls.Add(this.cmbConn);
            this.tabPage1.Controls.Add(this.lbl02);
            this.tabPage1.Controls.Add(this.cmbEntities);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(534, 631);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "General";
            this.tabPage1.Click += new System.EventHandler(this.tabPage1_Click);
            // 
            // grpBox
            // 
            this.grpBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpBox.Controls.Add(this.txtFTCH);
            this.grpBox.Controls.Add(this.cmbVariables);
            this.grpBox.Controls.Add(this.txtERR);
            this.grpBox.Location = new System.Drawing.Point(9, 124);
            this.grpBox.Name = "grpBox";
            this.grpBox.Size = new System.Drawing.Size(511, 480);
            this.grpBox.TabIndex = 19;
            this.grpBox.TabStop = false;
            this.grpBox.Text = "FetchXML";
            this.grpBox.Enter += new System.EventHandler(this.grpBox_Enter);
            // 
            // txtFTCH
            // 
            this.txtFTCH.Location = new System.Drawing.Point(6, 46);
            this.txtFTCH.Name = "txtFTCH";
            this.txtFTCH.Size = new System.Drawing.Size(497, 380);
            this.txtFTCH.TabIndex = 13;
            this.txtFTCH.Text = "";
            this.txtFTCH.TextChanged += new System.EventHandler(this.txtFTCH_TextChanged);
            this.txtFTCH.Leave += new System.EventHandler(this.txtFTCH_Leave);
            // 
            // cmbVariables
            // 
            this.cmbVariables.FormattingEnabled = true;
            this.cmbVariables.Location = new System.Drawing.Point(6, 19);
            this.cmbVariables.Name = "cmbVariables";
            this.cmbVariables.Size = new System.Drawing.Size(121, 21);
            this.cmbVariables.TabIndex = 18;
            this.cmbVariables.Text = "Insert Variable";
            this.cmbVariables.SelectedIndexChanged += new System.EventHandler(this.cmbVariables_SelectedIndexChanged);
            // 
            // txtERR
            // 
            this.txtERR.BackColor = System.Drawing.SystemColors.Info;
            this.txtERR.Location = new System.Drawing.Point(6, 432);
            this.txtERR.Multiline = true;
            this.txtERR.Name = "txtERR";
            this.txtERR.Size = new System.Drawing.Size(497, 42);
            this.txtERR.TabIndex = 12;
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(161, 95);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(130, 13);
            this.label4.TabIndex = 17;
            this.label4.Text = "Max No of Rows (0 for All)";
            // 
            // txtMR
            // 
            this.txtMR.Location = new System.Drawing.Point(297, 92);
            this.txtMR.Name = "txtMR";
            this.txtMR.Size = new System.Drawing.Size(72, 20);
            this.txtMR.TabIndex = 16;
            this.txtMR.Text = "0";
            this.txtMR.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtMR.TextChanged += new System.EventHandler(this.txtBS_TextChanged);
            this.txtMR.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtBS_KeyPress);
            // 
            // txtBS
            // 
            this.txtBS.Location = new System.Drawing.Point(85, 92);
            this.txtBS.Name = "txtBS";
            this.txtBS.Size = new System.Drawing.Size(46, 20);
            this.txtBS.TabIndex = 15;
            this.txtBS.Text = "2000";
            this.txtBS.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtBS.TextChanged += new System.EventHandler(this.txtBS_TextChanged);
            this.txtBS.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtBS_KeyPress);
            this.txtBS.KeyUp += new System.Windows.Forms.KeyEventHandler(this.txtBS_KeyUp);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 95);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(58, 13);
            this.label3.TabIndex = 14;
            this.label3.Text = "Batch Size";
            // 
            // cmbSRC
            // 
            this.cmbSRC.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbSRC.FormattingEnabled = true;
            this.cmbSRC.Items.AddRange(new object[] {
            "Entity",
            "FetchXML"});
            this.cmbSRC.Location = new System.Drawing.Point(85, 59);
            this.cmbSRC.Name = "cmbSRC";
            this.cmbSRC.Size = new System.Drawing.Size(284, 21);
            this.cmbSRC.TabIndex = 9;
            this.cmbSRC.SelectedIndexChanged += new System.EventHandler(this.cmbSRC_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 62);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(41, 13);
            this.label1.TabIndex = 8;
            this.label1.Text = "Source";
            // 
            // lblConn
            // 
            this.lblConn.AutoSize = true;
            this.lblConn.Location = new System.Drawing.Point(6, 28);
            this.lblConn.Name = "lblConn";
            this.lblConn.Size = new System.Drawing.Size(61, 13);
            this.lblConn.TabIndex = 6;
            this.lblConn.Text = "Connection";
            // 
            // cmbConn
            // 
            this.cmbConn.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbConn.FormattingEnabled = true;
            this.cmbConn.Location = new System.Drawing.Point(85, 25);
            this.cmbConn.Name = "cmbConn";
            this.cmbConn.Size = new System.Drawing.Size(285, 21);
            this.cmbConn.TabIndex = 2;
            this.cmbConn.SelectedIndexChanged += new System.EventHandler(this.cmbConn_SelectedIndexChanged);
            // 
            // lbl02
            // 
            this.lbl02.AutoSize = true;
            this.lbl02.Location = new System.Drawing.Point(6, 127);
            this.lbl02.Name = "lbl02";
            this.lbl02.Size = new System.Drawing.Size(33, 13);
            this.lbl02.TabIndex = 7;
            this.lbl02.Text = "Entity";
            // 
            // cmbEntities
            // 
            this.cmbEntities.DataSource = this.entityBindingSource;
            this.cmbEntities.DisplayMember = "LogicalName";
            this.cmbEntities.FormattingEnabled = true;
            this.cmbEntities.Location = new System.Drawing.Point(85, 124);
            this.cmbEntities.Name = "cmbEntities";
            this.cmbEntities.Size = new System.Drawing.Size(284, 21);
            this.cmbEntities.TabIndex = 4;
            this.cmbEntities.ValueMember = "LogicalName";
            this.cmbEntities.SelectedIndexChanged += new System.EventHandler(this.cmbEntities_SelectedIndexChanged);
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(2, 2);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(542, 657);
            this.tabControl1.TabIndex = 10;
            // 
            // SourceComponentEditor
            // 
            this.ClientSize = new System.Drawing.Size(547, 700);
            this.Controls.Add(this.btnRFH);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Name = "SourceComponentEditor";
            this.Load += new System.EventHandler(this.SourceComponentEditor_Load);
            ((System.ComponentModel.ISupportInitialize)(this.entityBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.crmAttributeBindingSource)).EndInit();
            this.tabPage2.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.grpBox.ResumeLayout(false);
            this.grpBox.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private void SourceComponentEditor_Load(object sender, EventArgs e) {
            if (!String.IsNullOrEmpty(model.ConnectionId)) {
                cmbConn.SelectedValue = model.ConnectionId;
            }
            else {
                cmbConn.SelectedIndex = -1;
            }

            if (model.SelectedEntity != null) {
                cmbEntities.SelectedValue = model.SelectedEntity.LogicalName;
            }
            else {
                cmbEntities.SelectedIndex = -1;
            }

            if(!string.IsNullOrEmpty(sourceType)) {
                cmbSRC.SelectedItem = sourceType;
            } else {
                cmbSRC.SelectedItem = sourceType = "Entity";
            }

            if(sourceType == "Entity") {
                lbl02.Visible = true;
                cmbEntities.Visible = true;
                grpBox.Visible = false;
            } else {
                lbl02.Visible = false;
                cmbEntities.Visible = false;
                grpBox.Visible = true;
            }

            if(!string.IsNullOrEmpty(fetchXml)) {
                txtFTCH.Text = fetchXml;
            }

            if(ssisVariables.Count > 0) {
                foreach(SsisVariable variable in ssisVariables) {
                    var x = variable;
                }
                cmbVariables.DataSource = ssisVariables.Where(n => n != null).ToList();
                cmbVariables.DisplayMember = "Name";
                cmbVariables.ValueMember = "Name";
                cmbVariables.SelectedIndex = 0;
            }

            formLoaded = true;
        }
        private void btnOK_Click(object sender, EventArgs e) {
            designTimeInstance.SetComponentProperty("State", model);
            string outp = string.Empty;
            using (var ms = new MemoryStream()) {
                serializer.WriteObject(ms, model);
                outp = Encoding.UTF8.GetString(ms.ToArray());
                designTimeInstance.SetComponentProperty("Serialized State", outp);
            }

            designTimeInstance.SetComponentProperty("Source", sourceType);
            designTimeInstance.SetComponentProperty("FetchXML", fetchXml);

            int batchSize = 2000;
            int maxRows = 0;
            int.TryParse(txtBS.Text, out batchSize);
            int.TryParse(txtMR.Text, out maxRows);

            designTimeInstance.SetComponentProperty("Batch Size", batchSize);
            designTimeInstance.SetComponentProperty("Max Rows Count", maxRows);

            SetOutputs();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e) {
            this.Close();
        }

        private void cmbConn_SelectedIndexChanged(object sender, EventArgs e) {
            if (formLoaded && cmbConn.SelectedItem is Connection selected) {
                if (selected.Id == "-1") {
                    IDtsConnectionService dtsConnectionService = sp.GetService(typeof(IDtsConnectionService)) as IDtsConnectionService;

                    var connArrayList = dtsConnectionService?.CreateConnection("CrmConnection");
                    if (connArrayList == null || connArrayList.Count == 0) {
                        return;
                    }
                    var connManager = (ConnectionManager)connArrayList[0];
                    conns.Insert(0, new Connection { Name = connManager.Name, Id = connManager.ID });
                    cmbConn.DataSource = null;
                    cmbConn.DataSource = conns;
                    cmbConn.DisplayMember = "Name";
                    cmbConn.ValueMember = "Id";
                    cmbConn.SelectedIndex = 0;
                    return;
                }
                ConnectionManager selectedManager = null;
                foreach (ConnectionManager connectionManager in connections) {
                    if (selected.Id == connectionManager.ID) {
                        selectedManager = connectionManager;
                        break;
                    }
                }

                metaData.RuntimeConnectionCollection[0].ConnectionManager = DtsConvert.GetExtendedInterface(selectedManager);
                metaData.RuntimeConnectionCollection[0].ConnectionManagerID = selectedManager.ID;
                string connString = selectedManager.ConnectionString;
                if (!string.IsNullOrEmpty(connString)) {
                    model.ConnectionString = connString;
                    crmComm = Model.Connection.CrmCommandsFactory(connString);
                    
                    model.ConnectionId = selectedManager.ID;
                    model.EntityList = crmComm.RetrieveEntityList().OrderBy(n => n.LogicalName).ToList();
                    entityBindingSource.DataSource = model.EntityList.Where(n => n.ValidForAdvancedFind || n.IsIntersect).ToList();
                    model.SelectedEntity = null;
                    model.Attributes = tempAttributes = new List<CrmAttribute>();
                    cmbEntities.SelectedIndex = -1;
                    crmAttributeBindingSource.DataSource = tempAttributes;
                }
                else
                {
                    //todo
                }
            }
        }

        private void cmbEntities_SelectedIndexChanged(object sender, EventArgs e) {
            if (formLoaded && cmbEntities.SelectedItem is Entity selectedEntity) {
                model.SelectedEntity = selectedEntity;
                model.Attributes = tempAttributes = crmComm.RetrieveAttributesList(model, selectedEntity).Where(n => n.DisplayForRead).OrderBy(n => n.LogicalName).ToList();
                crmAttributeBindingSource.DataSource = tempAttributes;
            }
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e) {
            if (e.ColumnIndex == 0 && e.RowIndex >= 0) {
                DataGridViewCell clickedCell = dataGridView1.CurrentCell;
                bool check = !(bool)clickedCell.EditedFormattedValue;

                tempAttributes[e.RowIndex].Selected = check;
            }
        }

        private void SetOutputs() {
            List<Col> existingCols = new List<Col>();
            foreach(IDTSOutputColumn100 column in output.OutputColumnCollection) {
                existingCols.Add(new Col {
                    Id = column.ID,
                    Name = column.Name
                });
            }

            var forRemove = existingCols.Where(n => !model.Attributes.Where(t => t.Selected).Select(x => x.LogicalName).Contains(n.Name)).ToList();
            forRemove.ForEach(n => output.OutputColumnCollection.RemoveObjectByID(n.Id));
            
            foreach (CrmAttribute attribute in model.Attributes.Where(n => n.Selected)) {
                IDTSOutputColumn100 column;
                Col existing = existingCols.FirstOrDefault(n => n.Name == attribute.LogicalName);
                if (existing == null) {
                    column = output.OutputColumnCollection.New();
                    column.Name = attribute.LogicalName;
                } else {
                    column = output.OutputColumnCollection.FindObjectByID(existing.Id);
                }

                column.SetDataTypeProperties(attribute.SsisDataType, attribute.Length, attribute.Precision, attribute.Scale, 0);
            }
        }

        private void chkShow_CheckedChanged(object sender, EventArgs e) {
            if(sender is CheckBox chk) {
                if(chk01.Checked && chk02.Checked) {
                    if(chk.Name == "chk01") {
                        chk02.Checked = false;
                    } else {
                        chk01.Checked = false;
                    }
                }

                if(!chk01.Checked && !chk02.Checked) {//show all
                    crmAttributeBindingSource.DataSource = tempAttributes = model.Attributes.OrderBy(n => n.LogicalName).ToList();
                } else if(chk01.Checked && !chk02.Checked) {//hide mapped
                    crmAttributeBindingSource.DataSource = tempAttributes = model.Attributes.Where(n => !n.Selected).OrderBy(n => n.LogicalName).ToList();
                }
                else if(!chk01.Checked && chk02.Checked) {//hide unmapped
                    crmAttributeBindingSource.DataSource = tempAttributes = model.Attributes.Where(n => n.Selected).OrderBy(n => n.LogicalName).ToList();
                }
            }
        }

        private void dataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e) {
            if(tempAttributes.Count > 1) {
                if(e.ColumnIndex == 0) {
                    if(orderBySeleceted) {
                        crmAttributeBindingSource.DataSource = tempAttributes = (from row in tempAttributes
                                                                                 orderby row.Selected descending, row.LogicalName
                                                                                 select row).ToList();
                    } else {
                        crmAttributeBindingSource.DataSource = tempAttributes = (from row in tempAttributes
                                                                                 orderby row.Selected, row.LogicalName
                                                                                 select row).ToList();
                    }

                    orderBySeleceted = !orderBySeleceted;
                } else if(e.ColumnIndex == 1) {
                    crmAttributeBindingSource.DataSource = tempAttributes = orderByName ? 
                            tempAttributes.OrderByDescending(n => n.LogicalName).ToList() : tempAttributes.OrderBy(n => n.LogicalName).ToList();
                    orderByName = !orderByName;
                }
                else if(e.ColumnIndex == 2) {
                    if(orderByType) {
                        crmAttributeBindingSource.DataSource = tempAttributes = (from r in tempAttributes
                                                                                 orderby r.CrmAttributeType.ToString() descending, r.LogicalName
                                                                                 select r).ToList();
                    } else {
                        crmAttributeBindingSource.DataSource = tempAttributes = (from r in tempAttributes
                                                                                 orderby r.CrmAttributeType.ToString(), r.LogicalName
                                                                                 select r).ToList();
                    }

                    orderByType = !orderByType;
                }
            }
        }

        private void cmbSRC_SelectedIndexChanged(object sender, EventArgs e) {
            sourceType = cmbSRC.SelectedItem as string;
            if (formLoaded && !string.IsNullOrEmpty(sourceType)) {
                crmAttributeBindingSource.DataSource = model.Attributes = tempAttributes = new List<CrmAttribute>();
                if (sourceType == "Entity") {
                    lbl02.Visible = true;
                    cmbEntities.Visible = true;
                    grpBox.Visible = false;
                }
                else {
                    lbl02.Visible = false;
                    cmbEntities.Visible = false;
                    grpBox.Visible = true;

                    model.SelectedEntity = null;
                    cmbEntities.SelectedIndex = -1;
                }
            }
        }

        private void txtFTCH_Leave(object sender, EventArgs e) {
            if (!string.IsNullOrEmpty(txtFTCH.Text.Trim())) {
                var cursorPosition = grpBox.PointToClient(Cursor.Position);
                if(cursorPosition.X <= grpBox.Size.Width && cursorPosition.X >= 0 && cursorPosition.Y <= grpBox.Size.Width && cursorPosition.Y >= 0) {
                    return;
                }
                fetchXml = txtFTCH.Text.Trim();
                string cleanXml = fetchHelpers.SetVariables(fetchXml, ssisVariables);
                string error = fetchHelpers.SetColumns(cleanXml, model, crmComm);
                model.Attributes.ForEach(n => n.Selected = true);
                if (error != null) {
                    model.Error = error;
                    txtERR.Visible = true;
                    txtERR.Text = error;
                } else {
                    txtERR.Visible = false;
                    tempAttributes = model.Attributes;
                    crmAttributeBindingSource.DataSource = tempAttributes;
                }
            }
        }

        private void btnRFH_Click(object sender, EventArgs e) {
            if (sourceType.ToLower() == "fetchxml") {
                txtFTCH_Leave(null, null);
            }
            else {
                if (cmbConn.SelectedItem is Connection) {
                    model.EntityList = crmComm.RetrieveEntityList().OrderBy(n => n.LogicalName).ToList();
                    Entity selectedEntity = cmbEntities.SelectedItem as Entity;
                    entityBindingSource.DataSource = model.EntityList.Where(n => n.ValidForAdvancedFind || n.IsIntersect).ToList();
                    if (selectedEntity != null){
                        if (model.EntityList.All(n => n.LogicalName != selectedEntity.LogicalName)){
                            model.SelectedEntity = null;
                            model.Attributes = tempAttributes = new List<CrmAttribute>();
                            cmbEntities.SelectedIndex = -1;
                            crmAttributeBindingSource.DataSource = tempAttributes;
                        } else {
                            cmbEntities.SelectedValue = selectedEntity.LogicalName;
                            List<CrmAttribute> attributes = crmComm.RetrieveAttributesList(model, selectedEntity).Where(n => n.DisplayForRead).OrderBy(n => n.LogicalName).ToList();
                            List<CrmAttribute> newAttributes = new List<CrmAttribute>();
                            List<CrmAttribute> modifiedAttributes = new List<CrmAttribute>();
                            List<CrmAttribute> deletedAttributes = new List<CrmAttribute>();

                            foreach (CrmAttribute attribute in attributes) {
                                CrmAttribute old = model.Attributes.FirstOrDefault(n => n.LogicalName == attribute.LogicalName);
                                if(old == null) {
                                    newAttributes.Add(attribute);
                                    model.Attributes.Add(attribute);
                                    if(tempAttributes.FirstOrDefault(n => n.LogicalName == attribute.LogicalName) == null) {
                                        tempAttributes.Add(attribute);
                                    }
                                } else {
                                    if(old.CrmAttributeType != attribute.CrmAttributeType || old.Length != attribute.Length ||
                                       old.Precision != attribute.Precision || old.Scale != attribute.Scale) {
                                        attribute.Selected = old.Selected;
                                        modifiedAttributes.Add(attribute);
                                        model.Attributes.Remove(old);
                                        model.Attributes.Add(attribute);
                                        if(tempAttributes.Remove(old)) {
                                            tempAttributes.Add(attribute);
                                        }
                                    }
                                }
                            }

                            deletedAttributes = model.Attributes.Where(n => attributes.All(k => k.LogicalName != n.LogicalName)).ToList();
                            foreach(CrmAttribute attribute in deletedAttributes) {
                                model.Attributes.Remove(attribute);
                                tempAttributes.Remove(attribute);
                            }

                            List<ModifiedAttribute> modified = newAttributes.Select(n => new ModifiedAttribute {
                                Changed = "new",
                                LogicalName = n.LogicalName,
                                Type = n.CrmAttributeType.ToString()
                            }).ToList();
                            modified.AddRange(modifiedAttributes.Select(k => new ModifiedAttribute {
                                Changed = "modified",
                                LogicalName = k.LogicalName,
                                Type = k.CrmAttributeType.ToString()
                            }).ToList());
                            modified.AddRange(deletedAttributes.Select(m => new ModifiedAttribute {
                                Changed = "deleted",
                                LogicalName = m.LogicalName,
                                Type = m.CrmAttributeType.ToString()
                            }));
                            if(modified.Count > 0) {
                                model.Attributes = model.Attributes.OrderBy(n => n.LogicalName).ToList();
                                tempAttributes = tempAttributes.OrderBy(n => n.LogicalName).ToList();
                                crmAttributeBindingSource.DataSource = tempAttributes;
                                //crmAttributeBindingSource.ResetBindings(true);
                            }
                            UpdatedAttributesForm form = new UpdatedAttributesForm(modified);
                            form.ShowDialog();
                        }
                    }
                }
            }
        }

        private void txtFTCH_TextChanged(object sender, EventArgs e) {
            
        }

        private void tabPage1_Click(object sender, EventArgs e) {

        }

        private void crmAttributeBindingSource_CurrentChanged(object sender, EventArgs e) {

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e) {

        }

        private void txtBS_KeyPress(object sender, KeyPressEventArgs e) {
            if(!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) {
                e.Handled = true;
            }
        }

        private void txtBS_TextChanged(object sender, EventArgs e) {

        }

        private void txtBS_KeyUp(object sender, KeyEventArgs e) {
            TextBox box = sender as TextBox;
            try {
                int value = -1;
                Int32.TryParse(box?.Text.Trim(), out value);

                if (box != null) {
                    box.Text = box.Text.TrimStart('0');
                    if (box.Name == txtBS.Name && value > 5000) {
                        box.Text = 5000.ToString();
                    }

                    if (box.Name == txtMR.Name && string.IsNullOrEmpty(box.Text)) {
                        box.Text = "0";
                    }

                    if (box.Name == txtBS.Name && string.IsNullOrEmpty(box.Text)) {
                        box.Text = 5000.ToString();
                    }
                }
            }
            catch {
                txtBS.Text = 5000.ToString();
                txtMR.Text = "0";
            }
        }

        private void groupBox1_Enter(object sender, EventArgs e) {

        }

        private void cmbVariables_SelectedIndexChanged(object sender, EventArgs e) {
            if(formLoaded && cmbVariables.SelectedItem is SsisVariable item && item.Name != variablesComboText) {
                var startIndex = txtFTCH.SelectionStart;
                txtFTCH.Text = txtFTCH.Text.Insert(startIndex, $"@[{item.Name}]");
                txtFTCH.SelectionStart = startIndex + item.Name.Length + 3;
                cmbVariables.SelectedIndex = 0;
            } 
        }

        private void grpBox_Enter(object sender, EventArgs e) {

        }
    }
}
