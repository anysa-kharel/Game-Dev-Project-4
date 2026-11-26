using System;
using System.Collections.Generic;
using UnityEngine;

// Namespaced persistence for the original demo. Adventure checkpoints are managed in memory.
public static class TemplatePrefs
{
    public static bool Enabled = true;
    private const string Prefix = "MineshaftSurvival.v2/";
    private const string Registry = Prefix + "Keys";
    private static readonly HashSet<string> Keys = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { Enabled = true; Keys.Clear(); }

    private static string Key(string key) { return Prefix + (key ?? ""); }
    private static void Remember(string key)
    {
        if (Keys.Count == 0)
            foreach (string saved in PlayerPrefs.GetString(Registry, "").Split('\n'))
                if (!string.IsNullOrEmpty(saved)) Keys.Add(saved);
        if (Keys.Add(Key(key))) PlayerPrefs.SetString(Registry, string.Join("\n", Keys));
    }
    public static bool HasKey(string key) => Enabled && PlayerPrefs.HasKey(Key(key));
    public static float GetFloat(string key, float fallback = 0) => Enabled ? PlayerPrefs.GetFloat(Key(key), fallback) : fallback;
    public static int GetInt(string key, int fallback = 0) => Enabled ? PlayerPrefs.GetInt(Key(key), fallback) : fallback;
    public static string GetString(string key, string fallback = "") => Enabled ? PlayerPrefs.GetString(Key(key), fallback) : fallback;
    public static void SetFloat(string key, float value) { if (Enabled) { Remember(key); PlayerPrefs.SetFloat(Key(key), value); } }
    public static void SetInt(string key, int value) { if (Enabled) { Remember(key); PlayerPrefs.SetInt(Key(key), value); } }
    public static void SetString(string key, string value) { if (Enabled) { Remember(key); PlayerPrefs.SetString(Key(key), value); } }
    public static void Save() { if (Enabled) PlayerPrefs.Save(); }
    public static void DeleteAll()
    {
        if (!Enabled) return;
        foreach (string key in PlayerPrefs.GetString(Registry, "").Split('\n'))
            if (!string.IsNullOrEmpty(key)) PlayerPrefs.DeleteKey(key);
        PlayerPrefs.DeleteKey(Registry); Keys.Clear(); PlayerPrefs.Save();
    }
    public static string ObjectKey(Component component)
    {
        var path = new List<int>();
        for (Transform t = component.transform; t != null; t = t.parent) path.Add(t.GetSiblingIndex());
        path.Reverse();
        int index = Array.IndexOf(component.GetComponents(component.GetType()), component);
        return component.gameObject.scene.path + "/" + string.Join("/", path) + "/" + component.GetType().Name + "/" + index;
    }
}
