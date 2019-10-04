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

public class PathScript3 : MonoBehaviour
{
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
    public string datasetPrefix = "";
    [Space(10)]

    //delay among individual movement measurements
    //  going too low (~100ms) is not recommended, as there is some measurement delay, esp. with low-end systems
    [Range(0.01f, 5f)]
    public float movementLogInterval = 0.5f;
    //target camera (careful with some Unity plugins of HMDs: there may be multiple cameras in such scenes)
    public Camera headCamera;
    public bool directCameraAccess;
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

    //data buffers
    private string pathBuffer;
    private string etBuffer;

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

    // ----------------------------------------------------------------------------------------------------------------
    // Program initialization and run update
    // ----------------------------------------------------------------------------------------------------------------

    // Use this for initialization
    void Start()
    {
        specialKeysLength = specialKeys.Length;
        specialKeyMeaningsLength = specialKeyMeanings.Length;
        GenerateFileNames(true);
        StartCoroutine(PathLogger());
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
		string fileNameTime = System.DateTime.Now.ToString("_yyyyMMdd_HHmmss");
		this.pathFileName = @"d:\" + datasetPrefix + "_" + "path" + fileNameTime + ".txt";
		this.etFileName = @"d:\" + datasetPrefix + "_" + "et" + fileNameTime + ".txt";
        this.et2FileName = @"d:\" + datasetPrefix + "_" + "et2" + fileNameTime + ".txt";
        this.collisionFileName = @"d:\" + datasetPrefix + "_" + "collision" + fileNameTime + ".txt";
		this.controllerFileName = @"d:\" + datasetPrefix + "_" + "controller" + fileNameTime + ".txt";
        this.eventLogFileName = @"d:\" + datasetPrefix + "_" + "eventlog" + fileNameTime + ".txt";

        //append the first row to files to indicate variable names, if specified
        if (includeVariableNames)
        {
            if (logMovement)
            {
                System.IO.File.Create(pathFileName).Dispose();
                System.IO.File.AppendAllText(pathFileName, "id,hour,min,sec,ms," +
                                                           "xpos,ypos,zpos," +
                                                           "uMousePos,vMousePos,wMousePos," + 
                                                           "uGazePos,vGazePos,wGazePos\r\n");
            }
			if (logCollisions)
            {
                System.IO.File.Create(collisionFileName).Dispose();
                System.IO.File.AppendAllText(collisionFileName, "id,hour,min,sec,ms," +
                                                                "xpos,ypos,zpos," +
                                                                "uMousePos,vMousePos,wMousePos," +
                                                                "uGazePos,vGazePos,wGazePos," +
                                                                "objectName,xobj,yobj,zobj\r\n");
            }
            if (logController)
            {
                System.IO.File.Create(controllerFileName).Dispose();
                System.IO.File.AppendAllText(controllerFileName, "id,hour,min,sec,ms," +
                                                                 "xpos,ypos,zpos," +
                                                                 "xangle,yangle,zangle,xrot,yrot,zrot," +
                                                                 "keyPressed,isDown,keyDirection\r\n");
            }
            if (logEyeTracking)
            {
                System.IO.File.Create(etFileName).Dispose();
                System.IO.File.AppendAllText(etFileName, "id,hour,min,sec,ms," +
                                                         "xpos,ypos,zpos," +
                                                         "uMousePos,vMousePos,wMousePos," +
                                                         "uGazePos,vGazePos,wGazePos," +
                                                         "objName,objFocusType,xobj,yobj,zobj\r\n");
            }
            if (logEyeTracking2)
            {
                System.IO.File.Create(et2FileName).Dispose();
                System.IO.File.AppendAllText(etFileName, "id,hour,min,sec,ms," +
                                                         "xpos,ypos,zpos," +
                                                         "uMousePos,vMousePos,wMousePos," +
                                                         "uGazePos,vGazePos,wGazePos," +
                                                         "xobj,yobj,zobj\r\n");
            }
            if (eventLog)
            {
                System.IO.File.Create(eventLogFileName).Dispose();
                System.IO.File.AppendAllText(eventLogFileName, "id,hour,min,sec,ms," +
                                                               "eventInfo\r\n");
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
            string currentData = pathCounter + "," + GetCurrentTime() + ",";
            if (directCameraAccess)
            {
                currentData += GetCurrentPositionDirectly() + "\r\n";
            }
            else
            {
                currentData += GetCurrentPosition() + "\r\n";
            }

            //log, or buffer to log
            pathBuffer += currentData;
            if (pathCounter % bufferSize == 0)
            {
                System.IO.File.AppendAllText(pathFileName, pathBuffer);
                pathBuffer = "";
                Debug.Log("Emptied " + bufferSize + " buffer");
            }
            
            pathCounter++;

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
                string currentData = etCounter + "," + GetCurrentTime() + "," + GetCurrentPosition() + ","
                                   + objName + "," + objFocusType + "," + objCoordinates + "\r\n";
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
            string currentData = etCounter + "," + GetCurrentTime() + "," + GetCurrentPosition()
                  + ", " + fixationPosition.x + ", " + fixationPosition.y + ", " + fixationPosition.z + "\r\n";

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
                string currentData = collisionCounter + "," + GetCurrentTime() + "," + GetCurrentPosition() + ","
                                   + objName + "," + objCoordinates + "\r\n";
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
            string currentData = controllerCounter + "," + GetCurrentTime() + "," + GetCurrentPosition() + ","
                               + keyPress + "," + isDown + "," + keyDirection + "\r\n";
            System.IO.File.AppendAllText(controllerFileName, currentData);
            controllerCounter++;
        }		
	}

    public void logEventData(string eventInfo)
    {
        if (eventLog)
        {
            string currentData = eventLogCounter + "," + GetCurrentTime() + "," + eventInfo + "\r\n";
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
        return System.DateTime.Now.ToString("HH,mm,ss") + "," + milliseconds;
    }

    // Get current player position and look direction
    string GetCurrentPosition()
    {
        string coordinates = transform.position.ToString().Trim('(', ')');
        //player mouse center, player gaze center (VR only)
        //transform.rotation -- in radians; transform.rotation.eulerAngles -- in degrees
        string rotationMouse = transform.rotation.eulerAngles.ToString().Trim('(', ')');
        //string rotationGaze = Camera.main.transform.rotation.eulerAngles.ToString().Trim('(', ')');
        string rotationGaze = headCamera.transform.rotation.eulerAngles.ToString().Trim('(', ')');
        //Debug.Log("camera gloabl Y rotation: " + headCamera.transform.rotation.eulerAngles.y);
        //Debug.Log("camera local  Y rotation: " + headCamera.transform.localRotation.eulerAngles.y);
        return coordinates + "," + rotationMouse + "," + rotationGaze;
    }

    // Get current player position and look direction -- directly acessing the main camera (works better)
    string GetCurrentPositionDirectly()
    {
        string coordinates = transform.position.ToString().Trim('(', ')');
        string rotationMouse = Camera.main.transform.eulerAngles.x.ToString() + ", "
                             + Camera.main.transform.eulerAngles.y.ToString() + ", 0";
        string rotationGaze = rotationMouse; //not relevant, as this solution logs them the same
        return coordinates + "," + rotationMouse + "," + rotationGaze;
    }

    public string cleanNumericData(string inputString)
    {
        string outputString = inputString.Replace("(", "").Replace(")", ""); //do more cleaning, if applicable
        return outputString;
    }
}
