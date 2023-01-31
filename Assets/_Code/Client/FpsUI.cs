// Copyright 2012-2017 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.
using UnityEngine;

namespace TzarGames.Common
{
    public class FpsUI : MonoBehaviour
    {
        [SerializeField]
        TMPro.TextMeshProUGUI text = default;

        [SerializeField]
        double interval = 1.0 / 3.0;

        [SerializeField]
        string textFormat = "FPS: {0:F1}";

        [SerializeField]
        int minHighFps = 45;

        [SerializeField]
        int minAverageFps = 29;

        int frames = 0;
        double lastCountTime = 0;
        double currentFps = 0;

        bool enableCount = true;

        void Update()
        {
            if(enableCount == false)
            {
                return;
            }

            double difference = Time.time - lastCountTime;

            if (difference >= interval)
            {
                currentFps = frames * (1.0 / difference);
                text.text = string.Format(textFormat, currentFps);
                frames = 0;
                lastCountTime = Time.time;

                if(currentFps > minHighFps)
                {
                    text.color = Color.green;
                }
                else if(currentFps > minAverageFps)
                {
                    text.color = Color.yellow;
                }
                else
                {
                    text.color = Color.red;
                }
            }

            frames++;
        }

        //[ConsoleCommand]
        //void fps()
        //{
        //    enableCount = !enableCount;
        //    text.enabled = enableCount;

        //    if(enableCount)
        //    {
        //        lastCountTime = Time.time;
        //        frames = 0;
        //    }
        //}
    }
}
