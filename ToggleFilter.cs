using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleFilter : MonoBehaviour
{
    //iterate from zero to target value; then toggle
    public int defaultValue = 0;
    public int toggleValue = 3;
    public bool toggleAfterDelay;
    public float toggleDelay = 0;
    private int currentValue;
    private float toggleTime;
    //control vars
    public bool resetValueOnToggle;
    public bool toggleOnlyOnce;
    private bool canToggle = true;
    private bool canToggleInTime = false;
    //target toggle
    public List<GameObject> TargetToggleObjects;
    public int targetControlNumber = 0;

    // Start is called before the first frame update
    void Start()
    {
        currentValue = defaultValue;
    }

    // Update is called once per frame
    void Update()
    {
        if (canToggleInTime)
        {
            if (Time.time >= (toggleTime + toggleDelay))
            {                
                this.Toggle(0);
                canToggleInTime = false;
            }
        }
    }

    public void Toggle(int controlNumber)
    {
        if (toggleAfterDelay && !canToggleInTime)
        {
            toggleTime = Time.time;
            canToggleInTime = true;
        }
        else if (canToggle)
        {
            currentValue++;
            if (currentValue == toggleValue)
            {
                foreach (GameObject obj in TargetToggleObjects)
                {
                    if (obj.GetComponent<ToggleScript>() != null)
                    {
                        //toggle the target
                        obj.GetComponent<ToggleScript>().Toggle(targetControlNumber);
                        //reset the iterator counter, if wanted
                        if (resetValueOnToggle)
                        {
                            currentValue = defaultValue;
                        }
                        //disable the iterator, if wanted
                        if (toggleOnlyOnce)
                        {
                            canToggle = false;
                        }

                    }
                    else
                    {
                        Debug.LogWarning("Object " + obj.name +
                                         " has no toggleScript! Iterator " + this.name + " not toggling!");
                    }
                }
            }
        }
        else
        {
            Debug.Log("Iterator " + this.name + " already disabled. Not toggling.");
        }
    }
}
