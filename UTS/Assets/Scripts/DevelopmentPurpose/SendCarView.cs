using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public class SendCarView : MonoBehaviour
{
    public bool saveDataset = false;
    public string datasetFolderPath;


    private Camera targetCameraGameobject;
    private System.Diagnostics.Stopwatch _stopwatch = new();
    private System.Diagnostics.Stopwatch _saveDatasetStopwatch = new();
    private NamedPipeClientStream _clientNamedPipe;
    private StreamReader clientStreamReader;
    private BinaryWriter clientStreamWriter;

    void Start()
    {
        targetCameraGameobject = GetComponent<Camera>();
        _stopwatch.Start();
        _saveDatasetStopwatch.Start();
        reconnectNamedPipeAsync();
        EditorApplication.pauseStateChanged += HandleOnPlayModeChanged;
    }

    void reconnectNamedPipeAsync()
    {
        Thread startAsync = new Thread(reconnectNamedPipe);
        startAsync.Start();
    }

    void reconnectNamedPipe()
    {
        if (_clientNamedPipe != null)
        {
            _clientNamedPipe.Close();
            _clientNamedPipe.Dispose();
            clientStreamReader = null;
            clientStreamWriter = null;
        }
        Debug.Log("Connecting...");
        _clientNamedPipe = new("RobotikaNuelValen");
        _clientNamedPipe.Connect();
        clientStreamReader = new StreamReader(_clientNamedPipe);
        clientStreamWriter = new BinaryWriter(_clientNamedPipe);
        Debug.Log("Connected to the server");
    }


    private bool? onPauseThreadShouldRun = null;
    private Thread _onPauseThread;
    void HandleOnPlayModeChanged(PauseState pauseState) {
        // This allows us to restart the WPF application and having the image immediately displayed
        // without having to unpause then re-pause.
        if (pauseState == PauseState.Paused) {
            onPauseThreadShouldRun = true;
            _onPauseThread = new Thread(UpdateContinously);
            _onPauseThread.Start();
        } else if (pauseState == PauseState.Unpaused && _onPauseThread != null) {
            onPauseThreadShouldRun = false;
        }
    }
    void UpdateContinously() {
        while (onPauseThreadShouldRun ?? false) {
            sendSceneToServer(true);
            Thread.Sleep(100);
        }
        onPauseThreadShouldRun = null;
    }


    private Texture2D cameraTexture2D;
    private byte[] cameraSceneBytesData;
    void Update() {
        if (onPauseThreadShouldRun != null)
            return;
        const int MaxFPS = 40;
        if (_stopwatch.ElapsedMilliseconds < 1000 / MaxFPS)
            return;
        _stopwatch.Restart();

        if (Input.GetKeyDown("p")) {
            Console.WriteLine("keydown p");
            saveDataset = !saveDataset;
        }
        sendSceneToServer(false);
    }

    void sendSceneToServer(bool isPaused) {
        try {
            // if (cameraTexture2D != null) {
                // Resources.UnloadAsset(cameraTexture2D);
                // Object.DestroyImmediate(cameraTexture2D, allowDestroyingAssets: true);
                // DestroyImmediate(cameraTexture2D, allowDestroyingAssets: true);
            // }
            cameraTexture2D = CamCapture(cameraTexture2D);
            cameraSceneBytesData = cameraTexture2D.EncodeToPNG();
        }
        catch (Exception e) when (e is UnityException || e is InvalidOperationException) {
            if (!e.Message.Contains("main thread")) throw;
        }

        try {
            if (cameraSceneBytesData != null && !isPaused)
                saveDatasetTo(cameraSceneBytesData);
            if (clientStreamWriter == null)
                return;
            if (!_clientNamedPipe.IsConnected){
                // was connected but now disconnected
                Debug.Log("Disconnected...");
                reconnectNamedPipeAsync();
            }

            if (cameraSceneBytesData != null && clientStreamWriter != null) {
                Debug.Log("Writing to namedpipeline");
                clientStreamWriter?.Write(cameraSceneBytesData.Length);
                clientStreamWriter?.Write(cameraSceneBytesData);
                clientStreamWriter?.Flush();
            }
        } catch (Exception e) when (e is IOException || e is ObjectDisposedException) {
            if (e.Message.Contains("Pipe is broken")) return;
            if (e.Message.Contains("closed pipe")) return;
            throw;
        }
    }

    private void saveDatasetTo(byte[] byteData, [CanBeNull] string datasetFolderPath = null) {
        const int MaxFPS = 2;
        if (_saveDatasetStopwatch.ElapsedMilliseconds < 1000 / MaxFPS)
            return;
        _saveDatasetStopwatch.Restart();

        datasetFolderPath = datasetFolderPath ?? this.datasetFolderPath;
        if (!IsPathValidRootedLocal(this.datasetFolderPath)) {
            Debug.LogError($"Invalid path datasetFolderPath, given: {datasetFolderPath}");
            return;
        }

        if (!saveDataset) {
            return;
        }
        Directory.CreateDirectory(datasetFolderPath);
        var fileNumberLocation = Path.Join(datasetFolderPath, "-fileno");
        int fileNumber = File.Exists(fileNumberLocation) ? Int32.Parse(File.ReadAllText(fileNumberLocation)) : 0;
        var datasetFileName = Path.Join(datasetFolderPath, $"{fileNumber}.png");

        Debug.Log($"Saving dataset {fileNumber}.png");
        fileNumber++;
        File.WriteAllText(fileNumberLocation, $"{fileNumber}");

        using (var streamWriter = new FileStream(datasetFileName, FileMode.Create))
        {
            streamWriter.Write(byteData);
        }
    }
    public bool IsPathValidRootedLocal(String pathString) {
        Uri pathUri;
        Boolean isValidUri = Uri.TryCreate(pathString, UriKind.Absolute, out pathUri);
        return isValidUri && pathUri != null && pathUri.IsLoopback;
    }


    /**
     *  reuseTexture2D: increase efficiency so that we're not destroy & re-creating multiple times
     */
    Texture2D CamCapture([CanBeNull] Texture2D reuseTexture2D = null)
    {
        var height = targetCameraGameobject.pixelHeight;
        var width = targetCameraGameobject.pixelWidth;
        Debug.Assert(height > 400);
        Debug.Assert(width > 400);
        RenderTexture tempRT = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 4
        };

        var oldCameraTargetTexture = targetCameraGameobject.targetTexture;
        var oldActiveRenderTexture = RenderTexture.active;

        targetCameraGameobject.targetTexture = tempRT;
        if (tempRT != null) RenderTexture.active = tempRT;
        targetCameraGameobject.Render();

        Texture2D? image = reuseTexture2D;
        if (image == null || image.IsDestroyed()) {
            Debug.Log("Creating new Texture2D");
            image = new Texture2D(width, height, TextureFormat.ARGB32, false, true);
        }else {
            image.Reinitialize(width, height, TextureFormat.ARGB32, false);
        }
        image.hideFlags = HideFlags.HideAndDontSave;
        image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0);
        image.Apply();

        RenderTexture.active = oldActiveRenderTexture;
        targetCameraGameobject.targetTexture = oldCameraTargetTexture;
        tempRT.DiscardContents();
        DestroyImmediate(tempRT, allowDestroyingAssets: true);

        return image;
    }

    void OnApplicationQuit() {
        if (_clientNamedPipe != null && _clientNamedPipe.IsConnected) {
            _clientNamedPipe.Close();
            _clientNamedPipe.Dispose();
        }
    }
}
