using System;
using System.Collections.Generic;
using System.Text;

using CadCommon;


namespace FileImportExport.VRML
{
    public class VrmlConversionContext
    {
        public VrmlFile vrmlFile; // input file
        public Transform transform;

        //public Material material;
        public MaterialProperties material;
        
        public MeshIndexed mesh;  // output mesh (display)

        public VrmlFile OutputVrmlFile; // output file

        public VrmlConversionContext()
        {
        }

        public VrmlConversionContext(VrmlConversionContext source)
        {
            this.vrmlFile = source.vrmlFile;
            this.mesh = source.mesh;
        }
    }
}
