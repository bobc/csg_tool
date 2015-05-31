using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
//using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

using RMC;
using CadCommon;

namespace TestCsg
{
    public class ProjectSettings
    {
        [XmlIgnore]
        public string ProjectFileName;

        // filename, material, units, scale
        public List<FileProperties> fileList;

        public string MaterialsFileName;


        [XmlIgnore]
        public List<FileBase> Files;

        [XmlIgnore]
        public List<MaterialProperties> Materials;

        [XmlIgnore]
        public MaterialProperties DefaultMaterial;

        public ProjectSettings()
        {
            fileList = new List<FileProperties>();
            Files = new List<FileBase>();

            Materials = new List<MaterialProperties>();
            DefaultMaterial = new MaterialProperties();
        }

        public int FindFileIndex (FileBase file)
        {
            return Files.IndexOf(file);
        }

        public string GetRelativePathName(string FileName)
        {
            string result = "";

            result = PathUtils.RelativePathTo(PathUtils.GetPath(ProjectFileName), FileName);

            return result;
        }

        public void MakeRelativePaths()
        {
            foreach (FileProperties props in fileList)
            {
                props.FileName = GetRelativePathName(props.FileName);
            }

            MaterialsFileName = GetRelativePathName(MaterialsFileName);
        }

        public void ExpandPaths()
        {
            foreach (FileProperties props in fileList)
            {
                props.FileName = PathUtils.ResolvePath(props.FileName, PathUtils.GetPath(ProjectFileName));
            }
            MaterialsFileName = PathUtils.ResolvePath(MaterialsFileName, PathUtils.GetPath(ProjectFileName));
        }

        public MaterialProperties GetMaterialByName(string Name)
        {

            foreach (MaterialProperties material in Materials)
                if (string.Compare(Name, material.Name, true) == 0)
                    return material;

            return DefaultMaterial;
        }

        //
        public static ProjectSettings LoadFromXmlFile(string FileName)
        {
            ProjectSettings result = null;
            XmlSerializer serializer = new XmlSerializer(typeof(ProjectSettings));

            if (!File.Exists(FileName))
                return result;

            FileStream fs = new FileStream(FileName, FileMode.Open);

            try
            {
                result = (ProjectSettings)serializer.Deserialize(fs);
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                }
            }

            return result;
        }

        public bool SaveToXmlFile(string FileName)
        {
            bool result = false;
            XmlSerializer serializer = new XmlSerializer(typeof(ProjectSettings));
            TextWriter Writer = null;

            AppSettingsBase.CreateDirectory(FileName);
            try
            {
                Writer = new StreamWriter(FileName, false, Encoding.UTF8);

                serializer.Serialize(Writer, this);
                result = true;
            }
            finally
            {
                if (Writer != null)
                {
                    Writer.Close();
                }
            }
            return result;
        }

    }
}
