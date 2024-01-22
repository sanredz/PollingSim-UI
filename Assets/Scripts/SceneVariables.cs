using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RVO{
    public static class SceneVariables
    {
        public static float timeStep;
        public static int agentTotalCount;
        public static bool pause;

        static SceneVariables()
        {
            timeStep = 0f;
            agentTotalCount = 20;
            pause = true;
        }
    }
}