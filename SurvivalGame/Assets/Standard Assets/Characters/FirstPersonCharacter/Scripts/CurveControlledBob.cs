using System;
using UnityEngine;

namespace UnityStandardAssets.Utility
{
    // Walking/running camera sway used by the Standard Assets controller.
    [Serializable]
    public class CurveControlledBob
    {
        public float HorizontalBobRange = 0.1f;
        public float VerticalBobRange = 0.1f;
        public AnimationCurve Bobcurve = CreateDefaultCurve();
        public float VerticaltoHorizontalRatio = 1f;

        private Vector3 m_OriginalCameraPosition;
        private float m_BobBaseInterval;
        private float m_CurveStart;
        private float m_CurveDuration;
        private float m_HorizontalPhase;
        private float m_VerticalPhase;
        private bool m_Initialized;

        public void Setup(Camera camera, float bobBaseInterval)
        {
            if (camera == null)
            {
                throw new ArgumentNullException(nameof(camera));
            }

            if (Bobcurve == null || Bobcurve.length < 2)
            {
                Bobcurve = CreateDefaultCurve();
            }

            m_OriginalCameraPosition = camera.transform.localPosition;
            m_BobBaseInterval = bobBaseInterval > 0f ? bobBaseInterval : 0.5f;
            m_CurveStart = Bobcurve[0].time;
            m_CurveDuration = Mathf.Max(0.001f,
                Bobcurve[Bobcurve.length - 1].time - m_CurveStart);
            m_HorizontalPhase = 0f;
            m_VerticalPhase = 0f;
            m_Initialized = true;
        }

        public Vector3 DoHeadBob(float speed)
        {
            Vector3 position = m_OriginalCameraPosition;
            if (!m_Initialized)
            {
                return position;
            }

            position.x += Bobcurve.Evaluate(m_CurveStart + m_HorizontalPhase)
                * HorizontalBobRange;
            position.y += Bobcurve.Evaluate(m_CurveStart + m_VerticalPhase)
                * VerticalBobRange;

            float advance = Mathf.Max(0f, speed) * Time.fixedDeltaTime / m_BobBaseInterval;
            m_HorizontalPhase = Mathf.Repeat(m_HorizontalPhase + advance, m_CurveDuration);
            m_VerticalPhase = Mathf.Repeat(
                m_VerticalPhase + advance * VerticaltoHorizontalRatio, m_CurveDuration);

            return position;
        }

        private static AnimationCurve CreateDefaultCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.5f, 1f),
                new Keyframe(1f, 0f),
                new Keyframe(1.5f, -1f),
                new Keyframe(2f, 0f));
        }
    }
}
