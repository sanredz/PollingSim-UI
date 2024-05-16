using System.IO;
using UnityEngine;

namespace RVO {
    public class ResultsLogger
    {
        public static void LogResults(string results)
        {
            //string path = Application.dataPath + "/Results-" + SceneVariables.scenes[SceneVariables.scene] +".txt";
            string path = Application.dataPath + "/Results.txt";

            using (StreamWriter writer = new StreamWriter(path, true)) // Set to 'true' to append to the file instead of overwriting
            {
                writer.WriteLine(results);
            }
        }
    }
}