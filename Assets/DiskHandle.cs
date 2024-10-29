using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR;

public class DiskHandle : MonoBehaviour
{
    bool open = false;
    bool moving = false;
    public GameObject menu;

    public XRBaseInteractor rightInteractor;
    bool hasFocus = false;
    XRInputController inputcontroller;
    private void Start()
    {
        inputcontroller = GameObject.Find("XR Interaction Hands Setup").GetComponent<XRInputController>();
        if (rightInteractor != null)
        {
            rightInteractor.hoverEntered.AddListener(HandleInteractorHoverEnter);
            rightInteractor.hoverExited.AddListener(HandleInteractorHoverExit);
        }        
    }
    void HandleInteractorHoverEnter(HoverEnterEventArgs args)
    {
        if (args.interactableObject.colliders[0].gameObject.GetInstanceID() == gameObject.GetInstanceID())
        {
            hasFocus = true;
        }
    }

    void HandleInteractorHoverExit(HoverExitEventArgs args)
    {
        if (args.interactableObject.colliders[0].gameObject.GetInstanceID() == gameObject.GetInstanceID())
        {
            hasFocus = false;
        }
    }
    private void Update()
    {
        if (hasFocus)
        {
            if (inputcontroller.rightButtonTrigger.Down)
            {
                if (open)
                {
                    StartCoroutine(Close());
                }
                else
                {
                    StartCoroutine(Open());
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (moving == false)
        {

            if (open)
            {                
                StartCoroutine(Close());
            }
            else
            {
                StartCoroutine(Open());
            }
        }
    }
    IEnumerator Open()
    {
        moving = true;
        float angle = 0;
        while (angle < 1.55f)
        {
            angle += 0.155f;
            transform.parent.localRotation = Quaternion.EulerAngles(0,0, angle);
           yield return new WaitForEndOfFrame();
        }
        open = true;
        moving = false;
        angle = 1.55f;
        transform.parent.localRotation = Quaternion.EulerAngles(0, 0, angle);
        menu.SetActive(true);        
    }

    IEnumerator Close()
    {
        moving = true;
        float angle = 1.55f;
        while (angle > 0)
        {
            angle -= 0.155f;
            transform.parent.localRotation = Quaternion.EulerAngles(0, 0, angle);
            yield return new WaitForEndOfFrame();
        }
        open = false;
        moving = false;
        angle = 0;
        transform.parent.localRotation = Quaternion.EulerAngles(0, 0, angle);
        menu.SetActive(false);
    }
    public void CloseDisk()
    {
        StartCoroutine(Close());
    }
}
