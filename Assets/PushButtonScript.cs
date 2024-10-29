using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SharpC64;
using UnityEngine.XR.Interaction.Toolkit;

public class PushButtonScript : MonoBehaviour
{
    public AudioSource audio;
    public AudioClip clip;
    public XRBaseInteractor rightInteractor;
    public XRBaseInteractor leftInteractor;
    public bool special = false;
    public short KeyCode = 0;

    KeyboardScript parentScript;
    GameObject collidedObject;
    XRInputController inputcontroller;

    Frodo frodo;

    Vector3 startPos;
    Vector3 endPos;
    bool leftpressed = false;
    bool rightpressed = false;
    bool hasFocusLeft = false;
    bool hasFocusRight = false;


    void Start()
    {
        inputcontroller = GameObject.Find("XR Interaction Hands Setup").GetComponent<XRInputController>();
        if (leftInteractor != null && rightInteractor != null)
        {
            leftInteractor.hoverEntered.AddListener(HandleInteractorHoverEnterLeft);
            leftInteractor.hoverExited.AddListener(HandleInteractorHoverExitLeft);
            rightInteractor.hoverEntered.AddListener(HandleInteractorHoverEnterRight);
            rightInteractor.hoverExited.AddListener(HandleInteractorHoverExitRight);
        } 
        startPos = gameObject.transform.localPosition;
        endPos = startPos;
        endPos.y = startPos.y - 0.006f;
        parentScript = transform.parent.GetComponent<KeyboardScript>();
        frodo = GameObject.Find("C65Script").GetComponent<C65>().frodo;
        
    }

    void HandleInteractorHoverEnterLeft(HoverEnterEventArgs args)
    {
        if (args.interactableObject.colliders[0].gameObject.GetInstanceID() == gameObject.GetInstanceID())
        {
            hasFocusLeft = true;
        }
    }

    void HandleInteractorHoverExitLeft(HoverExitEventArgs args)
    {
        if (args.interactableObject.colliders[0].gameObject.GetInstanceID() == gameObject.GetInstanceID())
        {
            hasFocusLeft = false;
        }
    }
    void HandleInteractorHoverEnterRight(HoverEnterEventArgs args)
    {
        if (args.interactableObject.colliders[0].gameObject.GetInstanceID() == gameObject.GetInstanceID())
        {
            hasFocusRight = true;
        }
    }

    void HandleInteractorHoverExitRight(HoverExitEventArgs args)
    {
        if (args.interactableObject.colliders[0].gameObject.GetInstanceID() == gameObject.GetInstanceID())
        {
            hasFocusRight = false;
        }
    }

