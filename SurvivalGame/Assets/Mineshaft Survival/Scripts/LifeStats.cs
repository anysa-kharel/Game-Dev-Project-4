using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityStandardAssets.Characters.FirstPerson;
public class LifeStats : MonoBehaviour
{
    public float Health = 1000f, Thirst = 1000f, Hunger = 1000f;
    public Slider HealthSlider, ThirstSlider, HungerSlider;
    public GameObject DeathScreen;
    [System.NonSerialized] public bool ExternallyManaged;
    private float nextSave;
    private void Start()
    {
        if (!ExternallyManaged)
        {
            Health = TemplatePrefs.GetFloat("Health", Health);
            Thirst = TemplatePrefs.GetFloat("Thirst", Thirst);
            Hunger = TemplatePrefs.GetFloat("Hunger", Hunger);
        }
    }
    public void Tick(float seconds, float thirstRate = 3.5f, float hungerRate = 2.2f)
    {
        if (Health <= 0f || seconds <= 0f) return;
        Thirst = Mathf.Max(0f, Thirst - thirstRate * seconds);
        Hunger = Mathf.Max(0f, Hunger - hungerRate * seconds);
        if (Thirst <= 0f) Health -= 6f * seconds;
        if (Hunger <= 0f) Health -= 5f * seconds;
    }
    private void Update()
    {
        if (!ExternallyManaged) Tick(Time.deltaTime, 0.4f, 0.3f);
        Health = Mathf.Clamp(Health, 0f, 1000f); Hunger = Mathf.Clamp(Hunger, 0f, 1000f); Thirst = Mathf.Clamp(Thirst, 0f, 1000f);
        if (HealthSlider != null) { HealthSlider.maxValue = 1000; HealthSlider.value = Health; }
        if (ThirstSlider != null) { ThirstSlider.maxValue = 1000; ThirstSlider.value = Thirst; }
        if (HungerSlider != null) { HungerSlider.maxValue = 1000; HungerSlider.value = Hunger; }
        if (!ExternallyManaged)
        {
            if (DeathScreen != null) DeathScreen.SetActive(Health <= 0f);
            var controller = GetComponent<FirstPersonController>(); if (controller != null && Health <= 0f) controller.enabled = false;
            if (Health <= 0f) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            if (Time.time > nextSave) { nextSave = Time.time + 6; TemplatePrefs.SetFloat("Health", Health); TemplatePrefs.SetFloat("Thirst", Thirst); TemplatePrefs.SetFloat("Hunger", Hunger); TemplatePrefs.Save(); }
        }
    }
    public void Respawn()
    {
        if (StormSurvival.StormGame.Instance != null) { StormSurvival.StormGame.Instance.Retry(); return; }
        TemplatePrefs.DeleteAll(); Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
