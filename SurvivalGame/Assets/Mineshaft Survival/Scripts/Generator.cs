using UnityEngine;
public class Generator : MonoBehaviour
{
    public float CurrentFuel;
    public float MaxFuel = 1000f;
    public bool Toggled;
    public GameObject VisualObjects;
    public float FuelPerSecond = 1.8f;
    private string key;
    private float nextSave;
    private void Awake() { key = TemplatePrefs.ObjectKey(this) + "/Generator"; }
    public void Start() { LoadStats(); ApplyVisuals(); }
    public void Update()
    {
        MaxFuel = Mathf.Max(1f, MaxFuel);
        CurrentFuel = Mathf.Clamp(CurrentFuel - (Toggled ? FuelPerSecond * Time.deltaTime : 0f), 0f, MaxFuel);
        if (CurrentFuel <= 0f) Toggled = false;
        ApplyVisuals();
        if (Time.time >= nextSave) { nextSave = Time.time + 5f; SaveStats(); }
    }
    private void ApplyVisuals() { if (VisualObjects != null) VisualObjects.SetActive(Toggled && CurrentFuel > 0f); }
    public void LoadStats()
    {
        if (TemplatePrefs.HasKey(key)) CurrentFuel = TemplatePrefs.GetFloat(key);
        if (TemplatePrefs.HasKey(key + "/On")) Toggled = TemplatePrefs.GetInt(key + "/On") == 1;
    }
    public void SaveStats() { TemplatePrefs.SetFloat(key, CurrentFuel); TemplatePrefs.SetInt(key + "/On", Toggled ? 1 : 0); TemplatePrefs.Save(); }
    public void Toggle() { Toggled = !Toggled && CurrentFuel > 0f; ApplyVisuals(); SaveStats(); }
}
