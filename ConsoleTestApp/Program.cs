using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrmComponents;
using CrmComponents.Model;
using Microsoft.SqlServer.Dts.Runtime;
using Microsoft.SqlServer.Dts.Pipeline;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;

namespace ConsoleTestApp
{
    class Program
    {
        static void Main(string[] args) {

            while(true) {
                string pressedKey = Console.ReadKey().KeyChar.ToString(); //.ToString();
                int selectedCase = 0;
                int.TryParse(pressedKey, out selectedCase);

                switch(selectedCase) {
                    case 1:
                        PackageSimulation p1 = new PackageSimulation();
                        p1.LoadSimulationSource();
                        break;
                    case 2:
                        PackageSimulation p2 = new PackageSimulation();
                        p2.LoadSimulationDestination();
                        break;
                    case 3: //load package with destination
                        PackageSimulation p3 = new PackageSimulation();
                        p3.LoadConnection();
                        break;
                    case 5: //create package
                        PackageSimulation p4 = new PackageSimulation();
                        p4.Create();
                        break;
                    default:
                        break;
                }
            }
            //ConnectionManager m = null;

            //CrmConnectionEditor editorForm = new CrmConnectionEditor();
            //editorForm.Initialize(m, null);
            //editorForm.ShowDialog();
            //var o = editorForm.conn;

            //SourceComponentEditor sourceEditorForm = new SourceComponentEditor(null, null, null);
            //sourceEditorForm.ShowDialog();


            //Package package = new Package();
            //Executable exec1 = package.Executables.Add("STOCK:PipelineTask");
            //TaskHost thMainPipe = exec1 as TaskHost;
            //MainPipe dataFlowTask = thMainPipe.InnerObject as MainPipe;

            //string pkg = @"D:\ssis components\Integration Services Project5\Integration Services Project5\Package.dtsx";
            //Application app = new Application();
            //Package p = app.LoadPackage(pkg, null);

            //TasksTest tt = new TasksTest();
            //tt.Main(3);
        }
    }
}
