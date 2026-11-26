using UnityEngine;
public class Eatable : MonoBehaviour
{
    public float FoodAmount = 4, MaxFood = 4, SaturationAmount = 250, WaterAmount = 200;
    public GameObject fullCan, emptyCan;
    private string key;
    private void Awake() { key = TemplatePrefs.ObjectKey(this) + "/Food"; }
    private void Start() { Load(); }
    public void Save() { TemplatePrefs.SetFloat(key, FoodAmount); TemplatePrefs.Save(); UpdateVisuals(); }
    public void Load() { if (TemplatePrefs.HasKey(key)) FoodAmount = TemplatePrefs.GetFloat(key); UpdateVisuals(); }
    private void UpdateVisuals() { bool empty = FoodAmount <= 1; if (fullCan != null) fullCan.SetActive(!empty); if (emptyCan != null) emptyCan.SetActive(empty); }
}
