// --------------------------------------------------------------------------------------------------------------------
// Unity VR PathScript, version 2018-08-23
// This script collects all viable data a virtual environment user can generate.
//  
// The data is collected upon delay (e.g. 0.2s means five logs per second).
//     Use movementLogInterval variable to set this up.
// The data structure is: log # | hour | minute | second | milliseconds | x position | y | z | u camera angle | v | w 
// During the data-collection process, logs are continually written to a unique timestamped file (fileName variable).
//     The data is saved to a .csv file format (easily processed by any statistics software).
//
// Usage: Attach this script to an intended FPSController (dragging & dropping within the Hierarchy browser will do).
// --------------------------------------------------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

public class PathScript3_5 : MonoBehaviour
{
    //data format
    private string separatorDecimal = ".";        
    private string separatorItem = ",";
    private NumberFormatInfo numberFormat;

    //log buffering
    [Range(1, 100)]
    public int bufferSize = 1;
    [Space(10)]

    //set up which variables are to be logged (some of these are called from external scripts on associated objects)
    public bool logMovement;
    public bool logCollisions;
    public bool logController;
    public bool logEyeTracking;
    public bool logEyeTracking2;
    public bool eventLog;
    public bool logMovingObjects;
    public List<GameObject> movingObjects;
    [Space(10)]

    //delay among individual movement measurements
    //  going too low (~100ms) is not recommended, as there is some measurement delay, esp. with low-end systems
    [Range(0f, 5f)]
    public float movementLogInterval = 0.5f;
    //target camera (careful with some Unity plugins of HMDs: there may be multiple cameras in such scenes)
    public Camera headCamera;
    public bool directCameraAccess;
    [Space(10)]

    //log files naming convention and save location
    public string datasetPrefix = "";
    public string saveLocation = "D:\\";
    [Space(10)]

    //extra controller keys
    public bool allowSpecialKeys;
    public KeyCode[] specialKeys;
    public string[] specialKeyMeanings;
    private int specialKeysLength;
    private int specialKeyMeaningsLength;

    //data file names
    private string pathFileName;
	private string etFileName;
    private string et2FileName;
    private string collisionFileName;
	private string controllerFileName;
    private string eventLogFileName;
    private string movingObjectsFileName;

    //data buffers
    private string pathBuffer;
    private string etBuffer;
    private string movingObjectsBuffer;

	//keyPress states
	private bool isPressedUp;
	private bool isPressedDown;
	private bool isPressedLeft;
	private bool isPressedRight;

    //service variables - to prevent streams of same data
	private string etLastFocusType;
	private string collisionLastObject;

    //measurement iterator
    private int pathCounter;
    private int etCounter;
    private int collisionCounter;
    private int controllerCounter;
    private int eventLogCounter;
    private int movingObjectsCounter;
    //participant/data marker
    private string fileNameTime;

    // ----------------------------------------------------------------------------------------------------------------
    // Program initialization and run update
    // ----------------------------------------------------------------------------------------------------------------

    // Use this for initialization
    void Start()
    {
        //to have a standardized decimal separator across different system locales
        //usage: someNumber.ToString(numberFormat)
        numberFormat = new NumberFormatInfo();
        numberFormat.NumberDecimalSeparator = separatorDecimal;

        specialKeysLength = specialKeys.Length;
        specialKeyMeaningsLength = specialKeyMeanings.Length;
        GenerateFileNames(true);
        StartCoroutine(PathLogger());
        StartCoroutine(MovingObjectsLogger());
	}

