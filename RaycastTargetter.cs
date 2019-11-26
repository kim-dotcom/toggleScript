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
    public bool logCoordinatesSource;
    public bool logCoordinatesTarget;
    [Range(0f,5f)]
    public float logDelay = 0.01f;
    [Space(10)]

    public bool visualizeRay;
    [Space(10)]

    //logger object (PathScript) and logging setup
    public GameObject Logger;
    public bool customLogfile;
    public string customLogfileName = "raycastTargetter";

    //log data format
    private string dataformatDefault;
    private string dataformatEvents;
    private string dataformatSource;
    private string dataformatTarget;
    private string dataformatConcatenated;

    //auxiliaries
    private Vector3 sourceCoordinates;
    private Vector3 targetCoordinates;
    private Vector3 normalizedDirection;
    private bool initValidized;
    private bool initWaited;
    private string separatorDecimal = ".";
    private string separatorItem = ",";
    private NumberFormatInfo numberFormat;

    void Start()
    {
        //see if there are source/target objects to latch to
        initValidized = false;
        initWaited = false;
        if (raySource == null || rayDestination == null) {
            Debug.LogWarning("RaycasterTargetter failed to initialize. Check ray source/destination.");
        }
        else if (Logger == null || Logger.GetComponent<PathScript>() == null)
        {
            Debug.LogWarning("Logger failed to initialize. Check Logger GameObject (PathScript).");
        }
        //if so, get the coordinates and allow raycast processing
        else
        {
            //init coordinates
            initRaycastCoordinates();
            initValidized = true;
            //init file format
            Dictionary<string, string> fileFormatDict = new Dictionary<string, string>();
            fileFormatDict = Logger.GetComponent<PathScript>().getLogFormat();
            separatorItem = fileFormatDict["separatorFormat"];
            separatorDecimal = fileFormatDict["decimalFormat"];
            numberFormat = new NumberFormatInfo();
            numberFormat.NumberDecimalSeparator = separatorDecimal;
            //init logger
            if (customLogfile || customLogfileName != "")
            {
                dataformatDefault = "userId" + separatorItem + "logId" + separatorItem + "timestamp" + separatorItem +
                                    "hour" + separatorItem + "min" + separatorItem + "sec" + separatorItem +"ms";
                dataformatEvents = "objectHitEvaluation" + separatorItem + "objectHitName";
                dataformatSource = "dataType" + separatorItem +
                                   "sourceX" + separatorItem + "sourceY" + separatorItem + "sourceZ" + separatorItem +
                                   "vectorX" + separatorItem + "vectorY" + separatorItem + "vectorZ";
                dataformatTarget = "targetX" + separatorItem + "targetY" + separatorItem + "targetZ" + separatorItem +
                                   "sourceTargetDistance" + separatorItem + "sourceHitDistance";
                dataformatConcatenated = dataformatDefault;
                if (logEvents) dataformatConcatenated += separatorItem + dataformatEvents;
                if (logCoordinatesSource) dataformatConcatenated += separatorItem + dataformatSource;
                if (logCoordinatesTarget) dataformatConcatenated += separatorItem + dataformatTarget;
                dataformatConcatenated += "\r\n";
                Logger.GetComponent<PathScript>().generateCustomFileNames(dataformatConcatenated, customLogfileName,
                                                                          gameObject.name);
            }
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
            RaycastHit hit = new RaycastHit();
            //log hit object events
            if (logEvents)
            {
                if (Physics.Raycast(sourceCoordinates, normalizedDirection, out hit, rayDistance))
                {
                    logData += "objectHit" + separatorItem + hit.collider.gameObject.name;
                }
                else if (hit.collider == null) {
                    logData += "noObjectHit" + separatorItem + "null";
                }
            }
            //log raycast coordinates(origin, normalized direction)
            if (logCoordinatesSource)
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
            //log object hit coordinates
            if (logCoordinatesTarget)
            {
                if (logEvents || logCoordinatesSource) logData += separatorItem;
                logData += targetCoordinates.x + separatorItem +
                           targetCoordinates.y + separatorItem + 
                           targetCoordinates.z + separatorItem +
                           Vector3.Distance(sourceCoordinates, targetCoordinates) + separatorItem +
                           hit.distance;
            }
            //log the data and wait till the next cycle
            //Debug.Log("RCTargetter sending this: " + logData + ", to this file: " + customLogfileName);
            if (customLogfile)
            {
                Logger.GetComponent<PathScript>().logCustomData(customLogfileName, logData);
            }
            else
            {
                Logger.GetComponent<PathScript>().logEventData(logData);
            }
            
            yield return new WaitForSeconds(logDelay);
        }
    }

    //do this on start and then per every coroutine tick
    void initRaycastCoordinates()
    {
            sourceCoordinates = raySource.transform.position;
            targetCoordinates = rayDestination.transform.position;
            normalizedDirection = (targetCoordinates - sourceCoordinates).normalized;
    }
}
