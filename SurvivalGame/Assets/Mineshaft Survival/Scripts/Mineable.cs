using StormSurvival;
using UnityEngine;

public class Mineable : MonoBehaviour
{
    public float Health = 240f;
    public bool CanMine => Health > 0f && !notified;
    public event System.Action<Mineable> Depleted;

    private string key;
    private bool notified;

    private void Awake() { key = TemplatePrefs.ObjectKey(this) + "/Mineable"; }
    private void Start() { Load(); }

    public void Load()
    {
        if (TemplatePrefs.HasKey(key)) Health = TemplatePrefs.GetFloat(key);
        MineRefresh();
    }

    public void Save()
    {
        TemplatePrefs.SetFloat(key, Health);
        TemplatePrefs.Save();
    }

    public void Strike(float amount, Vector3 contact, Vector3 normal)
    {
        if (!CanMine || amount <= 0f) return;

        Health = Mathf.Max(0f, Health - amount);
        bool breaks = Health <= 0f;
        var game = StormGame.Instance;
        bool animate = game != null && game.Player != null && gameObject.activeInHierarchy;
        if (animate) StormHarvestEffects.Play(game, gameObject, HarvestSurface.Stone, contact, normal, breaks);

        if (breaks) FinishMining(animate);
        else Save();
    }

    public void MineRefresh()
    {
        if (Health <= 0f) FinishMining(false);
    }

    private void FinishMining(bool animate)
    {
        if (notified) return;
        notified = true;
        Health = 0f;
        Save();
        Depleted?.Invoke(this);
        if (!animate) Destroy(gameObject);
    }
}
