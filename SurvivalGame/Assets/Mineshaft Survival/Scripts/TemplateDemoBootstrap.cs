using UnityEngine;
using UnityEngine.UI;

public class TemplateDemoBootstrap : MonoBehaviour
{
    private void Start()
    {
        foreach (var label in FindObjectsOfType<Text>(true))
            if (label.font == null) label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
