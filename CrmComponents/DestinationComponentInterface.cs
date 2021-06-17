using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.SqlServer.Dts.Pipeline.Design;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Microsoft.SqlServer.Dts.Runtime;
using Microsoft.SqlServer.Dts.Runtime.Design;

namespace CrmComponents
{
    public class DestinationComponentInterface : IDtsComponentUI
    {
        IDTSComponentMetaData100 md;
        IServiceProvider sp;
        private IDtsConnectionService cs;

        public void Help(System.Windows.Forms.IWin32Window parentWindow) {
        }

        public void New(System.Windows.Forms.IWin32Window parentWindow) {
        }

        public void Delete(System.Windows.Forms.IWin32Window parentWindow) {
        }

        public bool Edit(System.Windows.Forms.IWin32Window parentWindow, Variables vars, Connections cons) {
            bool dialogResult = false;
            DestinationComponentEditor componentEditor = new DestinationComponentEditor(cons, vars, md,sp);
            DialogResult result = componentEditor.ShowDialog(parentWindow);
            if (result == DialogResult.OK) {
                dialogResult = true;
            }

            return dialogResult;
        }

        public void Initialize(IDTSComponentMetaData100 dtsComponentMetadata, IServiceProvider serviceProvider) {
            // Store the component metadata.  
            this.md = dtsComponentMetadata;
            this.sp = serviceProvider;
            this.cs = serviceProvider.GetService(typeof(IDtsConnectionService)) as IDtsConnectionService;
        }
    }
}
