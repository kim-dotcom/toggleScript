using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerTimer : MonoBehaviour
{
    public enum TriggerActivation {byKeypress, byCollider, onStart};
    public TriggerActivation triggerActivateBy;
    public KeyCode triggerKey;
    private bool triggeredByCollider;
    [Space(10)]

    public KeyCode stopKey;
    public KeyCode resetKey;
    [Space(10)]
    private bool isStopped;
    private int stopCount;
    private int resumeCount;
    private int resetCount;
    private bool isTriggered;

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

    // Start is called before the first frame update
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

    // Update is called once per frame
    void FixedUpdate()
    {
        if ((triggerActivateBy == TriggerActivation.byKeypress) &&  Input.GetKeyDown(resetKey))
        {
            resetTrigger();
        }
        if ((triggerActivateBy == TriggerActivation.byKeypress) &&  Input.GetKeyDown(stopKey))
        {
            stopTrigger();
        }        
        if ((triggerActivateBy == TriggerActivation.byKeypress) &&  Input.GetKeyDown(triggerKey))
        {
            startTrigger();
        }

        if (isTriggered && !isStopped)
        {
            timer += Time.deltaTime;
            int i = 0;
            foreach (TargetObjectLists item in TargetObjects)
            {
                if (!item.wasTriggered && (timer >=  normalizedTriggerTimes[i]))
                {
                    foreach (GameObject subObject in item.thisTimeObjects)
                    {
                        if (subObject.GetComponent<ToggleScript>() != null)
                        {
                            subObject.GetComponent<ToggleScript>().Toggle(item.controlNumber);
                        }
                    }
                    TargetObjects[i].wasTriggered = true;
                    TargetObjects[i].triggerCount++;
                }
                i++;               
            }
        }
    }

    void OnTriggerEnter(Collider Col)
    {
        if ((Col.gameObject.tag == "Player") && !triggeredByCollider)
        {
            startTrigger();
            triggeredByCollider = true;
        }
    }

    void normalizeTriggerTimes()
    {        
        if (timeFormat == TriggerTimeFormat.additive)
        {
            int i = 0;
            float previousTime = 0;  
            foreach (TargetObjectLists item in TargetObjects)
            {
                Debug.Log(item.timeToTrigger + ", " + previousTime);
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
        isTriggered = false;
        isStopped = false;
        triggeredByCollider = false;
        timer = 0;

        if (triggerActivateBy == TriggerActivation.onStart)
        {
                startTrigger();
        }
        resetCount++;
    }
}
