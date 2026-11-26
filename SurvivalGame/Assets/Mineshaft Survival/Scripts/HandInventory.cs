using UnityEngine;
public class HandInventory : MonoBehaviour
{
    public GameObject Slot1, Slot2, Slot3;
    public int selected;
    [System.NonSerialized] public bool InputEnabled = true;
    private void Start() { Refresh(); }
    private void Update()
    {
        if (!InputEnabled || Time.timeScale <= 0f) return;
        int previous = selected;
        if (Input.GetKeyDown(KeyCode.Alpha1)) selected = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) selected = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) selected = 2;
        float scroll = Input.GetAxis("Mouse ScrollWheel"); if (scroll > 0) selected++; else if (scroll < 0) selected--;
        selected = (selected % 3 + 3) % 3;
        if (previous != selected) Refresh();
    }
    public void Refresh()
    {
        selected = (selected % 3 + 3) % 3;
        bool torchAvailable = StormSurvival.StormGame.Instance == null || StormSurvival.StormGame.Instance.Needs.HasTorch;
        if (Slot1 != null) Slot1.SetActive(selected == 0 && torchAvailable);
        if (Slot2 != null) Slot2.SetActive(selected == 1);
        if (Slot3 != null) Slot3.SetActive(selected == 2);
    }
}
