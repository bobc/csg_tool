using System;
using System.Collections.Generic;
using System.Text;

using CadCommon;

using FileImportExport.AMF;
using FileImportExport.STL;
using FileImportExport.VRML;
using FileImportExport.STEP;

namespace FileImportExport
{
    public class Exporter
    {
        public static MaterialProperties GetMaterialByName(string Name, List<MaterialProperties> materials)
        {
            foreach (MaterialProperties material in materials)
                if (string.Compare(Name, material.Name, true) == 0)
                    return material;

            return new MaterialProperties(); // default material
        }


        public static bool ExportToVrml(FileBase file, FileProperties fileProperties, List<MaterialProperties> materials, VrmlFile vrmlFile)
        {

            if (file is StlFile)
            {
                StlFile StlFile = file as StlFile;
                //TODO: 
                vrmlFile.AddShape(StlFile.Mesh, fileProperties.Material);
            }
            else if (file is AmfFile)
            {
                AmfFile AmfFile = file as AmfFile;
                AmfObject obj = AmfFile.Objects[0]; //todo

                // get the id of the material from the volume
                // get name from material metadata
                // get Material properties from Materials or list (or default)
                int volNum;
                for (volNum = 0; volNum < obj.Mesh.Volumes.Count; volNum++)
                {
                    string materialName = AmfFile.GetMaterialNameFromId(obj.Mesh.Volumes[volNum].MaterialId);
                    MaterialProperties material = GetMaterialByName(materialName, materials);
                    vrmlFile.AddShape(obj.Mesh.GetMeshForVolume(volNum), material);
                }
            }
            else if (file is VrmlFile)
            {
                VrmlFile inputVrmlfile = file as VrmlFile;
                MeshIndexed mesh = new MeshIndexed();

                VrmlConversionContext context = new VrmlConversionContext();

                // traverse the tree looking for Shape,IndexedFaceSet
                // keep transfrom
                // discard group? switch
                // add shapes to mesh

                context.vrmlFile = inputVrmlfile;
                context.transform = new Transform();
                context.material = new MaterialProperties();
                context.OutputVrmlFile = vrmlFile;

                mesh.Rotation = fileProperties.Rotation;

                foreach (NodeStatement statement in context.vrmlFile.Scene.Statements)
                    FindShapes(context, statement);

            }
            //else error

            return true;
        }

        private static void FindShapes(VrmlConversionContext context, NodeStatement statement)
        {
            // "USE" has no Node
            if (statement.Type == NodeType.Use)
            {
                NodeStatement useTarget = context.vrmlFile.FindName(statement.NameId);
                if (useTarget != null)
                    FindShapes(context, useTarget);
            }
            else
            {
                if (statement.Node.TypeId == "IndexedFaceSet")
                {
                    {
                        //
                        //Trace("adding shape");

                        IndexedFaceSet faces = VrmlIndexedFaceSet.GetIndexedFaceSet(statement.Node);
                        faces.Scale(context.transform.Scale);
                        faces.Translate(context.transform.Translation);

                        //TODO                        AddToMesh(context.mesh, faces, context.material.GetMaterialProperties());
                        MeshIndexed mesh = new MeshIndexed();
                        if (!string.IsNullOrEmpty(context.transform.Name))
                            mesh.Name = context.transform.Name;
                        mesh.AddToMesh(faces, context.material);
                        context.OutputVrmlFile.AddShape(mesh, context.material);
                    }
                }
                else if (statement.Node.TypeId == "Transform")
                {
                    context.transform.Fill(statement); // copy context?
                }
                else if (statement.Node.TypeId == "Material")
                {
                    //VrmlConversionContext newContext = new VrmlConversionContext(context);
                    //newContext.transform = context.transform;
                    //context = newContext;
                    Material mat = new Material();
                    mat.Fill(statement);
                    context.material = mat.GetMaterialProperties();
                    context.material.Name = statement.NameId;

                    context.material.ambientIntensity = 1.0; // fix for kicad
                }

                // recurse down
                foreach (NodeBodyElement bodyElem in statement.Node.nodeBody)
                {
                    if (bodyElem.Field.FieldValue is smfNodeValue)
                    {
                        smfNodeValue smfNode = bodyElem.Field.FieldValue as smfNodeValue;
                        foreach (NodeStatement substatement in smfNode.Values)
                        {
                            FindShapes(context, substatement);
                        }
                    }
                }
            }
        }

    }
}
