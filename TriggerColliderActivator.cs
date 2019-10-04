using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerColliderActivator : MonoBehaviour {
    public int controlNumber;
    [Space(10)]

    public bool toggleOnlyOnce;
    [Space(10)]

    public bool triggerOnEnter = true;
    public bool triggerOnExit;
    public bool triggerOnStay;
    public float triggerStayTime;    
    private int toggleCount;
    private float colliderStayTime;
    private bool triggeredOnThisStay;
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

    //once FPSController enters the collider, trigger
    void OnTriggerEnter(Collider Col)
    {
        if (triggerOnEnter)
        {
            TriggerPerCollider(Col);
        }      
    }

    //once FPSController exits the collider, trigger
    void OnTriggerExit(Collider Col)
    {
        if (triggerOnExit)
        {
            TriggerPerCollider(Col);
        }
        //so that OnTriggerStay() conditions are reset and object can be re-entered
        colliderStayTime = 0f;
        triggeredOnThisStay = false;
    }

    //if FPSController stays within the collider for some time...
    private void OnTriggerStay(Collider Col)
    {
        colliderStayTime += Time.deltaTime;
        if (triggerOnStay && !triggeredOnThisStay && (colliderStayTime >= triggerStayTime))
        {
            TriggerPerCollider(Col);
            //so that OnTriggerStay() is triggered only once per stay
            triggeredOnThisStay = true;
        }
    }

    //the trigger function
    void TriggerPerCollider (Collider Col)
    {
        //only the object tagged as "Player" (FPSController) in the inspector can trigger this
            //there should be only one such object!
            //that is, no subObjects of Player should be tagged as this!!! (no multiple triggers per interaction)
        if (Col.gameObject.tag == "Player" && (!(toggleOnlyOnce && (toggleCount > 0))))
        {
            foreach (GameObject obj in TargetObjects)
            {
                //toggle target is either a filter or a script (mutually exclusive)
                //default trying filter - if exists
                if (obj.GetComponent<ToggleFilter>() != null)
                {
                    obj.GetComponent<ToggleFilter>().Toggle(controlNumber);
                    Logger.GetComponent<PathScript3>().logEventData("ColliderTrigger " + this.name
                                                                    + " triggered filter " + obj.name);
                }
                //otherwise, try togglescript
                else
                {
                    if (obj.GetComponent<ToggleScript>() != null)
                    {
                        obj.GetComponent<ToggleScript>().Toggle(controlNumber);
                        Logger.GetComponent<PathScript3>().logEventData("ColliderTrigger " + this.name
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
        toggleCount++;
    }
}