using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CrmComponents.Helpers.Enums;
using CrmComponents.Model;

namespace CrmComponents
{
    public partial class LookupForm : Form {
        private LookupMatch match;
        private bool formLoaded = false;
        private List<CrmAttribute> attributes;
        private List<AlternateKey> keys;
        public LookupForm(LookupMatch match) {
            InitializeComponent();
            this.match = match;
            cmbENT.DataSource = match.PossibleLookups.Select(n => n.TargetEntityName).ToList();
        }

        private void LookupForm_Load(object sender, EventArgs e) {
            formLoaded = true;
            if (!string.IsNullOrEmpty(match.TargetLookupEntity?.TargetEntityName)) {
                cmbENT.SelectedItem = match.TargetLookupEntity.TargetEntityName;
            }
            else {
                cmbENT.SelectedItem = match.PossibleLookups[0].TargetEntityName;
                cmbENT_SelectedIndexChanged(null, null);
            }
            
            cmbMethod.SelectedItem = Dictionaries.GetInstance().ComplexMappingNames.First(n => n.Key == (int)match.MatchingType).Value;

            if (match.TargetFields != null && match.TargetFields.Count > 0 && match.TargetFields[0].LogicalName != null) {
                cmbATTR.SelectedValue = match.TargetFields[0].LogicalName;
            }

            if (match.AlternateKey != null) {
                int i = 0;
                foreach(object item in cmbAlternateKey.Items) {
                    if((item as AlternateKey).LogicalName == match.AlternateKey.LogicalName) {
                        break;
                    }

                    i++;
                }

                cmbAlternateKey.SelectedIndex = i;
            }

            if (!string.IsNullOrEmpty(match.MatchedMultiple)) {
                if (match.MatchedMultiple == MatchingEnum.RaiseError.ToString()) {
                    rb01.Checked = true;
                }

                if (match.MatchedMultiple == MatchingEnum.MatchOne.ToString()) {
                    rb02.Checked = true;
                }
            }

            if (!string.IsNullOrEmpty(match.MatchNotFound)) {
                if (match.MatchNotFound == MatchingEnum.RaiseError.ToString()) {
                    rb03.Checked = true;
                }

                if (match.MatchNotFound == MatchingEnum.DefaultValue.ToString()) {
                    rb04.Checked = true;
                }
            }

            if (!string.IsNullOrEmpty(match.DefaultValue)) {
                txt01.Text = match.DefaultValue;
            }
            else {
                txt01.Text = "null";
            }
            cmbAlternateKey_SelectedIndexChanged(null, null);
        }

