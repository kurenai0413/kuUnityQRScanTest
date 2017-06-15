using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class kuCursorManager : MonoBehaviour {

    public GameObject CursorOnHologram;

    public GameObject CursorOffHologram;

    public float DistanceFromCollision = 0.01f;

    private void Awake()
    {
        if (CursorOnHologram != null)
        {
            CursorOnHologram.SetActive(false);
        }
        if (CursorOffHologram != null)
        {
            CursorOffHologram.SetActive(false);
        }

        if (kuGazeManager.Instance == null)
        {
            Debug.LogWarning("CursorManager requires a GazeManager in your scene.");
            enabled = false;
        }
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private void LateUpdate()
    {
        // Enable/Disable the cursor based whether gaze hit a hologram
        if (CursorOnHologram != null)
        {
            CursorOnHologram.SetActive(kuGazeManager.Instance.Hit);
        }
        if (CursorOffHologram != null)
        {
            CursorOffHologram.SetActive(!kuGazeManager.Instance.Hit);
        }

        // Place the cursor at the calculated position.
        gameObject.transform.position = kuGazeManager.Instance.Position
                                      + kuGazeManager.Instance.Normal * DistanceFromCollision;

        // Orient the cursor to match the surface being gazed at.
        gameObject.transform.up = kuGazeManager.Instance.Normal;
    }
}
