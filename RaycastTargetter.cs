using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class RaycastTargetter : MonoBehaviour
{
    //raycast source/target and settings
    public GameObject raySource;
    public GameObject rayDestination;
    public float rayDistance = 500f;
    [Space(10)]

    //logging setup and intensity
    public bool logEvents;
    public bool logCoordinates;
    [Range(0f,5f)]
    public float logDelay = 0.01f;
    [Space(10)]

    public bool visualizeRay;
    [Space(10)]

    //logger object (PathScript)
    public GameObject Logger;

    //auxiliaries
    private Vector3 sourceCoordinates;
    private Vector3 targetCorrdinates;
    private Vector3 normalizedDirection;
    private bool initValidized;
    private bool initWaited;
    private string separatorDecimal = ".";
    private string separatorItem = ",";
    private NumberFormatInfo numberFormat;

    void Awake()
    {
        //see if there are source/target objects to latch to
        initValidized = false;
        initWaited = false;
        if (raySource == null || rayDestination == null) {
            Debug.LogWarning("RaycasterTargetter failed to initialize. Check ray source/destination.");
        }
        else if (Logger == null || Logger.GetComponent<PathScript3_5>() == null)
        {
            Debug.LogWarning("Logger failed to initialize. Check Logger GameObject (PathScript3_5).");
        }
        //if so, get the coordinates and allow raycast processing
        else
        {
            //init coordinates
            initRaycastCoordinates();
            initValidized = true;
            //init file format
            Dictionary<string, string> fileFormatDict = new Dictionary<string, string>();
            fileFormatDict = Logger.GetComponent<PathScript3_5>().getLogFormat();
            separatorItem = fileFormatDict["separatorFormat"];
            separatorDecimal = fileFormatDict["decimalFormat"];
            numberFormat = new NumberFormatInfo();
            numberFormat.NumberDecimalSeparator = separatorDecimal;
            //init logger
            StartCoroutine(RaycastInit(1f));
        }
    }

    void Update()
    {
        if (initValidized && visualizeRay)
        {
            //Not working so far...
            //Debug.DrawLine(sourceCoordinates, Logger.transform.position, Color.red, 10f, true);
        }
    }

    //RaycastLogger starts with delay (PathScript needs to initialize write-ready files first)
    public IEnumerator RaycastInit(float waitTime)
    {
        while (!initWaited)
        {
            yield return new WaitForSeconds(waitTime);
            StartCoroutine(RaycastLogger());
            StopCoroutine(RaycastInit(0f));
            initWaited = true;
        }
    }

    //raycast logging, every once in a logDelay
    IEnumerator RaycastLogger()
    {
        while (initValidized)
        {
            string logData = "";
            initRaycastCoordinates();
            //log hit object events
            if (logEvents)
            {
                RaycastHit hit = new RaycastHit();
                if (Physics.Raycast(sourceCoordinates, normalizedDirection, out hit, rayDistance))
                {
                    logData += "objectHit" + separatorItem + hit.collider.gameObject.name;
                }
                else if (hit.collider == null) {
                    logData += "noObjectHit" + separatorItem + "null";
                }
            }
            //log raycast coordinates(origin, normalized direction)
            if (logCoordinates)
            {
                if (logEvents) logData += separatorItem;
                logData += "rayCoordinates" + separatorItem +
                           sourceCoordinates.x + separatorItem +
                           sourceCoordinates.y + separatorItem +
                           sourceCoordinates.z + separatorItem +
                           normalizedDirection.x + separatorItem +
                           normalizedDirection.y + separatorItem +
                           normalizedDirection.z;
            }
            //log the data and wait till the next cycle
            Logger.GetComponent<PathScript3_5>().logEventData(logData);
            yield return new WaitForSeconds(logDelay);
        }
    }

    //do this on start and then per every coroutine tick
    void initRaycastCoordinates()
    {
            sourceCoordinates = raySource.transform.position;
            targetCorrdinates = rayDestination.transform.position;
            normalizedDirection = (targetCorrdinates - sourceCoordinates).normalized;
    }
}
