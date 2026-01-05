using MediaPipe.BlazeFace;
using UnityEngine;

namespace Tutorial_4
{
    public class HeadTracker : MonoBehaviour
    {
        [Tooltip("Index of your webcam.")]
        [SerializeField] private int webcamIndex = 0;
        [Tooltip("Threshold of the face detector")]
        [Range(0f, 1f)] 
        [SerializeField] private float threshold = 0.5f;
        [SerializeField] private ResourceSet resources;
        [Tooltip("Focal length of your webcam in pixels")]
        [SerializeField] private int focalLength = 799;
        [Tooltip("Distance between your eyes in meters.")]
        [SerializeField] private float ipd = 0.061f;
        // distance to screen = 0.56m
        public Vector3 DetectedFace { get; private set; }

        private FaceDetector _detector;
        private WebCamTexture _webCamTexture;

        private void Start()
        {
            _detector = new FaceDetector(resources);
            
            // Source - https://stackoverflow.com/a
            // Posted by S.Richmond
            // Retrieved 2025-11-19, License - CC BY-SA 3.0

            var devices = WebCamTexture.devices;
            /*
            foreach (var device in devices)
            {
                Debug.Log(device.name);
            }
            */
            if (devices.Length == 0)
            {
                Debug.LogWarning("No webcam found");
                return;
            }
            
            var device = devices[webcamIndex];
            _webCamTexture = new WebCamTexture(device.name);
            _webCamTexture.Play();
        }

        private void OnDestroy()
        {
            _detector?.Dispose();
        }

        private void Update()
        {
            if (_webCamTexture == null)
            {
                return;
            }
            
            _detector.ProcessImage(_webCamTexture, threshold);
            if (_detector.Detections.Length == 0)
            {
                DetectedFace = Vector3.zero;
                return;
            }
            
            SetCameraPosition(_detector.Detections[0]);
        }

        private void SetCameraPosition(Detection face)
        {
            // template code:
            DetectedFace = face.leftEye;

            // Calculate eye positions in pixel coordinates
            Vector2 leftEyePC = new Vector2(_webCamTexture.width * face.leftEye.x, _webCamTexture.height * (1.0f - face.leftEye.y));
            Vector2 rightEyePC = new Vector2(_webCamTexture.width * face.rightEye.x, _webCamTexture.height * (1.0f - face.rightEye.y));

            print("Left eye position: " + leftEyePC);
            print("Right eye position: " + rightEyePC);
            print("ipd in UV: " + Vector3.Distance(leftEyePC, rightEyePC));
        }
    }
}