using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CrmComponents.Model;

namespace CrmComponents
{
    public partial class UpdatedAttributesForm : Form {
        public UpdatedAttributesForm(List<ModifiedAttribute> attributes)
        {
            InitializeComponent();
            modifiedAttributeBindingSource.DataSource = attributes;
            if(attributes.Count == 0) {
                this.lbl01.Visible = true;
                this.dgv01.Visible = false;
                this.lbl01.Location = new Point(12, 12);
                this.lbl01.Text = "There are no changes";
            } else {
                this.lbl01.Visible = false;
                this.dgv01.Visible = true;
            }
        }

        private void UpdatedAttributesForm_Load(object sender, EventArgs e) {

        }

        private void crmAttributeBindingSource_CurrentChanged(object sender, EventArgs e) {

        }

        private void dgv01_CellContentClick(object sender, DataGridViewCellEventArgs e) {

        }
    }
}
