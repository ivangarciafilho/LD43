using System.Collections;
using System.Collections.Generic;
using UnityEngine;


    public class FramerateSampler
    {
        private float updateInterval = 1f;
        // For fps calculation
        private float newPeriod = 0;
        private int intervalTotalFrames = 0;
        private int intervalFrameSum = 0;

        // final values
        public int CurrentFps;
        
        public void Update()
        {
            intervalTotalFrames++;
            intervalFrameSum += (int)(1f / Time.deltaTime);
            if (Time.time > newPeriod)
            {
                CurrentFps = intervalFrameSum / intervalTotalFrames;
                intervalTotalFrames = 0;
                intervalFrameSum = 0;

                newPeriod += updateInterval;
            }
        }
    }