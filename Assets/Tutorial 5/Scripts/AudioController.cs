using System;
using UnityEngine;

namespace Tutorial_5
{
    public enum EffectType { None, ILD, ITD }
    
    [RequireComponent(typeof(AudioSource))]
    public class AudioController : MonoBehaviour
    {
        [SerializeField] private EffectType currentEffect = EffectType.None;
        [Tooltip("Use position of player and audio source in the scene instead of the stereo pan slider")]
        [SerializeField] private bool useScene;
        
        //[Range(0, 1)]
        //[SerializeField] private float volume = 1f;
        //[Range(-90, 90)] [Tooltip("0 is left, 1 is right")]
        //[SerializeField] private float angle = 0.5f;
        [Tooltip("Maximum ITD delay in milliseconds")]
        [SerializeField] private float maxHaasDelay = 20f;
        [SerializeField] private int sampleRate = 44100;

        [Header("References")]
        [SerializeField] private AudioClip audioClip;
        [SerializeField] private Transform player;
        
        private AudioSource audioSource;
        
        private float[] leftDelayBuffer;
        private float[] rightDelayBuffer;
        private int bufferSize;
        private int leftWriteIndex = 0;
        private int rightWriteIndex = 0;
        private int leftReadIndex = 0;
        private int rightReadIndex = 0;
        private int leftDelaySamples;
        private int rightDelaySamples;

        private float volume = 1f; // overall volume
        private float angle = 0f; // angle in degrees
        private float soundFalloffDistance = 15f; // distance at which sound is at zero volume

        void Start()
        {
            // Initialize audio source
            audioSource.clip = audioClip;
            audioSource.loop = true;
            audioSource.Play();

            // Initialize delay buffers
            bufferSize = Mathf.CeilToInt((maxHaasDelay / 1000f) * sampleRate);
            leftDelayBuffer = new float[bufferSize];
            rightDelayBuffer = new float[bufferSize];
            
            // main camera is the player
            player = Camera.main.transform;
        }

        private void Update()
        {
            if (useScene)
            {
                UpdateParamsFromScene();
            }
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (channels < 2 || leftDelayBuffer == null || rightDelayBuffer == null)
            {
                Debug.LogError("This script requires a stereo audio source with initialized delay buffers.");
                return;
            }
            
            for (var i = 0; i < data.Length; i++)
            {
                // apply volume
                data[i] *= volume;
            }

            // Apply the selected effect
            switch (currentEffect)
            {
                case EffectType.ILD:
                    ApplyILD(data, channels);
                    break;
                case EffectType.ITD:
                    ApplyITD(data, channels);
                    break;
            }
        }
    
        private void ApplyILD(float[] data, int channels)
        {
            float theta = angle * (float)Math.PI / 180f;
            float intensity = Mathf.Sin(theta);

            // channgels always 2 (stereo); data interleaved LRLRLR...
            for (int i = 0; i < data.Length; i += 2)
            {
                // Left channel
                data[i] *= Mathf.Clamp01(0.5f - intensity * 0.5f);
                // Right channel
                data[i + 1] *= Mathf.Clamp01(0.5f + intensity * 0.5f);
            }
        }


        private void ApplyITD(float[] data, int channels)
        {
            // 1. Physics Math (Woodworth Model)
            // We calculate the required delay in seconds based on the head radius and angle
            float r = 0.1f; // head radius in meters
            float C = 343f; // speed of sound in m/s

            // Convert angle to Radians (using Mathf.Deg2Rad is cleaner than PI/180)
            float thetaRad = angle * Mathf.Deg2Rad;
            float absTheta = Mathf.Abs(thetaRad);

            // Formula: Time = (r/c) * (theta + sin(theta))
            float delaySeconds = (r / C) * (absTheta + Mathf.Sin(absTheta));

            // Convert time to samples
            int delaySamples = (int)(delaySeconds * sampleRate);

            // Safety clamp to prevent reading outside the buffer
            delaySamples = Mathf.Clamp(delaySamples, 0, bufferSize - 1);

            // 2. Determine which ear is delayed
            // If angle is positive (Right), the Left ear is delayed.
            // If angle is negative (Left), the Right ear is delayed.
            int leftDelay = (angle > 0) ? delaySamples : 0;
            int rightDelay = (angle < 0) ? delaySamples : 0;

            // 3. Process Audio (Circular Buffer)
            for (int i = 0; i < data.Length; i += 2)
            {
                // --- WRITE STEP ---
                // Store the current raw audio into the history buffers
                leftDelayBuffer[leftWriteIndex] = data[i];
                rightDelayBuffer[rightWriteIndex] = data[i + 1];

                // --- READ STEP ---
                // Calculate where to read from: "Current Write Position" minus "Delay"
                // We add bufferSize before modulo (%) to handle wrapping around negative numbers
                int lReadIndex = (leftWriteIndex - leftDelay + bufferSize) % bufferSize;
                int rReadIndex = (rightWriteIndex - rightDelay + bufferSize) % bufferSize;

                // Apply the delayed samples to the output
                data[i] = leftDelayBuffer[lReadIndex];
                data[i + 1] = rightDelayBuffer[rReadIndex];

                // --- ADVANCE ---
                // Move the write pointers forward
                leftWriteIndex = (leftWriteIndex + 1) % bufferSize;
                rightWriteIndex = (rightWriteIndex + 1) % bufferSize;
            }
        }

        private void UpdateParamsFromScene()
        {
            Vector3 acPos = this.transform.position;
            Vector3 playerPos = player.position;
            Vector3 playerForward = player.transform.forward;
            Vector3 direction = playerPos - acPos;

            float distance = direction.magnitude;
            this.volume = SoundFallow(distance);

            direction.Normalize();
            float angleNew = -Vector3.SignedAngle(playerForward, direction, Vector3.up);
            this.angle = angleNew;
        }

        private float SoundFallow(float d)
        {
            float y = 1 - (d / soundFalloffDistance); // linear falloff
            return Mathf.Clamp01(y);
        }

        private void OnValidate()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
            audioSource.hideFlags = HideFlags.HideInInspector;
        }
    }
}
