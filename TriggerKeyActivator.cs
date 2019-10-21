using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerKeyActivator : MonoBehaviour {
    //the following structure is necessary, so as to have a 2D array (obejects per keypress) visible in the inspector
    [System.Serializable]
    public struct TargetObjectLists
    {
        //controlNumber is sent on a per key basis
        public int controlNumber;
        public bool toggleOnlyOnce;
        public GameObject[] thisKeyObjects;
        [HideInInspector]  public int triggerCount;
    }

    //public int controlNumber;
    public List<KeyCode> triggerKeys;
    public TargetObjectLists[] TargetObjects;
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

    // Update is called once per frame
    void Update () {
        foreach (KeyCode key in triggerKeys)
        {
            if (Input.GetKeyDown(key))
            {
                if (!TargetObjects[triggerKeys.IndexOf(key)].toggleOnlyOnce ||
                    (TargetObjects[triggerKeys.IndexOf(key)].toggleOnlyOnce &&
                     TargetObjects[triggerKeys.IndexOf(key)].triggerCount == 0))
                {
                    //account for this now, if exceptions are thrown later in the for loop
                    TargetObjects[triggerKeys.IndexOf(key)].triggerCount++;
                    Debug.Log("Pressed " + key);
                    //on keyPress, trigeer all the objects in this key's list
                    foreach (GameObject subObject in TargetObjects[triggerKeys.IndexOf(key)].thisKeyObjects)
                    {
                        //toggle target is either a filter or a script (mutually exclusive)
                        //default trying filter - if exists
                        if (subObject.GetComponent<ToggleFilter>() != null)
                        {
                            subObject.GetComponent<ToggleFilter>()
                                    .Toggle(TargetObjects[triggerKeys.IndexOf(key)]
                                    .controlNumber);
                            Logger.GetComponent<PathScript3>().logEventData("KeyTrigger " + this.name
                                                                            + " triggered filter " + subObject.name);
                        }
                        //otherwise, try togglescript
                        else
                        {
                            if (subObject.GetComponent<ToggleScript>() != null)
                            {
                                subObject.GetComponent<ToggleScript>()
                                    .Toggle(TargetObjects[triggerKeys.IndexOf(key)]
                                    .controlNumber);
                                //Logger.GetComponent<PathScript3>().logEventData("KeyTrigger " + this.name
                                //                                                + " triggered" toggle " + subObject.name");
                            }
                            //if nothing, throw a warning
                            else
                            {
                                Debug.LogWarning("Object " + subObject.name +
                                                 " has no toggleScript! Trigger " + this.name + " not toggling!");
                            }
                        }
                    }                    
                }
            }
        }
    }
}
