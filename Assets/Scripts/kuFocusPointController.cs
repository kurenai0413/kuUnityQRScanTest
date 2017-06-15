using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.VR.WSA;

public class kuFocusPointController : MonoBehaviour {

    public float DefaultStartDistance = 2.0f;
    public float MinDistance = 0.1f;

    public float FocusPointDistance { get; private set; }
    public Vector3 FocusPointPosition { get; private set; }

    public Vector3 FocusPointNormal { get { return -Camera.main.transform.forward; } }

    private const float LerpPowerCloser = 7.0f;
    private const float LerpPowerFarther = 10.0f;

    void Awake()
    {
        if (Camera.main == null)
        {
            Debug.LogError("You need to choose a main camera that will be used for the scene");
            this.enabled = false;
            return;
        }

        this.MinDistance = Camera.main.nearClipPlane + this.MinDistance;
        Debug.Log("near clip plan: " + Camera.main.nearClipPlane + ", minDistance: " + MinDistance.ToString());
        this.FocusPointDistance = this.DefaultStartDistance;
        this.FocusPointPosition = Camera.main.transform.position
                                + (Camera.main.transform.forward * this.FocusPointDistance);
    }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    void LateUpdate()
    {
        // use the last location
        var newFocusPos = Camera.main.transform.position
                        + (Camera.main.transform.forward * this.FocusPointDistance);

        // determine its distance to that point
        var newFocusPointDistance = (Camera.main.transform.position - newFocusPos).magnitude;
        
        // otherwise, lerp based on whether we are going away from the camera
        if (newFocusPointDistance > this.FocusPointDistance)
        {
            this.FocusPointDistance
                = Mathf.Lerp(this.FocusPointDistance, 
                             newFocusPointDistance, 
                             LerpPowerFarther * Time.deltaTime);
        }
        else
        {
            this.FocusPointDistance
                = Mathf.Lerp(newFocusPointDistance, 
                             this.FocusPointDistance, 
                             LerpPowerCloser * Time.deltaTime);
        }

        if (this.FocusPointDistance <= this.MinDistance)
        {
            this.FocusPointDistance = this.MinDistance;
        }

        // set the position
        this.FocusPointPosition
            = Camera.main.transform.position
            + (Camera.main.transform.forward * this.FocusPointDistance);

        HolographicSettings.SetFocusPointForFrame(this.FocusPointPosition, 
                                                  this.FocusPointNormal);
    }
}