	//for logController (keyPress)
	void Update()
    {
		//multiple keys can be (un)pressed in a single frame
		if (Input.GetKeyDown("up"))    { logControllerData("up",    true);  }
		if (Input.GetKeyDown("down"))  { logControllerData("down",  true);  }
		if (Input.GetKeyDown("left"))  { logControllerData("left",  true);  }
		if (Input.GetKeyDown("right")) { logControllerData("right", true);	}
		if (Input.GetKeyUp("up"))      { logControllerData("up",    false); }
		if (Input.GetKeyUp("down"))    { logControllerData("down",  false); }
		if (Input.GetKeyUp("left"))    { logControllerData("left",  false); }
		if (Input.GetKeyUp("right"))   { logControllerData("right", false); }
		//other, special keys
        if (allowSpecialKeys)
        {
            //if (Input.GetKeyDown ("x")) { logControllerData("event_marker", true); }
            //if (Input.GetKeyDown("q")) { logControllerData("teleporter", true); }
            for (int i = 0; i < specialKeysLength; i++)
            {
                if (i <= specialKeyMeaningsLength)
                {
                    if (specialKeyMeanings[i] != "")
                    {
                        if (Input.GetKeyDown(specialKeys[i])) { logControllerData(specialKeyMeanings[i], true); }
                    }
                    else
                    {
                        if (Input.GetKeyDown(specialKeys[i])) { logControllerData("special-" + i, true); }
                    }                    
                }
            }
        }
		
	}

