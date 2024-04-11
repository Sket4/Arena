// Copyright 2012-2017 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.
using System.Collections;
using UnityEngine;

namespace Arena.Client.UI
{
    public class FadeUI : MonoBehaviour
    {
        [SerializeField]
        UnityEngine.UI.Image image = default;

        [SerializeField]
        float fadeTime = 1;

        Coroutine coroutine;

        private void Awake()
        {
            image.canvasRenderer.SetAlpha(0);
            image.enabled = true;
        }

        public void FadeInHalf(System.Action completeCallback)
        {
            image.CrossFadeAlpha(0.5f, fadeTime, true);
            startFadingTimer(completeCallback);
        }
        public void FadeInFull(System.Action completeCallback)
        {
            image.CrossFadeAlpha(1, fadeTime, true);
            startFadingTimer(completeCallback);
        }
        public void FadeOut(System.Action completeCallback)
        {
            image.CrossFadeAlpha(0, fadeTime, true);
            startFadingTimer(completeCallback);
        }

        void startFadingTimer(System.Action completeCallback)
        {
            if(coroutine != null)
            {
                StopCoroutine(coroutine);
            }
            coroutine = StartCoroutine(fadeCompleteRoutine(completeCallback));
        }

        IEnumerator fadeCompleteRoutine(System.Action callback)
        {
            if (callback == null)
            {
                yield break;
            }
            yield return new WaitForSeconds(fadeTime);
            coroutine = null;
            callback();
        }
    }
}
