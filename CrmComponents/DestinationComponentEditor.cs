using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CrmComponents.Helpers.Enums;
using CrmComponents.Model;
using CrmComponents.Soap;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Microsoft.SqlServer.Dts.Runtime;
using Microsoft.SqlServer.Dts.Runtime.Design;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using Variable = Microsoft.SqlServer.Dts.Runtime.Variable;

namespace CrmComponents
{
    public class DestinationComponentEditor : Form {
        class Connection
        {
            public string Name { get; set; }
            public string Id { get; set; }
        }

        class Col {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        //fields
        private Connections connections;
        private Variables variables;
        private IDTSComponentMetaData100 metadata;
        private CManagedComponentWrapper designTimeInstance;
        private IDTSInput100 input;
        private IDTSOutput100 output;

        private CrmCommands crmComm;
        private EntityModel model;
        private List<Connection> conns;
        private string operation;
        private string previousOperation;
        
        private string matchingCriteria;
        public string alternateKey;
        public string multipleMatches;
        public string matchNotFound;
        public string errorHandling;
        private List<CrmAttribute> currentAttributes = new List<CrmAttribute>();
        /*model.Attributes (read and create and update), currentAttributes (read or create or update)
            hide/show mapped/unmapped/all => filtering on dgv binding source level: crmAttributeBindingSource.DataSource = currentAttributes.Where(n => ....)
        */
        private int batchSize;
        private int noOfThreads;
        private bool formLoaded = false;
        private DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(EntityModel));
        private Label label1;
        private Label label2;
        private ComboBox cmbCON;
        private ComboBox cmbENT;
        private BindingSource crmAttributeBindingSource;
        private System.ComponentModel.IContainer components;
        private BindingSource entityBindingSource;
        private BindingSource ssisInputBindingSource;
        private Label label3;
        private ComboBox cmbOPR;
        private Button btnOK;
        private Button btnCNL;
        private Label label4;
        private ComboBox cmbMTC;
        private Label label5;
        private ComboBox cmbKEY;
        private BindingSource alternateKeyBindingSource;
        private Label label6;
        private ComboBox cmbMM;
        private Label label7;
        private ComboBox cmbMNF;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private Button btnREFR;
        private Label label8;
        private Label label9;
        private NumericUpDown nudThreads;
        private NumericUpDown nudBatchSize;
        private GroupBox groupBox2;
        private CheckBox chk01;
        private CheckBox chk02;
        private Button btnAut;
        private Button btnClr;
        private Label label10;
        private ComboBox cmbERR;
        private DataGridView dgv2;
        private IServiceProvider sp;
        private DataGridViewCheckBoxColumn Mch;
        private DataGridViewComboBoxColumn Input2;
        private DataGridViewTextBoxColumn logicalNameDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn crmAttributeTypeDataGridViewTextBoxColumn;
        private DataGridViewLinkColumn Matching;
        private Panel panel1;
        private string newConnectionlabel = "New connection...";

