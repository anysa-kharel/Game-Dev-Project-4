using System;
using System.Collections;
using UnityEngine;

namespace UnityStandardAssets.Utility
{
    [Serializable]
    public class FOVKick
    {
        public Camera Camera;
        [HideInInspector] public float originalFov;
        public float FOVIncrease = 3f;
        public float TimeToIncrease = 1f;
        public float TimeToDecrease = 1f;
        public AnimationCurve IncreaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public void Setup(Camera camera)
        {
            if (camera == null) throw new ArgumentNullException(nameof(camera));
            Camera = camera;
            originalFov = camera.fieldOfView;
        }

        public void ChangeCamera(Camera camera)
        {
            Setup(camera);
        }

        public IEnumerator FOVKickUp()
        {
            return AnimateTo(originalFov + FOVIncrease, TimeToIncrease);
        }

        public IEnumerator FOVKickDown()
        {
            return AnimateTo(originalFov, TimeToDecrease);
        }

        private IEnumerator AnimateTo(float target, float duration)
        {
            if (Camera == null) yield break;
            if (duration <= 0f)
            {
                Camera.fieldOfView = target;
                yield break;
            }

            float initial = Camera.fieldOfView;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float blend = IncreaseCurve != null && IncreaseCurve.length > 0
                    ? Mathf.Clamp01(IncreaseCurve.Evaluate(progress)) : progress;
                Camera.fieldOfView = Mathf.Lerp(initial, target, blend);
                yield return null;
            }
            Camera.fieldOfView = target;
        }
    }
}
