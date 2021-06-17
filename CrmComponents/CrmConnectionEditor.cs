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
using CrmComponents.Soap;
using Microsoft.SqlServer.Dts.Runtime;

namespace CrmComponents
{
    public partial class CrmConnectionEditor : Form {
        private List<KeyValuePair<int, string>> serviceTypes;
        private List<Tuple<int, ServiceTypeEnum, string>> connectionTypes;
        
        private List<Organization> organizations;
        private bool connectionValid = false;
        public Connection conn;
        private string prevConnString = "";

        List<string> onlineDiscoveries = new List<string>(new string[] {
            "https://disco.crm.dynamics.com",
            "https://disco.crm2.dynamics.com",
            "https://disco.crm3.dynamics.com",
            "https://disco.crm4.dynamics.com",
            "https://disco.crm5.dynamics.com",
            "https://disco.crm6.dynamics.com",
            "https://disco.crm7.dynamics.com",
            "https://disco.crm8.dynamics.com",
            "https://disco.crm9.dynamics.com",
            "https://disco.crm11.dynamics.com",
            "https://disco.crm.microsoftdynamics.de"
        });

        private ConnectionManager _connectionManager;
        public ConnectionManager ConnectionManager {
            get { return _connectionManager; }
            set { _connectionManager = value; }
        }

        // Setting and getting ServiceProvider
        private IServiceProvider _serviceProvider = null;
        public IServiceProvider ServiceProvider {
            get { return _serviceProvider; }
            set { _serviceProvider = value; }
        }

        private bool loaded = false;
        private int soapLast;
        private int restLast;

        public CrmConnectionEditor() {
            InitializeComponent();
            serviceTypes = Dictionaries.GetInstance().ServiceTypeNames.ToList();
            cmbSERV.DataSource = serviceTypes;
            cmbSERV.DisplayMember = "Value";
            cmbSERV.ValueMember = "Key";

            connectionTypes = Dictionaries.GetInstance().ConnectionTypeNames.ToList();
            cmbCONN.DataSource = connectionTypes;
            cmbCONN.DisplayMember = "Item3";
            cmbCONN.ValueMember = "Item1";

            soapLast = connectionTypes.First(n => n.Item2 == ServiceTypeEnum.SOAP).Item1;
            restLast = connectionTypes.First(n => n.Item2 == ServiceTypeEnum.REST).Item1;
            
        }

        public void Initialize(ConnectionManager connectionManager, IServiceProvider serviceProvider) {
            this._connectionManager = connectionManager;
            this._serviceProvider = serviceProvider;
        }

        private void CrmConnectionEditor_Load(object sender, EventArgs e) {
            string connectionString = ConnectionManager?.ConnectionString;
            conn = new Connection(connectionString);

            if(conn.ServiceType == ServiceTypeEnum.SOAP) {
                txtUN.Text = conn.Username ?? "";
                txtPSW.Text = conn.Password ?? "";
                txtDMN.Text = conn.Domain ?? "";
            }
            else if(conn.ConnectionType == ConnectionTypeEnum.Password) {
                txtUsr2.Text = conn.Username ?? "";
                txtPass2.Text = conn.Password ?? "";
            }
            else if(conn.ServiceType == ServiceTypeEnum.REST) {
                txtAuthSrv.Text = conn.AuthorizationServer ?? "https://login.microsoftonline.com/";
                txtAppId.Text = conn.ClientId ?? "";
                txtCrmSrv.Text = conn.ServerUrl ?? "";
                if(conn.ConnectionType == ConnectionTypeEnum.ClientCredentials) {
                    txtSecrt.Text = conn.ClientSecret ?? "";
                }
                else if(conn.ConnectionType == ConnectionTypeEnum.Certificate) {
                    txtCert.Text = conn.CertificateThumbprint ?? "";
                }
            }

            nudTimeout.Value = conn.TimeoutSec ?? 120;

            if(conn.Organization != null && conn.ServiceType == ServiceTypeEnum.SOAP) {
                cmbORG.DataSource = new List<Organization>(new Organization[]{ new Organization { FriendlyName = "", UniqueName = null }, conn.Organization});
                cmbORG.DisplayMember = "FriendlyName";
                cmbORG.ValueMember = "UniqueName";
                cmbORG.SelectedIndex = 1;
            }

            loaded = true;

            //ServiceType
            int servType = (int)conn.ServiceType;
            cmbSERV.SelectedIndex = serviceTypes.TakeWhile(n => n.Key != servType).Count();
            if(cmbSERV.SelectedIndex == 0) {
                cmbSERV_SelectedIndexChanged(null, null);
            }
            //ConnectionType
            int connType = (int)conn.ConnectionType;
            var ds = connectionTypes.Where(n => n.Item2 == conn.ServiceType).ToList();
            cmbCONN.SelectedIndex = ds.TakeWhile(n => n.Item1 != connType).Count();
            if(cmbCONN.SelectedIndex == 0) {
                cmbCONN_SelectedIndexChanged(null, null);
            }
            txtAuthSrv.Text = conn.AuthorizationServer ?? "https://login.microsoftonline.com/";

        }

