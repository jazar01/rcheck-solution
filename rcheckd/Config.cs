using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using YamlDotNet.Serialization;

namespace rcheckd
{
    public class Config
    {
        public string Logfile { get; set; }
        public bool Debug { get; set; }
        public List<FileRecord> Files { get; set; }

        // public Config() { }   /* YamlDotNet requires a constructor with no parameters */

        /// <summary>
        /// Deserialze configuration data from a yaml file
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Config GetYaml(string path)
        {
            string yml = File.ReadAllText(path);
            var deserializer = new DeserializerBuilder()
                 .IgnoreUnmatchedProperties()
                 .Build();
            Config c = deserializer.Deserialize<Config>(yml);
            return c;
        }

        /// <summary>
        /// formats the confuration data as a string
        /// useful for logging and debugging
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Rcheckd configuration: Logfile: " + Logfile + " Debug: " + Debug + "\nFiles:\n");
            foreach (FileRecord fr in Files)
            {
                sb.Append("Path=" + fr.Path);
                PropertyInfo[] properties = typeof(FileRecord).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (PropertyInfo property in properties)
                    if (property.Name != "Path")
                    {
                        var value = property.GetValue(fr);
                        if (value != null)
                            sb.Append("\n\t" + property.Name + "=" + value);
                    }
                sb.Append("\n");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets a property value from a file record in the configuration files
        /// </summary>
        /// <param name="file"></param>
        /// <param name="Parameter"></param>
        /// <returns>the value of the parameter or an empty string if not found</returns>
        public string GetFileParameter(string file, string Parameter )
        {
            try
            {
                FileRecord f = this.Files.Find(x => x.Path == file);
                var value = typeof(FileRecord).GetProperty(Parameter).GetValue(f);
                return value.ToString();
            }
            catch (Exception)
            {
                return "";  // if the property or value does not exist, we don't care, return nothing.
            }
        }
        public bool HasAction(string file)
        {
            try
            {
                FileRecord f = this.Files.Find(x => x.Path == file);
                var value = typeof(FileRecord).GetProperty("Action_Command").GetValue(f);

                return !string.IsNullOrEmpty(((string)value));
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool IsSentinal(string file)
        {
            try
            {
                FileRecord f = this.Files.Find(x => x.Path == file);
                var value = typeof(FileRecord).GetProperty("Sentinal").GetValue(f);
                return (bool) value;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public class FileRecord
    {
        public string Path { get; set; }
        public string Length { get; set; }
        public bool Sentinal { get; set; }
        public string Action_Command { get; set; }
        public string Action_Directory { get; set; }
        public string Action_Parameters { get; set; }
        public string Action_MaxWaitTime { get; set; }

        // note: this is a method not a property to avoid screwing up an serialize/deserialize stuff
        public int GetLength()
        { 
            if (string.IsNullOrEmpty(Length))
                return 0;
            else
                return BytesConvert.ToInt(Length);
        }

    }

}
