using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using CrmComponents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SqlServer.Dts.Runtime;
using Microsoft.SqlServer.Dts.Pipeline;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;

using Microsoft.SqlServer.Dts.Runtime.Design;

using Application = Microsoft.SqlServer.Dts.Runtime.Application;

namespace ConsoleTestApp
{
    class PackageSimulation
    {
        //doesn't work
        public void Create() {
            Package package = new Package();
            Executable exec1 = package.Executables.Add("STOCK:PipelineTask");
            //Executable exec = package.Executables.Add(
            //    typeof(Microsoft.SqlServer.Dts.Tasks.ExecuteSQLTask.ExecuteSQLTask).AssemblyQualifiedName);
            TaskHost thMainPipe = exec1 as TaskHost;
            MainPipe dataFlowTask = thMainPipe.InnerObject as MainPipe;
            //
            ConnectionManager cm = package.Connections.Add("CrmConnection");
            

            //
            Application app = new Application();
            IDTSComponentMetaData100 component1 = dataFlowTask.ComponentMetaDataCollection.New();
            component1.Name = "CRM Source";
            component1.ComponentClassID = app.PipelineComponentInfos["CRM Source"].CreationName;
            IDTSComponentMetaData100 component2 = dataFlowTask.ComponentMetaDataCollection.New();
            component2.Name = "CRM Source";
            component2.ComponentClassID = typeof(CrmComponents.SourceComponent).AssemblyQualifiedName;
            CManagedComponentWrapper instance = component2.Instantiate();
            instance.ProvideComponentProperties();
            component2.RuntimeConnectionCollection[0].ConnectionManager = DtsConvert.GetExtendedInterface(package.Connections[0]);
            component2.RuntimeConnectionCollection[0].ConnectionManagerID = package.Connections[0].ID;

            Form f = new Form();
            //SourceComponentEditor componentEditor = new SourceComponentEditor(package.Connections, null, component2);
            //componentEditor.ShowDialog(f);

            var zz = 1;
        }


        /*public void LoadPackage() {
         
            string pkg = @"D:\ssis components\Integration Services Project5\Integration Services Project5\Simulator.dtsx";
            //string pkg = @"D:\ssis components\Integration Services Project5\Integration Services Project5\Source.dtsx";
            Application app = new Application();
            

            Package package = app.LoadPackage(pkg, null);
            Executable exec1 = package.Executables[0];
            TaskHost thMainPipe = exec1 as TaskHost;
            MainPipe dataFlowTask = thMainPipe.InnerObject as MainPipe;
            IDTSComponentMetaData100 component1 = dataFlowTask.ComponentMetaDataCollection[0];

            var vars = package.Variables;
            var serviceCollection =  new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            var iserviceProvider = serviceCollection.BuildServiceProvider() as System.IServiceProvider;
            var conn = package.Connections[0];
            Thread t = new Thread((ThreadStart)(() => {
                Form f = new Form();
                CrmComponents.DestinationComponentEditor componentEditor = new CrmComponents.DestinationComponentEditor(package.Connections, vars, component1, iserviceProvider);
                componentEditor.ShowDialog(f);
                CrmComponents.CrmConnectionEditor connectionEditor = new CrmConnectionEditor();
                connectionEditor.Initialize(conn, iserviceProvider);
                connectionEditor.ShowDialog();
            }));
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();

            

            //app.SaveToXml("Simulator.dtsx", package, null);

            var aa = 0;
        }*/

        public void LoadConnection() {
            string relativePath = @"..\..\..\SsisProject\Source.dtsx";
            string absolutePath = Path.GetFullPath(relativePath);

            Application app = new Application();
            Package package = app.LoadPackage(absolutePath, null);
            Executable exec1 = package.Executables[0];
            TaskHost thMainPipe = exec1 as TaskHost;
            MainPipe dataFlowTask = thMainPipe.InnerObject as MainPipe;
            IDTSComponentMetaData100 component1 = dataFlowTask.ComponentMetaDataCollection[0];
            var variables = thMainPipe.Variables;
            var serviceCollection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            var iserviceProvider = serviceCollection.BuildServiceProvider() as System.IServiceProvider;


            Thread t = new Thread((ThreadStart)(() => {
                Form f = new Form();
                CrmConnectionEditor connEditor = new CrmConnectionEditor();
                connEditor.Initialize(package.Connections[0], iserviceProvider);
                connEditor.ShowDialog(f);
            }));
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
        }

