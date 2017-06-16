using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.WSA.WebCam;

public class kuCamManager : Singleton<kuCamManager> {

    private PhotoCapture    pCaptureObj = null;
    private Resolution      camRes;

    private int ImgWidth;
    private int ImgHeight;

    Matrix4x4 CamToWorldMat = new Matrix4x4();
    Matrix4x4 ProjMat = new Matrix4x4();

    public PhotoCaptureFrame frameToProcess = null;

    public RawImage RawImg;
    public Text BarcodeText;
    public Text DebugText;

    // Camera state
    public bool isCamEnabled { get; set; }
    public bool isProcessing { get; set; }

    void Awake()
    {
        isCamEnabled = false;
        isProcessing = false;  
    }

    // Use this for initialization
    void Start () {
        this.EnableCamCapture();
	}
	
	// Update is called once per frame
	void Update () {
        // DebugText.text = "in update.";
        if (isCamEnabled && !isProcessing)
        {
            pCaptureObj.TakePhotoAsync(OnCapturedPhotoToMemory);
        }   
    }

    void OnDestroy()
    {
        this.ReleaseCamCapture();
    }

    bool EnableCamCapture()
    {
        if (!isCamEnabled)
        {
            camRes = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

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
        if (result.success)
        {
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
            frameToProcess = photoCaptureFrame;

            frameToProcess.TryGetCameraToWorldMatrix(out CamToWorldMat);
            frameToProcess.TryGetProjectionMatrix(0.0f, 1.0f, out ProjMat);

            isProcessing = true;

            #region // Copy capture to texture. //
            // Create our Texture2D for use and set the correct resolution
            Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
            Texture2D targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height);
            // Copy the raw image data into our target texture
            frameToProcess.UploadImageDataToTexture(targetTexture);
            // Do as we wish with the texture such as apply it to a material, etc.
            #endregion

            RawImg.texture = targetTexture;
            RawImg.material.mainTexture = targetTexture;
        }

    }
}
