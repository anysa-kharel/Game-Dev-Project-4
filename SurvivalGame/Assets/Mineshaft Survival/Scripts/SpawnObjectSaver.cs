using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnObjectSaver : MonoBehaviour
{
    public int alive = 0;
    public string ObjType;
    public string ObjNumber = "0";

    string ObjID;

    void Start()
    {
        if (ObjNumber == "0")
        {
            ObjNumber = Random.Range(0, 99999).ToString();
        }
        string CurrentPref = TemplatePrefs.GetString("OBJECT");
        ObjID = ObjNumber + ObjType;
        if (!CurrentPref.Contains(ObjID))
        {
            if (!ObjNumber.Contains(ObjType))
            {
                TemplatePrefs.SetString("OBJECT", TemplatePrefs.GetString("OBJECT") + ObjID + ",");
            }

        }
        if (alive == 1)
        {
            Destroy(gameObject);
        }
        Save();
    }

    public void Save()
    {
        TemplatePrefs.SetFloat(ObjID + "px", transform.position.x);
        TemplatePrefs.SetFloat(ObjID + "py", transform.position.y);
        TemplatePrefs.SetFloat(ObjID + "pz", transform.position.z);
        TemplatePrefs.SetInt(ObjID + "a", alive);

        TemplatePrefs.Save();
    
    }

}
