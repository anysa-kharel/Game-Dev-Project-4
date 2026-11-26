using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UnityStandardAssets.Characters.FirstPerson
{
    // Desktop/gamepad input adapter; the incomplete CrossPlatformInput package is not needed.
    internal static class FirstPersonInput
    {
        public static Vector2 Move
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Vector2 keys = Vector2.zero;
                var keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    keys.x = Axis(keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed,
                        keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed);
                    keys.y = Axis(keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed,
                        keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed);
                }
                Vector2 stick = Gamepad.current != null ? Gamepad.current.leftStick.ReadValue() : Vector2.zero;
                return Vector2.ClampMagnitude(new Vector2(
                    Mathf.Abs(keys.x) >= Mathf.Abs(stick.x) ? keys.x : stick.x,
                    Mathf.Abs(keys.y) >= Mathf.Abs(stick.y) ? keys.y : stick.y), 1f);
#else
                return Vector2.ClampMagnitude(new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")), 1f);
#endif
            }
        }

        public static Vector2 Look
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Vector2 mouse = Mouse.current != null ? Mouse.current.delta.ReadValue() * 0.1f : Vector2.zero;
                Vector2 stick = Gamepad.current != null ? Gamepad.current.rightStick.ReadValue() * (50f * Time.deltaTime) : Vector2.zero;
                return mouse + stick;
#else
                return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
            }
        }

        public static bool JumpPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame ||
                    Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
#else
                return Input.GetButtonDown("Jump");
#endif
            }
        }

        public static bool RunHeld
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Keyboard.current != null && Keyboard.current.shiftKey.isPressed ||
                    Gamepad.current != null && Gamepad.current.leftStickButton.isPressed;
#else
                return Input.GetKey(KeyCode.LeftShift);
#endif
            }
        }

        public static bool EscapePressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
                return Input.GetKeyDown(KeyCode.Escape);
#endif
            }
        }

        public static bool PrimaryReleased
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
#else
                return Input.GetMouseButtonUp(0);
#endif
            }
        }

        private static float Axis(bool positive, bool negative)
        {
            return (positive ? 1f : 0f) - (negative ? 1f : 0f);
        }
    }
}
