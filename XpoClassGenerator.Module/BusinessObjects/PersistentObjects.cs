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
using System.IO.Compression;
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
        static bool IsAutoGenerationSupported(DBColumnType type)
        {
            return type == DBColumnType.Guid || type == DBColumnType.Int16 || type == DBColumnType.Int32;
        }
        public string GeneratePersistentClasses(IEnumerable<DBTable> tables, string classTemplate, string propertyTemplate, string nameSpace)
        {
            StringBuilder classesText = new StringBuilder();

            var KeyTemplate = PersistentObjects.GetEmbeddedResourceText("XpoClassGenerator.Module.BusinessObjects.KeyTemplate.txt");

            foreach (var table in tables)
            {
                // Replace placeholders in class template
                string classText = classTemplate
                    .Replace("%NameSpace%", nameSpace)
                    .Replace("%ClassName%", table.Name)
                    .Replace("//[Persistent(\"DatabaseTableName\")]", $"[Persistent(\"{table.Name}\")]"); // Uncomment and set table name

                StringBuilder propertiesText = new StringBuilder();


                DBColumnType primaryColumnType = table.GetColumn(table.PrimaryKey.Columns[0]).ColumnType;

                string PrimaryKeyProperty = GeneratePropertyCore(KeyTemplate, table.Columns.FirstOrDefault(c=>c.Name== table.PrimaryKey.Columns[0]));
                PrimaryKeyProperty = PrimaryKeyProperty.Replace("%AutoGenerated%", IsAutoGenerationSupported(primaryColumnType).ToString().ToLower());
                propertiesText.AppendLine(PrimaryKeyProperty);
                foreach (var column in table.Columns)
                {
                    if (column.IsKey)
                        continue;

                    string property = GeneratePropertyCore(propertyTemplate, column);

                    propertiesText.AppendLine(property);
                }

                classText = classText.Replace("%Properties%", propertiesText.ToString()); // Insert properties before closing brace of the class
                classesText.AppendLine(classText);
            }

            return classesText.ToString();
        }

        private static string GeneratePropertyCore(string propertyTemplate, DBColumn column)
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
            return property;
        }

        public static byte[] CreateZipFromDictionary(Dictionary<string, string> files)
        {
            using (var memoryStream = new MemoryStream())
            {
                // Create a new zip archive in memory stream
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var fileEntry in files)
                    {
                        // Create a new entry for each file
                        var entry = archive.CreateEntry(fileEntry.Key, CompressionLevel.Optimal);

                        // Write the file content to the entry
                        using (var entryStream = entry.Open())
                        using (var streamWriter = new StreamWriter(entryStream))
                        {
                            streamWriter.Write(fileEntry.Value);
                        }
                    }
                }
                // It's important to reset the position of the MemoryStream to the beginning
                memoryStream.Position = 0;

                // Read the memory stream and return the byte array
                return memoryStream.ToArray();
            }
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
                case DBColumnType.DateTime:
                    return nameof(DateTime);
                case DBColumnType.Decimal:
                    return "decimal";
                case DBColumnType.Double:
                    return "double";
                case DBColumnType.Boolean:
                    return "bool";
                case DBColumnType.Byte:
                    return "byte";
                case DBColumnType.ByteArray:
                    return "byte[]";
                case DBColumnType.Guid:
                    return nameof(Guid);
                // Add more mappings as required
                default:
                    return "string"; // Default to string if type is unknown
            }
        }

    }
}