using UnityEngine;
public class Mineable : MonoBehaviour
{
    public float Health = 240f;
    public event System.Action<Mineable> Depleted;
    private string key;
    private bool notified;
    private void Awake() { key = TemplatePrefs.ObjectKey(this) + "/Mineable"; }
    private void Start() { Load(); }
    public void Load() { if (TemplatePrefs.HasKey(key)) Health = TemplatePrefs.GetFloat(key); if (Health <= 0f) MineRefresh(); }
    public void Save() { TemplatePrefs.SetFloat(key, Health); TemplatePrefs.Save(); }
    public void MineRefresh()
    {
        if (Health > 0f || notified) return;
        notified = true; Health = 0f; Save();
        if (Depleted != null) Depleted(this);
        Destroy(gameObject);
    }
}
