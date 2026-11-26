using System;
using UnityEngine;

namespace UnityStandardAssets.Characters.FirstPerson
{
    [Serializable]
    public class MouseLook
    {
        public float XSensitivity = 2f;
        public float YSensitivity = 2f;
        public bool clampVerticalRotation = true;
        public float MinimumX = -90f;
        public float MaximumX = 90f;
        public bool smooth;
        public float smoothTime = 5f;
        public bool lockCursor = true;

        private Quaternion m_CharacterTargetRotation;
        private float m_Pitch;
        private float m_CameraYaw;
        private float m_CameraRoll;
        private bool m_CursorLocked = true;
        private int m_LastCursorFrame = -1;

        public void Init(Transform character, Transform camera)
        {
            m_CharacterTargetRotation = character.localRotation;
            m_Pitch = Mathf.DeltaAngle(0f, camera.localEulerAngles.x);
            m_CameraYaw = camera.localEulerAngles.y;
            m_CameraRoll = camera.localEulerAngles.z;
            m_CursorLocked = lockCursor;
            ApplyCursorLock();
        }

        public void LookRotation(Transform character, Transform camera)
        {
            if (Time.timeScale <= 0f || lockCursor && Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            Vector2 look = FirstPersonInput.Look;
            m_CharacterTargetRotation *= Quaternion.Euler(0f, look.x * XSensitivity, 0f);
            m_Pitch -= look.y * YSensitivity;
            if (clampVerticalRotation)
            {
                m_Pitch = Mathf.Clamp(m_Pitch, MinimumX, MaximumX);
            }

            Quaternion cameraTarget = Quaternion.Euler(m_Pitch, m_CameraYaw, m_CameraRoll);
            if (smooth)
            {
                float blend = Mathf.Clamp01(smoothTime * Time.deltaTime);
                character.localRotation = Quaternion.Slerp(character.localRotation, m_CharacterTargetRotation, blend);
                camera.localRotation = Quaternion.Slerp(camera.localRotation, cameraTarget, blend);
            }
            else
            {
                character.localRotation = m_CharacterTargetRotation;
                camera.localRotation = cameraTarget;
            }
        }

        public void SetCursorLock(bool value)
        {
            lockCursor = value;
            m_CursorLocked = value;
            ApplyCursorLock();
        }

        public void UpdateCursorLock()
        {
            // Protect callers that invoke this more than once in a rendered frame.
            if (m_LastCursorFrame == Time.frameCount) return;
            m_LastCursorFrame = Time.frameCount;
            if (!lockCursor) return;

            if (FirstPersonInput.EscapePressed) m_CursorLocked = false;
            else if (FirstPersonInput.PrimaryReleased) m_CursorLocked = true;
            ApplyCursorLock();
        }

        private void ApplyCursorLock()
        {
            bool locked = lockCursor && m_CursorLocked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
