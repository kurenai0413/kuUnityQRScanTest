#define Homography

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.WSA.WebCam;
using UnityEngine.UI;

#if !UNITY_EDITOR
using ZXing;
using ZXing.Common;
using ZXing.Common.Detector;
using ZXing.Multi.QrCode.Internal;
using kuOpenCVSharp;
#endif

public class kuQRCornerConstructor : Singleton<kuQRCornerConstructor> {

    bool isFrameCaptured = false;

    Matrix4x4 CamToWorldMat = new Matrix4x4();
    Matrix4x4 ProjMat       = new Matrix4x4();

    PhotoCaptureFrame   frameToProcess;

    public Text BarcodeText;
    public Text DebugText;

    // Use this for initialization
    void Start () {

    }
	
	// Update is called once per frame
	void Update () {
        isFrameCaptured = kuCamManager.Instance.isProcessing;

		if (isFrameCaptured)
        {
            frameToProcess = kuCamManager.Instance.frameToProcess;

            DebugText.text = frameToProcess.dataLength.ToString();
        }
	}
}
