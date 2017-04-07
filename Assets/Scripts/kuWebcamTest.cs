using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

#if !UNITY_EDITOR
using ZXing;
using ZXing.Common;
using ZXing.Common.Detector;
using ZXing.Multi.QrCode.Internal;
using kuOpenCVSharp;
#endif

public class kuWebcamTest : MonoBehaviour {

    public RawImage RawImg;

    public Text BarcodeText;
    public Text DebugText;

    private WebCamTexture myCamTex;

    public int aa;
    public int bb;
    private bool cc;

#if !UNITY_EDITOR
    //private BarcodeReader reader = new BarcodeReader();   
    private IBarcodeReader reader = new ZXing.BarcodeReader();
    private kuOpenCVSharpWrapper Wrapper = new kuOpenCVSharpWrapper();
    private Result res;
#endif

    // Use this for initialization
    void Start () {

#if !UNITY_EDITOR
        //DebugText.text = "Before new.";
        //DebugText.text = "After new.";

        //List<double> aaa = new List<double>();
        //List<double> bbb = new List<double>();
        //List<double> ccVec = new List<double>();
        
        //aaa.Add(75.0);    aaa.Add(255.0);
        //aaa.Add(75.0);    aaa.Add(75.0);
        //aaa.Add(255.0);   aaa.Add(75.0);
        //aaa.Add(225.0);   aaa.Add(225.0);
        
        //bbb.Add(159.0);   bbb.Add(243.5);
        //bbb.Add(162.0);   bbb.Add(133.0);
        //bbb.Add(261.0);   bbb.Add(129.0);
        //bbb.Add(243.5);   bbb.Add(222.5);

        //DebugText.text = "Set aa bb.";

        //cc = Wrapper.kuCalHomographySharp(aaa, bbb);

        //ccVec.Add(40.0);  ccVec.Add(290.0);
        //ccVec.Add(40.0);  ccVec.Add(40.0);
        //ccVec.Add(290.0); ccVec.Add(40.0);
        //ccVec.Add(290.0); ccVec.Add(290.0);

        //var dd = Wrapper.kuPerspectiveTransformSharp(ccVec);

        //DebugText.text = cc.ToString() + "\n";
        
        //foreach (var item in dd)
        //{
        //    DebugText.text += " " + item.ToString();
        //}
#endif

        myCamTex = new WebCamTexture();
        RawImg.texture = myCamTex;
        RawImg.material.mainTexture = myCamTex;
        myCamTex.Play();
    }

    private static byte[] Color32ArrayToByteArray(Color32[] colors)
    {
        if (colors == null || colors.Length == 0)
            return null;

        int lengthOfColor32 = Marshal.SizeOf(typeof(Color32));
        int length = lengthOfColor32 * colors.Length;
        byte[] bytes = new byte[length];

        GCHandle handle = default(GCHandle);
        try
        {
            handle = GCHandle.Alloc(colors, GCHandleType.Pinned);
            IntPtr ptr = handle.AddrOfPinnedObject();
            Marshal.Copy(ptr, bytes, 0, length);
        }
        finally
        {
            if (handle != default(GCHandle))
                handle.Free();
        }

        return bytes;
    }

    

    // Update is called once per frame
    void Update()
    {
#if !UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            DebugText.text = "kerker.";

            res = reader.Decode(Color32ArrayToByteArray(myCamTex.GetPixels32()),
                                myCamTex.width, myCamTex.height,
                                ZXing.BitmapFormat.BGRA32);//選擇剛剛新增的圖片進行解碼，並將解碼後的資料回傳
            

            if (res != null)
            {
                Debug.Log(res.Text);//將解碼後的資料列印出來

                BarcodeText.text = "Barcode Text: " + res.Text;
                DebugText.text = string.Empty;
                DebugText.text = "PtsNum: " + res.ResultPoints.Length + "\n";
                DebugText.text = "Detected result points:\n";
                //foreach (ResultPoint r in res.ResultPoints)
                //{
                //    DebugText.text += " (" + r.X + ", " + r.Y + ")";
                //}
                //DebugText.text += "\n";
            
                List<double> PatternPts       = new List<double>();
                List<double> DetectedPts      = new List<double>();
                List<double> PatternBorderPts = new List<double>();
        
                PatternPts.Add(75.0);    PatternPts.Add(255.0);
                PatternPts.Add(75.0);    PatternPts.Add(75.0);
                PatternPts.Add(255.0);   PatternPts.Add(75.0);
                PatternPts.Add(225.0);   PatternPts.Add(225.0);
                
                foreach (ResultPoint r in res.ResultPoints)
                {
                    DetectedPts.Add(r.X);   DetectedPts.Add(myCamTex.height - r.Y);
                    DebugText.text += " (" + r.X + ", " + (myCamTex.height - r.Y) + ")";
                }
                DebugText.text += "\n";

                bool cc = Wrapper.kuCalHomographySharp(PatternPts, DetectedPts);                
                DebugText.text += "Homography: " + cc + "\n";

                PatternBorderPts.Add(40.0);   PatternBorderPts.Add(290.0);
                PatternBorderPts.Add(40.0);   PatternBorderPts.Add(40.0);
                PatternBorderPts.Add(290.0);  PatternBorderPts.Add(40.0);
                PatternBorderPts.Add(290.0);  PatternBorderPts.Add(290.0);

                var dd = Wrapper.kuPerspectiveTransformSharp(PatternBorderPts);

                foreach (var item in dd)
                {
                    DebugText.text += " " + item.ToString();
                }
            }
            else
            {
                BarcodeText.text = "Not detected.";
                DebugText.text = "NULL.";
            }
        }
#endif
    }   // Update() end.
}
