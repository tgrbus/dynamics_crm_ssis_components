using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.SqlServer.Dts.Pipeline.Design;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Microsoft.SqlServer.Dts.Runtime;

namespace CrmComponents
{
    public class SourceComponentInterface : IDtsComponentUI {
        IDTSComponentMetaData100 md;
        IServiceProvider sp;

        public void Help(System.Windows.Forms.IWin32Window parentWindow) {
        }

        public void New(System.Windows.Forms.IWin32Window parentWindow) {
        }

        public void Delete(System.Windows.Forms.IWin32Window parentWindow) {
        }

        public bool Edit(System.Windows.Forms.IWin32Window parentWindow, Variables vars, Connections cons) {
            // Create and display the form for the user interface.  
            SourceComponentEditor componentEditor = new SourceComponentEditor(cons, vars, md, sp);

            DialogResult result = componentEditor.ShowDialog(parentWindow);

            if (result == DialogResult.OK)
                return true;

            return false;
        }

        public void Initialize(IDTSComponentMetaData100 dtsComponentMetadata, IServiceProvider serviceProvider) {
            // Store the component metadata.  
            this.md = dtsComponentMetadata;
            this.sp = serviceProvider;
        }
    }
}
