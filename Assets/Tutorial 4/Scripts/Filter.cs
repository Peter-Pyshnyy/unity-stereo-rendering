using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tutorial_4
{
    public class Filter : MonoBehaviour
    {
        [Header("Moving average")]
        [Range(1, 200)] public int samples = 30;
        
        [Header("Single Exponential")]
        [Range(0.01f, 1.0f)] public float seAlpha = 0.03f;
        
        [Header("Double Exponential")]
        [Range(0.0f, 1.0f)] public float deAlpha = 0.04f;
        [Range(0.0f, 1.0f)] public float deBeta = 0.5f;

        [Header("One Euro")] public float frequency = 60f; 

        // temp values for filters
        private readonly Queue<Vector3> _movingAverageBuffer = new();
        private Vector3 _singleExponential;
        private Vector3 _doubleExponential;
        private Vector3 _trend = Vector3.zero;

        private OneEuroFilter<Vector3> _oneEuro;

        private void Start()
        {
            _oneEuro = new OneEuroFilter<Vector3>(frequency);
        }

        public Vector3 MovingAverage(Vector3 value)
        {
            _movingAverageBuffer.Enqueue(value);
            
            if (_movingAverageBuffer.Count > samples)
            {
                _movingAverageBuffer.Dequeue();
            }

            Vector3 sum = Vector3.zero;

            foreach (var v in _movingAverageBuffer)
            {
                sum += v;
            }

            return sum / _movingAverageBuffer.Count;
        }

        public Vector3 SingleExponential(Vector3 value)
        {
            return value;
        }

        public Vector3 DoubleExponential(Vector3 value)
        {
            return value;
        }

        public Vector3 OneEuro(Vector3 value)
        {
            return _oneEuro.Filter(value);
        }
    }
}