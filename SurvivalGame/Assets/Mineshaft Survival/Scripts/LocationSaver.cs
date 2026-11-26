using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocationSaver : MonoBehaviour {

    [Header ("Objects position")]
    public float Xpos;
    public float Ypos;
    public float Zpos;
    [Header("Objects rotation")]
    public float Xrot;
    public float Yrot;
    public float Zrot;

    //[Header ("Objects ID")]
    string ObjID; 

    Vector3 Pos;
    Quaternion Rot;


    public void LoadPos()
    {
        if (TemplatePrefs.GetFloat("Xpos" + ObjID) == 0)
        {
            Xpos = transform.position.x;
        }
        else
        {
            Xpos = TemplatePrefs.GetFloat("Xpos" + ObjID);
        }

        if (TemplatePrefs.GetFloat("Ypos" + ObjID) == 0)
        {
            Ypos = transform.position.y;
        }
        else
        {
            Ypos = TemplatePrefs.GetFloat("Ypos" + ObjID);
        }

        if (TemplatePrefs.GetFloat("Zpos" + ObjID) == 0)
        {
            Zpos = transform.position.z;
        }
        else
        {
            Zpos = TemplatePrefs.GetFloat("Zpos" + ObjID);
        }

        Pos.Set(Xpos, Ypos, Zpos);
        transform.position = Pos;

        Debug.Log("Loaded " + transform.name + "'s Location " + Xpos + " " + Ypos + " " + Zpos);
    }


    public void SavePos()
    {
        Xpos = transform.position.x;
        Ypos = transform.position.y;
        Zpos = transform.position.z;


        TemplatePrefs.SetFloat("Xpos" + ObjID, Xpos);
        TemplatePrefs.SetFloat("Ypos" + ObjID, Ypos);
        TemplatePrefs.SetFloat("Zpos" + ObjID, Zpos);

        TemplatePrefs.Save();
        Debug.Log("Saved " + transform.name +  "'s Location " + Xpos + " " + Ypos + " " + Zpos);
    }



    public void LoadRot()
    {
        if (TemplatePrefs.GetFloat("Xrot" + ObjID) == 0)
        {
            Xrot = transform.rotation.eulerAngles.x;

        }
        else
        {
            Xrot = TemplatePrefs.GetFloat("Xrot" + ObjID);
        }

        if (TemplatePrefs.GetFloat("Yrot" + ObjID) == 0)
        {
            Yrot = transform.rotation.eulerAngles.y;
        }
        else
        {
            Yrot = TemplatePrefs.GetFloat("Yrot" + ObjID);
        }

        if (TemplatePrefs.GetFloat("Zrot" + ObjID) == 0)
        {
            Zrot = transform.rotation.eulerAngles.z;
        }
        else
        {
            Zrot = TemplatePrefs.GetFloat("Zrot" + ObjID);
        }

        Rot = Quaternion.Euler(Xrot, Yrot, Zrot);
        transform.rotation = Rot;

        Debug.Log("Loaded " + transform.name + "'s Rotation " + Xrot + " " + Yrot + " " + Zrot);
    }


    public void SaveRot()
    {
        Xrot = transform.eulerAngles.x;
        Yrot = transform.eulerAngles.y;
        Zrot = transform.eulerAngles.z;


        TemplatePrefs.SetFloat("Xrot" + ObjID, Xrot);
        TemplatePrefs.SetFloat("Yrot" + ObjID, Yrot);
        TemplatePrefs.SetFloat("Zrot" + ObjID, Zrot);

        TemplatePrefs.Save();
        Debug.Log("Saved " + transform.name + "'s Rotation " + Xrot + " " + Yrot + " " + Zrot);
    }











    void Start ()
    {
        if (!TemplatePrefs.Enabled) return;
        ObjID = TemplatePrefs.ObjectKey(this);
        LoadPos();
        LoadRot();
        StartCoroutine(AutoSave());

    }
	
    
    IEnumerator AutoSave()
    {
        yield return new WaitForSeconds(5);
        SavePos();
        SaveRot();
        StartCoroutine(AutoSave());
    }
}
