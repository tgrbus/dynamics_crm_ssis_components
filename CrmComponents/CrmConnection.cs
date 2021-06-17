using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using CrmComponents.Model;
using CrmComponents.Soap;
using Microsoft.SqlServer.Dts.Runtime;

namespace CrmComponents
{
    [DtsConnection(ConnectionType = "CrmConnection", DisplayName = "Crm Connection", ConnectionContact = "TestComponent", Description = "Svasta",
        /*IconResource = "CrmComponents.Resources.Icon1.ico", */
        UITypeName = "CrmComponents.CrmConnectionInterface, CrmComponents, Version=1.2014.12.0, Culture=neutral, PublicKeyToken=81423fe9ba0539ea",
        LocalizationType = typeof(CrmConnection)),
    EditorBrowsable(EditorBrowsableState.Never)]
    public class CrmConnection : ConnectionManagerBase, IDTSComponentPersist {
        #region Variables for internal use
        // The template for the connectionstring, but without the sensitive password property
        private const string CONNECTIONSTRING_TEMPLATE = "Url=<Url>;UserName=<UserName>;Password=<Password>";
        #endregion

        #region Get Set Properties
        /*
         * The properties of my connection manager that
         * will be saved in the XML of the SSIS package.
         * You can add a Category and Description
         * for each property making it clearer.
         */
        private string _connectionString = String.Empty;
        private string _password = String.Empty;
        public override string ConnectionString {
            get {
                return _connectionString;
            }
            //connectionstring is now readonly
            set {
                _connectionString = value;
            }
        }



        //private string _url = String.Empty;
        //[Category("my connection manager")]
        //[Description("Some URL to do something with in an other task or transformation")]
        //public string Url
        //{
        //    get { return this._url; }
        //    set { this._url = value; }
        //}

        //private string _userName = String.Empty;
        //[CategoryAttribute("my connection manager")]
        //[Description("The username needed to access the url")]
        //public string UserName
        //{
        //    get { return this._userName; }
        //    set { this._userName = value; }
        //}

        //// Notice the password property to hide the chars
        //private string _password = String.Empty;
        //[CategoryAttribute("my connection manager")]
        //[Description("The secret password")]
        //[PasswordPropertyText(true)]
        //public string Password
        //{
        //    get { return this._password; }
        //    set { this._password = value; }
        //}
        #endregion

        #region Overriden methods
        //public override object AcquireConnection(object txn)
        //{
        //    // Set the connectionstring
        //    UpdateConnectionString();
        //    return base.AcquireConnection(txn);
        //}

        public override void ReleaseConnection(object connection)
        {
            base.ReleaseConnection(connection);
        }

        public override DTSExecResult Validate(IDTSInfoEvents infoEvents) {
            // Very basic validation example:
            // Check if the URL field is filled.
            // Note: this is a runtime validation
            // In the form you can add some more
            // designtime validation.
            if (string.IsNullOrEmpty(ConnectionString)) {
                infoEvents.FireError(0, "My Custom Connection Manager", "There is no connection string!", string.Empty, 0);
                return DTSExecResult.Failure;
            }
            else {
                return DTSExecResult.Success;
            }
        }
        #endregion

        #region Update ConnectionString
        //private void UpdateConnectionString()
        //{
        //    // Create a connectionstring, but without sensitive properties like the password
        //    String connectionString = CONNECTIONSTRING_TEMPLATE;

        //    //connectionString = connectionString.Replace("<Url>", Url);
        //    //connectionString = connectionString.Replace("<UserName>", UserName);
        //    //connectionString = connectionString.Replace("<Password>", Password);

        //    _connectionString = connectionString;
        //    //ConnectionString = connectionString;
        //}
        #endregion

        #region Methods for IDTSComponentPersist
        // These two methods are for saving the data in the package XML without showing sensitive data
        void IDTSComponentPersist.LoadFromXML(System.Xml.XmlElement node, IDTSInfoEvents infoEvents)
        {
            //    // Checking if XML is correct. This might occur if the connection manager XML has been modified outside BIDS/SSDT
            //if (!node.Name.EndsWith("CrmConnectionManager"))
            //{
            //    throw new Exception(string.Format("Unexpected connectionmanager element when loading task - {0}.", "MYCONNECTIONMANAGER"));
            //}
            //    else
            //    {
            //        // Fill properties with values from package XML
            //        this._userName = node.Attributes.GetNamedItem("UserName").Value;
            //        this._url = node.Attributes.GetNamedItem("Url").Value;


            //        foreach (XmlNode childNode in node.ChildNodes)
            //        {
            //            if (childNode.Name == "Password")
            //            {
            //                this._password = childNode.InnerText;
            //            }
            //        }
            //base.Loa
            this._connectionString = node.Attributes.GetNamedItem("CrmConnectionString").Value;
        //    }
        }

        void IDTSComponentPersist.SaveToXML(System.Xml.XmlDocument doc, IDTSInfoEvents infoEvents)
        {
                XmlElement rootElement = doc.CreateElement("CrmConnectionManager");
            //rootElement.Prefix = "DTS";
                doc.AppendChild(rootElement);

            XmlAttribute connectionStringAttr = doc.CreateAttribute("CrmConnectionString");
            //connectionStringAttr.Prefix = "DTS";
            connectionStringAttr.Value = _connectionString;
            rootElement.Attributes.Append(connectionStringAttr);

            //    XmlAttribute userNameStringAttr = doc.CreateAttribute("UserName");
            //    userNameStringAttr.Value = _userName;
            //    rootElement.Attributes.Append(userNameStringAttr);

            //    XmlAttribute urlStringAttr = doc.CreateAttribute("Url");
            //    urlStringAttr.Value = _url;
            //    rootElement.Attributes.Append(urlStringAttr);

            //    if (!string.IsNullOrEmpty(_password))
            //    {
            //        XmlElement passwordElement = doc.CreateElement("Password");
            //        rootElement.AppendChild(passwordElement);
            //        passwordElement.InnerText = _password;

            //        // This will make the password property sensitive
            //        XmlAttribute passwordAttr = doc.CreateAttribute("Sensitive");
            //        passwordAttr.Value = "1";
            //        passwordElement.Attributes.Append(passwordAttr);
            //    }
        }
        #endregion

        /*public override void Update(ref string ObjectXml) {
            //base.Update(ref ObjectXml);
        }*/

        public override bool CanUpdate(string CreationName) {
            return base.CanUpdate(CreationName);
            return true;
        }
    }
}