    // Generate new file name on every run (as per timestamp)
    void GenerateFileNames(bool includeVariableNames)
    {
		fileNameTime = System.DateTime.Now.ToString("_yyyyMMdd_HHmmss");
		this.pathFileName = @saveLocation + datasetPrefix + "_" + "path" + fileNameTime + ".txt";
		this.etFileName = @saveLocation + datasetPrefix + "_" + "et" + fileNameTime + ".txt";
        this.et2FileName = @saveLocation + datasetPrefix + "_" + "et2" + fileNameTime + ".txt";
        this.collisionFileName = @saveLocation + datasetPrefix + "_" + "collision" + fileNameTime + ".txt";
		this.controllerFileName = @saveLocation + datasetPrefix + "_" + "controller" + fileNameTime + ".txt";
        this.eventLogFileName = @saveLocation + datasetPrefix + "_" + "eventlog" + fileNameTime + ".txt";
        this.movingObjectsFileName  = @saveLocation + datasetPrefix + "_" + "movingObj" + fileNameTime + ".txt";

        //append the first row to files to indicate variable names, if specified
        if (includeVariableNames)
        {
            if (logMovement)
            {
                System.IO.File.Create(pathFileName).Dispose();
                System.IO.File.AppendAllText(pathFileName,
                    "userId" + separatorItem + "logId" + separatorItem + "timestamp" + separatorItem +
                    "hour" + separatorItem + "min" + separatorItem +"sec" + separatorItem +"ms" + separatorItem +
                    "xpos" + separatorItem + "ypos" + separatorItem + "zpos" + separatorItem +
                    "uMousePos" + separatorItem + "vMousePos" + separatorItem + "wMousePos" + separatorItem + 
                    "uGazePos" + separatorItem + "vGazePos" + separatorItem + "wGazePos" +
                    "\r\n");
            }
			if (logCollisions)
            {
                System.IO.File.Create(collisionFileName).Dispose();
                System.IO.File.AppendAllText(collisionFileName, 
                    "userId" + separatorItem + "logId" + separatorItem + "timestamp" + separatorItem +
                    "hour" + separatorItem + "min" + separatorItem + "sec" + separatorItem +"ms" + separatorItem +
                    "xpos" + separatorItem + "ypos" + separatorItem + "zpos" + separatorItem +
                    "uMousePos" + separatorItem + "vMousePos" + separatorItem + "wMousePos" + separatorItem + 
                    "uGazePos" + separatorItem + "vGazePos" + separatorItem + "wGazePos" + separatorItem +
                    "objectName" + separatorItem + "xobj" + separatorItem + "yobj" + separatorItem + "zobj" +
                    "\r\n");
            }
            if (logController)
            {
                System.IO.File.Create(controllerFileName).Dispose();
                System.IO.File.AppendAllText(controllerFileName, 
                    "userId" + separatorItem + "logId" + separatorItem + "timestamp" + separatorItem +
                    "hour" + separatorItem + "min" + separatorItem + "sec" + separatorItem +"ms" + separatorItem +
                    "xpos" + separatorItem + "ypos" + separatorItem + "zpos" + separatorItem +
                    "xangle" + separatorItem + "yangle" + separatorItem + "zangle" + separatorItem +
                    "xrot" + separatorItem + "yrot" + separatorItem + "zrot" + separatorItem +
                    "keyPressed" + separatorItem + "isDown" + separatorItem + "keyDirection" +
                    "\r\n");
            }
            if (logEyeTracking)
            {
                System.IO.File.Create(etFileName).Dispose();
                System.IO.File.AppendAllText(etFileName, 
                    "userId" + separatorItem + "logId" + separatorItem + "timestamp" + separatorItem +
                    "hour" + separatorItem + "min" + separatorItem + "sec" + separatorItem +"ms" + separatorItem +
                    "xpos" + separatorItem + "ypos" + separatorItem + "zpos" + separatorItem +
                    "uMousePos" + separatorItem + "vMousePos" + separatorItem + "wMousePos" + separatorItem + 
                    "uGazePos" + separatorItem + "vGazePos" + separatorItem + "wGazePos" + separatorItem +
                    "objName" + separatorItem + "objFocusType" + separatorItem +
                    "xobj" + separatorItem + "yobj" + separatorItem + "zobj" +
                    "\r\n");
            }
            if (logEyeTracking2)
            {
                System.IO.File.Create(et2FileName).Dispose();
                System.IO.File.AppendAllText(etFileName,
                    "userId" + separatorItem + "logId" + separatorItem + "timestamp" + separatorItem +
                    "hour" + separatorItem + "min" + separatorItem + "sec" + separatorItem +"ms" + separatorItem +
                    "xpos" + separatorItem + "ypos" + separatorItem + "zpos" + separatorItem +
                    "uMousePos" + separatorItem + "vMousePos" + separatorItem + "wMousePos" + separatorItem + 
                    "uGazePos" + separatorItem + "vGazePos" + separatorItem + "wGazePos" + separatorItem +
                    "xobj" + separatorItem + "yobj" + separatorItem + "zobj" +
                    "\r\n");
            }
            if (eventLog)
            {
                System.IO.File.Create(eventLogFileName).Dispose();
                System.IO.File.AppendAllText(eventLogFileName,
                    "userId" + separatorItem + "logId" + separatorItem + "timestamp" + separatorItem +
                    "hour" + separatorItem + "min" + separatorItem + "sec" + separatorItem +"ms" +
                    "eventInfo" +
                    "\r\n");
            }
            if (logMovingObjects)
            {
                System.IO.File.Create(movingObjectsFileName).Dispose();
                System.IO.File.AppendAllText(movingObjectsFileName,
                    "userId" + separatorItem + "logId" + separatorItem + "timestamp" + separatorItem +
                    "hour" + separatorItem + "min" + separatorItem + "sec" + separatorItem +"ms" + separatorItem +
                    "objName" + separatorItem +
                    "xobj" + separatorItem + "yobj" + separatorItem + "zobj" + separatorItem +
                    "xobjRot" + separatorItem + "yobjRot" + separatorItem + "zobjRot" +
                    //not really needed - just for object ET purposes (observer-objects coordinates in one place)
                    separatorItem + "xpos" + separatorItem + "ypos" + separatorItem + "zpos" +
                    //-------------------------------------------------------------------------------------------
                    "\r\n");
            }
		}
	}

    // ----------------------------------------------------------------------------------------------------------------
    // Main logger functions
    // ----------------------------------------------------------------------------------------------------------------

