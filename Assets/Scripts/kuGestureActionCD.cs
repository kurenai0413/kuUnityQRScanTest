using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.WSA.WebCam;
using UnityEngine.UI;
using System.Linq;

public class kuGestureActionCD : MonoBehaviour {

    private PhotoCapture    photoCaptureObject = null;
    private Texture2D       targetTexture      = null;

    Matrix4x4 CamToWorldMat = new Matrix4x4();
    Matrix4x4 ProjMat       = new Matrix4x4();

    private int ImgWidth;
    private int ImgHeight;

    public Text BarcodeText;
    public Text DebugText;


    // Use this for initialization
    void Start () {
        PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    void OnPhotoCaptureCreated(PhotoCapture captureObject)
    {
        photoCaptureObject = captureObject;

        Resolution cameraResolution
            = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

        CameraParameters c = new CameraParameters();
        c.hologramOpacity = 0.0f;
        c.cameraResolutionWidth = cameraResolution.width;
        c.cameraResolutionHeight = cameraResolution.height;
        c.pixelFormat = CapturePixelFormat.BGRA32;

        ImgWidth = c.cameraResolutionWidth;
        ImgHeight = c.cameraResolutionHeight;

        //sDebugText.text = ImgWidth + " x " + ImgHeight;

        captureObject.StartPhotoModeAsync(c, OnPhotoModeStarted);
    }

    private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
    {
        if (result.success)
        {
            photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
        }
        else
        {
            DebugText.text = "Unable to start photo mode!";
        }
    }

    void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {

    }
}
