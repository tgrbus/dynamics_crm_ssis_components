using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.SqlServer.Dts.Runtime;
using Microsoft.SqlServer.Dts.Runtime.Design;

namespace CrmComponents
{
    public class CrmConnectionInterface : IDtsConnectionManagerUI
    {
        private ConnectionManager _connectionManager;
        private IServiceProvider _serviceProvider;

        public CrmConnectionInterface() {

        }

        public void Initialize(ConnectionManager connectionManager, IServiceProvider serviceProvider) {
            this._connectionManager = connectionManager;
            this._serviceProvider = serviceProvider;
        }


        public ContainerControl GetView() {
            CrmConnectionEditor editor = new CrmConnectionEditor();
            editor.ConnectionManager = this._connectionManager;
            editor.ServiceProvider = this._serviceProvider;
            return editor;
        }

        public void Delete(IWin32Window parentWindow) {
        }

        public bool Edit(System.Windows.Forms.IWin32Window parentWindow, Microsoft.SqlServer.Dts.Runtime.Connections connections, ConnectionManagerUIArgs connectionUIArg) {
            return EditConnection(parentWindow);
        }

        public bool New(System.Windows.Forms.IWin32Window parentWindow, Microsoft.SqlServer.Dts.Runtime.Connections connections, Microsoft.SqlServer.Dts.Runtime.Design.ConnectionManagerUIArgs connectionUIArg) {
            return EditConnection(parentWindow);
        }

        private bool EditConnection(IWin32Window parentWindow) {
            CrmConnectionEditor editor = new CrmConnectionEditor();

            editor.Initialize(_connectionManager, this._serviceProvider);
            return editor.ShowDialog(parentWindow) == DialogResult.OK;
        }
    }
}
