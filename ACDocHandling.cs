using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.IO;

namespace MYCOLLECTION
{
    public static class ACDocHandling
    {

        public static  Document CreateDoc(string templateFullPath, string drawingName)
        {
            if (!File.Exists(templateFullPath))
            {
                Application.ShowAlertDialog("template not found,default will be used");
                templateFullPath = "acad.dwt";
            }

            Document newDoc = Application.DocumentManager.Add(templateFullPath);
            Application.DocumentManager.MdiActiveDocument = newDoc;
            string currentFolder = Directory.GetCurrentDirectory();
            string savePath = Path.Combine(currentFolder, drawingName);
            newDoc.Database.SaveAs(savePath, DwgVersion.Current);
            return newDoc;
        }

        public static Document OpenDoc(string filepath)
        {
            DocumentCollection DocCol = Application.DocumentManager;
            try
            {
                foreach (Document doc in DocCol)
                {
                    if (doc.Name.Equals(filepath, StringComparison.OrdinalIgnoreCase))
                    {
                        DocCol.MdiActiveDocument = doc;
                        return doc;
                    }
                }
                if (System.IO.File.Exists(filepath))
                {
                    Document doc = DocCol.Open(filepath, false);
                    DocCol.MdiActiveDocument = doc;
                    return doc;
                }
                else
                {
                    Application.ShowAlertDialog("Error:File not Opened");

                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                Application.ShowAlertDialog("Exception :" + ex.Message + ex.StackTrace);
            }
            return DocCol.MdiActiveDocument;
        }
    }
}
