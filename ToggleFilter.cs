using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleFilter : MonoBehaviour
{
    //iterate from zero to target value; then toggle
    public int defaultValue = 0;
    public int targetValue = 3;
    private int currentValue;
    //control vars
    public bool resetValueOnToggle;
    public bool toggleOnlyOnce;
    private bool canToggle = true;
    //target toggle
    public GameObject targetToggleObject;
    public int targetControlNumber = 0;

    // Start is called before the first frame update
    void Start()
    {
        currentValue = defaultValue;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Toggle(int controlNumber)
    {
        if (canToggle)
        {
            currentValue++;
            if (currentValue == targetValue)
            {
                if (targetToggleObject.GetComponent<ToggleScript>() != null)
                {
                    //toggle the target
                    targetToggleObject.GetComponent<ToggleScript>().Toggle(targetControlNumber);
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
                    Debug.LogWarning("Object " + targetToggleObject.name +
                                     " has no toggleScript! Iterator " + this.name + " not toggling!");
                }
            }
        }
        else
        {
            Debug.Log("Iterator " + this.name + " already disabled. Not toggling.");
        }
    }
}
