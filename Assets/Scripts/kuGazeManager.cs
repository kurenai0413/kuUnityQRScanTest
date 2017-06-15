using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class kuGazeManager : Singleton<kuGazeManager> {

    public float MaxGazeDistance = 15.0f;

    public LayerMask RaycastLayerMask = Physics.DefaultRaycastLayers;

    public RaycastHit HitInfo { get; private set; }

    public bool Hit { get; private set; }

    public Vector3 Position { get; private set; }

    public Vector3 Normal { get; private set; }

    public GameObject FocusedObject { get; private set; }


    private Vector3     gazeOrigin;
    private Vector3     gazeDirection;
    private Quaternion  gazeRotation;
    private float lastHitDistance = 15.0f;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        gazeOrigin    = Camera.main.transform.position;
        gazeDirection = Camera.main.transform.forward;
        gazeRotation  = Camera.main.transform.rotation;

        HitInfo = kuUpdateRaycast();
	}

    private RaycastHit kuUpdateRaycast()
    {
        RaycastHit hitInfo;

        Hit = Physics.Raycast(gazeOrigin, gazeDirection, out hitInfo, MaxGazeDistance, RaycastLayerMask);

        if (Hit)
        {
            Position = hitInfo.point;
            Normal = hitInfo.normal;
            lastHitDistance = hitInfo.distance;
            FocusedObject = hitInfo.collider.gameObject;
        }
        else
        {
            // If the raycast does not hit a hologram, default the position to last hit distance in front of the user,
            // and the normal to face the user.
            Position = gazeOrigin + (gazeDirection * lastHitDistance);
            Normal = -gazeDirection;
            FocusedObject = null;
        }

        return hitInfo;
    }
}
