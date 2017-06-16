#define Homography
//#define PrintHographyResult

using System;
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

        private const int CapFrameNeeded = 4;

        bool        isFocusLocCalculated = false;
        Vector3     FocusPlaneNormal;
        Vector3     FocusPlanePoint;

        public GameObject CalibCube;

        public Text BarcodeText;
        public Text DebugText;

        public RawImage RawImg;

        int ImgWidth;
        int ImgHeight;

        int CapFrameCnt;

        PhotoCapture photoCaptureObject = null;
        Texture2D targetTexture = null;

        Matrix4x4 CamToWorldMat = new Matrix4x4();
        Matrix4x4 ProjMat = new Matrix4x4();

        private Vector3 [] CapFrameCamPos = new Vector3 [CapFrameNeeded];
        private Vector3 [,] CapFramePtsVec = new Vector3 [CapFrameNeeded, 4];

        private GameObject[] LineObj = new GameObject[CapFrameNeeded];
        private GameObject TestCube;

#if !UNITY_EDITOR 
        private IBarcodeReader reader = new ZXing.BarcodeReader();
        private kuOpenCVSharpWrapper Wrapper = new kuOpenCVSharpWrapper();
        private Result res;
#endif

        private void Awake()
        {
            CapFrameCnt = 0;
        }

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
            Vector3[,] BorderPts3D = new Vector3[CapFrameNeeded - 1,4];

            if (result.success)
            {
                
                photoCaptureFrame.TryGetCameraToWorldMatrix(out CamToWorldMat);
                photoCaptureFrame.TryGetProjectionMatrix(0.0f, 1.0f, out ProjMat);

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

                    Vector3 PtSum = new Vector3(0, 0, 0);
                    for (int i=0;i<4;i++)
                    {
                        Vector3 CamViewPt = new Vector3(0, 0, 0);
                        Vector2 qPt = new Vector2((float)BorderInFramePts.ElementAt(2 * i),
                                                  (float)BorderInFramePts.ElementAt(2 * i + 1));                 
                        kuDrawRaycast(LineObj[i], qPt, 0.01f, 
                                      out CapFrameCamPos[CapFrameCnt], out CamViewPt, 
                                      out CapFramePtsVec[CapFrameCnt, i]);                     
                    }

                    CapFrameCnt++;
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

                if (CapFrameCnt == CapFrameNeeded)
                {
                    DebugText.text += "Frame captured.";
                    // Calculate intersection here.

                    Vector3[] AveragedBorderPt = new Vector3[4];
                    for (int i=0;i<4;i++)
                    {
                        AveragedBorderPt[i] = new Vector3(0, 0, 0);
                    }

                    for (int fcnt = 0; fcnt < CapFrameNeeded - 1; fcnt++)
                    {
                        float[] PtsLineDist = new float[4];
                        
                        for (int i = 0; i < 4; i++)
                        {
                            BorderPts3D[fcnt, i]
                                = kuCalculate3DPoint(CapFrameCamPos[fcnt], CapFrameCamPos[fcnt+1],
                                                     CapFramePtsVec[fcnt, i], CapFramePtsVec[fcnt+1, i],
                                                     out PtsLineDist[i]);

                            AveragedBorderPt[i] += BorderPts3D[fcnt, i];

                            //GameObject PtsCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            //PtsCube.transform.position = BorderPts3D[fcnt, i];
                            //PtsCube.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);   // 單位是公尺
                            //PtsCube.transform.forward = CapFramePtsVec[0, i];
                            //PtsCube.GetComponent<Renderer>().material.color = Color.red;
                        }
                    }

                    Vector3 CenterPt = new Vector3(0, 0, 0);
                    for (int i = 0; i < 4; i++)
                    {
                        AveragedBorderPt[i] /= (CapFrameNeeded - 1);
                        CenterPt += AveragedBorderPt[i];

                        //GameObject PtsCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        //PtsCube.transform.position = AveragedBorderPt[i];
                        //PtsCube.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);   // 單位是公尺
                        //PtsCube.transform.forward = CapFramePtsVec[0, i];
                        //PtsCube.GetComponent<Renderer>().material.color = Color.green;

                        kuSetCube(AveragedBorderPt[i], CapFramePtsVec[0, i], 0.005f, Color.green);
                    }
                    CenterPt /= 4;


                    Vector3 CubeRight = AveragedBorderPt[2] - AveragedBorderPt[1];
                    CubeRight /= CubeRight.magnitude;
                    Vector3 CubeUp = AveragedBorderPt[0] - AveragedBorderPt[1];
                    CubeUp /= CubeUp.magnitude;
                    Vector3 CubeFoward = Vector3.Cross(CubeRight, CubeUp);
                    CubeFoward /= CubeFoward.magnitude;

                    // Draw a cube at position of TestPt
                    //GameObject PatternCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    //PatternCube.transform.position = CenterPt;
                    //PatternCube.transform.localScale = new Vector3(0.09f, 0.09f, 0.09f);   // 單位是公尺
                    //PatternCube.transform.forward = CubeFoward;
                    //PatternCube.transform.right = CubeRight;
                    //PatternCube.GetComponent<Renderer>().material.color = Color.red;

                    kuSetCube(TestCube, CenterPt+0.045f*CubeFoward, 
                              CubeFoward, CubeRight, 0.09f, Color.red);
                    FocusPlanePoint = CenterPt + 0.045f * CubeFoward;

                    isFocusLocCalculated = true;
        

                    //for (int i=0; i<4;i++)
                    //{
                    //    LineObj[i].SetActive(false);
                    //}
                }
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


        private void kuDrawRaycast(GameObject rayCastObj, Vector2 qPt, float CubeSize, 
                                   out Vector3 CamPosPt, out Vector3 CamViewPt, 
                                   out Vector3 PatternPtVec)
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
            CamPosPt  = WorldSpaceRayPoint1;
            CamViewPt = WorldSpaceRayPoint2;

            Vector3 OriVec = WorldSpaceRayPoint2 - WorldSpaceRayPoint1;
            if (OriVec.magnitude != 1.0f)
            {
                float length = OriVec.magnitude;
                OriVec /= length;
            }
            PatternPtVec = OriVec;

            //GameObject CamPosCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //CamPosCube.transform.position = WorldSpaceRayPoint1;
            //CamPosCube.transform.localScale = new Vector3(CubeSize, CubeSize, CubeSize);   // 單位是公尺
            //CamPosCube.transform.forward = OriVec;
            //CamPosCube.GetComponent<Renderer>().material.color = Color.blue;

            kuSetCube(WorldSpaceRayPoint1, OriVec, CubeSize, Color.blue);

            //Ray CornerRay = new Ray(WorldSpaceRayPoint2, OriVec);
            //RaycastHit HitInfo;
            //bool HitBool = Physics.Raycast(CornerRay, out HitInfo, 15.0f, Physics.DefaultRaycastLayers);

            //RaycastHit [] Hits = Physics.RaycastAll(rr);

            rayCastObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rayCastObj.transform.position = WorldSpaceRayPoint1;
            rayCastObj.transform.forward = OriVec;
            rayCastObj.transform.localScale = new Vector3(0.05f * CubeSize, 0.05f * CubeSize, 2.0f);   // 單位是公尺      
        }


        private Vector3 kuCalculate3DPoint(Vector3 CamPosA, Vector3 CamPosB, Vector3 LineVecA, Vector3 LineVecB, out float OrthoDist)
        {
            double delta, deltatt, deltarr;		// 解公垂線用的(二元一次方程式行列式解)
            double [,] OrthoEQ = new double[2,3];
            Vector3 Pt3D = new Vector3();
            Vector3 OrthoPtA = new Vector3();
            Vector3 OrthoPtB = new Vector3();

            OrthoEQ[0, 0] = Math.Pow(LineVecA.x, 2) + Math.Pow(LineVecA.y, 2) + Math.Pow(LineVecA.z, 2);     // 0
            OrthoEQ[0, 1] = -(LineVecA.x * LineVecB.x + LineVecA.y * LineVecB.y + LineVecA.z * LineVecB.z);  // 1
            OrthoEQ[0, 2] = LineVecA.x * CamPosB.x + LineVecA.y * CamPosB.y + LineVecA.z * CamPosB.z         // 2
                          - LineVecA.x * CamPosA.x - LineVecA.y * CamPosA.y - LineVecA.z * CamPosA.z;

            OrthoEQ[1, 0] = LineVecA.x * LineVecB.x + LineVecA.y * LineVecB.y + LineVecA.z * LineVecB.z;     // 3
            OrthoEQ[1, 1] = -(Math.Pow(LineVecB.x, 2) + Math.Pow(LineVecB.y, 2) + Math.Pow(LineVecB.z, 2));  // 4
            OrthoEQ[1, 2] = LineVecB.x * CamPosB.x + LineVecB.y * CamPosB.y + LineVecB.z * CamPosB.z         // 5
                          - LineVecB.x * CamPosA.x - LineVecB.y * CamPosA.y - LineVecB.z * CamPosA.z;

            delta   = OrthoEQ[0, 0] * OrthoEQ[1, 1] - OrthoEQ[0, 1] * OrthoEQ[1, 0];
            deltatt = OrthoEQ[0, 2] * OrthoEQ[1, 1] - OrthoEQ[0, 1] * OrthoEQ[1, 2];
            deltarr = OrthoEQ[0, 0] * OrthoEQ[1, 2] - OrthoEQ[0, 2] * OrthoEQ[1, 0];

            double LineScaleA = deltatt / delta;
            double LineScaleB = deltarr / delta;

            OrthoPtA.x = CamPosA.x + (float)LineScaleA * LineVecA.x;
            OrthoPtA.y = CamPosA.y + (float)LineScaleA * LineVecA.y;
            OrthoPtA.z = CamPosA.z + (float)LineScaleA * LineVecA.z;

            OrthoPtB.x = CamPosB.x + (float)LineScaleB * LineVecB.x;
            OrthoPtB.y = CamPosB.y + (float)LineScaleB * LineVecB.y;
            OrthoPtB.z = CamPosB.z + (float)LineScaleB * LineVecB.z;

            float SkewLineDist = Vector3.Distance(OrthoPtA, OrthoPtA);
            OrthoDist = SkewLineDist;

            Pt3D.x = (OrthoPtA.x + OrthoPtB.x) / 2;
            Pt3D.y = (OrthoPtA.y + OrthoPtB.y) / 2;
            Pt3D.z = (OrthoPtA.z + OrthoPtB.z) / 2;

            return Pt3D;
        }

        // Update is called once per frame
        void Update()
        {
            if (isFocusLocCalculated)
            {
                DebugText.text = "QQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQ";

                FocusPlaneNormal = -Camera.main.transform.forward;     // Normally the normal is best set to be the opposite of the main camera's forward vector
                                                                       // If the content is actually all on a plane (like text), set the normal to the normal of the plane
                                                                                           // and ensure the user does not pass through the plane
                DebugText.text = "Normal:" + FocusPlaneNormal.x + ", "
                                           + FocusPlaneNormal.y + ", "
                                           + FocusPlaneNormal.z;
                DebugText.text = "\nPosition:" + FocusPlanePoint.x + ", "
                                               + FocusPlanePoint.y + ", "
                                               + FocusPlanePoint.z;
                UnityEngine.VR.WSA.HolographicSettings.SetFocusPointForFrame(FocusPlanePoint, FocusPlaneNormal);
            }
        }

        void OnTappedEvent()
        {
            DebugText.text = "Tapped.";

            if (CapFrameCnt < CapFrameNeeded)
            {
                DebugText.text = string.Empty;
                DebugText.text += "Camera Point: "
                                + Camera.main.transform.position.x + ", "
                                + Camera.main.transform.position.y + ", "
                                + Camera.main.transform.position.z + "\n";

                PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
            }
        }

        void OnDestroy()
        {

        }

        void kuSetCube(Vector3 position, float cubeSize, Color color)
        {
            GameObject CubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            CubeObj.transform.position = position;
            CubeObj.transform.localScale = new Vector3(cubeSize, cubeSize, cubeSize);   // 單位是公尺
            CubeObj.GetComponent<Renderer>().material.color = color;
        }

        void kuSetCube(Vector3 position, Vector3 forwardVec, Vector3 rightVec, float cubeSize, Color color)
        {
            GameObject CubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            CubeObj.transform.position = position;
            CubeObj.transform.localScale = new Vector3(cubeSize, cubeSize, cubeSize);   // 單位是公尺
            //CubeObj.transform.forward = forwardVec;
            CubeObj.transform.right = rightVec;
            CubeObj.GetComponent<Renderer>().material.color = color;
        }

        void kuSetCube(GameObject cubeObj, Vector3 position, Vector3 forwardVec, Vector3 rightVec, float cubeSize, Color color)
        {
            cubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeObj.transform.position = position;
            cubeObj.transform.localScale = new Vector3(cubeSize, cubeSize, cubeSize);   // 單位是公尺
            cubeObj.transform.forward = forwardVec;
            cubeObj.transform.right   = rightVec;
            cubeObj.GetComponent<Renderer>().material.color = color;
        }

        void kuSetCube(Vector3 position, Vector3 forwardVec, float cubeSize, Color color)
        {
            GameObject CubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            CubeObj.transform.position = position;
            CubeObj.transform.localScale = new Vector3(cubeSize, cubeSize, cubeSize);   // 單位是公尺
            CubeObj.transform.forward = forwardVec;
            CubeObj.GetComponent<Renderer>().material.color = color;
        }
    }
}