    IEnumerator CloseButton()
    {
        if (hasFocusLeft)
            yield return new WaitUntil(()=>inputcontroller.leftButtonTrigger.Down == false);
        else if (hasFocusRight)
            yield return new WaitUntil(() => inputcontroller.rightButtonTrigger.Down == false);

        if (frodo == null)
            frodo = GameObject.Find("C65Script").GetComponent<C65>().frodo;

        if (special == false)
        {
            if (parentScript.shiftLock) frodo.TheC64.TheDisplay.PollKeyboard(15, false, true, frodo.TheC64.TheCIA1.KeyMatrix, frodo.TheC64.TheCIA1.RevMatrix, ref frodo.TheC64.joykey, true);
            frodo.TheC64.TheDisplay.PollKeyboard(KeyCode, false, true, frodo.TheC64.TheCIA1.KeyMatrix, frodo.TheC64.TheCIA1.RevMatrix, ref frodo.TheC64.joykey, true);
        }
        else
        {
            frodo.TheC64.PollKeyboard(KeyCode, false, true);
        }
        gameObject.transform.localPosition = startPos;
    }
    void ClickButton(Vector3 newpos)
    {
        if (frodo == null)
            frodo = GameObject.Find("C65Script").GetComponent<C65>().frodo;

        gameObject.transform.localPosition = newpos;
        if (KeyCode == 1000)
        {
            frodo.TheC64.Reset();
        }
        else
        {
            if (KeyCode == -2) parentScript.shiftLock = !parentScript.shiftLock;

            if (special == false)
            {
                if (parentScript.shiftLock) frodo.TheC64.TheDisplay.PollKeyboard(15, true, false, frodo.TheC64.TheCIA1.KeyMatrix, frodo.TheC64.TheCIA1.RevMatrix, ref frodo.TheC64.joykey, true);
                frodo.TheC64.TheDisplay.PollKeyboard(KeyCode, true, false, frodo.TheC64.TheCIA1.KeyMatrix, frodo.TheC64.TheCIA1.RevMatrix, ref frodo.TheC64.joykey, true);
            }
            else
            {
                frodo.TheC64.PollKeyboard(KeyCode, true, false);
            }
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
       System.Action func = () =>
       {

           if (frodo == null)
               frodo = GameObject.Find("C65Script").GetComponent<C65>().frodo;
           if (frodo == null) return;

           Vector3 newpos = endPos;
           newpos.y = collision.contacts[0].point.y - 0.006f;
           if (newpos.y < endPos.y || newpos.y > endPos.y)
               newpos.y = endPos.y;

           collidedObject = collision.collider.gameObject;
           audio.PlayOneShot(clip);
           //gameObject.transform.localPosition = newpos;

           ClickButton(newpos);
       };
        if (collision.collider.gameObject.name == "RightIndex" || collision.collider.gameObject.name == "Poke Point Right")
        {
            if (parentScript.rightIndexPressed == false)
            {
                if (collidedObject == null)
                {
                    parentScript.rightIndexPressed = true;
                    rightpressed = true;
                    func();
                }
            }
        }
        else if (collision.collider.gameObject.name == "LeftIndex" || collision.collider.gameObject.name == "Poke Point Left")
        {
            if (parentScript.leftIndexPressed == false)
            {
                if (collidedObject == null)
                {
                    parentScript.leftIndexPressed = true;
                    leftpressed = true;
                    func();
                }
            }
        }

    }
    private void OnCollisionExit(Collision collision)
    {
    }

    IEnumerator Wait(float sec, System.Action callback)
    {
        yield return new WaitForSeconds(sec);
        callback();
    }
    private void Update()
    {
        if ((hasFocusLeft && inputcontroller.leftButtonTrigger.Down) || (hasFocusRight && inputcontroller.rightButtonTrigger.Down))
        {
            ClickButton(endPos);
            StartCoroutine(Wait(0.5f,()=>{
                StartCoroutine(CloseButton());
            }));
        }
        if (collidedObject != null)
        {
            if (collidedObject.transform.position.y - gameObject.transform.position.y  > 0.015f)
            {
                gameObject.transform.localPosition = startPos;
                collidedObject = null;
                if (frodo == null)
                    frodo = GameObject.Find("C65Script").GetComponent<C65>().frodo;
                if (frodo == null)
                {
                    if (rightpressed) parentScript.rightIndexPressed = false;
                    if (leftpressed) parentScript.leftIndexPressed = false;
                    return;
                }

                if (special == false)
                {
                    if (parentScript.shiftLock) frodo.TheC64.TheDisplay.PollKeyboard(15, false, true, frodo.TheC64.TheCIA1.KeyMatrix, frodo.TheC64.TheCIA1.RevMatrix, ref frodo.TheC64.joykey, true);
                    frodo.TheC64.TheDisplay.PollKeyboard(KeyCode, false, true, frodo.TheC64.TheCIA1.KeyMatrix, frodo.TheC64.TheCIA1.RevMatrix, ref frodo.TheC64.joykey, true);
                }
                else
                {
                    frodo.TheC64.PollKeyboard(KeyCode, false, true);
                }

                if (rightpressed) parentScript.rightIndexPressed = false;
                if (leftpressed) parentScript.leftIndexPressed = false;
            }
        }
    }
}
