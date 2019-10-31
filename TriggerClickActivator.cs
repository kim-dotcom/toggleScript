using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerClickActivator : MonoBehaviour {
    public int controlNumber;
    [Space(10)]

    public bool toggleOnlyOnce;
    private int toggleCount;
    [Space(10)]

    public List<GameObject> TargetObjects;
    [Space(10)]

    public bool logTrigger;
    private GameObject Logger;

    private void Start()
    {
        if (logTrigger)
        {
            Logger = GameObject.FindGameObjectWithTag("MainCamera");
        }
    }

    //trigger the array of objects on click
    void OnMouseDown()
    {
        if (!(toggleOnlyOnce && (toggleCount > 0)))
        {
            //account for this now, if exceptions are thrown later in the for loop
            toggleCount++;
            foreach (GameObject obj in TargetObjects)
            {
                //toggle target is either a filter or a script (mutually exclusive)
                //default trying filter - if exists
                if (obj.GetComponent<ToggleFilter>() != null)
                {
                    obj.GetComponent<ToggleFilter>().Toggle(controlNumber);
                    Logger.GetComponent<PathScript>().logEventData("ClickTrigger " + this.name
                                                                    + " triggered filter " + obj.name);
                }
                //otherwise, try togglescript
                else
                {
                    if (obj.GetComponent<ToggleScript>() != null)
                    {
                        obj.GetComponent<ToggleScript>().Toggle(controlNumber);
                        Logger.GetComponent<PathScript>().logEventData("ClickTrigger " + this.name
                                                                        + " triggered toggle " + obj.name);
                    }
                    //if nothing, throw a warning
                    else
                    {
                        Debug.LogWarning("Object " + obj.name +
                                         " has no toggleScript! Trigger " + this.name + " not toggling!");
                    }
                }
            }
        }        
    }
}
