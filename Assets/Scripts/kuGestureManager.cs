using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.WSA.Input;

namespace kuHoloLensUtility
{
    public class kuGestureManager : Singleton<kuGestureManager>
    {

        public Text DebugText;

        public GameObject TargetObject;

        public GestureRecognizer NavigationRecognizer { get; private set; }

        public GestureRecognizer ManipulationRecognizer { get; private set; }

        public GestureRecognizer ActiveRecognizer { get; private set; }
        // will be used if we have multiple GestureRecognizer

        public bool IsNavigating { get; private set; }
        public bool IsManipulating { get; private set; }

        public Vector3 NavigationPosition { get; private set; }
        public Vector3 ManipulationPosition { get; private set; }

        // Use this for initialization
        private void Awake()
        {
            NavigationRecognizer = new GestureRecognizer();

            // Add recognizable gestures.
            NavigationRecognizer.SetRecognizableGestures(GestureSettings.Tap);

            NavigationRecognizer.TappedEvent += NavigationRecognizer_TappedEvent;

            NavigationRecognizer.StartCapturingGestures();
        }

        private void NavigationRecognizer_TappedEvent(InteractionSourceKind source, int tapCount, Ray ray)
        {
            // Call function: 
            // capture image, decode barcode
            // calculate homography, and calculate 3d coordinate of barcode corners.
            TargetObject.SendMessageUpwards("OnTappedEvent");
        }

        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}