        public DestinationComponentEditor(Connections cons, Variables vars, IDTSComponentMetaData100 md, IServiceProvider serviceProvider) {
            InitializeComponent();
            cmbOPR.Items.AddRange(new object[] {
                OperationTypeEnum.Create.ToString(), OperationTypeEnum.Update.ToString(), OperationTypeEnum.Upsert.ToString(),
                OperationTypeEnum.Delete.ToString(), OperationTypeEnum.Merge.ToString()
            });
            variables = vars;
            connections = cons;
            metadata = md;
            sp = serviceProvider;
            designTimeInstance = metadata.Instantiate();

            input = metadata.InputCollection[0];
            output = metadata.OutputCollection[0];

            foreach (IDTSCustomProperty100 property in metadata.CustomPropertyCollection) {
                switch(property.Name) {
                    case "State":
                        model = property.Value as EntityModel;
                        break;
                    case "Operation":
                        operation = property.Value as string;
                        previousOperation = operation;
                        break;
                    case "Matching Criteria":
                        matchingCriteria = property.Value as string;
                        break;
                    case "Alternate Key":
                        alternateKey = property.Value as string;
                        break;
                    case "Multiple Matches":
                        multipleMatches = property.Value as string;
                        break;
                    case "Match Not Found":
                        matchNotFound = property.Value as string;
                        break;
                    case "Batch Size":
                        batchSize = (int?)property.Value ?? 50;
                        break;
                    case "No of Threads":
                        noOfThreads = (int?)property.Value ?? 1;
                        break;
                    case "Error Handling":
                        errorHandling = (property.Value as string) ?? Dictionaries.GetInstance().ErrorHandlingNames[(int)ErrorHandlingEnum.Fail];
                        break;
                    default:
                        break;
                }
            }

            model = model ?? new EntityModel();
            conns = new List<Connection>();
            if(connections != null) {
                foreach(ConnectionManager connectionManager in connections) {
                    if (connectionManager.InnerObject is CrmConnection){
                        conns.Add(new Connection{Name = connectionManager.Name, Id = connectionManager.ID});
                    }
                }
            }
            conns.Add(new Connection { Name = newConnectionlabel, Id = "-1" });

            cmbCON.DataSource = conns;
            cmbCON.DisplayMember = "Name";
            cmbCON.ValueMember = "Id";

            model.EntityList = model.EntityList ?? new List<Entity>();
            entityBindingSource.DataSource = operation == OperationTypeEnum.Merge.ToString() ? model.ReturnMergeEnities() : model.EntityList;

            model.SsisInputs = model.SsisInputs ?? new List<SsisInput>(); 
            List<SsisInput> inputs = new List<SsisInput>();

            if (input?.GetVirtualInput() != null && input.GetVirtualInput().VirtualInputColumnCollection != null) {
                foreach(IDTSVirtualInputColumn100 column in input.GetVirtualInput().VirtualInputColumnCollection) {
                    SsisInput i = new SsisInput();
                    i.Name = column.Name;
                    i.SsisType = column.DataType;
                    if(model.SsisInputs != null) {
                        SsisInput founded = model.SsisInputs.FirstOrDefault(n => n.Name == column.Name);
                        i.CrmColumnName = founded?.CrmColumnName;
                    }

                    inputs.Add(i);
                }

                model.SsisInputs = inputs;
            }

            IDTSRuntimeConnectionCollection100 x = metadata.RuntimeConnectionCollection;
            if(x?[0]?.ConnectionManagerID != null) {
                string connString = x[0]?.ConnectionManager?.ConnectionString ?? model.ConnectionString;
                if(!string.IsNullOrEmpty(connString)) {
                    crmComm = Model.Connection.CrmCommandsFactory(connString);
                }
            }

            model.Attributes = currentAttributes = model.Attributes ?? new List<CrmAttribute>();
            
            crmAttributeBindingSource.DataSource = currentAttributes;

            if(model.SelectedEntity?.AlternateKeys != null) {
                alternateKeyBindingSource.DataSource = model.SelectedEntity.AlternateKeys;
            }

            model.MatchingColumns = model.MatchingColumns ?? new List<string>();
        }

        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.cmbCON = new System.Windows.Forms.ComboBox();
            this.cmbENT = new System.Windows.Forms.ComboBox();
            this.entityBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.dgv2 = new System.Windows.Forms.DataGridView();
            this.Mch = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.Input2 = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.logicalNameDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.crmAttributeTypeDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Matching = new System.Windows.Forms.DataGridViewLinkColumn();
            this.crmAttributeBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.ssisInputBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.label3 = new System.Windows.Forms.Label();
            this.cmbOPR = new System.Windows.Forms.ComboBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCNL = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.cmbMTC = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.cmbKEY = new System.Windows.Forms.ComboBox();
            this.alternateKeyBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.label6 = new System.Windows.Forms.Label();
            this.cmbMM = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.cmbMNF = new System.Windows.Forms.ComboBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.cmbERR = new System.Windows.Forms.ComboBox();
            this.label10 = new System.Windows.Forms.Label();
            this.nudBatchSize = new System.Windows.Forms.NumericUpDown();
            this.label9 = new System.Windows.Forms.Label();
            this.nudThreads = new System.Windows.Forms.NumericUpDown();
            this.label8 = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.chk02 = new System.Windows.Forms.CheckBox();
            this.chk01 = new System.Windows.Forms.CheckBox();
            this.btnREFR = new System.Windows.Forms.Button();
            this.btnAut = new System.Windows.Forms.Button();
            this.btnClr = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.entityBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgv2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.crmAttributeBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ssisInputBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.alternateKeyBindingSource)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudBatchSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudThreads)).BeginInit();
            this.tabPage2.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(32, 29);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(61, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Connection";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(32, 101);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(33, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Entity";
            // 
            // cmbCON
            // 
            this.cmbCON.FormattingEnabled = true;
            this.cmbCON.Location = new System.Drawing.Point(166, 26);
            this.cmbCON.Name = "cmbCON";
            this.cmbCON.Size = new System.Drawing.Size(335, 21);
            this.cmbCON.TabIndex = 2;
            this.cmbCON.SelectedIndexChanged += new System.EventHandler(this.cmbCON_SelectedIndexChanged);
            // 
            // cmbENT
            // 
            this.cmbENT.DataSource = this.entityBindingSource;
            this.cmbENT.DisplayMember = "LogicalName";
            this.cmbENT.FormattingEnabled = true;
            this.cmbENT.Location = new System.Drawing.Point(166, 98);
            this.cmbENT.Name = "cmbENT";
            this.cmbENT.Size = new System.Drawing.Size(211, 21);
            this.cmbENT.TabIndex = 3;
            this.cmbENT.ValueMember = "LogicalName";
            this.cmbENT.SelectedIndexChanged += new System.EventHandler(this.cmbENT_SelectedIndexChanged);
            // 
            // entityBindingSource
            // 
            this.entityBindingSource.DataSource = typeof(CrmComponents.Model.Entity);
            this.entityBindingSource.CurrentChanged += new System.EventHandler(this.entityBindingSource_CurrentChanged);
            // 
            // dgv2
            // 
            this.dgv2.AllowUserToAddRows = false;
            this.dgv2.AllowUserToDeleteRows = false;
            this.dgv2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgv2.AutoGenerateColumns = false;
            this.dgv2.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv2.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Mch,
            this.Input2,
            this.logicalNameDataGridViewTextBoxColumn,
            this.crmAttributeTypeDataGridViewTextBoxColumn,
            this.Matching});
            this.dgv2.DataSource = this.crmAttributeBindingSource;
            this.dgv2.Location = new System.Drawing.Point(17, 73);
            this.dgv2.Name = "dgv2";
            this.dgv2.RowHeadersVisible = false;
            this.dgv2.Size = new System.Drawing.Size(543, 533);
            this.dgv2.TabIndex = 4;
            this.dgv2.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv2_CellClick);
            this.dgv2.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv2_CellContentClick);
            this.dgv2.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv2_CellValueChanged);
            this.dgv2.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dgv2_ColumnHeaderMouseClick);
            this.dgv2.CurrentCellDirtyStateChanged += new System.EventHandler(this.dgv2_CurrentCellDirtyStateChanged);
            // 
            // Mch
            // 
            this.Mch.HeaderText = "ID Matching";
            this.Mch.Name = "Mch";
            this.Mch.Width = 85;
            // 
            // Input2
            // 
            this.Input2.HeaderText = "Input";
            this.Input2.Name = "Input2";
            this.Input2.Width = 115;
            // 
            // logicalNameDataGridViewTextBoxColumn
            // 
            this.logicalNameDataGridViewTextBoxColumn.DataPropertyName = "LogicalName";
            this.logicalNameDataGridViewTextBoxColumn.HeaderText = "LogicalName";
            this.logicalNameDataGridViewTextBoxColumn.Name = "logicalNameDataGridViewTextBoxColumn";
            this.logicalNameDataGridViewTextBoxColumn.Width = 140;
            // 
            // crmAttributeTypeDataGridViewTextBoxColumn
            // 
            this.crmAttributeTypeDataGridViewTextBoxColumn.DataPropertyName = "CrmAttributeType";
            this.crmAttributeTypeDataGridViewTextBoxColumn.HeaderText = "CrmAttributeType";
            this.crmAttributeTypeDataGridViewTextBoxColumn.Name = "crmAttributeTypeDataGridViewTextBoxColumn";
            // 
            // Matching
            // 
            this.Matching.HeaderText = "Complex Lookup";
            this.Matching.Name = "Matching";
            // 
            // crmAttributeBindingSource
            // 
            this.crmAttributeBindingSource.DataSource = typeof(CrmComponents.Model.CrmAttribute);
            // 
            // ssisInputBindingSource
            // 
            this.ssisInputBindingSource.DataSource = typeof(CrmComponents.Model.SsisInput);
            this.ssisInputBindingSource.CurrentChanged += new System.EventHandler(this.ssisInputBindingSource_CurrentChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(32, 67);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(53, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "Operation";
            // 
            // cmbOPR
            // 
            this.cmbOPR.FormattingEnabled = true;
            this.cmbOPR.Location = new System.Drawing.Point(166, 64);
            this.cmbOPR.Name = "cmbOPR";
            this.cmbOPR.Size = new System.Drawing.Size(121, 21);
            this.cmbOPR.TabIndex = 8;
            this.cmbOPR.SelectedIndexChanged += new System.EventHandler(this.cmbOPR_SelectedIndexChanged);
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(426, 4);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 9;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCNL
            // 
            this.btnCNL.Location = new System.Drawing.Point(507, 4);
            this.btnCNL.Name = "btnCNL";
            this.btnCNL.Size = new System.Drawing.Size(75, 23);
            this.btnCNL.TabIndex = 10;
            this.btnCNL.Text = "Cancel";
            this.btnCNL.UseVisualStyleBackColor = true;
            this.btnCNL.Click += new System.EventHandler(this.btnCNL_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(32, 133);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(86, 13);
            this.label4.TabIndex = 14;
            this.label4.Text = "Matching Criteria";
            // 
            // cmbMTC
            // 
            this.cmbMTC.FormattingEnabled = true;
            this.cmbMTC.Items.AddRange(new object[] {
            "Primary Key",
            "Alternate Key",
            "Manually Specify"});
            this.cmbMTC.Location = new System.Drawing.Point(166, 130);
            this.cmbMTC.Name = "cmbMTC";
            this.cmbMTC.Size = new System.Drawing.Size(121, 21);
            this.cmbMTC.TabIndex = 15;
            this.cmbMTC.SelectedIndexChanged += new System.EventHandler(this.cmbMTC_SelectedIndexChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(32, 209);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(70, 13);
            this.label5.TabIndex = 16;
            this.label5.Text = "Alternate Key";
            // 
            // cmbKEY
            // 
            this.cmbKEY.DataSource = this.alternateKeyBindingSource;
            this.cmbKEY.DisplayMember = "LogicalName";
            this.cmbKEY.FormattingEnabled = true;
            this.cmbKEY.Location = new System.Drawing.Point(166, 201);
            this.cmbKEY.Name = "cmbKEY";
            this.cmbKEY.Size = new System.Drawing.Size(211, 21);
            this.cmbKEY.TabIndex = 17;
            this.cmbKEY.ValueMember = "LogicalName";
            this.cmbKEY.SelectedIndexChanged += new System.EventHandler(this.cmbKEY_SelectedIndexChanged);
            // 
            // alternateKeyBindingSource
            // 
            this.alternateKeyBindingSource.DataSource = typeof(CrmComponents.Model.AlternateKey);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(32, 170);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(87, 13);
            this.label6.TabIndex = 19;
            this.label6.Text = "Multiple Matches";
            // 
            // cmbMM
            // 
            this.cmbMM.FormattingEnabled = true;
            this.cmbMM.Items.AddRange(new object[] {
            "Update All",
            "Match One",
            "Ignore",
            "Raise Error"});
            this.cmbMM.Location = new System.Drawing.Point(166, 167);
            this.cmbMM.Name = "cmbMM";
            this.cmbMM.Size = new System.Drawing.Size(121, 21);
            this.cmbMM.TabIndex = 20;
            this.cmbMM.SelectedIndexChanged += new System.EventHandler(this.cmbMM_SelectedIndexChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(313, 170);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(90, 13);
            this.label7.TabIndex = 21;
            this.label7.Text = "Match Not Found";
            // 
            // cmbMNF
            // 
            this.cmbMNF.FormattingEnabled = true;
            this.cmbMNF.Items.AddRange(new object[] {
            "Raise Error",
            "Ignore"});
            this.cmbMNF.Location = new System.Drawing.Point(410, 166);
            this.cmbMNF.Name = "cmbMNF";
            this.cmbMNF.Size = new System.Drawing.Size(121, 21);
            this.cmbMNF.TabIndex = 22;
            this.cmbMNF.SelectedIndexChanged += new System.EventHandler(this.cmbMNF_SelectedIndexChanged);
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(2, 1);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(592, 662);
            this.tabControl1.TabIndex = 23;
            // 
            // tabPage1
            // 
            this.tabPage1.BackColor = System.Drawing.SystemColors.Control;
            this.tabPage1.Controls.Add(this.cmbERR);
            this.tabPage1.Controls.Add(this.label10);
            this.tabPage1.Controls.Add(this.nudBatchSize);
            this.tabPage1.Controls.Add(this.label9);
            this.tabPage1.Controls.Add(this.nudThreads);
            this.tabPage1.Controls.Add(this.label8);
            this.tabPage1.Controls.Add(this.cmbCON);
            this.tabPage1.Controls.Add(this.cmbMNF);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.label7);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.cmbMM);
            this.tabPage1.Controls.Add(this.cmbENT);
            this.tabPage1.Controls.Add(this.label6);
            this.tabPage1.Controls.Add(this.label3);
            this.tabPage1.Controls.Add(this.cmbOPR);
            this.tabPage1.Controls.Add(this.cmbKEY);
            this.tabPage1.Controls.Add(this.label4);
            this.tabPage1.Controls.Add(this.label5);
            this.tabPage1.Controls.Add(this.cmbMTC);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(584, 636);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "General";
            this.tabPage1.Click += new System.EventHandler(this.tabPage1_Click);
            // 
            // cmbERR
            // 
            this.cmbERR.FormattingEnabled = true;
            this.cmbERR.Items.AddRange(new object[] {
            "Fail on Error",
            "Ignore Error",
            "Redirect Row"});
            this.cmbERR.Location = new System.Drawing.Point(166, 329);
            this.cmbERR.Name = "cmbERR";
            this.cmbERR.Size = new System.Drawing.Size(121, 21);
            this.cmbERR.TabIndex = 29;
            this.cmbERR.SelectedIndexChanged += new System.EventHandler(this.cmbERR_SelectedIndexChanged);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(35, 332);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(74, 13);
            this.label10.TabIndex = 28;
            this.label10.Text = "Error Handling";
            // 
            // nudBatchSize
            // 
            this.nudBatchSize.Location = new System.Drawing.Point(166, 261);
            this.nudBatchSize.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nudBatchSize.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudBatchSize.Name = "nudBatchSize";
            this.nudBatchSize.Size = new System.Drawing.Size(63, 20);
            this.nudBatchSize.TabIndex = 27;
            this.nudBatchSize.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.nudBatchSize.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(261, 264);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(94, 13);
            this.label9.TabIndex = 26;
            this.label9.Text = "Number of threads";
            // 
            // nudThreads
            // 
            this.nudThreads.Location = new System.Drawing.Point(374, 261);
            this.nudThreads.Maximum = new decimal(new int[] {
            15,
            0,
            0,
            0});
            this.nudThreads.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudThreads.Name = "nudThreads";
            this.nudThreads.Size = new System.Drawing.Size(47, 20);
            this.nudThreads.TabIndex = 25;
            this.nudThreads.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.nudThreads.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(32, 263);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(113, 13);
            this.label8.TabIndex = 23;
            this.label8.Text = "Batch Size (max 1000)";
            this.label8.Click += new System.EventHandler(this.label8_Click);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.groupBox2);
            this.tabPage2.Controls.Add(this.dgv2);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(584, 636);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Columns";
            this.tabPage2.UseVisualStyleBackColor = true;
            this.tabPage2.Click += new System.EventHandler(this.tabPage2_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.chk02);
            this.groupBox2.Controls.Add(this.chk01);
            this.groupBox2.Location = new System.Drawing.Point(17, 17);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(543, 37);
            this.groupBox2.TabIndex = 19;
            this.groupBox2.TabStop = false;
            // 
            // chk02
            // 
            this.chk02.AutoSize = true;
            this.chk02.Location = new System.Drawing.Point(117, 14);
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
            this.chk01.Location = new System.Drawing.Point(7, 14);
            this.chk01.Name = "chk01";
            this.chk01.Size = new System.Drawing.Size(90, 17);
            this.chk01.TabIndex = 0;
            this.chk01.Text = "Hide Mapped";
            this.chk01.UseVisualStyleBackColor = true;
            this.chk01.CheckedChanged += new System.EventHandler(this.chkShow_CheckedChanged);
            // 
            // btnREFR
            // 
            this.btnREFR.Location = new System.Drawing.Point(3, 4);
            this.btnREFR.Name = "btnREFR";
            this.btnREFR.Size = new System.Drawing.Size(137, 23);
            this.btnREFR.TabIndex = 24;
            this.btnREFR.Text = "Refresh CRM Metadata";
            this.btnREFR.UseVisualStyleBackColor = true;
            this.btnREFR.Click += new System.EventHandler(this.btnREFR_Click);
            // 
            // btnAut
            // 
            this.btnAut.Location = new System.Drawing.Point(146, 4);
            this.btnAut.Name = "btnAut";
            this.btnAut.Size = new System.Drawing.Size(133, 23);
            this.btnAut.TabIndex = 25;
            this.btnAut.Text = "Automaticly Map Fields";
            this.btnAut.UseVisualStyleBackColor = true;
            this.btnAut.Click += new System.EventHandler(this.btnAut_Click);
            // 
            // btnClr
            // 
            this.btnClr.Location = new System.Drawing.Point(285, 4);
            this.btnClr.Name = "btnClr";
            this.btnClr.Size = new System.Drawing.Size(100, 23);
            this.btnClr.TabIndex = 26;
            this.btnClr.Text = "Clear Mappings";
            this.btnClr.UseVisualStyleBackColor = true;
            this.btnClr.Click += new System.EventHandler(this.btnClr_Click);
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.panel1.Controls.Add(this.btnREFR);
            this.panel1.Controls.Add(this.btnClr);
            this.panel1.Controls.Add(this.btnCNL);
            this.panel1.Controls.Add(this.btnAut);
            this.panel1.Controls.Add(this.btnOK);
            this.panel1.Location = new System.Drawing.Point(2, 669);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(588, 30);
            this.panel1.TabIndex = 27;
            // 
            // DestinationComponentEditor
            // 
            this.ClientSize = new System.Drawing.Size(606, 705);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.tabControl1);
            this.Name = "DestinationComponentEditor";
            this.Load += new System.EventHandler(this.DestinationComponent_Load);
            ((System.ComponentModel.ISupportInitialize)(this.entityBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgv2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.crmAttributeBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ssisInputBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.alternateKeyBindingSource)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudBatchSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudThreads)).EndInit();
            this.tabPage2.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private void DestinationComponent_Load(object sender, EventArgs e) {
            tabControl1.SelectedTab = tabPage2;
            if (!String.IsNullOrEmpty(model.ConnectionId)){
                cmbCON.SelectedValue = model.ConnectionId;
            } else {
                cmbCON.SelectedIndex = -1;
            }

            if (model.SelectedEntity != null){
                cmbENT.SelectedValue = model.SelectedEntity.LogicalName;
            } else {
                cmbENT.SelectedIndex = -1;
            }

            if(!string.IsNullOrEmpty(operation)) {
                cmbOPR.SelectedItem = operation;
            }

            if(!string.IsNullOrEmpty(matchingCriteria)) {
                cmbMTC.SelectedItem = matchingCriteria;
            }

            if(!string.IsNullOrEmpty(alternateKey)) {
                cmbKEY.SelectedItem = model.SelectedEntity?.AlternateKeys.First(n => n.LogicalName == alternateKey);
            }

            if(!string.IsNullOrEmpty(multipleMatches)) {
                cmbMM.SelectedItem = multipleMatches;
            }

            if(!string.IsNullOrEmpty(matchNotFound)) {
                cmbMNF.SelectedItem = matchNotFound;
            }

            if(!string.IsNullOrEmpty(errorHandling)) {
                cmbERR.SelectedItem = errorHandling;
            }

            nudBatchSize.Value = batchSize;
            nudThreads.Value = noOfThreads;

            if(model?.MatchingColumns?.Count > 0) {
                SetMchColumns();
            }

            formLoaded = true;
            SetDgvComboCellsDataSource();
            tabControl1.SelectedTab = tabPage1;
            cmbOPR_SelectedIndexChanged(null, null);
        }

        private void btnOK_Click(object sender, EventArgs e) {

            if(matchingCriteria == "Manually Specify" && model.MatchingColumns?.Count == 0) {
                MessageBox.Show("Please set matching column(s)");
                return;
            }

            designTimeInstance.SetComponentProperty("State", model);
            string outp = string.Empty;
            using (var ms = new MemoryStream()) {
                serializer.WriteObject(ms, model);
                outp = Encoding.UTF8.GetString(ms.ToArray());
                designTimeInstance.SetComponentProperty("Serialized State", outp);
            }

            if (!string.IsNullOrEmpty(operation)) {
                designTimeInstance.SetComponentProperty("Operation", operation);
            }

            if (!string.IsNullOrEmpty(matchingCriteria)) {
                designTimeInstance.SetComponentProperty("Matching Criteria", matchingCriteria);
            }

            if (formLoaded && cmbKEY.SelectedItem is AlternateKey selectedKey) {
                alternateKey = selectedKey.LogicalName;
            }

            if (!string.IsNullOrEmpty(alternateKey)) {
                designTimeInstance.SetComponentProperty("Alternate Key", alternateKey);
            }

            if (!string.IsNullOrEmpty(multipleMatches)) {
                designTimeInstance.SetComponentProperty("Multiple Matches", multipleMatches);
            }

            if (!string.IsNullOrEmpty(matchNotFound)) {
                designTimeInstance.SetComponentProperty("Match Not Found", matchNotFound);
            }

            if(!string.IsNullOrEmpty(errorHandling)) {
                designTimeInstance.SetComponentProperty("Error Handling", errorHandling);
            }

            designTimeInstance.SetComponentProperty("Batch Size", (int)nudBatchSize.Value);
            designTimeInstance.SetComponentProperty("No of Threads", (int)nudThreads.Value);

            SetInputs();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCNL_Click(object sender, EventArgs e) {
            this.Close();
        }

        private void SetInputs() {
            List<Col> existingCols = new List<Col>();
            foreach (IDTSInputColumn100 column in input.InputColumnCollection) {
                existingCols.Add(new Col {
                    Id = column.LineageID,
                    Name = column.Name
                });
            }

            if (input?.GetVirtualInput() != null && input.GetVirtualInput().VirtualInputColumnCollection != null) {
                foreach (IDTSInput100 inp in metadata.InputCollection) {
                    IDTSVirtualInput100 vInput = inp.GetVirtualInput();
                    //vInput.Name;
                    foreach (IDTSVirtualInputColumn100 column in vInput.VirtualInputColumnCollection) {
                        designTimeInstance.SetUsageType(input.ID, vInput, column.LineageID, DTSUsageType.UT_READONLY);
                    }
                }
            }
        }

        private void SetMchColumns() {
            foreach(DataGridViewRow row in dgv2.Rows) {
                DataGridViewCheckBoxCell checkCell = (DataGridViewCheckBoxCell)row.Cells[0];                

                DataGridViewTextBoxCell attributeNameCell = (DataGridViewTextBoxCell)row.Cells[2];
                string attributeName = attributeNameCell.Value.ToString();
                if (model.MatchingColumns.Contains(attributeName)) {
                    checkCell.Value = 1;
                }
            }
        }

        private void SetDgvComboCellsDataSource() {
            List<string> notSelectedSsisColumns = model.SsisInputs.Where(n => string.IsNullOrEmpty(n.CrmColumnName)).Select(n => n.Name).OrderBy(n => n).ToList();
            foreach (DataGridViewRow row in dgv2.Rows) {
                DataGridViewComboBoxCell cellSsis = (DataGridViewComboBoxCell)row.Cells[1];
                string crmColumnName = row.Cells[2].Value.ToString();

                List<string> ssisColList = new List<string>();
                SsisInput i = model.SsisInputs.FirstOrDefault(n => n.CrmColumnName == crmColumnName);
                if(i != null) {
                    ssisColList.Add(i.Name);
                }

                ssisColList.Add("<ignore>");
                if(notSelectedSsisColumns.Count > 0) {
                    ssisColList.AddRange(notSelectedSsisColumns);
                }

                cellSsis.DataSource = ssisColList; 
                if(ssisColList.Count > 0 && cellSsis.Value?.ToString() != ssisColList[0]) {
                    cellSsis.Value = ssisColList[0];
                }


                CrmAttribute attr = model.Attributes.First(n => n.LogicalName == crmColumnName);
                attr.SsisInput = ssisColList[0] == "<ignore>" ? null : ssisColList[0];
                DataGridViewLinkCell lookupCell = (DataGridViewLinkCell)row.Cells[4];
                if(attr.IsForMatching() && cellSsis.Value?.ToString() != "<ignore>") {
                    lookupCell.Value = attr.ComplexMapping == ComplexMappingEnum.PrimaryKey ? "complex: no" : "complex: yes";
                } else {
                    lookupCell.Value = null;
                }

                DataGridViewCheckBoxCell cell3 = (DataGridViewCheckBoxCell)row.Cells[0];
                if(model.MatchingColumns.Contains(attr.LogicalName)) {
                    cell3.Value = 1;
                } else {
                    cell3.Value = 0;
                }
                    
            }
        }
        private void btnAut_Click(object sender, EventArgs e) {
            for(int i = 0; i < dgv2.Rows.Count; i++) {
                string crmName = dgv2.Rows[i].Cells[2].Value.ToString();
                SsisInput foundedInput = model.SsisInputs.FirstOrDefault(n => n.Name.ToLower() == crmName.ToLower());
                if(foundedInput != null) {
                    var cell = (DataGridViewComboBoxCell)dgv2.Rows[i].Cells[1];
                    cell.Value = foundedInput.Name;
                }
            }
        }

        private void btnClr_Click(object sender, EventArgs e) {
            for (int i = 0; i < dgv2.Rows.Count; i++) {
                var cell = (DataGridViewComboBoxCell)dgv2.Rows[i].Cells[1];
                cell.Value = "<ignore>";
            }
        }

        private void dgv2_CellValueChanged(object sender, DataGridViewCellEventArgs e) {
            if (formLoaded && e.ColumnIndex == 1 && e.RowIndex >= 0) {// ssis input => ColumnIndex == 1
                bool changed = false;
                DataGridViewComboBoxCell cbSsis = (DataGridViewComboBoxCell)dgv2.Rows[e.RowIndex].Cells[1];
                string selectedSsisColumn = cbSsis.Value.ToString();
                string crmName = dgv2.Rows[e.RowIndex].Cells[2].Value.ToString();
                CrmAttribute a = model.Attributes.FirstOrDefault(n => n.LogicalName == crmName);

                if (cbSsis.Value != null) {
                    if (selectedSsisColumn != "<ignore>") {
                        SsisInput i = model.SsisInputs.First(n => n.Name == selectedSsisColumn);
                        foreach(SsisInput ssisInput in model.SsisInputs.Where(n => n.CrmColumnName == crmName)) {
                            ssisInput.CrmColumnName = null;
                        }
                        if (i.CrmColumnName != crmName) {
                            changed = true;
                            i.CrmColumnName = crmName;
                            if(a != null) {
                                a.SsisInput = selectedSsisColumn;
                            }
                        }
                    }
                    else {//<ignore>
                        SsisInput i = model.SsisInputs.FirstOrDefault(n => n.CrmColumnName == crmName);
                        if (i != null) {
                            i.CrmColumnName = null;
                            changed = true;
                        }
                        if (a != null) {
                            a.ComplexMapping = ComplexMappingEnum.PrimaryKey;
                            if (!string.IsNullOrEmpty(a.SsisInput)) {
                                changed = true;
                                a.SsisInput = null;
                            }
                        }
                    }
                    if (changed) {
                        SetDgvComboCellsDataSource();
                    }
                }
            }
        }

        private void cmbCON_SelectedIndexChanged(object sender, EventArgs e) {
            Connection selected = cmbCON.SelectedItem as Connection;
            if(formLoaded && selected != null) {
                if(selected.Id == "-1") {
                    IDtsConnectionService dtsConnectionService = sp.GetService(typeof(IDtsConnectionService)) as IDtsConnectionService;

                    var connArrayList = dtsConnectionService?.CreateConnection("CrmConnection");
                    if(connArrayList == null || connArrayList.Count == 0) {
                        return;
                    }
                    var connManager = (ConnectionManager)connArrayList[0];
                    conns.Insert(0, new Connection { Name = connManager.Name, Id = connManager.ID });
                    cmbCON.DataSource = null;
                    cmbCON.DataSource = conns;
                    cmbCON.DisplayMember = "Name";
                    cmbCON.ValueMember = "Id";
                    cmbCON.SelectedIndex = 0;
                    return;
                }

                ConnectionManager selectedManager = null;
                foreach(ConnectionManager connectionManager in connections) {
                    if(selected.Id == connectionManager.ID) {
                        selectedManager = connectionManager;
                        break;
                    }
                }

                metadata.RuntimeConnectionCollection[0].ConnectionManager = DtsConvert.GetExtendedInterface(selectedManager);
                metadata.RuntimeConnectionCollection[0].ConnectionManagerID = selectedManager.ID;
                string connString = selectedManager.ConnectionString;

                if(!string.IsNullOrEmpty(connString)) {
                    model.ConnectionString = connString;
                    crmComm = Model.Connection.CrmCommandsFactory(connString);

                    model.ConnectionId = selectedManager.ID;

                    model.EntityList = crmComm.RetrieveEntityList().OrderBy(n => n.LogicalName).ToList();
                    if(operation == OperationTypeEnum.Merge.ToString()) {
                        return;
                    }

                    if (operation == "Update") {
                        entityBindingSource.DataSource = model.EntityList.Where(n => n.ValidForAdvancedFind).OrderBy(n => n.LogicalName).ToList();
                    } else {
                        entityBindingSource.DataSource = model.EntityList.Where(n => n.ValidForAdvancedFind || n.IsIntersect).ToList();
                    }

                    bool isSameEntity = true;
                    List<CrmAttribute> attributes = null;
                    if (cmbENT.SelectedItem is Entity selectedEntity) {
                        Entity existingEntity = new Entity { LogicalName = selectedEntity.LogicalName };
                        if(model.EntityList.FirstOrDefault(n => n.LogicalName == selectedEntity.LogicalName) != null) {
                            attributes = crmComm.RetrieveAttributesList(model, selectedEntity)
                                                                          .Where(n => string.IsNullOrEmpty(n.AttributeOf)).OrderBy(n => n.LogicalName).ToList();
                            if(model.Attributes == null || model.Attributes.Count == 0) {
                                isSameEntity = false;
                            }
                            foreach (AlternateKey key in existingEntity.AlternateKeys) {
                                AlternateKey k = selectedEntity.AlternateKeys.FirstOrDefault(n => n.LogicalName == key.LogicalName);
                                if(k == null) {
                                    isSameEntity = false;
                                    break;
                                }
                                if(k.KeyColumns.Any(n => !key.KeyColumns.Exists(l => l.LogicalName == n.LogicalName))) {
                                    isSameEntity = false;
                                    break;
                                }
                            }
                        } else {
                            isSameEntity = false;
                        }
                    }

                    if (isSameEntity) {
                        foreach(CrmAttribute attr in model.Attributes) {
                            if(!string.IsNullOrEmpty(attr.SsisInput)) {
                                CrmAttribute newAttr = attributes.FirstOrDefault(n => n.LogicalName == attr.LogicalName);
                                if(newAttr != null) {
                                    newAttr.SsisInput = attr.SsisInput;
                                } else {
                                    SsisInput ssisCol = model.SsisInputs.FirstOrDefault(n => n.CrmColumnName == attr.LogicalName);
                                    if(ssisCol != null) {
                                        ssisCol.CrmColumnName = null;
                                    }
                                }
                            }
                        }
                    }
                    else {
                        model.SelectedEntity = null;
                        alternateKeyBindingSource.DataSource = new List<AlternateKey>();

                        model.Attributes = currentAttributes = new List<CrmAttribute>();
                        cmbENT.SelectedIndex = -1;
                        crmAttributeBindingSource.DataSource = currentAttributes;
                        cmbKEY.SelectedIndex = -1;
                    }
                }
            }
        }

        private void cmbENT_SelectedIndexChanged(object sender, EventArgs e) {
            if(formLoaded && cmbENT.SelectedItem is Entity selectedEntity) {
                string selectedOperation = cmbOPR.SelectedItem as string;
                if (model.SelectedEntity != null && model.SelectedEntity != selectedEntity) {
                    foreach (SsisInput ssisInput in model.SsisInputs) {
                        ssisInput.CrmColumnName = null;
                    }
                }

                if (!string.IsNullOrEmpty(selectedOperation) && selectedOperation == OperationTypeEnum.Merge.ToString()) {
                    model.SelectedEntity = selectedEntity;
                    model.Attributes = currentAttributes = model.ReturnMergeAttributes();
                    crmAttributeBindingSource.DataSource = currentAttributes;
                    SetDgvComboCellsDataSource();
                    return;
                }
                
                model.SelectedEntity = selectedEntity;

                model.Attributes = currentAttributes = crmComm.RetrieveAttributesList(model, selectedEntity)
                                                              .Where(n => string.IsNullOrEmpty(n.AttributeOf)).OrderBy(n => n.LogicalName).ToList();
                if (operation == OperationTypeEnum.Create.ToString()) {
                    currentAttributes = model.Attributes.Where(n => n.DisplayForCreate || n.LogicalName == model.SelectedEntity.IdName).ToList();
                }
                else if(operation == OperationTypeEnum.Update.ToString()) {
                    currentAttributes = model.Attributes.Where(n => n.DisplayForUpdate || n.LogicalName == model.SelectedEntity.IdName).ToList();
                }
                else if(operation == OperationTypeEnum.Upsert.ToString()) {
                    currentAttributes = model.Attributes.Where(n => n.DisplayForUpdate || n.DisplayForCreate || n.LogicalName == model.SelectedEntity.IdName).ToList();
                }

                crmAttributeBindingSource.DataSource = currentAttributes;
                alternateKeyBindingSource.DataSource = model.SelectedEntity.AlternateKeys;
                cmbKEY.SelectedIndex = -1;
                SetDgvComboCellsDataSource();
                chk01.Checked = false;
                chk02.Checked = false;
                if (model.SelectedEntity?.IsIntersect == true) {
                    cmbMTC.Items.Remove("Primary Key");
                    cmbMTC.Items.Remove("Alternate Key");
                    matchingCriteria = cmbMTC.SelectedItem as string;
                }
                else {
                    if (!cmbMTC.Items.Contains("Primary Key")) {
                        cmbMTC.Items.Add("Primary Key");
                    }
                    if (!cmbMTC.Items.Contains("Alternate Key")) {
                        cmbMTC.Items.Add("Alternate Key");
                    }
                }
            }
        }

        private void cmbOPR_SelectedIndexChanged(object sender, EventArgs e) {
            string selectedOperation = cmbOPR.SelectedItem as string;
            if(formLoaded && !string.IsNullOrEmpty(selectedOperation)) {
                operation = selectedOperation;

                if(operation == OperationTypeEnum.Merge.ToString()) {
                    cmbMTC.Enabled = false;
                    cmbMM.Enabled = false;
                    cmbMNF.Enabled = false;
                    cmbKEY.Enabled = false;
                    btnREFR.Enabled = false;
                    if (previousOperation != OperationTypeEnum.Merge.ToString()) {
                        entityBindingSource.DataSource = model.ReturnMergeEnities();
                        cmbENT.SelectedIndex = -1;
                        model.Attributes = currentAttributes = new List<CrmAttribute>();
                        crmAttributeBindingSource.DataSource = currentAttributes;
                    }
                    previousOperation = operation;
                    return;
                } else {//Not Merge
                    if(previousOperation == OperationTypeEnum.Merge.ToString()) {
                        btnREFR.Enabled = true;
                        if (model.EntityList == null || model.EntityList.Count == 0) {
                            model.EntityList = crmComm.RetrieveEntityList().OrderBy(n => n.LogicalName).ToList();
                            if (operation == "Update") {
                                entityBindingSource.DataSource = model.EntityList.Where(n => n.ValidForAdvancedFind).OrderBy(n => n.LogicalName).ToList();
                            }
                            else {
                                entityBindingSource.DataSource = model.EntityList.Where(n => n.ValidForAdvancedFind || n.IsIntersect).ToList();
                            }
                        }
                        model.Attributes = currentAttributes = new List<CrmAttribute>();
                        crmAttributeBindingSource.DataSource = currentAttributes;
                    }
                }

                if (operation == OperationTypeEnum.Delete.ToString() && cmbMTC.Items.Contains("Alternate Key")) {
                    cmbMTC.Items.Remove("Alternate Key");
                    matchingCriteria = cmbMTC.SelectedItem as string;
                }
                else if (operation != OperationTypeEnum.Delete.ToString() && model.SelectedEntity?.IsIntersect != true && !cmbMTC.Items.Contains("Alternate Key")) {
                    cmbMTC.Items.Add("Alternate Key");
                }

                if (operation == OperationTypeEnum.Update.ToString()) {
                    crmAttributeBindingSource.DataSource = model.Attributes.Where(n => n.DisplayForUpdate).OrderBy(n => n.LogicalName).ToList();
                    SetDgvComboCellsDataSource();
                }
                else if(operation == OperationTypeEnum.Create.ToString()) {
                    crmAttributeBindingSource.DataSource = model.Attributes.Where(n => n.DisplayForCreate).OrderBy(n => n.LogicalName).ToList();
                    SetDgvComboCellsDataSource();
                }
                else if (operation == OperationTypeEnum.Upsert.ToString()) {
                    crmAttributeBindingSource.DataSource = model.Attributes.Where(n => n.DisplayForCreate || n.DisplayForUpdate).OrderBy(n => n.LogicalName).ToList();
                    SetDgvComboCellsDataSource();
                }
                else {
                    crmAttributeBindingSource.DataSource = model.Attributes;
                    SetDgvComboCellsDataSource();
                }

                if(operation == OperationTypeEnum.Create.ToString()) {
                    cmbMTC.Enabled = false;
                    cmbMM.Enabled = false;
                    cmbMNF.Enabled = false;
                    cmbKEY.Enabled = false;
                } else {
                    cmbMTC.Enabled = true;
                    cmbMM.Enabled = true;
                    cmbMNF.Enabled = true;
                    cmbKEY.Enabled = true;
                }

                if(previousOperation == OperationTypeEnum.Merge.ToString()) {
                    cmbENT.SelectedIndex = -1;
                }

                previousOperation = operation;
            }
        }        

        private void cmbMTC_SelectedIndexChanged(object sender, EventArgs e) {
            string matchCrit = cmbMTC.SelectedItem as string;

            if(formLoaded && !string.IsNullOrEmpty(matchCrit)) {
                matchingCriteria = matchCrit;
            }
        }

        private void dgv2_CurrentCellDirtyStateChanged(object sender, EventArgs e) {
            if (dgv2.IsCurrentCellDirty) {
                dgv2.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void dgv2_CellClick(object sender, DataGridViewCellEventArgs e) {
            if (e.ColumnIndex == 4 && e.RowIndex >= 0) {//Complex Mapping
                DataGridViewLinkCell cellComplexMapping = (DataGridViewLinkCell)dgv2.Rows[e.RowIndex].Cells[4];

                if (cellComplexMapping.Value != null && cellComplexMapping.Value.ToString().StartsWith("complex")) {
                    LookupMatch match = new LookupMatch();

                    DataGridViewTextBoxCell attributeNameCell = (DataGridViewTextBoxCell)dgv2.Rows[e.RowIndex].Cells[2];
                    string ssisName = ((DataGridViewComboBoxCell)dgv2.Rows[e.RowIndex].Cells[1]).Value.ToString(); 
                    string attributeName = attributeNameCell.Value.ToString();
                    CrmAttribute attr = model.Attributes.First(n => n.LogicalName == attributeName);

                    match.Model = model;
                    match.PossibleLookups = attr.PossibleLookups;
                    match.SsisInputs = model.SsisInputs.OrderBy(n => n.Name).ToList();
                    match.SsisColumnName = ssisName;
                    match.TargetLookupEntity = attr.LookupTarget;
                    match.TargetFields = attr.MatchingLookupAttributes;
                    match.MatchedMultiple = !string.IsNullOrEmpty(attr.MatchedMultiple) ? attr.MatchedMultiple : MatchingEnum.RaiseError.ToString();
                    match.MatchNotFound = !string.IsNullOrEmpty(attr.MatchNotFound) ? attr.MatchNotFound : MatchingEnum.RaiseError.ToString();
                    match.DefaultValue = attr.MatchingDefaultValue;
                    match.Commands = crmComm;
                    match.MatchingType = attr.ComplexMapping;
                    if(attr.ComplexMapping == ComplexMappingEnum.AlternateKey) {
                        match.AlternateKey = attr.LookupAlternateKey;
                    }

                    LookupForm form = new LookupForm(match);
                    form.ShowDialog();

                    if (form.DialogResult == DialogResult.OK) {
                        attr.LookupTarget = match.TargetLookupEntity;
                        attr.ComplexMapping = match.MatchingType;
                        if (match.MatchingType == ComplexMappingEnum.AlternateKey) {
                            attr.LookupAlternateKey = match.AlternateKey;
                        }
                        else {
                            attr.MatchingLookupAttributes = match.TargetFields;
                        }

                        attr.MatchedMultiple = match.MatchedMultiple;
                        attr.MatchNotFound = match.MatchNotFound;
                        attr.MatchingDefaultValue = match.DefaultValue;

                        cellComplexMapping.Value = attr.ComplexMapping == ComplexMappingEnum.PrimaryKey ? "complex: no" : "complex: yes";
                    }
                }
            }
            if(e.ColumnIndex == 0 && e.RowIndex >= 0) {//id matching
                DataGridViewCell clickedCell = dgv2.CurrentCell;
                bool check = !(bool)clickedCell.EditedFormattedValue;

                DataGridViewTextBoxCell attributeNameCell = (DataGridViewTextBoxCell)dgv2.Rows[e.RowIndex].Cells[2];
                string attributeName = attributeNameCell.Value.ToString();

                if (check) {
                    if (!model.MatchingColumns.Contains(attributeName)) {
                        model.MatchingColumns.Add(attributeName);
                    }
                }
                else {
                    if (model.MatchingColumns.Contains(attributeName)) {
                        model.MatchingColumns.Remove(attributeName);
                    }
                }
            }
        }

        private void cmbKEY_SelectedIndexChanged(object sender, EventArgs e) {
            if(formLoaded && cmbKEY.SelectedItem is AlternateKey selectedKey) {
                alternateKey = selectedKey.LogicalName;
            }
        }

        private void dgv2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void dgv2_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e) {
            try {
                if(model.Attributes.Count > 1) {
                    if(currentAttributes.Count > 1) {
                        if(e.ColumnIndex == 1) {//SsisInputs
                            int i = 0;
                            while(currentAttributes[i].SsisInput == currentAttributes[i + 1].SsisInput && i + 1 <= currentAttributes.Count - 1) {
                                i++;
                            }

                            if(string.Compare(currentAttributes[i].SsisInput, currentAttributes[i + 1].SsisInput, StringComparison.Ordinal) < 0) {
                                crmAttributeBindingSource.DataSource = currentAttributes = (from row in currentAttributes
                                                                        orderby row.SsisInput descending, row.LogicalName
                                                                        select row).ToList();
                            } else {
                                crmAttributeBindingSource.DataSource = currentAttributes = (from row in currentAttributes
                                                                        orderby row.SsisInput, row.LogicalName
                                                                        select row).ToList();
                            }

                            SetDgvComboCellsDataSource();
                        }

                        if(e.ColumnIndex == 2) { //Crm Attribute
                            crmAttributeBindingSource.DataSource = currentAttributes =
                                string.Compare(currentAttributes[0].LogicalName, currentAttributes[currentAttributes.Count - 1].LogicalName, StringComparison.Ordinal) < 0 ? 
                                    currentAttributes.OrderByDescending(n => n.LogicalName).ToList() : currentAttributes.OrderBy(n => n.LogicalName).ToList();
                            SetDgvComboCellsDataSource();
                        }

                        if(e.ColumnIndex == 3) {//CrmAttributeType
                            int i = 0;
                            while(currentAttributes[i].CrmAttributeType == currentAttributes[i + 1].CrmAttributeType && i + 1 < currentAttributes.Count - 1) {
                                i++;
                            }

                            if(string.Compare(currentAttributes[i].CrmAttributeType.ToString(), currentAttributes[i + 1].CrmAttributeType.ToString(), StringComparison.Ordinal) < 0) {
                                crmAttributeBindingSource.DataSource = currentAttributes = (from row in currentAttributes
                                                                        orderby row.CrmAttributeType.ToString() descending, row.LogicalName
                                                                        select row).ToList();
                            } else {
                                crmAttributeBindingSource.DataSource  = currentAttributes = (from row in currentAttributes
                                                                        orderby row.CrmAttributeType.ToString(), row.LogicalName
                                                                        select row).ToList();
                            }

                            SetDgvComboCellsDataSource();
                        }
                    }
                }
            } catch(Exception ex) {
                designTimeInstance.SetComponentProperty("Designtime Error", ex.Message);
            }
        }

        private void cmbMM_SelectedIndexChanged(object sender, EventArgs e) {
            string multipleMatch = cmbMM.SelectedItem as string;

            if (formLoaded && !string.IsNullOrEmpty(multipleMatch)) {
                multipleMatches = multipleMatch;
            }
        }

        private void cmbMNF_SelectedIndexChanged(object sender, EventArgs e) {
            string notFoundCrit = cmbMNF.SelectedItem as string;

            if (formLoaded && !string.IsNullOrEmpty(notFoundCrit)) {
                matchNotFound = notFoundCrit;
            }
        }

        private void tabPage1_Click(object sender, EventArgs e) {

        }

        private void btnREFR_Click(object sender, EventArgs e) {
            if(cmbCON.SelectedItem is Connection) {
                model.EntityList = crmComm.RetrieveEntityList().OrderBy(n => n.LogicalName).ToList();
                Entity selectedEntity = cmbENT.SelectedItem as Entity;
                entityBindingSource.DataSource = model.EntityList.Where(n => n.ValidForAdvancedFind || n.IsIntersect).ToList();
                if (selectedEntity != null) {
                    if (model.EntityList.All(n => n.LogicalName != selectedEntity.LogicalName)) {//there is no selectedEntity in new model.EntityList
                        model.SelectedEntity = null;
                        model.Attributes = currentAttributes = new List<CrmAttribute>();
                        cmbENT.SelectedIndex = -1;
                        crmAttributeBindingSource.DataSource = currentAttributes;
                    } else {
                        cmbENT.SelectedValue = selectedEntity.LogicalName;
                        List<CrmAttribute> attributes = crmComm.RetrieveAttributesList(model, selectedEntity).
                                                               Where(n => string.IsNullOrEmpty(n.AttributeOf)).OrderBy(n => n.LogicalName).ToList();
                        if(operation == "Create") {
                            attributes = attributes.Where(n => n.DisplayForCreate || n.LogicalName == selectedEntity.IdName).ToList();
                        } else {
                            attributes = attributes.Where(n => n.DisplayForUpdate || n.LogicalName == selectedEntity.IdName).ToList();
                        }
                        List<CrmAttribute> newAttributes = new List<CrmAttribute>();
                        List<CrmAttribute> modifiedAttributes = new List<CrmAttribute>();
                        List<CrmAttribute> deletedAttributes = new List<CrmAttribute>();

                        foreach(CrmAttribute attribute in attributes) {
                            CrmAttribute old = model.Attributes.FirstOrDefault(n => n.LogicalName == attribute.LogicalName);
                            if(old == null) {
                                attribute.SsisInput = null;
                                newAttributes.Add(attribute);
                                model.Attributes.Add(attribute);
                                if(!currentAttributes.Contains(attribute)) {
                                    currentAttributes.Add(attribute);
                                }
                            } else {
                                if(old.CrmAttributeType != attribute.CrmAttributeType || old.Length != attribute.Length ||
                                   old.Precision != attribute.Precision || old.Scale != attribute.Scale) {
                                    attribute.SsisInput = old.SsisInput;
                                    modifiedAttributes.Add(attribute);
                                    model.Attributes.Remove(old);
                                    model.Attributes.Add(attribute);
                                    if(currentAttributes.Remove(old)) {
                                        currentAttributes.Add(attribute);
                                    }
                                }
                            }
                        }

                        deletedAttributes = model.Attributes.Where(n => attributes.All(k => k.LogicalName != n.LogicalName)).ToList();
                        foreach (CrmAttribute attribute in deletedAttributes) {
                            model.Attributes.Remove(attribute);
                            currentAttributes.Remove(attribute);
                            SsisInput inp = model.SsisInputs.FirstOrDefault(n => n.CrmColumnName == attribute.LogicalName);
                            if(inp != null) {
                                inp.CrmColumnName = null;
                            }
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
                            currentAttributes = currentAttributes.OrderBy(n => n.LogicalName).ToList();
                            crmAttributeBindingSource.DataSource = currentAttributes;
                            SetDgvComboCellsDataSource();
                        }

                        UpdatedAttributesForm form = new UpdatedAttributesForm(modified);
                        form.ShowDialog();
                    }
                }
            }
        }

        private void tabPage2_Click(object sender, EventArgs e) {

        }

        private void label8_Click(object sender, EventArgs e) {

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
                    if(operation == OperationTypeEnum.Update.ToString()) {
                        crmAttributeBindingSource.DataSource = currentAttributes = model.Attributes.Where(n => n.DisplayForUpdate).OrderBy(n => n.LogicalName).ToList();
                    }
                    else if(operation == OperationTypeEnum.Create.ToString()) {
                        crmAttributeBindingSource.DataSource = currentAttributes = model.Attributes.Where(n => n.DisplayForCreate).OrderBy(n => n.LogicalName).ToList();
                    }
                    else if(operation == OperationTypeEnum.Upsert.ToString()) {
                        crmAttributeBindingSource.DataSource = currentAttributes = model.Attributes.Where(n => n.DisplayForCreate || n.DisplayForUpdate).OrderBy(n => n.LogicalName).ToList();
                    } else {
                        crmAttributeBindingSource.DataSource = currentAttributes = model.Attributes.OrderBy(n => n.LogicalName).ToList();
                    }
                }
                else if(chk01.Checked && !chk02.Checked) {//hide mapped
                    crmAttributeBindingSource.DataSource = currentAttributes = model.Attributes.Where(n => n.SsisInput == "<ignore>" || string.IsNullOrEmpty(n.SsisInput)).
                                                                                     OrderBy(n => n.LogicalName).ToList();
                }
                else if(!chk01.Checked && chk02.Checked) {//hide unmapped
                    crmAttributeBindingSource.DataSource = currentAttributes = model.Attributes.
                                                                                     Where(n => n.SsisInput != "<ignore>" && !string.IsNullOrEmpty(n.SsisInput)).
                                                                                     OrderBy(n => n.LogicalName).ToList();
                }
            }
            SetDgvComboCellsDataSource();
        }

        private void ssisInputBindingSource_CurrentChanged(object sender, EventArgs e) {

        }

        private void cmbERR_SelectedIndexChanged(object sender, EventArgs e) {
            string errorHandl = cmbERR.SelectedItem as string;

            if (formLoaded && !string.IsNullOrEmpty(errorHandl)) {
                errorHandling = errorHandl;
                if(errorHandling == "Fail on Error") {
                    metadata.InputCollection[0].ErrorRowDisposition = DTSRowDisposition.RD_FailComponent;
                    metadata.InputCollection[0].ErrorOrTruncationOperation = "Fail";
                } else if(errorHandling == "Ignore Error") {
                    metadata.InputCollection[0].ErrorRowDisposition = DTSRowDisposition.RD_IgnoreFailure;
                    metadata.InputCollection[0].ErrorOrTruncationOperation = "Ignore";
                } else {
                    metadata.InputCollection[0].ErrorRowDisposition = DTSRowDisposition.RD_RedirectRow;
                    metadata.InputCollection[0].ErrorOrTruncationOperation = "Redirect";
                }
            }
        }

        private void entityBindingSource_CurrentChanged(object sender, EventArgs e) {

        }
    }
}