        public void LoadSimulationSource()         {
            string relativePath = @"..\..\..\SsisProject\Source.dtsx";
            string absolutePath = Path.GetFullPath(relativePath);
            
            Application app = new Application();
            Package package = app.LoadPackage(absolutePath, null);
            Executable exec1 = package.Executables[0];
            TaskHost thMainPipe = exec1 as TaskHost;
            MainPipe dataFlowTask = thMainPipe.InnerObject as MainPipe;
            IDTSComponentMetaData100 component1 = dataFlowTask.ComponentMetaDataCollection[0];
            var variables = thMainPipe.Variables;
            var serviceCollection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            var iserviceProvider = serviceCollection.BuildServiceProvider() as System.IServiceProvider;

            //Form f = new Form();
            //SourceComponentEditor componentEditor = new SourceComponentEditor(package.Connections, variables, component1);
            //componentEditor.ShowDialog(f);
            Thread t = new Thread((ThreadStart)(() => {
                Form f = new Form();
                SourceComponentEditor componentEditor = new SourceComponentEditor(package.Connections, variables, component1, iserviceProvider);
                componentEditor.ShowDialog(f);
            }));
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();

            app.SaveToXml(absolutePath, package, null);
            var cc = 1;
        }

        public void LoadSimulationDestination() {
            string relativePath = @"..\..\..\SsisProject\Destination.dtsx";
            string absolutePath = Path.GetFullPath(relativePath);
            //string absolutePath = @"D:\ssis components\Integration Services Project20\Integration Services Project20\Package.dtsx";
            Application app = new Application();
            Package package = app.LoadPackage(absolutePath, null);
            Executable exec1 = package.Executables[0];
            TaskHost thMainPipe = exec1 as TaskHost;
            MainPipe dataFlowTask = thMainPipe.InnerObject as MainPipe;
            IDTSComponentMetaData100 component1 = dataFlowTask.ComponentMetaDataCollection[0];
            var serviceCollection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            var iserviceProvider = serviceCollection.BuildServiceProvider() as System.IServiceProvider;

            Thread t = new Thread((ThreadStart)(() => {
                Form f = new Form();
                DestinationComponentEditor componentEditor = new DestinationComponentEditor(package.Connections, null, component1, iserviceProvider);
                componentEditor.ShowDialog(f);
                /*CrmConnectionEditor connEditor = new CrmConnectionEditor();
                connEditor.Initialize(package.Connections[0], iserviceProvider);
                connEditor.ShowDialog();*/
            }));
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();

            app.SaveToXml(absolutePath, package, null);
            var cc = 1;
        }

        //doesn't work
        public void RunSimulationDestination() {
            string relativePath = @"D:\ssis components\Integration Services Project18\Integration Services Project18\Package.dtsx";
            string absolutePath = Path.GetFullPath(relativePath);
            MyEventListener eventListener = new MyEventListener();
            Application app = new Application();
            Package package = app.LoadPackage(absolutePath, eventListener);
            Variables vars = null;
            VariableDispenser dispenzer = package.VariableDispenser;
            dispenzer.GetVariables(ref vars);

            Executable exec1 = package.Executables[0];
            TaskHost thMainPipe = exec1 as TaskHost;
            MainPipe dataFlowTask = thMainPipe.InnerObject as MainPipe;
            IDTSComponentMetaData100 component1 = dataFlowTask.ComponentMetaDataCollection[0];

            //dataFlowTask.ComponentMetaDataCollection.RemoveObjectByID(component1.ID);
            //DestinationComponent destinationComponent = new DestinationComponent();
            //component1 = destinationComponent.ComponentMetaData;

            DTSExecResult pkgResult = package.Execute(package.Connections, vars, null, null, null);

            var xx = 0;
        }
    }

    class MyEventListener : Microsoft.SqlServer.Dts.Runtime.DefaultEvents {
        public override void OnProgress(TaskHost taskHost, string progressDescription, int percentComplete, int progressCountLow, int progressCountHigh, string subComponent, ref bool fireAgain)
        {
            base.OnProgress(taskHost, progressDescription, percentComplete, progressCountLow, progressCountHigh, subComponent, ref fireAgain);
            MainPipe dataFlowTask = taskHost.InnerObject as MainPipe;
            //IDTSComponentMetaData100 component1 = dataFlowTask.ComponentMetaDataCollection[0];
            //component1.
        }

        public override void OnExecutionStatusChanged(Executable exec, DTSExecStatus newStatus, ref bool fireAgain) {
            var zz = 1;
        }
    }
}