    //movement & path is logged continuously, from within this script (gotta be attached to (a part of) the controller)
    IEnumerator PathLogger()
    {
        while (logMovement)
        {
            string currentData = fileNameTime + separatorItem + pathCounter + separatorItem +
                                 GetCurrentTimestamp() + separatorItem + GetCurrentTime() + separatorItem;
            if (directCameraAccess)
            {
                currentData += GetCurrentPositionDirectly();
            }
            else
            {
                currentData += GetCurrentPosition();
            }
            currentData += "\r\n";

            //log, or buffer to log
            pathBuffer += currentData;
            if (pathCounter % bufferSize == 0)
            {
                System.IO.File.AppendAllText(pathFileName, pathBuffer);
                pathBuffer = "";
                Debug.Log("PathScript emptied a buffer of " + bufferSize + " items @" + Time.time);
            }
                 
            pathCounter++;
            yield return new WaitForSeconds(movementLogInterval);
        }
    }

    //logging of other moving objects is also a coroutine; these objects are accessed from the movingObjects<> list
    IEnumerator MovingObjectsLogger()
    {
        while (logMovingObjects)
        {
            string dataPerCycle = "";
            string dataPerEachItem = fileNameTime + separatorItem + movingObjectsCounter + separatorItem +
                                     GetCurrentTimestamp() + separatorItem + GetCurrentTime();
            foreach (GameObject item in movingObjects)
            {
                dataPerCycle += dataPerEachItem + separatorItem + item.name + separatorItem +
                                item.transform.position.x.ToString(numberFormat) + separatorItem +
                                item.transform.position.y.ToString(numberFormat) + separatorItem +
                                item.transform.position.z.ToString(numberFormat) + separatorItem +
                                item.transform.rotation.eulerAngles.x.ToString(numberFormat) + separatorItem +
                                item.transform.rotation.eulerAngles.y.ToString(numberFormat) + separatorItem +
                                item.transform.rotation.eulerAngles.z.ToString(numberFormat) +
                                //not really needed - just for object ET purposes (observer-objects coords in one place)
                                separatorItem + transform.position.x.ToString(numberFormat) + separatorItem +
                                transform.position.y.ToString(numberFormat) + separatorItem +
                                transform.position.z.ToString(numberFormat) +
                                //--------------------------------------------------------------------------------------
                                "\r\n";
            }

            //log, or buffer to log
            movingObjectsBuffer += dataPerCycle;
            if (movingObjectsCounter % bufferSize == 0)
            {
                System.IO.File.AppendAllText(movingObjectsFileName, movingObjectsBuffer);
                movingObjectsBuffer = "";
                Debug.Log("PathScript emptied a buffer of " + bufferSize + " moving object items @" + Time.time);
            }

            movingObjectsCounter++;
            yield return new WaitForSeconds(movementLogInterval);
        }
    }

    //object-based ET logger
	public void logEtData(string objName, string objFocusType, string objCoordinates)
    {
        if (logEyeTracking)
        {
            //necessary precondition, as continuous logging of gaze being kept on an object is not wanted here
            //if (etLastFocusType != objFocusType) {
                //get rid of extra brackets, if present
                objCoordinates = cleanNumericData(objCoordinates);
                string currentData = fileNameTime + separatorItem + etCounter + separatorItem +
                                     GetCurrentTimestamp() + separatorItem + GetCurrentTime() + separatorItem +
                                     GetCurrentPosition() + separatorItem +
                                     objName + separatorItem +
                                     objFocusType + separatorItem +
                                     objCoordinates + separatorItem +
                                     fileNameTime + "\r\n";
                System.IO.File.AppendAllText(etFileName, currentData);
                etCounter++;
                etLastFocusType = objFocusType;
            //}
        }
	}

