using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace XpoClassGenerator.Module.BusinessObjects
{
    [DefaultClassOptions]
    //[ImageName("BO_Contact")]
    //[DefaultProperty("DisplayMemberNameForLookupEditorsOfThisType")]
    //[DefaultListViewOptions(MasterDetailMode.ListViewOnly, false, NewItemRowPosition.None)]
    //[Persistent("DatabaseTableName")]
    // Specify more UI options using a declarative approach (https://documentation.devexpress.com/#eXpressAppFramework/CustomDocument112701).
    public class PersistentObjects : BaseObject
    { // Inherit from a different class to provide a custom primary key, concurrency and deletion behavior, etc. (https://documentation.devexpress.com/eXpressAppFramework/CustomDocument113146.aspx).
        // Use CodeRush to create XPO classes and properties with a few keystrokes.
        // https://docs.devexpress.com/CodeRushForRoslyn/118557
        public PersistentObjects(Session session)
            : base(session)
        {
        }
        public override void AfterConstruction()
        {
            base.AfterConstruction();
            this.ClassTemplate = GetEmbeddedResourceText("XpoClassGenerator.Module.BusinessObjects.ClassTemplate.txt");
            this.PropertyTemplate = GetEmbeddedResourceText("XpoClassGenerator.Module.BusinessObjects.PersistentPropertyTemplate.txt");
            // Place your initialization code here (https://documentation.devexpress.com/eXpressAppFramework/CustomDocument112834.aspx).
        }
        public static string GetEmbeddedResourceText(string resourcePath)
        {
            // Get the assembly where the resource is embedded
            Assembly assembly = Assembly.GetExecutingAssembly();

            // Access the resource stream
            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException("Could not find embedded resource");
                }

                // Read the stream content
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
        string propertyTemplate;
        string classTemplate;
        string connectionString;
        FileData classes;
        string nameSpace;

        [Size(SizeAttribute.DefaultStringMappingFieldSize)]
        public string NameSpace
        {
            get => nameSpace;
            set => SetPropertyValue(nameof(NameSpace), ref nameSpace, value);
        }

        [Size(SizeAttribute.Unlimited)]
        public string ConnectionString
        {
            get => connectionString;
            set => SetPropertyValue(nameof(ConnectionString), ref connectionString, value);
        }

        public FileData Classes
        {
            get => classes;
            set => SetPropertyValue(nameof(Classes), ref classes, value);
        }

        [Size(SizeAttribute.Unlimited)]
        public string ClassTemplate
        {
            get => classTemplate;
            set => SetPropertyValue(nameof(ClassTemplate), ref classTemplate, value);
        }
        
        [Size(SizeAttribute.Unlimited)]
        public string PropertyTemplate
        {
            get => propertyTemplate;
            set => SetPropertyValue(nameof(PropertyTemplate), ref propertyTemplate, value);
        }
        public string GeneratePersistentClasses(IEnumerable<DBTable> tables, string classTemplate, string propertyTemplate, string nameSpace)
        {
            StringBuilder classesText = new StringBuilder();

            foreach (var table in tables)
            {
                // Replace placeholders in class template
                string classText = classTemplate
                    .Replace("%NameSpace%", nameSpace)
                    .Replace("%ClassName%", table.Name)
                    .Replace("//[Persistent(\"DatabaseTableName\")]", $"[Persistent(\"{table.Name}\")]"); // Uncomment and set table name

                StringBuilder propertiesText = new StringBuilder();

                foreach (var column in table.Columns)
                {
                    // Adjust property type based on the column type, you might need to extend this part
                    string propertyType = MapColumnTypeToPropertyType(column.ColumnType);

                    // Replace placeholders in property template
                    string property = propertyTemplate
                        .Replace("%Property%", column.Name)
                        .Replace("%Field%", "_" + column.Name.ToLower())
                        .Replace("private string", $"private {propertyType}") // Set correct property type
                        .Replace("public string", $"public {propertyType}") // Set correct property type
                        .Replace("DatabaseColumnName", column.Name);

                    propertiesText.AppendLine(property);
                }

                classText = classText.Replace("     }", propertiesText.ToString() + "     }"); // Insert properties before closing brace of the class
                classesText.AppendLine(classText);
            }

            return classesText.ToString();
        }

        private static string MapColumnTypeToPropertyType(DBColumnType columnType)
        {
            // This is a very basic mapping, extend according to your needs
            switch (columnType)
            {
                case DBColumnType.Int32:
                    return "int";
                case DBColumnType.String:
                    return "string";
                // Add more mappings as required
                default:
                    return "string"; // Default to string if type is unknown
            }
        }

    }
}