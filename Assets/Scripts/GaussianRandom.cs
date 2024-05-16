using System;
using UnityEngine;

namespace RVO{
    public class GaussianRandom
    {
        private float? spareValue = null;

        public float NextGaussian(float mean = 0, float standardDeviation = 1)
        {
            if (spareValue.HasValue)
            {
                float tmp = spareValue.Value;
                spareValue = null;
                return mean + standardDeviation * tmp;
            }
            else
            {
                float u, v, s;
                do
                {
                    u = UnityEngine.Random.value * 2 - 1;
                    v = UnityEngine.Random.value * 2 - 1;
                    s = u * u + v * v;
                }
                while (s >= 1 || s == 0);

                s = Mathf.Sqrt((-2.0f * Mathf.Log(s)) / s);

                spareValue = v * s;
                return mean + standardDeviation * (u * s);
            }
        }
    }
}