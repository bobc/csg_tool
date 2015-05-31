using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using RMC;
using CadCommon;



namespace TestCsg
{
    public class AppSettings
    {
        public Point MainPos;
        public Size MainSize;
        public Size LeftPanel;
        public Size BottomPanel;

        // public bool ShowFullArea;
        // public string Folder;
        // public string Filename;
        // public ProcessSettings ProcessSettings;

        public bool ShowGrid;
        public UnitsSpecification GridUnits;

        public UnitsSpecification ImportDefaultUnits;

        public UnitsSpecification ExportUnits;


        [XmlIgnore]
        public Form1 MainForm;

        public AppSettings()
        {
            this.GridUnits = new UnitsSpecification(Units.millimeters, 1);
            this.ImportDefaultUnits = new UnitsSpecification(Units.millimeters, 1);
            this.ExportUnits = new UnitsSpecification(Units.inch, 0.1);
        }

        public AppSettings(Form1 MainForm)
        {
            this.MainForm = MainForm;
            this.GridUnits = new UnitsSpecification(Units.millimeters, 1);
            this.ImportDefaultUnits = new UnitsSpecification(Units.millimeters, 1);
            this.ExportUnits = new UnitsSpecification(Units.inch, 0.1);

            OnSaving();
        }

        public void OnLoad()
        {
            MainForm.Location = MainPos;
            MainForm.Width = MainSize.Width;
            MainForm.Height = MainSize.Height;
            //MainForm.splitContainer1.SplitterDistance = BottomPanel.Height;
            MainForm.splitContainer1.SplitterDistance = LeftPanel.Width;
            //MainForm.ShowFullArea = this.ShowFullArea;
        }

        public void OnSaving()
        {
            MainPos = MainForm.Location;
            MainSize = new Size(MainForm.Width, MainForm.Height);
            //BottomPanel.Height = MainForm.splitContainer1.Panel1.Height;
            LeftPanel.Width = MainForm.splitContainer1.Panel1.Width;
            //this.ShowFullArea = MainForm.ShowFullArea;
        }

        public static AppSettings LoadFromXmlFile(string FileName)
        {
            AppSettings result = null;
            XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));

            if (!File.Exists(FileName))
                return result;

            FileStream fs = new FileStream(FileName, FileMode.Open);

            try
            {
                result = (AppSettings)serializer.Deserialize(fs);

                if (result.GridUnits == null)
                    result.GridUnits = new UnitsSpecification(Units.millimeters, 1);
                if (result.ImportDefaultUnits == null)
                    result.ImportDefaultUnits = new UnitsSpecification(Units.millimeters, 1);
                if (result.ExportUnits == null)
                    result.ExportUnits = new UnitsSpecification(Units.inch, 0.1);
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
            XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
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