        private void SetDgv() {
            string method = cmbMethod.SelectedItem as string;
            var mE = string.IsNullOrEmpty(method) ? ComplexMappingEnum.Manual : (ComplexMappingEnum)Dictionaries.GetInstance().GetEnumFromString(method);
            if (mE == ComplexMappingEnum.Manual) {
                dgv.Columns[0].ReadOnly = false;
                dgv.AllowUserToAddRows = false;
                dgv.AllowUserToDeleteRows = true;

                dgv.Rows.Clear();

                foreach (CrmAttribute crmAttribute in match.TargetFields) {
                    DataGridViewRow row = (DataGridViewRow)dgv.RowTemplate.Clone();
                    row.CreateCells(dgv);

                    //crm column
                    DataGridViewComboBoxCell cellCrm = (DataGridViewComboBoxCell)row.Cells[0];
                    cellCrm.DataSource = (new List<string>(new[] { "" })).Concat(attributes.Select(n => n.LogicalName)).ToList();
                    cellCrm.Value = crmAttribute.LogicalName;
                    //ssis column
                    DataGridViewComboBoxCell cellSsis = (DataGridViewComboBoxCell)row.Cells[2];
                    cellSsis.DataSource = match.SsisInputs.Select(n => n.Name).ToList();
                    if (!string.IsNullOrEmpty(crmAttribute.SsisInput) && match.SsisInputs.Exists(n => n.Name == crmAttribute.SsisInput)) {
                        cellSsis.Value = crmAttribute.SsisInput;
                    }
                    //crm type
                    row.Cells[1].Value = crmAttribute.CrmAttributeType.ToString();
                    dgv.Rows.Add(row);
                }
            }
            else if (mE == ComplexMappingEnum.AlternateKey) {
                if (cmbAlternateKey.SelectedItem is AlternateKey aKey) {
                    dgv.Rows.Clear();
                    foreach(CrmAttribute attribute in aKey.KeyColumns) {
                        DataGridViewRow row = (DataGridViewRow)dgv.RowTemplate.Clone();
                        row.CreateCells(dgv);
                        //crm column
                        DataGridViewComboBoxCell cellCrm = (DataGridViewComboBoxCell)row.Cells[0];
                        cellCrm.DataSource = aKey.KeyColumns.Select(n => n.LogicalName).ToList();
                        cellCrm.Value = attribute.LogicalName;
                        //ssis column
                        DataGridViewComboBoxCell cellSsis = (DataGridViewComboBoxCell)row.Cells[2];
                        cellSsis.DataSource = match.SsisInputs.Select(n => n.Name).ToList();
                        if(!string.IsNullOrEmpty(attribute.SsisInput) && match.SsisInputs.Exists(n => n.Name == attribute.SsisInput)) {
                            cellSsis.Value = attribute.SsisInput;
                        }
                        //crm type
                        row.Cells[1].Value = attribute.CrmAttributeType.ToString();
                        dgv.Rows.Add(row);
                    }
                    
                    dgv.Columns[0].ReadOnly = true;
                    dgv.AllowUserToAddRows = false;
                    dgv.AllowUserToDeleteRows = false;
                }
                else {
                    crmAttributeBindingSource1.DataSource = new List<CrmAttribute>();
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e) {
            DataGridViewRow row = (DataGridViewRow)dgv.RowTemplate.Clone();
            row.CreateCells(dgv);
            //crm column
            DataGridViewComboBoxCell cellCrm = (DataGridViewComboBoxCell)row.Cells[0];
            cellCrm.DataSource = (new List<string>(new[] { "" })).Concat(attributes.Select(n => n.LogicalName)).ToList();
            //ssis column
            DataGridViewComboBoxCell cellSsis = (DataGridViewComboBoxCell)row.Cells[2];
            cellSsis.DataSource = match.SsisInputs.Select(n => n.Name).ToList();
            dgv.Rows.Add(row);
            DeleteEmptyRows();
        }

        private void DeleteEmptyRows() {
            List<DataGridViewRow> rowsForDelete = new List<DataGridViewRow>();
            for (int i = 0; i < dgv.RowCount - 1; i++) {
                string cellString = dgv.Rows[i].Cells[0].EditedFormattedValue?.ToString();
                if(string.IsNullOrEmpty(cellString)) {
                    rowsForDelete.Add(dgv.Rows[i]);
                }
            }

            foreach(DataGridViewRow row in rowsForDelete) {
                dgv.Rows.Remove(row);
            }
        }

        private void dgv_CellValueChanged(object sender, DataGridViewCellEventArgs e) {
            if(e.ColumnIndex == 0 && e.RowIndex >= 0 && e.RowIndex < dgv.RowCount - 1) {
                string crmColumnName = dgv.Rows[e.RowIndex].Cells[0].Value?.ToString();
                if(!string.IsNullOrEmpty(crmColumnName)) {
                    CrmAttribute selected = attributes.First(n => n.LogicalName == crmColumnName);
                    dgv.Rows[e.RowIndex].Cells[1].Value = selected.CrmAttributeType.ToString();
                }
            }
        }

        private void btnOK_Click(object sender, EventArgs e) {
            string entityName = cmbENT.SelectedItem as string;
            if(!string.IsNullOrEmpty(entityName)) {
                match.TargetLookupEntity = match.PossibleLookups.First(n => n.TargetEntityName == entityName);
                match.DefaultValue = rb04.Checked ? txt01.Text.Trim() : null;

                match.MatchingType = (ComplexMappingEnum)Dictionaries.GetInstance().ComplexMappingNames.First(n => n.Value == (string)cmbMethod.SelectedItem).Key;
                if (match.MatchingType == ComplexMappingEnum.AlternateKey && cmbAlternateKey.SelectedItem is AlternateKey aKey) {
                    foreach (DataGridViewRow dgvRow in dgv.Rows) {
                        string crmColumn = dgvRow.Cells[0].Value.ToString();
                        CrmAttribute attr = aKey.KeyColumns.First(n => n.LogicalName == crmColumn);
                        attr.SsisInput = dgvRow.Cells[2].Value?.ToString();
                    }

                    if (aKey.KeyColumns.Exists(n => string.IsNullOrEmpty(n.SsisInput))) {
                        MessageBox.Show("All columns of alternate key must be mapped");
                        return;
                    }

                    match.AlternateKey = aKey;
                } else if(match.MatchingType == ComplexMappingEnum.Manual) {
                    ReadDgv();
                    if(match.TargetFields.Count == 0) {
                        MessageBox.Show("None matching column(s) selected");
                        return;
                    }
                }
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCNC_Click(object sender, EventArgs e) {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void rb_CheckedChanged(object sender, EventArgs e) {
            RadioButton rb = sender as RadioButton;
            string r1 = null;
            string r2 = null;

            foreach (Control control in this.grp01.Controls) {
                if (control is RadioButton radio) {
                    if (radio.Checked && radio.Name == "rb01") {
                        match.MatchedMultiple = MatchingEnum.RaiseError.ToString();
                    }

                    if (radio.Checked && radio.Name == "rb02") {
                        match.MatchedMultiple = MatchingEnum.MatchOne.ToString();
                    }
                }
            }

            foreach (Control control in grp02.Controls) {
                if (control is RadioButton radio) {
                    if (radio.Checked && radio.Name == "rb03") {
                        match.MatchNotFound = MatchingEnum.RaiseError.ToString();
                    }

                    if (radio.Checked && radio.Name == "rb04") {
                        match.MatchNotFound = MatchingEnum.DefaultValue.ToString();
                    }
                }
            }
        }

        private void cmbENT_SelectedIndexChanged(object sender, EventArgs e) {
            string selected = cmbENT.SelectedItem as string;
            if(!string.IsNullOrEmpty(selected)) {
                Entity entity = new Entity { LogicalName = selected, EntitySetName = match.Model.EntityList.First(n => n.LogicalName == selected).EntitySetName};
                attributes = match.Commands.RetrieveAttributesList(match.Model, entity)
                                                              .Where(n => n.CrmAttributeType == AttributeTypeEnum.Memo
                                                                          || n.CrmAttributeType == AttributeTypeEnum.String
                                                                          || n.CrmAttributeType == AttributeTypeEnum.Decimal
                                                                          || n.CrmAttributeType == AttributeTypeEnum.Integer
                                                                          || n.CrmAttributeType == AttributeTypeEnum.Money)
                                                              .OrderBy(n => n.LogicalName).ToList();
                keys = entity.AlternateKeys;
                var aKey = cmbAlternateKey.SelectedItem as AlternateKey ?? match.AlternateKey;
                if(aKey != null) {
                    var sameKey = keys.FirstOrDefault(n => n.LogicalName == aKey.LogicalName);
                    if(sameKey != null) {
                        if(sameKey.KeyColumns.All(n => n.LogicalName == aKey.KeyColumns.FirstOrDefault(k => k.LogicalName == n.LogicalName)?.LogicalName)) {
                            int i = keys.IndexOf(sameKey);
                            keys.Remove(sameKey);
                            keys.Insert(i, aKey);
                        }
                    }
                    
                }
                var existingKey = keys.FirstOrDefault(n => n.LogicalName == match.AlternateKey?.LogicalName);
                if(existingKey != null) {
                    foreach (CrmAttribute column in match.AlternateKey.KeyColumns) {
                        if (!string.IsNullOrEmpty(column.SsisInput)) {
                            existingKey.KeyColumns.First(n => n.LogicalName == column.LogicalName).SsisInput = column.SsisInput;
                        }
                    }
                }
                alternateKeyBindingSource.DataSource = keys;
            }
        }

        private void cmbMethod_SelectedIndexChanged(object sender, EventArgs e) {
            string method = cmbMethod.SelectedItem as string;
            var mE = string.IsNullOrEmpty(method) ? ComplexMappingEnum.Manual : (ComplexMappingEnum)Dictionaries.GetInstance().GetEnumFromString(method);
            if(mE == ComplexMappingEnum.PrimaryKey) {
                match.MatchingType = ComplexMappingEnum.PrimaryKey;
                rb01.Enabled = rb02.Enabled = rb03.Enabled = rb04.Enabled = false;
                txt01.Enabled = false;
                cmbAlternateKey.Enabled = cmbATTR.Enabled = false;
                btnAdd.Enabled = false;
                DisableDgv();
            } else if(mE == ComplexMappingEnum.AlternateKey) {
                rb01.Enabled = rb02.Enabled = rb03.Enabled = rb04.Enabled = false;
                txt01.Enabled = false;
                cmbAlternateKey.Enabled = true;
                btnAdd.Enabled = false;
                EnableDgv();
                SetDgv();
            }
            else {//Manually Specify
                rb01.Enabled = rb02.Enabled = rb03.Enabled = rb04.Enabled = true;
                txt01.Enabled = true;
                cmbAlternateKey.Enabled = false;
                btnAdd.Enabled = true;
                EnableDgv();
                SetDgv();
            }
            ReadDgv();
        }

        private void cmbAlternateKey_SelectedIndexChanged(object sender, EventArgs e) {
            if (cmbAlternateKey.SelectedItem is AlternateKey aKey) {
                SetDgv();
            }
            else {
                crmAttributeBindingSource1.DataSource = new List<CrmAttribute>();
            }
        }

        private void DisableDgv() {
            foreach(DataGridViewColumn dgvColumn in dgv.Columns) {
                dgvColumn.ReadOnly = true;
                dgvColumn.DefaultCellStyle.BackColor = Color.Gray;
            }
        }

        private void EnableDgv() {
            foreach (DataGridViewColumn dgvColumn in dgv.Columns) {
                dgvColumn.ReadOnly = false;
                dgvColumn.DefaultCellStyle.BackColor = Color.White;
            }
        }

        private void ReadDgv() {
            string method = cmbMethod.SelectedItem as string;
            var mE = string.IsNullOrEmpty(method) ? ComplexMappingEnum.Manual : (ComplexMappingEnum)Dictionaries.GetInstance().GetEnumFromString(method);
            if (mE == ComplexMappingEnum.AlternateKey && cmbAlternateKey.SelectedItem is AlternateKey aKey) {
                foreach(DataGridViewRow dgvRow in dgv.Rows) {
                    string crmColumn = dgvRow.Cells[0].Value?.ToString();
                    if(crmColumn != null) {
                        CrmAttribute attr = aKey.KeyColumns.First(n => n.LogicalName == crmColumn);
                        attr.SsisInput = dgvRow.Cells[2].Value?.ToString();
                    }
                }
            }
            else if(mE == ComplexMappingEnum.Manual) {
                match.TargetFields = new List<CrmAttribute>();
                foreach(DataGridViewRow dgvRow in dgv.Rows) {
                    string crmColumn = dgvRow.Cells[0].Value?.ToString();
                    if(!string.IsNullOrEmpty(crmColumn)) {
                        CrmAttribute attr = attributes.First(n => n.LogicalName == crmColumn);
                        string ssisInputName = dgvRow.Cells[2].Value?.ToString();
                        if(!string.IsNullOrEmpty(ssisInputName)) {
                            attr.SsisInput = ssisInputName;
                            match.TargetFields.Add(attr);
                        }
                    }
                }
            }
        }
    }
}