    //coordinate-based ET logger
    public void logEtData2(Vector3 fixationPosition)
    {
        //to implement this, try the following:
        //public GameObject Player = null;
        //void Start()
        //{
        //  Player = GameObject.FindGameObjectWithTag("Player");
        //}
        //Player.GetComponent<PathScript3>().logEt2Data(Vector3 fixationPosition);
        if (logEyeTracking2)
        {
            string currentData = fileNameTime + separatorItem + etCounter + separatorItem +
                                 GetCurrentTimestamp() + separatorItem + GetCurrentTime() + separatorItem +
                                 GetCurrentPosition() + separatorItem +
                                 fixationPosition.x.ToString(numberFormat) + separatorItem +
                                 fixationPosition.y.ToString(numberFormat) + separatorItem +
                                 fixationPosition.z.ToString(numberFormat) + separatorItem +
                                 fileNameTime + "\r\n";

            //log, or buffer to log
            etBuffer += currentData;
            if (etCounter % bufferSize == 0)
            {
                System.IO.File.AppendAllText(etFileName, etBuffer);
                etBuffer = "";
            }
            etCounter++;
        }
    }
	
	public void logCollisionData(string objName, string objCoordinates)
    {
        if (logCollisions)
        {
            //necessary precondition, as continuous logging of object collision is not wanted here
            if (collisionLastObject != objName)
            {
                objCoordinates = cleanNumericData(objCoordinates);
                string currentData = fileNameTime + separatorItem + collisionCounter + separatorItem +
                                     GetCurrentTimestamp() + separatorItem + GetCurrentTime() + separatorItem +
                                     GetCurrentPosition() + separatorItem +
                                     objName + separatorItem +
                                     objCoordinates + separatorItem +
                                     fileNameTime + "\r\n";
                System.IO.File.AppendAllText(collisionFileName, currentData);
                collisionCounter++;
                collisionLastObject = objName;
            }
        }	    
	}

	public void logControllerData(string keyPress, bool isDown)
    {
        if (logController)
        {
            //keyPress direction logic
            if (keyPress == "up" && !isPressedDown)    { isPressedUp = !isPressedUp; }
            if (keyPress == "down" && !isPressedUp)    { isPressedDown = !isPressedDown; }
            if (keyPress == "left" && !isPressedRight) { isPressedLeft = !isPressedLeft; }
            if (keyPress == "right" && !isPressedLeft) { isPressedRight = !isPressedRight; }

            //---------------------------------------------------------------------------------------------------------
            //ADD FIX FOR THE LIKES OF PRESS.LEFT, PRESS.RIGHT, KEYUP.LEFT (+ UP/DOWN). DIRECTION CHANGES IN SUCH CASES

            //current movement direction (pressing an opposite (e.g. left while already moving right) does nothing)
            string keyDirection;
            if (!isPressedUp && !isPressedDown && !isPressedLeft && !isPressedRight)
            {
                keyDirection = "still";
            }
            else if (isPressedUp && !isPressedDown && !isPressedLeft && !isPressedRight)
            {
                keyDirection = "up";
            }
            else if (!isPressedUp && isPressedDown && !isPressedLeft && !isPressedRight)
            {
                keyDirection = "down";
            }
            else if (!isPressedUp && !isPressedDown && isPressedLeft && !isPressedRight)
            {
                keyDirection = "left";
            }
            else if (!isPressedUp && !isPressedDown && !isPressedLeft && isPressedRight)
            {
                keyDirection = "right";
            }
            else if (isPressedUp && !isPressedDown && isPressedLeft && !isPressedRight)
            {
                keyDirection = "up-left";
            }
            else if (isPressedUp && !isPressedDown && !isPressedLeft && isPressedRight)
            {
                keyDirection = "up-right";
            }
            else if (!isPressedUp && isPressedDown && isPressedLeft && !isPressedRight)
            {
                keyDirection = "down-left";
            }
            else if (!isPressedUp && isPressedDown && !isPressedLeft && isPressedRight)
            {
                keyDirection = "down-right";
            }
            else
            { //just in case...
                keyDirection = "still";
            }

            //logging
            string currentData = fileNameTime + separatorItem + controllerCounter + separatorItem +
                                 GetCurrentTimestamp() + separatorItem + GetCurrentTime() + separatorItem +
                                 GetCurrentPosition() + separatorItem +
                                 keyPress + separatorItem +
                                 isDown + separatorItem +
                                 keyDirection + separatorItem +
                                 fileNameTime + "\r\n";
            System.IO.File.AppendAllText(controllerFileName, currentData);
            controllerCounter++;
        }		
	}

