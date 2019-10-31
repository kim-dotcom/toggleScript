using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerDistance : MonoBehaviour
{
    //the distance of which object (usually FPSController, but can be sth else)
    public GameObject TargetObject;
    [Range(1,200)]
    //how often does the check happen? To save some performance...
    public int distanceCheckFreq;
    [Space(10)]
    protected int checkIterator = 0;
    protected float currentDistance;
    
    //the following structure is necessary, so as to have a 2D array (objects per distance) visible in the inspector
    [System.Serializable]
    public struct TargetDistanceLists
    {
        //controlNumber is sent on a per key basis
        public int controlNumber;
        public float minDistance;
        public float maxDistance;
        public GameObject[] thisDistanceObjects;
        [HideInInspector]  public int triggerCount;
    }
    public TargetDistanceLists[] targetDistances;
    [Space(10)]

    public bool logTrigger;
    private GameObject Logger;

    void Start()
    {
        if (logTrigger)
        {
            Logger = GameObject.FindGameObjectWithTag("MainCamera");
        }
        //do this on start, because the conditions can be met right away
        getDistance();
    }

    void Update()
    {
        //check for distance every nth frame
        checkIterator = (checkIterator + 1) % distanceCheckFreq;
        if (checkIterator == 0)
        {
            getDistance();
            //Debug.Log(currentDistance);

            for (int i = 0; i < targetDistances.Length; i++)
            {
                //if within min-max distance range...
                if ((currentDistance >= targetDistances[i].minDistance) &&
                    (currentDistance < targetDistances[i].maxDistance))
                {
                    //..toggle the objects in that range
                    for (int j = 0; j < targetDistances[i].thisDistanceObjects.Length; j++)
                    {
                        //TODO: toggle only if distance interval changes (ATM, it toggles every nth frame regardless)
                        targetDistances[i].thisDistanceObjects[j].GetComponent<ToggleScript>()
                        .Toggle(targetDistances[i].controlNumber);
                        targetDistances[i].triggerCount++;
                        LogBehavior(targetDistances[i].controlNumber, targetDistances[i].thisDistanceObjects[j].name,
                                    currentDistance);
                    }
                }
            }
        }
    }

    void getDistance()
    {
        currentDistance = Vector3.Distance (this.transform.position, TargetObject.transform.position);
    }

    void LogBehavior (int controlNumber, string objectName, float distance) 
    {
        if (logTrigger && (Logger.GetComponent<PathScript>() != null))
        {
            Logger.GetComponent<PathScript>().logEventData("TriggerDistance " + this.name + " at state " +
                                                           controlNumber + " on object " + objectName +
                                                           ", distance " + distance);
        }
    }
}
