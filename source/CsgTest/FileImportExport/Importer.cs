using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using CadCommon;

using FileImportExport.AMF;
using FileImportExport.STL;
using FileImportExport.VRML;
using FileImportExport.STEP;

namespace FileImportExport
{
    public class Importer
    {
        // import file
        public static FileBase ImportFile(string FileName, UnitsSpecification DefaultInputUnits, out string ErrorMessage)
        {
            FileBase result = null;
            bool LoadedOk = false;
            string FileExt = Path.GetExtension(FileName);

            ErrorMessage = null;
            switch (FileExt)
            {
                case ".stl":
                    {
                        StlFile StlFile = new StlFile();
                        result = StlFile;
                        result.Units = new UnitsSpecification(DefaultInputUnits);
                        LoadedOk = StlFile.LoadFromFile(FileName);
                    }
                    break;

                case ".amf":
                    try
                    {
                        AmfFile AmfFile = new AmfFile();
                        result = AmfFile;
                        result.Units = new UnitsSpecification(DefaultInputUnits);
                        LoadedOk = AmfFile.LoadFromFile(FileName);
                    }
                    catch (Exception ex)
                    {
                        result.LastError = ex.Message;
                    }
                    break;

                case ".wrl":
                    try
                    {
                        VrmlFile vrmlFile = new VrmlFile();
                        result = vrmlFile;
                        result.Units = new UnitsSpecification(DefaultInputUnits);
                        LoadedOk = vrmlFile.LoadFromFile(FileName);
                        //if (LoadedOk)
                        //{
                        //    // *** test
                        //    vrmlFile.SaveToFileExt(@"c:\temp\test_out.wrl");
                        //}
                    }
                    catch (Exception ex)
                    {
                        result.LastError = ex.Message;
                    }
                    break;

                case ".stp":
                    {
                        StepFile stepFile = new StepFile();
                        result = stepFile;
                        result.Units = new UnitsSpecification(DefaultInputUnits);
                        LoadedOk = stepFile.LoadFromFile(FileName);
                    }
                    break;
            }

            if (LoadedOk)
            {
                result.FileName = FileName;
            }
            else
            {
                ErrorMessage = result.LastError;
                result = null;
            }
            //    Project.Files.Add(result);
            //    Document.Modified = true;
            //    AddInputMeshNode(result);
            //    textBox1.AppendText(string.Format("File {0} loaded ok", FileName) + Environment.NewLine);
            //}
            //else
            //    textBox1.AppendText(string.Format("Error reading file {0}: {1}", FileName, result.LastError) + Environment.NewLine);

            return result;
        }


    }
}
