using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MYCOLLECTION
{
    public class VideoFrameSimulation
    {

        [CommandMethod("ImportRasterImagesAnimated")]
        public async void ImportRasterImagesAnimated()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Editor ed = doc.Editor;
            Database db = doc.Database;

            using (DocumentLock doclock = doc.LockDocument())
            {
                // Prompt for folder containing raster images
                PromptOpenFileOptions pfo = new PromptOpenFileOptions("\nSelect an image to begin (Folder selection will be inferred):");
                pfo.Filter = "Image files (*.jpg;*.png;*.bmp)|*.jpg;*.png;*.bmp";
                PromptFileNameResult pfr = ed.GetFileNameForOpen(pfo);

                if (pfr.Status != PromptStatus.OK)
                    return;

                string startImagePath = pfr.StringResult;
                string folderPath = Path.GetDirectoryName(startImagePath);  // Folder containing the image

                // Get all image files in the selected folder
                string[] imagePaths = Directory.GetFiles(folderPath, "*.jpg")
                                                .Concat(Directory.GetFiles(folderPath, "*.png"))
                                                .Concat(Directory.GetFiles(folderPath, "*.bmp"))
                                                .ToArray();

                if (imagePaths.Length == 0)
                {
                    ed.WriteMessage($"\nNo image files found in the folder: {folderPath}");
                    return;
                }

                try
                {
                    ed.WriteMessage($"\nFound {imagePaths.Length} image files. Starting animation...");

                    // Animated raster image drawing
                    await DrawRasterImagesAnimated(doc, db, ed, imagePaths, 100);

                    ed.WriteMessage("\nAnimation complete!");
                    ed.Command("._ZOOM", "_E");  // Zoom to extents after animation
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\nError: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }
        [CommandMethod("ProcessVideoFrames")]
        public async void ProcessVideoFrames()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            // Step 1: Prompt user to select a video file
            PromptOpenFileOptions pfo = new PromptOpenFileOptions("\nSelect a video to begin (Folder selection will be inferred):");
            pfo.Filter = "Video files (*.mp4;*.avi;*.mov)|*.mp4;*.avi;*.mov";
            PromptFileNameResult pfr = doc.Editor.GetFileNameForOpen(pfo);

            if (pfr.Status != PromptStatus.OK)
            {
                doc.Editor.WriteMessage("\nNo video file selected.");
                return;
            }

            string videoPath = pfr.StringResult;
            string tempFolder = Path.Combine(Path.GetTempPath(), "VideoFrames");
            Directory.CreateDirectory(tempFolder);

            // Step 2: Use FFmpeg to extract frames from the video
            string ffmpegArgs = $"-i \"{videoPath}\" -vf \"fps=10\" \"{tempFolder}\\frame_%04d.png\"";
            string ffmpegPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "ffmpeg.exe");

            if (!File.Exists(ffmpegPath))
            {
                doc.Editor.WriteMessage("\nError: ffmpeg.exe not found in the app folder.");
                return;
            }
            using (DocumentLock doclock = doc.LockDocument())
            {

                Process ffmpegProcess = new Process();
                ffmpegProcess.StartInfo.FileName = ffmpegPath;
                ffmpegProcess.StartInfo.Arguments = ffmpegArgs;
                ffmpegProcess.StartInfo.UseShellExecute = false;
                ffmpegProcess.StartInfo.CreateNoWindow = true;
                ffmpegProcess.Start();
                ffmpegProcess.WaitForExit();

                // Step 3: Get all extracted image frames from the temporary folder
                string[] imagePaths = Directory.GetFiles(tempFolder, "*.png").ToArray();

                if (imagePaths.Length == 0)
                {
                    doc.Editor.WriteMessage("\nNo frames extracted from the video.");
                    return;
                }
                Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
                try
                {

                    ed.WriteMessage($"\nFound {imagePaths.Length} image files. Starting animation...");

                    // Animated raster image drawing
                    await DrawRasterImagesAnimated(doc, doc.Database, ed, imagePaths, 100);

                    ed.WriteMessage("\nAnimation complete!");
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\nError: {ex.Message}\n{ex.StackTrace}");
                }
                // Step 5: Clean up the temporary frames folder
                // Directory.Delete(tempFolder, true);
            }
        }
        private async Task DrawRasterImagesAnimated(Document doc, Database db, Editor ed, string[] imagePaths, int delay)
        {
            Extents3d extents = new Extents3d(new Point3d(0, 0, 0), new Point3d(1920, 1080, 0));
            bool Zoomed = false;
            // Loop through image paths and display them one by one with delay
            foreach (var imagePath in imagePaths)
            {
                try
                {
                    //ed.WriteMessage($"\nInserting image: {imagePath}");

                    // Insert each raster image into the model space.
                    AttachRasterImage(db, imagePath, out extents);
                    if (!Zoomed)
                    {
                        ZoomExtents(ed, extents);
                    }
                    // Refresh the AutoCAD UI (important for drawing to show up)
                    doc.Editor.Regen();

                    // Wait for the specified delay before drawing the next image
                    await Task.Delay(delay);  // This ensures animation delay without blocking UI

                    // Clear the previous frame from model space (before committing the transaction)
                    ClearModelSpace(db);
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\nError inserting image {imagePath}: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }
        public void AttachRasterImage(Database acCurDb, string strFileName, out Extents3d ext)
        {

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Define the name and image to use
                string strImgName = Guid.NewGuid().ToString();

                RasterImageDef acRasterDef;
                bool bRasterDefCreated = false;
                ObjectId acImgDefId;

                // Get the image dictionary
                ObjectId acImgDctID = RasterImageDef.GetImageDictionary(acCurDb);

                // Check to see if the dictionary does not exist, it not then create it
                if (acImgDctID.IsNull)
                {
                    acImgDctID = RasterImageDef.CreateImageDictionary(acCurDb);
                }

                // Open the image dictionary
                DBDictionary acImgDict = acTrans.GetObject(acImgDctID, OpenMode.ForRead) as DBDictionary;

                // Check to see if the image definition already exists
                if (acImgDict.Contains(strImgName))
                {
                    acImgDefId = acImgDict.GetAt(strImgName);

                    acRasterDef = acTrans.GetObject(acImgDefId, OpenMode.ForWrite) as RasterImageDef;
                }
                else
                {
                    // Create a raster image definition
                    RasterImageDef acRasterDefNew = new RasterImageDef();

                    // Set the source for the image file
                    acRasterDefNew.SourceFileName = strFileName;

                    // Load the image into memory
                    acRasterDefNew.Load();

                    // Add the image definition to the dictionary
                    acTrans.GetObject(acImgDctID, OpenMode.ForWrite);
                    acImgDefId = acImgDict.SetAt(strImgName, acRasterDefNew);

                    acTrans.AddNewlyCreatedDBObject(acRasterDefNew, true);

                    acRasterDef = acRasterDefNew;

                    bRasterDefCreated = true;
                }

                // Open the Block table for read
                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                OpenMode.ForWrite) as BlockTableRecord;

                // Create the new image and assign it the image definition
                using (RasterImage acRaster = new RasterImage())
                {
                    acRaster.ImageDefId = acImgDefId;

                    // Define the width and height of the image
                    Vector3d width;
                    Vector3d height;

                    // Check to see if the measurement is set to English (Imperial) or Metric units
                    if (acCurDb.Measurement == MeasurementValue.English)
                    {
                        width = new Vector3d((acRasterDef.ResolutionMMPerPixel.X * acRaster.ImageWidth) / 25.4, 0, 0);
                        height = new Vector3d(0, (acRasterDef.ResolutionMMPerPixel.Y * acRaster.ImageHeight) / 25.4, 0);
                    }
                    else
                    {
                        width = new Vector3d(acRasterDef.ResolutionMMPerPixel.X * acRaster.ImageWidth, 0, 0);
                        height = new Vector3d(0, acRasterDef.ResolutionMMPerPixel.Y * acRaster.ImageHeight, 0);
                    }

                    // Define the position for the image 
                    Point3d insPt = new Point3d(5.0, 5.0, 0.0);

                    // Define and assign a coordinate system for the image's orientation
                    CoordinateSystem3d coordinateSystem = new CoordinateSystem3d(insPt, width * 2, height * 2);
                    acRaster.Orientation = coordinateSystem;

                    // Set the rotation angle for the image
                    acRaster.Rotation = 0;

                    // Add the new object to the block table record and the transaction
                    acBlkTblRec.AppendEntity(acRaster);
                    acTrans.AddNewlyCreatedDBObject(acRaster, true);

                    // Connect the raster definition and image together so the definition
                    // does not appear as "unreferenced" in the External References palette.
                    RasterImage.EnableReactors(true);
                    acRaster.AssociateRasterDef(acRasterDef);
                    ext = acRaster.GeometricExtents;
                    if (bRasterDefCreated)
                    {
                        acRasterDef.Dispose();
                    }
                }

                // Save the new object to the database
                acTrans.Commit();

            }
        }
        private void ClearModelSpace(Database db)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord modelSpace = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                // Collect all objects in model space to be removed
                var objectsToDelete = new ObjectIdCollection();
                foreach (ObjectId objId in modelSpace)
                {
                    var entity = objId.GetObject(OpenMode.ForWrite) as Entity;
                    if (entity != null)
                    {
                        objectsToDelete.Add(objId);  // Add the entity to the collection
                    }
                }

                // Erase the objects from model space
                foreach (ObjectId objId in objectsToDelete)
                {
                    Entity entity = objId.GetObject(OpenMode.ForWrite) as Entity;
                    if (entity != null)
                    {
                        entity.Erase();  // Erase the entity
                    }

                }
                tr.Commit();
            }

        }
        public void ZoomExtents(Editor ed, Extents3d extents)
        {
            try
            {
                using (ViewTableRecord view = ed.GetCurrentView())
                {
                    view.CenterPoint = new Point2d(
                        (extents.MinPoint.X + extents.MaxPoint.X) / 2.0,
                        (extents.MinPoint.Y + extents.MaxPoint.Y) / 2.0);
                    view.Height = extents.MaxPoint.Y - extents.MinPoint.Y;
                    view.Width = extents.MaxPoint.X - extents.MinPoint.X;
                    ed.SetCurrentView(view);
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage(ex.Message + " " + ex.StackTrace);
            }
        }
    }
}
