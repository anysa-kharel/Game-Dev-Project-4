using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OilHolder : MonoBehaviour {

    [Header ("Stats")]
    public int HoldingNow = 0; //How many oil player currently holds (Can hold 2 max)

    [Header ("Mug models")]
    public GameObject MugFull;
    public GameObject MugHalf;
    public GameObject MugEmpty;

	void Start () {
        Load();
	}
	
    public void Save()
    {
        TemplatePrefs.SetInt("OilHold", HoldingNow);
        TemplatePrefs.Save();
        Load();
    }
    public void Load()
    {
        HoldingNow = Mathf.Clamp(TemplatePrefs.GetInt("OilHold", HoldingNow), 0, 2);

        if(HoldingNow == 0)
        {
            if (MugEmpty != null) MugEmpty.SetActive(true);
            if (MugHalf != null) MugHalf.SetActive(false);
            if (MugFull != null) MugFull.SetActive(false);
        }
        if (HoldingNow == 1)
        {
            if (MugEmpty != null) MugEmpty.SetActive(false);
            if (MugHalf != null) MugHalf.SetActive(true);
            if (MugFull != null) MugFull.SetActive(false);
        }
        if (HoldingNow == 2)
        {
            if (MugEmpty != null) MugEmpty.SetActive(false);
            if (MugHalf != null) MugHalf.SetActive(false);
            if (MugFull != null) MugFull.SetActive(true);
        }
    }
    

}
