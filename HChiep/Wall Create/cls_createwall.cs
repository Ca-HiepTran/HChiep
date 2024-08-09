using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.SharePoint.Client;

namespace HChiep
{
    [Transaction(TransactionMode.Manual)]
    public class cls_createwall : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // SharePoint file path
            string sharePointFilePath = "/DocumentLibrary/Book1.xlsx";

            // Download the file from SharePoint
            string localFilePath = DownloadFileFromSharePoint(sharePointFilePath).Result;
            if (string.IsNullOrEmpty(localFilePath))
            {
                return Result.Failed;
            }

            List<WallData> wallDataList;
            try
            {
                wallDataList = ReadWallDataFromExcel(localFilePath).ToList();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Error reading Excel file: {ex.Message}");
                return Result.Failed;
            }

            if (wallDataList == null || wallDataList.Count == 0)
            {
                TaskDialog.Show("Error", "No data found in the selected Excel file.");
                return Result.Failed;
            }

            using (Transaction trans = new Transaction(doc, "Create Walls"))
            {
                trans.Start();
                foreach (var data in wallDataList)
                {
                    CreateWallType(doc, data);
                }
                trans.Commit();
            }

            return Result.Succeeded;
        }

        private async Task<string> DownloadFileFromSharePoint(string fileUrl)
        {
            string localFilePath = Path.Combine(Path.GetTempPath(), Path.GetFileName(fileUrl));

            try
            {
                var securePassword = new SecureString();
                foreach (char c in "yourpassword") securePassword.AppendChar(c);

                using (var context = new ClientContext("https://yoursharepointsite.com"))
                {
                    context.Credentials = new SharePointOnlineCredentials("youremail@domain.com", securePassword);
                    var fileInfo = Microsoft.SharePoint.Client.File.OpenBinaryDirect(context, fileUrl);

                    using (var fileStream = System.IO.File.Create(localFilePath))
                    {
                        fileInfo.Stream.CopyTo(fileStream);
                    }
                }

                return localFilePath;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Error downloading file from SharePoint: {ex.Message}");
                return null;
            }
        }

        private IEnumerable<WallData> ReadWallDataFromExcel(string filePath)
        {
            using (SpreadsheetDocument document = SpreadsheetDocument.Open(filePath, false))
            {
                WorkbookPart workbookPart = document.WorkbookPart;
                Sheet sheet = workbookPart.Workbook.Sheets.Elements<Sheet>().FirstOrDefault();
                WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id);
                SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();

                bool headerRow = true;
                foreach (Row row in sheetData.Elements<Row>())
                {
                    if (headerRow)
                    {
                        headerRow = false;
                        continue;
                    }

                    var wallData = new WallData
                    {
                        Name = GetCellValue(document, row.Elements<Cell>().ElementAt(0)),
                        MaterialName = GetCellValue(document, row.Elements<Cell>().ElementAt(1)),
                        Thickness = Convert.ToDouble(GetCellValue(document, row.Elements<Cell>().ElementAt(2)))
                    };

                    yield return wallData;
                }
            }
        }

        private string GetCellValue(SpreadsheetDocument document, Cell cell)
        {
            SharedStringTablePart stringTablePart = document.WorkbookPart.SharedStringTablePart;
            string value = cell.CellValue?.InnerXml;

            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                return stringTablePart.SharedStringTable.ChildElements[Int32.Parse(value)].InnerText;
            }
            else
            {
                return value;
            }
        }

        private void CreateWallType(Document doc, WallData data)
        {
            Material material = GetOrCreateMaterial(doc, data.MaterialName);
            if (material == null)
            {
                TaskDialog.Show("Error", $"Failed to create or find material: {data.MaterialName}");
                return;
            }

            WallType wallType = GetOrCreateWallType(doc, data.Name);
            if (wallType == null)
            {
                TaskDialog.Show("Error", $"Failed to create or find wall type: {data.Name}");
                return;
            }

            UpdateWallTypeStructure(wallType, material, data.Thickness);
        }

        private Material GetOrCreateMaterial(Document doc, string materialName)
        {
            Material material = new FilteredElementCollector(doc)
                .OfClass(typeof(Material))
                .Cast<Material>()
                .FirstOrDefault(m => m.Name.Equals(materialName, StringComparison.OrdinalIgnoreCase));

            if (material == null)
            {
                using (Transaction materialTrans = new Transaction(doc, "Create Material"))
                {
                    materialTrans.Start();
                    ElementId materialId = Material.Create(doc, materialName);
                    material = doc.GetElement(materialId) as Material;
                    materialTrans.Commit();
                }
            }

            return material;
        }

        private WallType GetOrCreateWallType(Document doc, string wallTypeName)
        {
            WallType wallType = new FilteredElementCollector(doc)
                .OfClass(typeof(WallType))
                .Cast<WallType>()
                .FirstOrDefault(wt => wt.Name.Equals(wallTypeName, StringComparison.OrdinalIgnoreCase));

            if (wallType == null)
            {
                WallType baseWallType = new FilteredElementCollector(doc)
                    .OfClass(typeof(WallType))
                    .Cast<WallType>()
                    .FirstOrDefault();

                if (baseWallType != null)
                {
                    wallType = baseWallType.Duplicate(wallTypeName) as WallType;
                }
            }

            return wallType;
        }

        private void UpdateWallTypeStructure(WallType wallType, Material material, double thickness)
        {
            CompoundStructure compoundStructure = wallType.GetCompoundStructure();
            List<CompoundStructureLayer> layers = new List<CompoundStructureLayer>
            {
                new CompoundStructureLayer(thickness / 304.8, MaterialFunctionAssignment.Structure, material.Id) // Convert mm to feet
            };

            compoundStructure.SetLayers(layers);
            wallType.SetCompoundStructure(compoundStructure);
        }

        private class WallData
        {
            public string Name { get; set; }
            public string MaterialName { get; set; }
            public double Thickness { get; set; }
        }
    }
}
