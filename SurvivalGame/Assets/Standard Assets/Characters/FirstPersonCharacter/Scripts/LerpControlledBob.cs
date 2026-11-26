using System;
using System.Collections;
using UnityEngine;

namespace UnityStandardAssets.Utility
{
    // Landing camera offset used by the Standard Assets FirstPersonController.
    [Serializable]
    public class LerpControlledBob
    {
        public float BobDuration = 0.2f;
        public float BobAmount = 0.1f;

        private float m_Offset;

        public float Offset()
        {
            return m_Offset;
        }

        public IEnumerator DoBobCycle()
        {
            m_Offset = 0f;
            float duration = BobDuration;
            float amount = BobAmount;
            if (duration <= 0f || Mathf.Approximately(amount, 0f))
            {
                yield break;
            }

            var fixedStep = new WaitForFixedUpdate();

            // Lower the camera, then return it to its original height.
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.fixedDeltaTime)
            {
                m_Offset = Mathf.Lerp(0f, amount, elapsed / duration);
                yield return fixedStep;
            }

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.fixedDeltaTime)
            {
                m_Offset = Mathf.Lerp(amount, 0f, elapsed / duration);
                yield return fixedStep;
            }

            m_Offset = 0f;
        }
    }
}
