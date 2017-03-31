using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if !UNITY_EDITOR
using kuOpenCVSharp;
#endif

public class kuAPITest : MonoBehaviour {
    public Text DebugText;
    public int aa;
    public int bb;
    private bool cc;

    // Use this for initialization
    void Start()
    {
#if !UNITY_EDITOR
        DebugText.text = "Before new.";

        //kuOpenCVSharpWrapper Wrapper = new kuOpenCVSharpWrapper();        
        kuOpenCVSharpWrapper Wrapper = new kuOpenCVSharpWrapper();

        DebugText.text = "After new.";

        List<double> aaa = new List<double>();
        List<double> bbb = new List<double>();
        List<double> ccVec = new List<double>();
        
        aaa.Add(75.0);    aaa.Add(255.0);
        aaa.Add(75.0);    aaa.Add(75.0);
        aaa.Add(255.0);   aaa.Add(75.0);
        aaa.Add(225.0);   aaa.Add(225.0);
        
        bbb.Add(159.0);   bbb.Add(243.5);
        bbb.Add(162.0);   bbb.Add(133.0);
        bbb.Add(261.0);   bbb.Add(129.0);
        bbb.Add(243.5);   bbb.Add(222.5);

        DebugText.text = "Set aa bb.";

        cc = Wrapper.kuCalHomographySharp(aaa, bbb);

        ccVec.Add(40.0);  ccVec.Add(290.0);
        ccVec.Add(40.0);  ccVec.Add(40.0);
        ccVec.Add(290.0); ccVec.Add(40.0);
        ccVec.Add(290.0); ccVec.Add(290.0);

        var dd = Wrapper.kuPerspectiveTransformSharp(ccVec);

        DebugText.text = cc.ToString() + "\n";
        
        foreach (var item in dd)
        {
            DebugText.text += " " + item.ToString();
        }
#endif

    }

    // Update is called once per frame
    void Update () {
		
	}
}
