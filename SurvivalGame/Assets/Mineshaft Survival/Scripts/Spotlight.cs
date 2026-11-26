using UnityEngine;
public class Spotlight : MonoBehaviour
{
    public float MaxDistance = 20f;
    public bool Toggled = true;
    public Renderer lightCube;
    public GameObject Lights;
    public Material LampOn, LampOff;
    public float distance;
    public GameObject generator;
    private string key;
    private float nextSearch;
    private bool lastLit;
    private void Awake() { key = TemplatePrefs.ObjectKey(this) + "/Spotlight"; }
    public void Start() { Load(); }
    public void Update()
    {
        if (generator == null && Time.time > nextSearch) { nextSearch = Time.time + 2; generator = GameObject.FindGameObjectWithTag("Generator"); }
        var source = generator != null ? generator.GetComponent<Generator>() : null;
        distance = source != null ? Vector3.Distance(transform.position, generator.transform.position) : MaxDistance + 1;
        bool lit = source != null && source.Toggled && source.CurrentFuel > 0f && distance <= MaxDistance && Toggled;
        if (Lights != null) Lights.SetActive(lit);
        if (lightCube != null && (lit != lastLit || lightCube.sharedMaterial == null)) lightCube.sharedMaterial = lit ? LampOn : LampOff;
        lastLit = lit;
    }
    public void Toggle() { Toggled = !Toggled; Save(); }
    public void Save() { TemplatePrefs.SetInt(key, Toggled ? 1 : 0); TemplatePrefs.Save(); }
    public void Load() { if (TemplatePrefs.HasKey(key)) Toggled = TemplatePrefs.GetInt(key) == 1; }
}
