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
                
                photoCaptureFrame.TryGetCameraToWorldMatrix(out CamToWorldMat);
                photoCaptureFrame.TryGetProjectionMatrix(out ProjMat);

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

                //DebugText.text = "Copy to byte.\n";

#if !UNITY_EDITOR
#if Homography
                res = reader.Decode(imageBufferList.ToArray(),
                                    ImgWidth, ImgHeight,
                                    ZXing.BitmapFormat.BGRA32);//選擇剛剛新增的圖片進行解碼，並將解碼後的資料回傳


                if (res != null)
                {
                    Debug.Log(res.Text);//將解碼後的資料列印出來

                    BarcodeText.text = "Barcode Text: " + res.Text + "\n";
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

                
                    Vector2 qPt1 = new Vector2((float)BorderInFramePts.ElementAt(0),
                                               (float)BorderInFramePts.ElementAt(1));

                    DebugText.text += "qPt1: " + qPt1.x + ", " + qPt1.y + "\n";                  
                    kuDrawRaycast(qPt1, 0.01f);

                    //Vector2 qPt2 = new Vector2((float)BorderInFramePts.ElementAt(2),
                    //                           (float)BorderInFramePts.ElementAt(3));    
                    //DebugText.text += "qP21: " + qPt2.x + ", " + qPt2.y + "\n";
                    //kuDrawRaycast(qPt2);

                    //Vector2 qPt3 = new Vector2((float)BorderInFramePts.ElementAt(4),
                    //                           (float)BorderInFramePts.ElementAt(5));    
                    //DebugText.text += "qPt3: " + qPt3.x + ", " + qPt3.y + "\n";
                    //kuDrawRaycast(qPt3);

                    //Vector2 qPt4 = new Vector2((float)BorderInFramePts.ElementAt(6),
                    //                           (float)BorderInFramePts.ElementAt(7));    
                    //DebugText.text += "qPt4: " + qPt4.x + ", " + qPt4.y + "\n";
                    //kuDrawRaycast(qPt4);
                }
                else
                {
                    BarcodeText.text = "Not detected.";
                    DebugText.text   += "Not detected.";
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


        private void kuDrawRaycast(Vector2 qPt, float CubeSize)
        {
            Vector2 ImagePosZeroToOne = new Vector2(qPt.x / ImgWidth,
                                                    (float)(1.0 - (qPt.y / (float)ImgHeight)));
            Vector2 ImagePosProjected = new Vector2((float)(ImagePosZeroToOne.x * 2.0 - 1.0),
                                                    (float)(ImagePosZeroToOne.y * 2.0 - 1.0));
            Vector3 aaa = new Vector3(ImagePosProjected.x, ImagePosProjected.y, 1.0f);
            Vector3 CamSpacePos = UnProjectVector(ProjMat, aaa);
            Vector3 CameraSpaceOrigin = new Vector3(0, 0, 0);
            Vector3 WorldSpaceRayPoint1 = CamToWorldMat.MultiplyPoint3x4(CameraSpaceOrigin);
            // camera location in world space
            Vector3 WorldSpaceRayPoint2 = CamToWorldMat.MultiplyPoint3x4(CamSpacePos);
            // ray point in world space

            Vector3 OriVec = WorldSpaceRayPoint2 - WorldSpaceRayPoint1;

            GameObject CamPosCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            CamPosCube.transform.position = WorldSpaceRayPoint1;
            CamPosCube.transform.localScale = new Vector3(CubeSize, CubeSize, CubeSize);   // 單位是公尺
            CamPosCube.transform.forward = OriVec;
            CamPosCube.GetComponent<Renderer>().material.color = Color.blue;

            DebugText.text += "WorldSpaceRayPoint1: ("
                            + WorldSpaceRayPoint1.x + ", "
                            + WorldSpaceRayPoint1.y + ", "
                            + WorldSpaceRayPoint1.z + ")\n";
            

            GameObject WorldPtCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            WorldPtCube.transform.position = WorldSpaceRayPoint2;
            WorldPtCube.transform.localScale = new Vector3(CubeSize, CubeSize, CubeSize);   // 單位是公尺
            WorldPtCube.transform.forward = OriVec;
            WorldPtCube.GetComponent<Renderer>().material.color = Color.red;

            DebugText.text += "WorldSpaceRayPoint2: ("
                            + WorldSpaceRayPoint2.x + ", "
                            + WorldSpaceRayPoint2.y + ", "
                            + WorldSpaceRayPoint2.z + ")\n";

            

            //DebugText.text += "OriVec: ("
            //                + OriVec.x + ", "
            //                + OriVec.y + ", "
            //                + OriVec.z + ")\n";

            //Ray CornerRay = new Ray(WorldSpaceRayPoint2, OriVec);
            //RaycastHit HitInfo;
            //bool HitBool = Physics.Raycast(CornerRay, out HitInfo, 15.0f, Physics.DefaultRaycastLayers);

            //RaycastHit [] Hits = Physics.RaycastAll(rr);

            if (OriVec.magnitude != 1.0f)
            {
                float length = OriVec.magnitude;
                OriVec /= length;
            }

            GameObject LineCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            LineCube.transform.position = WorldSpaceRayPoint1;
            LineCube.transform.forward = OriVec;
            LineCube.transform.localScale = new Vector3(0.1f * CubeSize, 0.1f * CubeSize, 100.0f);   // 單位是公尺

            //DebugText.text += "Hit: " + HitBool.ToString() + "\n";

            //if (HitBool)
            //{
            //    DebugText.text += "HitInfo: " +
            //    HitInfo.transform.position.x + ", " +
            //    HitInfo.transform.position.y + ", " +
            //    HitInfo.transform.position.z + "\n";

            //    GameObject cubeFront = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //    cubeFront.transform.position = HitInfo.transform.position;
            //    cubeFront.transform.forward = OriVec;
            //    cubeFront.transform.localScale = new Vector3(2 * CubeSize, 2 * CubeSize, 2 * CubeSize);   // 單位是公尺
            //    cubeFront.GetComponent<Renderer>().material.color = Color.cyan;

            //}

            //kuDrawLine(OriVec, WorldSpaceRayPoint1, 1000, 0.01f);
        }

        private void kuDrawLine(Vector3 OriVec, Vector3 CenterPt, int Length, float thickness)
        {
            //if (OriVec.magnitude != 1.0f)
            //{
            //    float length = OriVec.magnitude;
            //    OriVec /= length;
            //    //OriVec *= thickness;
            //}

            DebugText.text += "RR\n";

            Ray rr = new Ray(CenterPt, OriVec);
            RaycastHit HitInfo;
            bool HitBool = Physics.Raycast(rr, out HitInfo, 10);
            DebugText.text += "Hit: " + HitBool.ToString() + "\n";
            DebugText.text += "HitInfo: " +
                HitInfo.transform.position.x + ", " +
                HitInfo.transform.position.y + ", " +
                HitInfo.transform.position.z + "\n";

            GameObject cubeFront = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeFront.transform.position = HitInfo.transform.position;
            cubeFront.transform.forward = OriVec;
            cubeFront.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);   // 單位是公尺


            //Vector3 FrontLinePt = new Vector3();
            //Vector3 BackLinePt = new Vector3();

            //for (int i=0;i<Length; i++)
            //{
            //    FrontLinePt = CenterPt + (i * OriVec);
            //    BackLinePt = CenterPt - (i * OriVec);

            //    //GameObject cubeFront = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //    //cubeFront.transform.position = FrontLinePt;
            //    //cubeFront.transform.forward = OriVec;
            //    //cubeFront.transform.localScale = new Vector3(thickness, thickness, thickness);   // 單位是公尺

            //    GameObject cubeBack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //    cubeBack.transform.position = BackLinePt;
            //    cubeBack.transform.forward = OriVec;
            //    cubeBack.transform.localScale = new Vector3(thickness, thickness, thickness);   // 單位是公尺

            //}


        }

        // Update is called once per frame
        void Update()
        {

        }

        void OnTappedEvent()
        {
            DebugText.text = "Tapped.";

            DebugText.text = string.Empty;
            DebugText.text += "Camera Point: "
                            + Camera.main.transform.position.x + ", "
                            + Camera.main.transform.position.y + ", "
                            + Camera.main.transform.position.z + "\n";

            PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
        }
    }
}

