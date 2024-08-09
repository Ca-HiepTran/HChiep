using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ExcelDataReader;
using HChiep.Wall_Create;

namespace HChiep
{
    [Transaction(TransactionMode.Manual)]
    public class cls_createwall1 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Open file dialog to select the Excel file
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Excel Files|*.xls;*.xlsx;*.xlsm",
                Title = "Select an Excel File"
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return Result.Cancelled;
            }

            string excelFilePath = openFileDialog.FileName;
            var wallDataList = ReadWallDataFromExcel(excelFilePath);

            // Prompt for overwrite decision once
            CFcreatewall warningWindow = new CFcreatewall();
            bool? dialogResult = warningWindow.ShowDialog();

            if (dialogResult != true)
            {
                return Result.Cancelled;
            }

            bool applyToAll = warningWindow.ApplyToAll;

            using (TransactionGroup transGroup = new TransactionGroup(doc, "Create Walls"))
            {
                transGroup.Start();

                foreach (var data in wallDataList)
                {
                    List<WallType> similarWalls = GetSimilarWalls(doc, data.Name);

                    if (similarWalls.Count > 0)
                    {
                        if (applyToAll)
                        {
                            DeleteWalls(doc, similarWalls);
                            CreateWallType(doc, data);
                        }
                        else
                        {
                            return Result.Cancelled;
                        }
                    }
                    else
                    {
                        CreateWallType(doc, data);
                    }
                }

                transGroup.Assimilate();
            }

            return Result.Succeeded;
        }

        private List<WallType> GetSimilarWalls(Document doc, string wallTypeName)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(WallType))
                .Cast<WallType>()
                .Where(wt => wt.Name.Equals(wallTypeName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private void DeleteWalls(Document doc, List<WallType> walls)
        {
            using (Transaction trans = new Transaction(doc, "Delete Existing Walls"))
            {
                trans.Start();
                foreach (var wall in walls)
                {
                    doc.Delete(wall.Id);
                }
                trans.Commit();
            }
        }

        private void CreateWallType(Document doc, WallData data)
        {
            // Retrieve or create the base materials
            Material steelMaterial = GetOrCreateBaseMaterial(doc, "Steel");
            Material concreteMaterial = GetOrCreateBaseMaterial(doc, "Concrete");

            // Find or create wall type
            WallType wallType = new FilteredElementCollector(doc)
                .OfClass(typeof(WallType))
                .Cast<WallType>()
                .FirstOrDefault(wt => wt.Name.Equals(data.Name, StringComparison.OrdinalIgnoreCase));

            if (wallType == null)
            {
                // Duplicate an existing basic wall type
                WallType baseWallType = new FilteredElementCollector(doc)
                    .OfClass(typeof(WallType))
                    .Cast<WallType>()
                    .FirstOrDefault(wt => wt.Kind == WallKind.Basic);

                if (baseWallType != null)
                {
                    using (Transaction wallTypeTrans = new Transaction(doc, "Create Wall Type"))
                    {
                        wallTypeTrans.Start();
                        wallType = baseWallType.Duplicate(data.Name) as WallType;
                        wallTypeTrans.Commit();
                    }
                }
                else
                {
                    return;
                }
            }

            // Sort layers by type and set the order: exterior, core, interior
            List<CompoundStructureLayer> exteriorLayers = new List<CompoundStructureLayer>();
            List<CompoundStructureLayer> coreLayers = new List<CompoundStructureLayer>();
            List<CompoundStructureLayer> interiorLayers = new List<CompoundStructureLayer>();

            foreach (var layer in data.Layers)
            {
                Material layerMaterial;
                if (layer.Type == 0)
                {
                    // Core layer should use steel material
                    layerMaterial = GetOrCreateMaterial(doc, layer.MaterialName, steelMaterial, steelMaterial);
                }
                else
                {
                    // Other layers should use concrete material
                    layerMaterial = GetOrCreateMaterial(doc, layer.MaterialName, steelMaterial, concreteMaterial);
                }

                CompoundStructureLayer compoundLayer = new CompoundStructureLayer(
                    layer.Thickness / 304.8,
                    GetMaterialFunctionAssignment(layer.Function),
                    layerMaterial.Id);

                switch (layer.Type)
                {
                    case 1:
                        exteriorLayers.Add(compoundLayer);
                        break;
                    case 0:
                        coreLayers.Add(compoundLayer);
                        break;
                    case 2:
                        interiorLayers.Add(compoundLayer);
                        break;
                }
            }

            List<CompoundStructureLayer> sortedLayers = exteriorLayers
                .Concat(coreLayers)
                .Concat(interiorLayers)
                .ToList();

            // Apply the layers to the wall type
            using (Transaction structureTrans = new Transaction(doc, "Set Wall Structure"))
            {
                structureTrans.Start();

                CompoundStructure compoundStructure = wallType.GetCompoundStructure();
                compoundStructure.SetLayers(sortedLayers);
                wallType.SetCompoundStructure(compoundStructure);

                structureTrans.Commit();
            }
        }

        private Material GetOrCreateBaseMaterial(Document doc, string materialName)
        {
            Material material = new FilteredElementCollector(doc)
                .OfClass(typeof(Material))
                .Cast<Material>()
                .FirstOrDefault(m => m.Name.Equals(materialName, StringComparison.OrdinalIgnoreCase));

            if (material == null)
            {
                using (Transaction materialTrans = new Transaction(doc, $"Create {materialName} Material"))
                {
                    materialTrans.Start();
                    ElementId materialId = Material.Create(doc, materialName);
                    material = doc.GetElement(materialId) as Material;

                    if (material != null)
                    {
                        material.MaterialClass = materialName; // Set class name same as material name
                        material.Color = new Color(128, 128, 128); // Default grey color, adjust as needed
                        material.Transparency = 0;
                        material.Shininess = 50;
                    }

                    materialTrans.Commit();
                }
            }

            return material;
        }

        private Material GetOrCreateMaterial(Document doc, string materialName, Material steelMaterial, Material concreteMaterial)
        {
            Material material = new FilteredElementCollector(doc)
                .OfClass(typeof(Material))
                .Cast<Material>()
                .FirstOrDefault(m => m.Name.Equals(materialName, StringComparison.OrdinalIgnoreCase));

            if (material == null)
            {
                Material baseMaterial = materialName.Equals("Steel", StringComparison.OrdinalIgnoreCase) ? steelMaterial : concreteMaterial;

                using (Transaction materialTrans = new Transaction(doc, $"Create {materialName} Material"))
                {
                    materialTrans.Start();
                    ElementId materialId = Material.Create(doc, materialName);
                    material = doc.GetElement(materialId) as Material;

                    if (material != null && baseMaterial != null)
                    {
                        material.Color = baseMaterial.Color;
                        material.Transparency = baseMaterial.Transparency;
                        material.Shininess = baseMaterial.Shininess;
                        material.Smoothness = baseMaterial.Smoothness;
                        material.SurfaceForegroundPatternColor = baseMaterial.SurfaceForegroundPatternColor;
                        material.SurfaceBackgroundPatternColor = baseMaterial.SurfaceBackgroundPatternColor;
                        material.CutBackgroundPatternColor = baseMaterial.CutBackgroundPatternColor;
                        material.CutForegroundPatternColor = baseMaterial.CutForegroundPatternColor;
                        material.ThermalAssetId = baseMaterial.ThermalAssetId;
                        material.MaterialClass = baseMaterial.MaterialClass;
                        material.MaterialCategory = baseMaterial.MaterialCategory;
                    }

                    materialTrans.Commit();
                }
            }

            return material;
        }

        private MaterialFunctionAssignment GetMaterialFunctionAssignment(string function)
        {
            switch (function)
            {
                case "Finish2":
                    return MaterialFunctionAssignment.Finish2;
                case "Structure":
                    return MaterialFunctionAssignment.Structure;
                case "Finish1":
                    return MaterialFunctionAssignment.Finish1;
                case "Insulation":
                    return MaterialFunctionAssignment.Insulation;
                case "StructuralDeck":
                    return MaterialFunctionAssignment.StructuralDeck;
                case "Membrane":
                    return MaterialFunctionAssignment.Membrane;
                case "Substrate":
                    return MaterialFunctionAssignment.Substrate;
                default:
                    return MaterialFunctionAssignment.Structure;
            }
        }

        private static List<WallData> ReadWallDataFromExcel(string filePath)
        {
            var wallDataList = new List<WallData>();
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    bool headerRow = true;
                    while (reader.Read())
                    {
                        if (headerRow)
                        {
                            headerRow = false;
                            continue;
                        }

                        var wallData = new WallData
                        {
                            Name = reader.GetString(1),
                            Layers = new List<LayerData>()
                        };

                        for (int i = 0; i < 4; i++)
                        {
                            int baseIndex = 2 + i * 4;
                            string materialName = reader.GetString(baseIndex);
                            if (string.IsNullOrEmpty(materialName)) break;

                            double thickness = reader.GetDouble(baseIndex + 1);
                            string function = reader.GetString(baseIndex + 2);
                            int type = (int)reader.GetDouble(baseIndex + 3); 

                            wallData.Layers.Add(new LayerData
                            {
                                MaterialName = materialName,
                                Thickness = thickness,
                                Function = function,
                                Type = type
                            });
                        }

                        wallDataList.Add(wallData);
                    }
                }
            }

            return wallDataList;
        }

        public class WallData
        {
            public string Name { get; set; }
            public List<LayerData> Layers { get; set; }
        }

        public class LayerData
        {
            public string MaterialName { get; set; }
            public double Thickness { get; set; }
            public string Function { get; set; }
            public int Type { get; set; }
        }
    }
}
