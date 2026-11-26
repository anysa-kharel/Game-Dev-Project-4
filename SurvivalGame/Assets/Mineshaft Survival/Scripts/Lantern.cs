using UnityEngine;
public class Lantern : MonoBehaviour
{
    public float Fuel = 1000f;
    public bool toggled = true;
    public Material LampOn, LampOff;
    public GameObject Light;
    public Renderer Glass;
    public GameObject Player;
    private string key;
    private float nextSave;
    private void Awake() { key = TemplatePrefs.ObjectKey(this) + "/Lantern"; }
    private void Start() { Player = GameObject.FindGameObjectWithTag("Player"); LoadSettings(); }
    private void Update()
    {
        Fuel = Mathf.Clamp(Fuel - (toggled ? 1.2f * Time.deltaTime : 0f), 0f, 1000f);
        if (Fuel <= 0f && toggled) TurnOff();
        if (Time.time > nextSave) { nextSave = Time.time + 6; SaveSettings(); }
    }
    private void Apply() { if (Glass != null) Glass.sharedMaterial = toggled ? LampOn : LampOff; if (Light != null) Light.SetActive(toggled); }
    public void TurnOn() { toggled = Fuel > 0f; Apply(); }
    public void TurnOff() { toggled = false; Apply(); }
    public void Toggle() { if (toggled) TurnOff(); else TurnOn(); SaveSettings(); }
    public void LoadSettings() { if (TemplatePrefs.HasKey(key)) Fuel = TemplatePrefs.GetFloat(key); if (TemplatePrefs.HasKey(key + "/On")) toggled = TemplatePrefs.GetInt(key + "/On") == 1; if (Fuel <= 0) toggled = false; Apply(); }
    public void SaveSettings() { TemplatePrefs.SetFloat(key, Fuel); TemplatePrefs.SetInt(key + "/On", toggled ? 1 : 0); TemplatePrefs.Save(); }
}
