using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

[InitializeOnLoad]
public class SendCarView : MonoBehaviour
{
    private Camera targetCameraGameobject;
    private System.Diagnostics.Stopwatch _stopwatch = new();
    private NamedPipeClientStream _clientNamedPipe;
    private StreamReader clientStreamReader;
    private BinaryWriter clientStreamWriter;

    void Start()
    {
        targetCameraGameobject = GetComponent<Camera>();
        _stopwatch.Start();
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



    private Thread _threadOnPause;
    void HandleOnPlayModeChanged(PauseState pauseState) {
        // This allows us to restart the WPF application and having the image immediately displayed
        // without having to unpause then re-pause.
        if (pauseState == PauseState.Paused) {
            _threadOnPause = new Thread(UpdateContinously);
            _threadOnPause.Start();
        } else if (pauseState == PauseState.Unpaused && _threadOnPause != null) {
            _threadOnPause.Abort();
            _threadOnPause = null;
        }
    }
    void UpdateContinously() {
        while (true) {
            Update();
            Thread.Sleep(100);
        }
    }


    private Texture2D cameraTexture2D;
    private byte[] cameraSceneBytesData;
    void Update() {
        const int MaxFPS = 10;
        if (_stopwatch.ElapsedMilliseconds < 1000 / MaxFPS)
            return;
        _stopwatch.Restart();
        if (clientStreamWriter == null)
            return;
        if (!_clientNamedPipe.IsConnected){
            // was connected but now disconnected
            Debug.Log("Disconnected...");
            reconnectNamedPipeAsync();
        }

        try {
            if (cameraTexture2D != null)
                Destroy(cameraTexture2D);
            cameraTexture2D = CamCapture();
            cameraSceneBytesData = cameraTexture2D.EncodeToPNG();
        }
        catch (Exception e) when (e is UnityException || e is InvalidOperationException) {
            if (!e.Message.Contains("main thread")) throw;
        }

        try {
            clientStreamWriter?.Write(cameraSceneBytesData.Length);
            clientStreamWriter?.Write(cameraSceneBytesData);
            clientStreamWriter?.Flush();
        }
        catch (Exception e) when (e is IOException || e is ObjectDisposedException) {
            if (e.Message.Contains("Pipe is broken")) return;
            if (e.Message.Contains("closed pipe")) return;
            throw;
        }
    }

    Texture2D CamCapture()
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
        RenderTexture.active = tempRT;
        targetCameraGameobject.Render();

        Texture2D image = new Texture2D(width, height, TextureFormat.ARGB32, false, true);
        image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0);
        image.Apply();

        RenderTexture.active = oldActiveRenderTexture;
        targetCameraGameobject.targetTexture = oldCameraTargetTexture;

        return image;
    }
}
