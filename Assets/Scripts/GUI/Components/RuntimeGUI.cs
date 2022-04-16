using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityVolumeRendering
{
    /// <summary>
    /// This is a basic runtime GUI, that can be used during play mode.
    /// You can import datasets, and edit them.
    /// Add this component to an empty GameObject in your scene (it's already in the test scene) and click play to see the GUI.
    /// </summary>
    public class RuntimeGUI : MonoBehaviour
    {
        private bool ranOpen = false;

        // for testing in Unity editor
        /*private void OnGUI()
        {
            if(!ranOpen) {
                StartCoroutine(OnOpenDICOMDatasetResult("http://localhost:8000/studies/test"));
            }
        }*/
        public void DisplayDicom(string url)
        {
            StartCoroutine(OnOpenDICOMDatasetResult(url));
        }
        private void OnOpenPARDatasetResult(RuntimeFileBrowser.DialogResult result)
        {
            if (!result.cancelled)
            {
                DespawnAllDatasets();
                string filePath = result.path;
                ParDatasetImporter parimporter = new ParDatasetImporter(filePath);
                VolumeDataset dataset = parimporter.Import(); //overriden somewhere
                if (dataset != null)
                {
                        VolumeObjectFactory.CreateObject(dataset);
                }
            }
        }
        
        private void OnOpenRAWDatasetResult(RuntimeFileBrowser.DialogResult result)
        {
            if(!result.cancelled)
            {

                // We'll only allow one dataset at a time in the runtime GUI (for simplicity)
                DespawnAllDatasets();

                // Did the user try to import an .ini-file? Open the corresponding .raw file instead
                string filePath = result.path;
                if (System.IO.Path.GetExtension(filePath) == ".ini")
                    filePath = filePath.Replace(".ini", ".raw");

                // Parse .ini file
                DatasetIniData initData = DatasetIniReader.ParseIniFile(filePath + ".ini");
                if(initData != null)
                {
                    // Import the dataset
                    RawDatasetImporter importer = new RawDatasetImporter(filePath, initData.dimX, initData.dimY, initData.dimZ, initData.format, initData.endianness, initData.bytesToSkip);
                    VolumeDataset dataset = importer.Import();
                    // Spawn the object
                    if (dataset != null)
                    {
                        VolumeObjectFactory.CreateObject(dataset);
                    }
                }
            }
        }

        IEnumerator<UnityWebRequestAsyncOperation> OnOpenDICOMDatasetResult(string url)
        {
            ranOpen = true;
            DespawnAllDatasets();
            // Import the dataset
            string[] urlSplit = url.Split('/');
            string filename = urlSplit[urlSplit.Length - 1];
            string savePath = "";
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                yield return webRequest.SendWebRequest();
                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError) {
                    Debug.Log(webRequest.error);
                    // TODO: Error handling
                    yield break;
                }

                savePath = string.Format("{0}/{1}", Application.persistentDataPath, filename);  
                string zipFileName = savePath + ".zip";    
                System.IO.File.WriteAllBytes(zipFileName, webRequest.downloadHandler.data);
                ZipFile.ExtractToDirectory(zipFileName, Application.persistentDataPath);
                System.IO.File.Delete(zipFileName);
            }
            
            IEnumerable<string> fileCandidates = Directory.EnumerateFiles(savePath, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(p => p.EndsWith(".dcm", StringComparison.InvariantCultureIgnoreCase) || p.EndsWith(".dicom", StringComparison.InvariantCultureIgnoreCase) || p.EndsWith(".dicm", StringComparison.InvariantCultureIgnoreCase));
            DICOMImporter importer = new DICOMImporter(fileCandidates, filename);
            List<DICOMImporter.DICOMSeries> seriesList = importer.LoadDICOMSeries();
            float numVolumesCreated = 0;
            foreach (DICOMImporter.DICOMSeries series in seriesList)
            {
                VolumeDataset dataset = importer.ImportDICOMSeries(series);
                // Spawn the object
                if (dataset != null)
                {
                    VolumeRenderedObject obj = VolumeObjectFactory.CreateObject(dataset);
                    obj.transform.position = new Vector3(numVolumesCreated, 0, 0);
                    numVolumesCreated++;
                }
            }
        }

        // For testing purposes
        private void OnOpenDICOMDatasetResultLocal(string path)
        {
            ranOpen = true;
            DespawnAllDatasets();
            // Import the dataset
            string[] pathSplit = path.Split('\\');
            string filename = pathSplit[pathSplit.Length - 1];
            //string.Format("{0}/{1}", Application.persistentDataPath, filename);        
            //System.IO.File.WriteAllBytes(savePath + ".zip", webRequest.downloadHandler.data);
            ZipFile.ExtractToDirectory(path + ".zip", Application.persistentDataPath);
            System.IO.File.Delete(path + ".zip");

            IEnumerable<string> fileCandidates = Directory.EnumerateFiles(Application.persistentDataPath + "\\" + filename, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(p => p.EndsWith(".dcm", StringComparison.InvariantCultureIgnoreCase) || p.EndsWith(".dicom", StringComparison.InvariantCultureIgnoreCase) || p.EndsWith(".dicm", StringComparison.InvariantCultureIgnoreCase));

            DICOMImporter importer = new DICOMImporter(fileCandidates, filename);
            List<DICOMImporter.DICOMSeries> seriesList = importer.LoadDICOMSeries();
            float numVolumesCreated = 0;
            foreach (DICOMImporter.DICOMSeries series in seriesList)
            {
                VolumeDataset dataset = importer.ImportDICOMSeries(series);
                // Spawn the object
                if (dataset != null)
                {
                    VolumeRenderedObject obj = VolumeObjectFactory.CreateObject(dataset);
                    obj.transform.position = new Vector3(numVolumesCreated, 0, 0);
                    numVolumesCreated++;
                }
            }
        }

        private void DespawnAllDatasets()
        {
            VolumeRenderedObject[] volobjs = GameObject.FindObjectsOfType<VolumeRenderedObject>();
            foreach(VolumeRenderedObject volobj in volobjs)
            {
                GameObject.Destroy(volobj.gameObject);
            }
        }
    }
}
