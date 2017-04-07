#define Homography
//#define PrintHographyResult

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.WSA.WebCam;
using System.IO;
using System.Linq;

#if !UNITY_EDITOR
using ZXing;
using ZXing.Common;
using ZXing.Common.Detector;
using ZXing.Multi.QrCode.Internal;
using kuOpenCVSharp;
#endif


namespace kuHoloLensUtility
{
    public class kuGestureAction : MonoBehaviour
    {
        public Text BarcodeText;
        public Text DebugText;

        public RawImage RawImg;

        int ImgWidth;
        int ImgHeight;

        PhotoCapture photoCaptureObject = null;
        Texture2D targetTexture = null;

        Matrix4x4 CamToWorldMat = new Matrix4x4();
        Matrix4x4 ProjMat = new Matrix4x4();

#if !UNITY_EDITOR 
        private IBarcodeReader reader = new ZXing.BarcodeReader();
        private kuOpenCVSharpWrapper Wrapper = new kuOpenCVSharpWrapper();
        private Result res;
#endif

        // Use this for initialization
        void Start()
        {
            
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

            ImgWidth  = c.cameraResolutionWidth;
            ImgHeight = c.cameraResolutionHeight;

            DebugText.text = ImgWidth + " x " + ImgHeight;

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

        void OnCapturedPhotoToDisk(PhotoCapture.PhotoCaptureResult result)
        {
            if (result.success)
            {
                //DebugText.text += "\nSaved Photo to disk!";
                photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
            }
            else
            {
                DebugText.text = "Failed to save Photo to disk";
            }
        }

        void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
        {
            List<double> PatternPts = new List<double>();
            List<double> DetectedPts = new List<double>();
            List<double> PatternBorderPts = new List<double>();
            List<double> BorderInFramePts = new List<double>();

            if (result.success)
            {
                DebugText.text = "Capturing....";

                #region // Copy capture to texture. //
                // Create our Texture2D for use and set the correct resolution
                Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
                Texture2D targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height);
                // Copy the raw image data into our target texture
                photoCaptureFrame.UploadImageDataToTexture(targetTexture);
                // Do as we wish with the texture such as apply it to a material, etc.
                #endregion

                #region // Copy to byte array. //
                List<byte> imageBufferList = new List<byte>();
                // Copy the raw IMFMediaBuffer data into our empty byte list.
                photoCaptureFrame.CopyRawImageDataIntoBuffer(imageBufferList);
                #endregion

                DebugText.text = "Copy to byte.\n";


#if !UNITY_EDITOR
#if Homography
                res = reader.Decode(imageBufferList.ToArray(),
                                    ImgWidth, ImgHeight,
                                    ZXing.BitmapFormat.BGRA32);//選擇剛剛新增的圖片進行解碼，並將解碼後的資料回傳


                if (res != null)
                {
                    Debug.Log(res.Text);//將解碼後的資料列印出來

                    BarcodeText.text = "Barcode Text: " + res.Text + "\n";
                    DebugText.text = string.Empty;
                    //BarcodeText.text += "PtsNum: " + res.ResultPoints.Length + "\n";
                    BarcodeText.text += "Detected result points:\n";

                    foreach (ResultPoint r in res.ResultPoints)
                    {
                        DetectedPts.Add(r.X);   DetectedPts.Add(r.Y);
                        BarcodeText.text += " (" + r.X + ", " +  r.Y + ")";
                    }
                    BarcodeText.text += "\n";

        
                    PatternPts.Add(75.0);    PatternPts.Add(255.0);
                    PatternPts.Add(75.0);    PatternPts.Add(75.0);
                    PatternPts.Add(255.0);   PatternPts.Add(75.0);
                    PatternPts.Add(225.0);   PatternPts.Add(225.0);

                    bool cc = Wrapper.kuCalHomographySharp(PatternPts, DetectedPts);                
                    BarcodeText.text += "Homography: " + cc + "\n";

                    PatternBorderPts.Add(40.0);   PatternBorderPts.Add(290.0);
                    PatternBorderPts.Add(40.0);   PatternBorderPts.Add(40.0);
                    PatternBorderPts.Add(290.0);  PatternBorderPts.Add(40.0);
                    PatternBorderPts.Add(290.0);  PatternBorderPts.Add(290.0);


                    var dd = Wrapper.kuPerspectiveTransformSharp(PatternBorderPts);

                    foreach (var item in dd)
                    {
                        BarcodeText.text += " " + item.ToString();

                        BorderInFramePts.Add((double)item);
                    }
                }
                else
                {
                    BarcodeText.text = "Not detected.";
                    DebugText.text   = "Not detected.";
                }
#endif
#endif

                RawImg.texture = targetTexture;
                RawImg.material.mainTexture = targetTexture;
            }


            // Clean up
            photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);

            //string filename = string.Format(@"CapturedImage{0}_n.jpg", Time.time);
            //string filePath = System.IO.Path.Combine(Application.persistentDataPath, filename);
            //DebugText.text += "\nsave to " + filePath + filename;
            //photoCaptureObject.TakePhotoAsync(filePath, PhotoCaptureFileOutputFormat.JPG, OnCapturedPhotoToDisk);

        }

        void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
        {
            photoCaptureObject.Dispose();
            photoCaptureObject = null;
        }

        public static Vector3 UnProjectVector(Matrix4x4 proj, Vector3 to)
        {
            Vector3 from = new Vector3(0, 0, 0);
            var axsX = proj.GetRow(0);
            var axsY = proj.GetRow(1);
            var axsZ = proj.GetRow(2);
            from.z = to.z / axsZ.z;
            from.y = (to.y - (from.z * axsY.z)) / axsY.y;
            from.x = (to.x - (from.z * axsX.z)) / axsX.x;
            return from;
        }

        // Update is called once per frame
        void Update()
        {

        }

        void OnTappedEvent()
        {
            DebugText.text = "Tapped.";

            PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
        }
    }
}

