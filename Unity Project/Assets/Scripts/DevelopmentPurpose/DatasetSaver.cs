using System;
using System.IO;
using DefaultNamespace;
using EventsEmitter.models;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;

namespace DevelopmentPurpose
{
    public class DatasetSaver: MonoBehaviour
    {
        private System.Diagnostics.Stopwatch _saveDatasetStopwatch = new ();
        public int MaxFPS = 2;

        public bool saveDataset = false;
        public string datasetFolderPath;
        public string keyToToggleSaveDataset = "p";



        void Start() {
            _saveDatasetStopwatch.Start();
        }
        void Update() {
            if (Input.GetKeyDown(keyToToggleSaveDataset)) {
                Console.WriteLine($"saveDataset={saveDataset}");
                saveDataset = !saveDataset;
            }
        }


        public void OnImageUpdated(ImageUpdatedEventArgs eventArgs) {
            saveDatasetTo(eventArgs.imageData);
        }

        private void saveDatasetTo(byte[] byteData, [CanBeNull] string datasetFolderPath = null) {
            if (_saveDatasetStopwatch.ElapsedMilliseconds < 1000 / MaxFPS)
                return;
            _saveDatasetStopwatch.Restart();

            datasetFolderPath = datasetFolderPath ?? this.datasetFolderPath;
            if (!IsPathValidRootedLocal(this.datasetFolderPath)) {
                CustomLogger.Log($"Invalid path datasetFolderPath, given: {datasetFolderPath}");
                return;
            }

            if (!saveDataset) {
                return;
            }
            Directory.CreateDirectory(datasetFolderPath);
            var fileNumberLocation = Path.Join(datasetFolderPath, "-fileno");
            int fileNumber = File.Exists(fileNumberLocation) ? Int32.Parse(File.ReadAllText(fileNumberLocation)) : 0;
            var datasetFileName = Path.Join(datasetFolderPath, $"{fileNumber}.png");

            CustomLogger.Log($"Saving dataset {fileNumber}.png");
            fileNumber++;
            File.WriteAllText(fileNumberLocation, $"{fileNumber}");

            using (var streamWriter = new FileStream(datasetFileName, FileMode.Create))
            {
                streamWriter.Write(byteData);
            }
        }

        public static bool IsPathValidRootedLocal(String pathString) {
            Uri pathUri;
            Boolean isValidUri = Uri.TryCreate(pathString, UriKind.Absolute, out pathUri);
            return isValidUri && pathUri != null && pathUri.IsLoopback;
        }

    }
}