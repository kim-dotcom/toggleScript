using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleScript : MonoBehaviour {
    //to reference the object the script is attached to
    private GameObject thisObject;

    //to override the trigger-specified control number
    public bool overrideControlNumber;
    public int thisControlNumber;    
    [Space(10)]

    //to make the toggle irreversible 
    public bool toggleOnlyOnce;
    //to self-toggle on application start
    public bool toggleSelfOnStart;
    private bool toggleState;
    private int toggleCount;
    [Space(10)]

    //to activate with a specifed delay
    public bool delayedActivation;
    public float activationDelay;
    //to deactivate after specified time
    public bool selfDeactivating;
    public float deactivationDelay;
    private bool selfActivatingTimerOn;
    private bool selfDeactivatingTimerOn;
    private bool delayedActivationAllowed;
    private float toggleActivationTime;
    private float toggleDeactivationTime;
    private float toggleInitTime;
    private bool activationDelayElapsed;
    private bool deactivationDelayElapsed;
    private int delayedActivationStoredControlNumber;
    [Space(10)]

    //this gameObject serves as a teleporter target
        //better be empty gameobject
    public bool isTeleporter;
    private GameObject teleportedFPSController;
    [Space(10)]

    //this gameObject is an on/off light
    //has to be attached to an actual light for this to work
    public bool isLightSwitch;
    public bool lightDimmedOnStart;
    public float lightFadeSpeed;
    public float lightMinIntensity;
    public float lightMaxIntensity;
    private float lightTargetIntensity;
    private Light thisLight;
    private bool lightIsOn;
    [Space(10)]

    //to change the color of the object (to highlight it)
    public bool changeColor;
    public Color targetColor;
    private Color originalColor;
    [Space(10)]

    //to change the texture of the object to one in the list
    public bool changeTexture;
    public List<Material> textureSet;
    private Material originalTexture;
    [Space(10)]

    //to change canvas text value to one in the list
    public bool changeCanvasText;
    public List<string> canvasTextSet;
    private Text thisObjectCanvasText;
    private string originalCanvasText;
    [Space(10)]

    //to hide/show (or show/hide) on toggle
    public bool hideOnToggle;
    public bool hiddenByDefault;
    [Space(10)]

    //to cycle hide/show of a list of child/linked gameObjects
    public bool cycleChildObjects;
    public List<GameObject> childObjectSet;
    [Space(10)]

    //to rotate the object at a specified speed/direction
    public bool rotate;    
    public Vector3 RotationSpeed;    
    public bool rotateInstantly;
    public Vector3 targetRotation;
    private Quaternion originalRotation;
    [Space(10)]

    //to rescale the object
    public bool changeScale;
    public Vector3 targetScale;
    public bool scaleContinuously;
    public bool scaleLoop;
    public float scaleSpeed;
    private Vector3 originalObjectScale;
    private bool isScaling;
    private Vector3 continuousTargetScale;
    [Space(10)]

    //to move the object on toggle
    public bool changeCoordinates;
    public Vector3 targetCoordinates;
    public List<Vector3> targetCoordinatesSet;
    public bool changeContinuously;
    //public bool changeLoop;
    public float changeSpeed;
    private Vector3 originalCoordinates;
    private bool isTransitioning;
    //private int transitioningSteps;
    private Vector3 transitioningTo;
    [Space(10)]

    //to play audio
    public bool playAudio;
    public List<AudioClip> audioFiles;
    private AudioSource audioPlayer;

    // ================================================================================================================

    // Use this for initialization
    void Start() {
        thisObject = gameObject;
        //to have default values to revert to once toggled back
        originalCoordinates = thisObject.transform.position;
        originalRotation = thisObject.transform.rotation;
        originalObjectScale = thisObject.transform.localScale;
        continuousTargetScale = targetScale;
        //transitioningSteps = targetCoordinatesSet.Capacity;

        //more default values (cosmetic if statement, to prevent nullPointerException where no Renderer exists)
        if (thisObject.GetComponent<Renderer>() != null)
        {
            originalColor = thisObject.GetComponent<Renderer>().material.color;
            originalTexture = thisObject.GetComponent<Renderer>().material;
        }

        //all chil/linked objects are hidden on start by default
        if (cycleChildObjects)
        {
            hideAllObjectsInList(childObjectSet);
        }

        //hide the object, if specifed
        if (hiddenByDefault)
        {
            thisObject.SetActive(false);
        }

        //get the FPSController to be teleported
        if (isTeleporter)
        {
            teleportedFPSController = GameObject.Find("FPSController");
            if (teleportedFPSController == null)
            {
                teleportedFPSController = GameObject.Find("Player");
            }
        }

        //get the light attached to this gameObject
        if (isLightSwitch)
        {
            thisLight = thisObject.GetComponent<Light>();
            if (lightDimmedOnStart)
            {
                thisLight.intensity = lightMinIntensity;
                lightIsOn = false;
            }
            else
            {
                thisLight.intensity = lightMaxIntensity;
                lightIsOn = true;
            }
        }

        //get the Canvas and its defautl value
        if (changeCanvasText)
        {
            thisObjectCanvasText = thisObject.GetComponent<Text>();
            originalCanvasText = thisObject.GetComponent<Text>().text;
        }

        //get the audioSource
        if (playAudio)
        {
            audioPlayer = thisObject.GetComponent<AudioSource>();
        }

        //if self-toggling, do it now
            //do it as the last thing on Start()
        if (toggleSelfOnStart)
        {
            this.Toggle(thisControlNumber);
        }
    }

    // ================================================================================================================

    void Update()
    {
        //rotation is continuous; if toggleState active, keep rotating acc. to provided direction/speed
        if (rotate && !rotateInstantly && toggleState)
        {
            thisObject.transform.Rotate(RotationSpeed.x * Time.deltaTime,
                                        RotationSpeed.y * Time.deltaTime,
                                        RotationSpeed.z * Time.deltaTime, Space.World);
        }

        //transition of position that is continuous; it is either this, or the instant one (per !changeContinuously)
        if (isTransitioning)
        {
            float changeStep = changeSpeed * Time.deltaTime;
            thisObject.transform.position = Vector3.MoveTowards(transform.position, transitioningTo, changeStep);
            //do this till destination is reached
            if (Vector3.Distance(transform.position, transitioningTo) < 0.001f)
            {
                isTransitioning = false;
            }
        }

        //scaling of position that is continuous; it is either this, or the instant one (per !changeContinuously)
            //sa,e principle as with transitioning
        if (isScaling)
        {
            float scaleStep = scaleSpeed * Time.deltaTime;
            thisObject.transform.localScale = Vector3.Lerp(thisObject.transform.localScale,
                                                           continuousTargetScale, scaleStep);
            //do this till targetScale is reached
            if (Mathf.Abs(thisObject.transform.localScale.magnitude - continuousTargetScale.magnitude) < 0.1f)
            {
                //then either end, or loop back, as per scaleLoop
                if (Mathf.Abs(thisObject.transform.localScale.magnitude - originalObjectScale.magnitude) > 0.15f)
                {
                    continuousTargetScale = originalObjectScale;
                }
                else
                {
                    continuousTargetScale = targetScale;
                }

                if (!scaleLoop)
                {
                    isScaling = false;
                }
            }
        }

        //light switch transition
        if (isLightSwitch && (( lightIsOn && (thisLight.intensity != lightMaxIntensity))
                          ||  (!lightIsOn && (thisLight.intensity != lightMinIntensity))
                             ))
        {
            thisLight.intensity = Mathf.Lerp(thisLight.intensity, lightTargetIntensity, lightFadeSpeed * Time.deltaTime);
        }

        //delayed activation check
        if (selfActivatingTimerOn && ((toggleInitTime + activationDelay) < Time.time))
        {
            //allow only once, then do it & turn off the timer
            delayedActivationAllowed = true;
            this.Toggle(delayedActivationStoredControlNumber);
            delayedActivationAllowed = false;
            selfActivatingTimerOn = false;
        }

        //delayed deactivation check
        if (selfDeactivatingTimerOn && ((toggleInitTime + deactivationDelay) < Time.time))
        {
            //allow only once, then do it & turn off the timer
            delayedActivationAllowed = true;
            this.Toggle(0);
            delayedActivationAllowed = false;
            selfDeactivatingTimerOn = false;
        }
    }

    // ================================================================================================================

    //the main Toggle() function
        //it has an on-off toggleState (either swithc on as specified in the inspector, or return to default values)
        //the controlNumber is used to further specify an action (e.g., if one of many values from a list is used)
            //regarding these, generally, if toggleState == 0, return to default value
    public void Toggle(int controlNumber) {
        //store the toggle time
            //if delayed start or self-deactivating toggle, this will be checked against later in update()
        toggleInitTime = Time.time;
        Debug.Log("Toggled " + thisObject);

        //if sent controlNumber is to be overriden with this objects one, do it now
        if (overrideControlNumber)
        {
            controlNumber = thisControlNumber;
        }

        //if delayed activation, start the timer and store the controlNumber that was sent
        if (delayedActivation && !delayedActivationAllowed)
        {
            selfActivatingTimerOn = true;
            delayedActivationStoredControlNumber = controlNumber;
        }

        //main body of the function
            //executes for standard toggles OR for the 1st time of a toggleOnlyOnce switch
            //executes for non/delayedActivation calls OR after the delayedActivation timer elapsed
        if (!(toggleOnlyOnce && (toggleCount > 0))
            && (!delayedActivation || (delayedActivation && delayedActivationAllowed)))
        {
            //if self-deactivating, start the timer now
            if (selfDeactivating)
            {
                selfDeactivatingTimerOn = true;
            }

            //to show/hide the gameObject
            if (hideOnToggle)
            {
                //if control number specified & not this one, hide
                if (controlNumber != 0)
                {
                    if (controlNumber == thisControlNumber)
                    {
                        thisObject.SetActive(true);
                    }
                    else
                    {
                        thisObject.SetActive(false);
                    }
                }
                //if no control number, show/hide per toggleState
                else
                {
                    thisObject.SetActive(toggleState);
                }
            }

            //to change the gameObjects albedo shader color
            if (changeColor)
            {
                if (!toggleState)
                {
                    thisObject.GetComponent<Renderer>().material.color = targetColor;
                }
                else
                {
                    thisObject.GetComponent<Renderer>().material.color = originalColor;
                    //TODO: other material alterations
                }
            }

            //to change the material of the gameObject
                //if the material is to fit well onto the object (e.g., a texture), set its x/y scale/offset first
            if (changeTexture)
            {
                if (controlNumber == 0)
                {
                    thisObject.GetComponent<Renderer>().material = originalTexture;
                }
                else
                {
                    if (textureSet[controlNumber - 1] != null)
                    {
                        thisObject.GetComponent<Renderer>().material = textureSet[controlNumber - 1];
                    }
                }
            }

            //to cycle a show/hide of child/linked gameObjects
                //this doesn't concern thisObject, but other objects that are linked to it
                //that is, this function breaks the otherwise uniform principle of the the script
            if (cycleChildObjects)
            {
                hideAllObjectsInList(childObjectSet);
                foreach (GameObject childObject in childObjectSet)
                {
                    Debug.Log("Control Number: " + controlNumber
                              +  ", target object: " + (childObjectSet.IndexOf(childObject) + 1));
                    //show the object specified in controlNumber, hide the others
                    if (controlNumber == (childObjectSet.IndexOf(childObject) + 1))
                    {
                        childObject.SetActive(true);
                    }
                }
            }

            //rotate the object
                //not working for proBuilder objects
                //actual rotation happens out in the update() function
            if (rotate)
            {
                //return the object to original rotation state, upon toggling off
                if (toggleState)
                {
                    thisObject.transform.rotation = originalRotation;
                }
                if (!toggleState && rotateInstantly)
                {
                    thisObject.transform.localEulerAngles = targetRotation;
                    //quaternions, eulers, localAngles, space.local/world... what a mess!
                    //see here: https://stackoverflow.com/questions/42865961/how-do-i-change-the-rotation-of-a-directional-light-c-unity-5-5
                }
            }

            //rescale the object
                //not working for proBuilder objects
                //this may or may not be continuous translation (either happens in an instant, or Update() transition)
            if (changeScale)
            {
                //the instant version
                if (!scaleContinuously)
                {
                    //return the object to original rotation state, upon toggling off
                    if (toggleState)
                    {
                        thisObject.transform.localScale = originalObjectScale;
                    }
                    //otherwise, set to new scale
                    else
                    {
                        thisObject.transform.localScale = targetScale;
                    }
                }
                //the Update() version
                else
                {
                    //where to where (front-to-back, or back-to-front rescaling)
                    if (toggleState)
                    {
                        if (scaleLoop)
                        {
                            isScaling = false;
                            thisObject.transform.localScale = originalObjectScale;
                        }
                        else
                        {
                            isScaling = true;
                        }
                    }
                    else
                    {
                        isScaling = true;
                    }                    
                }
            }

            //move the object
                //not working for proBuilder objects
                //this may or may not be continuous translation (either happens in an instant, or Update() transition)
            if (changeCoordinates)
            {
                //the instant version
                if (!changeContinuously)
                {
                    //move the object on toggle
                    if (!toggleState)
                    {
                        thisObject.transform.position = new Vector3(originalCoordinates.x + targetCoordinates.x,
                                                                    originalCoordinates.y + targetCoordinates.y,
                                                                    originalCoordinates.z + targetCoordinates.z);
                    }
                    //return the object to original rotation state, upon toggling off
                    else
                    {
                        thisObject.transform.position = originalCoordinates;
                    }
                }
                //the Update() version
                else
                {                    
                    //where to where (front-to-back, or back-to-front transition)
                    if (!toggleState)
                    {
                        transitioningTo = originalCoordinates + targetCoordinates;
                    }
                    else
                    {
                        transitioningTo = originalCoordinates;
                    }
                    isTransitioning = true;
                }
            }

            //to teleport the FPSController to this objects coordinates
                //bettter be an empty object, so that no unwanted collision/clipping happens
                //rotation is not working without tampering with private FPSController
                //e.g. https://answers.unity.com/questions/835931/rotate-first-person-controller-via-script.html
            if (isTeleporter)
            {
                teleportedFPSController.transform.position = originalCoordinates;
                teleportedFPSController.transform.rotation = originalRotation;                
            }

            //to turn the light on/off
                //the actual tranistion happens in Update()
            if (isLightSwitch)
            {
                lightIsOn = !lightIsOn;
                if (lightIsOn)
                {
                    lightTargetIntensity = lightMaxIntensity;
                }
                else
                {
                    lightTargetIntensity = lightMinIntensity;
                }
            }

            //to change this gameObject's Canvas/Text
                //the gameObject has to have both a Canvas and Text added to it
                //otherwise, the text will not show (not even prior to changing it)
            if (changeCanvasText)
            {
                //if controlNumber not specified, revert to default text
                if (controlNumber == 0)
                {
                    thisObjectCanvasText.text = originalCanvasText; 
                }
                //otherwise, set the new text aa a specified one from a List of strings
                else
                {
                    thisObjectCanvasText.text = canvasTextSet[controlNumber - 1];
                }                
            }

            //play the audioSource
            if (playAudio)
            {
                if (controlNumber == 0)
                {
                    audioPlayer.Stop();
                }
                else
                {
                    if (audioFiles[controlNumber - 1] != null)
                    {
                        audioPlayer.clip = audioFiles[controlNumber - 1];
                        audioPlayer.Play(0);
                    }
                }                
            }

            //set the new toggle state (on-off)
            toggleCount++;
            toggleState = !toggleState;
        }
	}

    // ================================================================================================================
    
    //hide all objects in the provided list
    void hideAllObjectsInList (List<GameObject> ObjectList)
    {
        foreach (GameObject subObject in ObjectList)
        {
            subObject.SetActive(false);
        }
    }
}