using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.WSA.WebCam;

public class kuCamManager : MonoBehaviour {

    private PhotoCapture    pCaptureObj = null;
    private Resolution      camRes;

    private int ImgWidth;
    private int ImgHeight;

    Matrix4x4 CamToWorldMat = new Matrix4x4();
    Matrix4x4 ProjMat = new Matrix4x4();

    private PhotoCaptureFrame frameToProcess = null;

    public RawImage     RawImg;
    public Text         BarcodeText;
    public Text         DebugText;

    // Camera state
    bool isCamEnabled = false;
    bool isProcessing = false;

    // Use this for initialization
    void Start () {
        DebugText.text = "in start.";
        this.EnableCamCapture();
	}
	
	// Update is called once per frame
	void Update () {
        // DebugText.text = "in update.";
        if (isCamEnabled)
        {
            DebugText.text = "kerkerker";

            pCaptureObj.TakePhotoAsync(OnCapturedPhotoToMemory);
        }   
    }

    bool EnableCamCapture()
    {
        DebugText.text = "in EnableCamCapture.";
        if (!isCamEnabled)
        {
            DebugText.text = "in res.";

            camRes
                = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

            PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
        }

        return true;
    }

    void ReleaseCamCapture()
    {
        if (pCaptureObj == null)
        {
            return;
        }

        pCaptureObj.StopPhotoModeAsync(OnStopPhotoMode);
    }

    void OnPhotoCaptureCreated(PhotoCapture captureObject)
    {
        DebugText.text = "in OnPhotoCaptureCreated.";

        pCaptureObj = captureObject;
  
        CameraParameters cParam = new CameraParameters();
        cParam.hologramOpacity = 0.0f;
        cParam.cameraResolutionWidth  = camRes.width;
        cParam.cameraResolutionHeight = camRes.height;
        cParam.pixelFormat = CapturePixelFormat.BGRA32;

        ImgWidth  = cParam.cameraResolutionWidth;
        ImgHeight = cParam.cameraResolutionHeight;

        pCaptureObj.StartPhotoModeAsync(cParam, OnPhotoModeStarted);
    }

    void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
    {
        DebugText.text = "in OnPhotoModeStarted";

        if (result.success)
        {
            DebugText.text = "in OnPhotoModeStarted if";

            isCamEnabled = true;
        }
    }

    void OnStopPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        pCaptureObj.Dispose();
        pCaptureObj = null;
        isCamEnabled = false;
    }

    void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        if (result.success)
        {
            DebugText.text = "in capture to memory.";

            frameToProcess = photoCaptureFrame;

            frameToProcess.TryGetCameraToWorldMatrix(out CamToWorldMat);
            frameToProcess.TryGetProjectionMatrix(0.0f, 1.0f, out ProjMat);

            #region // Copy capture to texture. //
            // Create our Texture2D for use and set the correct resolution
            Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
            Texture2D targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height);
            // Copy the raw image data into our target texture
            photoCaptureFrame.UploadImageDataToTexture(targetTexture);
            // Do as we wish with the texture such as apply it to a material, etc.
            #endregion

            RawImg.texture = targetTexture;
            RawImg.material.mainTexture = targetTexture;
        }

    }
}
