using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RVO{
    public static class SceneVariables
    {
        public static float timeStep;
        public static int agentTotalCount; 
        public static bool pause;
        public static int runCount = 4;
        public static int maxRuns = 5; 
        public static int scene = 0;
        public static string[] scenes = {"24A", "24B", "24C", "33A", "33B", "33C", "14A", "14B", "14C", "23A", "23B", "23C"}; // Always start with 24A
        // public static string[] scenes = {"24B"};

        static SceneVariables()
        {
            timeStep = 0.2f;
            agentTotalCount = 50;
            pause = false;
        }
    }
}