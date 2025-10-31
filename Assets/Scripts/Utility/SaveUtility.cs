using System;
using UnityEngine;

namespace HypnicEmpire
{
    public class SaveUtility
    {
        public static string SaveFilePath => Application.persistentDataPath + "/saveGame.dat";

        private const float SaveInterval = 600.0f; // Save every 10 minutes
        private static float SaveTimer = 0.0f;

        public static Action SaveCallback;

        public static void Update()
        {
            //  Iterate the SaveTimer and save the game if the interval is reached
            SaveTimer += Time.deltaTime;
            if (SaveTimer >= SaveInterval)
            {
                SaveCallback?.Invoke();
                SaveTimer = 0.0f;
            }
        }
    }
}