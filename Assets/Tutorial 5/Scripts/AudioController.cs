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
        
        [Range(0, 1)]
        [SerializeField] private float volume = 1f;
        [Range(-90, 90)] [Tooltip("0 is left, 1 is right")]
        [SerializeField] private float angle = 0.5f;
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
            float r = 0.1f; // head radius in meters
            float C = 343f; // speed of sound in m/s
            float theta = angle * (float)Math.PI / 180f;
            float delay = r * (theta + Mathf.Sin(theta)) / C;
            float delayInSamples = delay * sampleRate;
            int shift = (int)Mathf.Abs(delayInSamples);

            // left and right delay samples
            if (delayInSamples < 0) { 
                for (int i = 0; i < shift; i++)
                {
                    leftDelayBuffer[i] = 0.0f;
                }
                for (int i = 0; i < bufferSize - shift; i++)
                {
                    leftDelayBuffer[i + shift] = data[i * 2];
                }
            } else
            {
                for (int i = 0; i < shift; i++)
                {
                    rightDelayBuffer[i] = 0.0f;
                }
                for (int i = 0; i < bufferSize - shift; i++)
                {
                    rightDelayBuffer[i + shift] = data[i * 2 + 1];
                }
            }

            for (int i = 0; i < data.Length; i += 2)
            {
                // Left channel
                data[i] = leftDelayBuffer[leftReadIndex];
                leftReadIndex = (leftReadIndex + 1) % bufferSize;
                // Right channel
                data[i + 1] = rightDelayBuffer[rightReadIndex];
                rightReadIndex = (rightReadIndex + 1) % bufferSize;
            }

        }

        private void UpdateParamsFromScene()
        {
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