        private void btnOK_Click(object sender, EventArgs e) {
            string connString = ConstructConnectionString();
            conn = new Connection(connString);
            if((conn.ServiceType == ServiceTypeEnum.SOAP || conn.ConnectionType == ConnectionTypeEnum.Password) && cmbORG.SelectedIndex == 0) {
                MessageBox.Show("Organization is not selected");
            }
            else if (connString != null) {
                CrmCommands comm = Connection.CrmCommandsFactory(conn);
                bool test = comm.WhoAmI();
                if(test) {
                    connString = conn.ConnectionString();
                    ConnectionManager.Properties["ConnectionString"].SetValue(_connectionManager, connString);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                } else {
                    MessageBox.Show("Can't connect");
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e) {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void cmbSERV_SelectedIndexChanged(object sender, EventArgs e) {
            if(loaded && cmbSERV.SelectedItem is KeyValuePair<int, string> servType) {
                if (cmbCONN.SelectedItem is Tuple<int, ServiceTypeEnum, string> connType) {
                    if (connType.Item2 == ServiceTypeEnum.SOAP) {
                        soapLast = connType.Item1;
                    }
                    else {
                        restLast = connType.Item1;
                    }
                }
                if (servType.Key == (int)ServiceTypeEnum.SOAP) {
                    pnlSoap.Visible = true;
                    pnlRest.Visible = false;
                    pnlCert.Visible = false;
                    pnlClient.Visible = false;
                    pnlPass.Visible = false;

                    var ds = connectionTypes.Where(n => n.Item2 == ServiceTypeEnum.SOAP).ToList();
                    cmbCONN.DataSource = ds;
                    cmbCONN.DisplayMember = "Item3";
                    cmbCONN.ValueMember = "Item1";
                    cmbCONN.SelectedIndex = ds.TakeWhile(n => n.Item1 != soapLast).Count();
                } else {
                    pnlSoap.Visible = false;
                    pnlRest.Visible = true;
                    var ds = connectionTypes.Where(n => n.Item2 == ServiceTypeEnum.REST).ToList();
                    cmbCONN.DataSource = ds;
                    cmbCONN.DisplayMember = "Item3";
                    cmbCONN.ValueMember = "Item1";
                    cmbCONN.SelectedIndex = ds.TakeWhile(n => n.Item1 != restLast).Count();
                }
            }
        }

        private void cmbCONN_SelectedIndexChanged(object sender, EventArgs e) {
            if (loaded && cmbCONN.SelectedItem is Tuple<int, ServiceTypeEnum, string> connType) {
                ServiceTypeEnum serviceTypeEnum = (ServiceTypeEnum)((cmbSERV.SelectedItem as KeyValuePair<int, string>?)?.Key ?? 0);
                var selectedKey = connType.Item1;
                txtDMN.Enabled = selectedKey == (int)ConnectionTypeEnum.Ad;

                if (selectedKey == (int)ConnectionTypeEnum.Online) {
                    cmbDisc.DataSource = onlineDiscoveries;
                    if (conn.DiscoveryService != null) {
                        cmbDisc.DataSource = onlineDiscoveries;
                        var index = onlineDiscoveries.IndexOf(conn.DiscoveryService.ToString().Replace("/XRMServices/2011/Discovery.svc", "").ToLower());
                        if (index >= 0) {
                            cmbDisc.SelectedIndex = index;
                        }
                        else {
                            cmbDisc.Text = conn.DiscoveryService?.ToString() ?? "";
                        }
                    }
                    pnlSoap.Visible = true;
                    pnlRest.Visible = false;
                }
                else if (selectedKey == (int)ConnectionTypeEnum.Password) {
                    pnlPass.Visible = true;
                    pnlClient.Visible = false;
                    pnlCert.Visible = false;
                    pnlSoap.Visible = false;
                    pnlRest.Visible = true;
                }
                else if(selectedKey == (int)ConnectionTypeEnum.Ad || selectedKey == (int)ConnectionTypeEnum.Adfs) {
                    cmbDisc.DataSource = null;
                    pnlSoap.Visible = true;
                    pnlRest.Visible = false;
                }
                else if(selectedKey == (int)ConnectionTypeEnum.ClientCredentials) {
                    pnlClient.Visible = true;
                    pnlCert.Visible = false;
                    pnlPass.Visible = false;
                    pnlSoap.Visible = false;
                    pnlRest.Visible = true;
                }
                else if(selectedKey == (int)ConnectionTypeEnum.Certificate) {
                    pnlCert.Visible = true;
                    pnlClient.Visible = false;
                    pnlPass.Visible = false;
                    pnlSoap.Visible = false;
                    pnlRest.Visible = true;
                }
            }
        }

        private void cmbORG_SelectedIndexChanged(object sender, EventArgs e) {
            if (cmbORG.SelectedItem is Organization item && item.UniqueName != null) {
                Organization oldOrganization = conn.Organization;
                conn.Organization = item;
                CrmCommands comm = Connection.CrmCommandsFactory(conn);
                if (!comm.CheckOrganizationUri()) {
                    MessageBox.Show("Can't connect to organization");
                }
            }
        }

        private void cmbORG_Click(object sender, EventArgs e) {
            string connString = ConstructConnectionString();
            conn = new Connection(connString);
            CrmCommands comm = Connection.CrmCommandsFactory(conn);
            try {
                List<Organization> orgs = new List<Organization> { new Organization { FriendlyName = "", UniqueName = null } };
                orgs.AddRange(comm.RetrieveOrganizations());

                cmbORG.DataSource = orgs;
                cmbORG.DisplayMember = "FriendlyName";
                cmbORG.ValueMember = "UniqueName";

                cmbORG.DroppedDown = true;
                cmbORG.SelectedIndex = -1;
                prevConnString = connString;
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        private string ConstructConnectionString() {
            string connectionString = null;
            var x = cmbCONN.SelectedItem as Tuple<int, ServiceTypeEnum, string>;
            ConnectionTypeEnum connectionTypeEnum = (ConnectionTypeEnum)x.Item1;
            ServiceTypeEnum serviceTypeEnum = (ServiceTypeEnum)((cmbSERV.SelectedItem as KeyValuePair<int, string>?)?.Key ?? 0);
            
            if(serviceTypeEnum == ServiceTypeEnum.SOAP) {
                if(!string.IsNullOrEmpty(cmbDisc.Text) && !string.IsNullOrEmpty(txtUN.Text) && !string.IsNullOrEmpty(txtPSW.Text)) {
                    if(connectionTypeEnum != ConnectionTypeEnum.Ad || (connectionTypeEnum == ConnectionTypeEnum.Ad && !string.IsNullOrEmpty(txtDMN.Text))) {
                        Organization selectedOrganization = cmbORG.SelectedItem as Organization;
                        string discoveryUri = cmbDisc.Text.Trim();
                        if(!discoveryUri.Contains("XRMServices/2011/Discovery.svc")) {
                            discoveryUri = discoveryUri.EndsWith("/") ? discoveryUri : discoveryUri + "/";
                            discoveryUri += "XRMServices/2011/Discovery.svc";
                        }

                        Connection conn = new Connection();
                        conn.ServiceType = ServiceTypeEnum.SOAP;
                        conn.ConnectionType = connectionTypeEnum;
                        conn.DiscoveryService = new Uri(discoveryUri);
                        conn.Organization = selectedOrganization;
                        conn.Username = txtUN.Text?.Trim();

                        conn.Domain = txtDMN.Text?.Trim();
                        conn.TimeoutSec = (int?)nudTimeout.Value;

                        if(connectionTypeEnum == ConnectionTypeEnum.Password) {
                            conn.Password = txtPass2.Text.Trim();
                        } else {
                            conn.Password = txtPSW.Text?.Trim();
                        }

                        connectionString = conn.ConnectionString();
                    }
                }
            }
            else if(serviceTypeEnum == ServiceTypeEnum.REST) {
                Connection conn = new Connection();
                conn.ServiceType = ServiceTypeEnum.REST;
                conn.ConnectionType = connectionTypeEnum;
                conn.AuthorizationServer = txtAuthSrv.Text.Trim();
                conn.ClientId = txtAppId.Text.Trim();
                conn.ServerUrl = txtCrmSrv.Text.Trim();
                conn.ClientSecret = txtSecrt.Text.Trim();
                conn.CertificateThumbprint = txtCert.Text.Trim();
                conn.Username = txtUsr2.Text.Trim();
                conn.Password = txtPass2.Text.Trim();
                conn.TimeoutSec = (int?)nudTimeout.Value;
                connectionString = conn.ConnectionString();
            }

            return connectionString;
        }

        private void label8_Click(object sender, EventArgs e) {

        }

        private void cmbDisc_SelectedIndexChanged(object sender, EventArgs e) {

        }

        private void txtUN_TextChanged(object sender, EventArgs e) {

        }

        private void btnTest_Click(object sender, EventArgs e) {
            string connString = ConstructConnectionString();
            conn = new Connection(connString);
            CrmCommands comm = Connection.CrmCommandsFactory(conn);
            bool test = comm.WhoAmI();
            string result = test ? "Succesfully connected" : "Error";
            MessageBox.Show(result, "Testing");
        }
    }
}