    public void logEventData(string eventInfo)
    {
        if (eventLog)
        {
            string currentData = fileNameTime + separatorItem + eventLogCounter + separatorItem +
                                 GetCurrentTimestamp() + separatorItem + GetCurrentTime() + separatorItem +
                                 eventInfo + separatorItem +
                                 fileNameTime + "\r\n";
            System.IO.File.AppendAllText(eventLogFileName, currentData);
            eventLogCounter++;
        }
    }

    // ----------------------------------------------------------------------------------------------------------------
    // Auxilary functions
    // ----------------------------------------------------------------------------------------------------------------

    // Get current time
    string GetCurrentTime()
    {
        long milliseconds = (System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond) % 1000;
        return System.DateTime.Now.ToString("HH,mm,ss").Replace(",", separatorItem) + separatorItem + milliseconds;
    }

    //Get current time in Unix epoch format
    string GetCurrentTimestamp()
    {
        var currentTime = new System.DateTimeOffset(System.DateTime.Now).ToUniversalTime().ToUnixTimeSeconds();
        return currentTime.ToString();
    }

    // Get current player position and look direction
    string GetCurrentPosition()
    {
        //string coordinates = transform.position.ToString().Trim('(', ')');
        string coordinates = transform.position.x.ToString(numberFormat) + separatorItem +
                             transform.position.y.ToString(numberFormat) + separatorItem +
                             transform.position.z.ToString(numberFormat);
        //player mouse center, player gaze center (VR only)
        //transform.rotation -- in radians; transform.rotation.eulerAngles -- in degrees
        //string rotationMouse = transform.rotation.eulerAngles.ToString().Trim('(', ')');
        string rotationMouse = transform.rotation.eulerAngles.x.ToString(numberFormat) + separatorItem +
                               transform.rotation.eulerAngles.y.ToString(numberFormat) + separatorItem +
                               transform.rotation.eulerAngles.z.ToString(numberFormat);
        //string rotationGaze = headCamera.transform.rotation.eulerAngles.ToString().Trim('(', ')');
        string rotationGaze = headCamera.transform.rotation.eulerAngles.x.ToString(numberFormat) + separatorItem +
                              headCamera.transform.rotation.eulerAngles.y.ToString(numberFormat) + separatorItem +
                              ((Mathf.Round(headCamera.transform.rotation.eulerAngles.z * 100)) / 100.0)
                              .ToString(numberFormat); //dirty fix
        //Debug.Log("camera gloabl Y rotation: " + headCamera.transform.rotation.eulerAngles.y);
        //Debug.Log("camera local  Y rotation: " + headCamera.transform.localRotation.eulerAngles.y);
        return coordinates + separatorItem + rotationMouse + separatorItem + rotationGaze;
    }

    // Get current player position and look direction -- directly acessing the main camera (works better)
    //TODO: probably delete, uselesss...
    string GetCurrentPositionDirectly()
    {
        string coordinates = cleanNumericData(transform.position.ToString());
        string rotationMouse = Camera.main.transform.eulerAngles.x.ToString(numberFormat) + separatorItem +
                               Camera.main.transform.eulerAngles.y.ToString(numberFormat) + separatorItem + "0";
        string rotationGaze = rotationMouse; //not relevant, as this solution logs them the same
        return coordinates + separatorItem + rotationMouse + separatorItem + rotationGaze;
    }

    public string cleanNumericData(string inputString)
    {
        string outputString = inputString.Replace("(", "").Replace(")", "").Replace(",", separatorItem);
        //do more cleaning, if applicable
        return outputString;
    }
}
