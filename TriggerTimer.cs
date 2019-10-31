using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerTimer : MonoBehaviour
{
    //trigger behavior
    public enum TriggerActivation {byKeypress, byCollider, onStart};
    public TriggerActivation triggerActivateBy;
    public KeyCode triggerKey;
    private bool triggeredByCollider;
    [Space(10)]

    //other control keys (universally usable)
    public KeyCode stopKey;
    public KeyCode resetKey;
    [Space(10)]
    private bool isStopped;
    private int stopCount;
    private int resumeCount;
    private int resetCount;
    private bool isTriggered;

    //time format (either in absolute values from start, or as +n from previous)
    public enum TriggerTimeFormat {absolute, additive};
    public TriggerTimeFormat timeFormat;
    [HideInInspector] public List<float> normalizedTriggerTimes;
    //the following structure is necessary, so as to have a 2D array (obejects per keypress) visible in the inspector
    [System.Serializable]
    public struct TargetObjectLists
    {
        //controlNumber is sent on a per key basis
        public int controlNumber;
        public float timeToTrigger;
        public GameObject[] thisTimeObjects;
        [HideInInspector]  public int triggerCount;
        [HideInInspector] public bool wasTriggered;
    }
    public TargetObjectLists[] TargetObjects;
    private float timeTimerStarted;
    private float timer;
    [Space(10)]

    public bool logTrigger;
    private GameObject Logger;

    void Start()
    {
        if (logTrigger)
        {
            Logger = GameObject.FindGameObjectWithTag("MainCamera");
        }

        normalizeTriggerTimes();

        if (triggerActivateBy == TriggerActivation.onStart)
        {
                startTrigger();
        }
    }

    void Update()
    {
        //key controls
        if (Input.GetKeyDown(resetKey))
        {
            resetTrigger();
        }
        if (Input.GetKeyDown(stopKey))
        {
            stopTrigger();
        }        
        if ((triggerActivateBy == TriggerActivation.byKeypress) &&  Input.GetKeyDown(triggerKey))
        {
            startTrigger();
        }
        //per-update triggering (enabled and not paused)
        if (isTriggered && !isStopped)
        {
            timer += Time.deltaTime;
            int i = 0;
            //foreach object, because they may not be in a correct order (abs. time values)
            foreach (TargetObjectLists item in TargetObjects)
            {
                if (!item.wasTriggered && (timer >=  normalizedTriggerTimes[i]))
                {
                    foreach (GameObject subObject in item.thisTimeObjects)
                    {
                        if (subObject.GetComponent<ToggleScript>() != null)
                        {
                            subObject.GetComponent<ToggleScript>().Toggle(item.controlNumber);
                            LogBehavior(item.controlNumber, subObject.name);
                        }
                    }
                    TargetObjects[i].wasTriggered = true;
                    TargetObjects[i].triggerCount++;
                }
                i++;
            }
        }
    }

    //triggered on collider
    void OnTriggerEnter(Collider Col)
    {
        if ((Col.gameObject.tag == "Player") && !triggeredByCollider)
        {
            startTrigger();
            triggeredByCollider = true;
        }
    }

    //standard time format for tigger, be it additive/absolute values
    void normalizeTriggerTimes()
    {        
        if (timeFormat == TriggerTimeFormat.additive)
        {
            int i = 0;
            float previousTime = 0;  
            foreach (TargetObjectLists item in TargetObjects)
            {
                normalizedTriggerTimes.Add(TargetObjects[i].timeToTrigger + previousTime);
                previousTime = normalizedTriggerTimes[i];
                i++;
            }
        }
        else 
        {
            foreach (TargetObjectLists item in TargetObjects)
            {
                normalizedTriggerTimes.Add(item.timeToTrigger);
            }
        }
    }

    void startTrigger()
    {
        isTriggered = true;
        timeTimerStarted = Time.time;
    }

    void stopTrigger()
    {
        //Debug.Log("stopped at " + timer + ", " + isStopped);
        if (!isStopped) {
            isStopped = true;
            stopCount++;
        }
        else
        {
            isStopped = false;
            resumeCount++;
        }        
    }

    void resetTrigger() {
        //Debug.Log("reset at " + timer);
        isTriggered = false;
        isStopped = false;
        triggeredByCollider = false;
        timer = 0;
        for (int i = 0; i < TargetObjects.Length; i++)
        {
            TargetObjects[i].wasTriggered = false;
        }

        if (triggerActivateBy == TriggerActivation.onStart)
        {
                startTrigger();
        }
        resetCount++;
    }

    void LogBehavior (int controlNumber, string objectName) 
    {
        if (logTrigger && (Logger.GetComponent<PathScript>() != null))
        {
            Logger.GetComponent<PathScript>().logEventData("TriggerTimer " + this.name + " triggered with state " +
                                                           controlNumber + " on object " + objectName);
        }
    }
}
