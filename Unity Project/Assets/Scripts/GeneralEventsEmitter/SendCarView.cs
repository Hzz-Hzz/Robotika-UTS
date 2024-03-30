using System;
using System.IO;
using System.Threading;
using EventsEmitter.models;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;


[System.Serializable]
public class ImageUpdatedEvent : UnityEvent<ImageUpdatedEventArgs>
{
}

[InitializeOnLoad]
public class SendCarView : MonoBehaviour
{
    public ImageUpdatedEvent ImageUpdated;

    public int maximumFPS = 20;
    public bool keepRunningWhenEditorIsPaused = true;


    private Camera targetCameraGameobject;
    private System.Diagnostics.Stopwatch _stopwatch = new();

    void Start()
    {
        targetCameraGameobject = GetComponent<Camera>();
        _stopwatch.Start();
        EditorApplication.pauseStateChanged += HandleOnPlayModeChanged;
    }


    private bool? is_OnPauseThread_running = null;
    private Thread _onPauseThread;
    void HandleOnPlayModeChanged(PauseState pauseState) {
        if (!keepRunningWhenEditorIsPaused)
            return;

        // This allows us to restart the WPF application (server) and having the image immediately displayed
        // without having to unpause then re-pause.
        if (pauseState == PauseState.Paused) {
            is_OnPauseThread_running = true;
            _onPauseThread = new Thread(UpdateContinously);
            _onPauseThread.Start();
        } else if (pauseState == PauseState.Unpaused && _onPauseThread != null) {
            is_OnPauseThread_running = false;
        }
    }
    void UpdateContinously() {
        while (is_OnPauseThread_running ?? false) {
            publishEvent(true);
            Thread.Sleep(100);
        }
        is_OnPauseThread_running = null;
    }


    private Texture2D cameraTexture2D;
    private byte[] cameraSceneBytesData;
    void Update() {

        if (is_OnPauseThread_running != null)
            return;
        if (_stopwatch.ElapsedMilliseconds < 1000 / maximumFPS)
            return;
        _stopwatch.Restart();
        publishEvent(false);
    }

    public void publishEvent(bool paused) {
        try {
            cameraTexture2D = CamCapture(cameraTexture2D);
            cameraSceneBytesData = cameraTexture2D.EncodeToPNG();
            ImageUpdated?.Invoke(new ImageUpdatedEventArgs() {
                imageData = cameraSceneBytesData,
                paused = paused
            });
        }
        catch (Exception e) when (e is UnityException || e is InvalidOperationException) {
            if (!e.Message.Contains("main thread")) throw;
        }
    }


    /**
     *  reuseTexture2D: increase efficiency so that we're not destroy & re-creating multiple times
     */
    Texture2D CamCapture([CanBeNull] Texture2D reuseTexture2D = null)
    {
        var height = targetCameraGameobject.pixelHeight;
        var width = targetCameraGameobject.pixelWidth;
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
}