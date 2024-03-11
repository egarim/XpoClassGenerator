using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Layout;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Templates;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo.DB;
using DevExpress.Xpo.Metadata;
using DevExpress.Xpo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DevExpress.XtraRichEdit.Model.History;
using System.ServiceModel.Channels;
using XpoClassGenerator.Module.BusinessObjects;

namespace XpoClassGenerator.Module.Controllers
{
    // For more typical usage scenarios, be sure to check out https://documentation.devexpress.com/eXpressAppFramework/clsDevExpressExpressAppViewControllertopic.aspx.
    public partial class GenerateXpoClassesController : ObjectViewController<DetailView, PersistentObjects>
    {
        SimpleAction GenerateClasses;
  
        protected ConnectionProviderSql sqlProvider;
        protected IDataStore xpoProvider;
        protected ReflectionDictionary reflectionDictionary;
        // Use CodeRush to create Controllers and Actions with a few keystrokes.
        // https://docs.devexpress.com/CodeRushForRoslyn/403133/
        public GenerateXpoClassesController()
        {
            InitializeComponent();
          


            GenerateClasses = new SimpleAction(this, "GenerateClasses", "View");
            GenerateClasses.Execute += GenerateClasses_Execute;
            
            // Create some items
            //SelectTableAction.Items.Add(new ChoiceActionItem("MyItem1", "My Item 1", 1));

            // Target required Views (via the TargetXXX properties) and create their Actions.
        }
        private void GenerateClasses_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var CurrentPersistentObjects = this.View.CurrentObject as PersistentObjects;
            xpoProvider = XpoDefault.GetConnectionProvider("Integrated Security=SSPI;Pooling=false;Data Source=(localdb)\\mssqllocaldb;Initial Catalog=XafFlechaAmarillaSecurity", AutoCreateOption.SchemaAlreadyExists);
            reflectionDictionary = new ReflectionDictionary();
            sqlProvider = xpoProvider as ConnectionProviderSql;
            var Tables = sqlProvider.GetStorageTablesList(false);

            var DbTables = sqlProvider.GetStorageTables(Tables);
            foreach (var item in DbTables)
            {

                var ClassText=CurrentPersistentObjects.GeneratePersistentClasses(new List<DBTable>() { item }, CurrentPersistentObjects.ClassTemplate, CurrentPersistentObjects.PropertyTemplate, CurrentPersistentObjects.NameSpace);
                //AddClass(reflectionDictionary, item);
            }
        }

            // Execute your business logic (https://docs.devexpress.com/eXpressAppFramework/112737/).
     
    
        protected override void OnActivated()
        {
            base.OnActivated();


          


            // Perform various tasks depending on the target View.
        }
        public static XPClassInfo AddClass(XPDictionary dict, DBTable table)
        {
            if (table.PrimaryKey.Columns.Count > 1)
                throw new NotSupportedException("Compound primary keys are not supported");
            XPClassInfo classInfo = dict.CreateClass(dict.GetClassInfo(typeof(XPLiteObject)), table.Name.Replace('.', '_'));
            classInfo.AddAttribute(new PersistentAttribute(table.Name));
            DBColumnType primaryColumnType = table.GetColumn(table.PrimaryKey.Columns[0]).ColumnType;
            classInfo.CreateMember(table.PrimaryKey.Columns[0], DBColumn.GetType(primaryColumnType),
                new KeyAttribute(IsAutoGenerationSupported(primaryColumnType)));
            foreach (DBColumn col in table.Columns)
                if (!col.IsKey)
                    classInfo.CreateMember(col.Name, DBColumn.GetType(col.ColumnType));
            return classInfo;
        }
        static bool IsAutoGenerationSupported(DBColumnType type)
        {
            return type == DBColumnType.Guid || type == DBColumnType.Int16 || type == DBColumnType.Int32;
        }
        protected override void OnViewControlsCreated()
        {
            base.OnViewControlsCreated();
            // Access and customize the target View control.
        }
        protected override void OnDeactivated()
        {
            // Unsubscribe from previously subscribed events and release other references and resources.
            base.OnDeactivated();
        }
    }
}
