using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

public class XRInputController : MonoBehaviour
{
    public struct ButtonState
    {
        public bool Down;
        public bool Up;
        public bool Hold;
        public bool lastState;
    }

    private InputDevice deviceLeft;
    private InputDevice deviceRight;
    private InputDevice headSet;

    [HideInInspector] public ButtonState leftButtonA;
    [HideInInspector] public ButtonState rightButtonA;

    [HideInInspector] public ButtonState leftButtonB;
    [HideInInspector] public ButtonState rightButtonB;

    [HideInInspector] public ButtonState leftButtonTrigger;
    [HideInInspector] public ButtonState rightButtonTrigger;

    [HideInInspector] public ButtonState leftButtonTriggerSense;
    [HideInInspector] public ButtonState rightButtonTriggerSense;

    [HideInInspector] public float leftTriggerValue;
    [HideInInspector] public float rightTriggerValue;

    [HideInInspector] public float leftGripValue;
    [HideInInspector] public float rightGripValue;


    [HideInInspector] public Vector2 leftPrimaryAxisValue;
    [HideInInspector] public Vector2 rightPrimaryAxisValue;


    [HideInInspector] public ButtonState leftButtonGrip;
    [HideInInspector] public ButtonState rightButtonGrip;

    [HideInInspector] public Vector2 leftJoystick;
    [HideInInspector] public Vector2 rightJoystick;

    [HideInInspector] public ButtonState leftJoyButtonUp;
    [HideInInspector] public ButtonState leftJoyButtonDown;
    [HideInInspector] public ButtonState leftJoyButtonLeft;
    [HideInInspector] public ButtonState leftJoyButtonRight;

    [HideInInspector] public ButtonState leftJoyButtonPress;

    [HideInInspector] public ButtonState rightJoyButtonUp;
    [HideInInspector] public ButtonState rightJoyButtonDown;
    [HideInInspector] public ButtonState rightJoyButtonLeft;
    [HideInInspector] public ButtonState rightJoyButtonRight;

    [HideInInspector] public ButtonState rightJoyButtonPress;

    [HideInInspector] public ButtonState leftMenuButtonPress;

    [HideInInspector] public ButtonState headSetMounted;

    [HideInInspector] public Vector3 leftHandPosition;
    [HideInInspector] public Vector3 rightHandPosition;
    [HideInInspector] public Quaternion leftHandRotation;
    [HideInInspector] public Quaternion rightHandRotation;

    [HideInInspector] public Vector3 headSetPosition;
    [HideInInspector] public Quaternion headSetRotation;


    public UnityEvent onKeyDown;
    public static readonly InputFeatureUsage<Vector3> PointerPosition = new InputFeatureUsage<Vector3>("PointerPosition");
    public static readonly InputFeatureUsage<Quaternion> PointerRotation = new InputFeatureUsage<Quaternion>("PointerRotation");


    [HideInInspector] GameObject xrrig;
    private void Awake()
    {
        xrrig = GameObject.Find("XR Origin (XR Rig)");
        leftButtonTrigger.Hold = false;
        rightButtonTrigger.Hold = false;
    }

    void OnEnable()
    {
        InputDeviceCharacteristics rightTrackedControllerFilter = InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Right, rightHandedControllers;

        List<InputDevice> allCharDevices = new List<InputDevice>();
        List<InputDevice> allDevices = new List<InputDevice>();
        InputDevices.GetDevices(allDevices);
        InputDevices.GetDevicesWithCharacteristics(rightTrackedControllerFilter, allCharDevices);
        foreach (InputDevice device in allCharDevices)
        {
            InputDevices_deviceConnected(device);
            device.subsystem.TryRecenter();
        }

        foreach (InputDevice device in allDevices)
            InputDevices_deviceConnected(device);
        
        InputDevices.deviceConnected += InputDevices_deviceConnected;
        InputDevices.deviceDisconnected += InputDevices_deviceDisconnected;
    }

    private void OnDisable()
    {
        InputDevices.deviceConnected -= InputDevices_deviceConnected;
        InputDevices.deviceDisconnected -= InputDevices_deviceDisconnected;
    }

    private void InputDevices_deviceConnected(InputDevice device)
    {
        bool discardedValue;
        if (device.TryGetFeatureValue(CommonUsages.primaryButton, out discardedValue))
        {
            if (device.characteristics.HasFlag(InputDeviceCharacteristics.Controller) && device.characteristics.HasFlag(InputDeviceCharacteristics.HeldInHand) && device.characteristics.HasFlag(InputDeviceCharacteristics.Right))
            {
                deviceRight = device;
                device.subsystem.TryRecenter();
            }

            if (device.characteristics.HasFlag(InputDeviceCharacteristics.Controller) && device.characteristics.HasFlag(InputDeviceCharacteristics.HeldInHand) && device.characteristics.HasFlag(InputDeviceCharacteristics.Left))
                deviceLeft = device;

            if (device.characteristics.HasFlag(InputDeviceCharacteristics.HeadMounted))
                headSet = device;
        }

    }

    private void InputDevices_deviceDisconnected(InputDevice device)
    {
    }

    void Update()
    {
        ClearStates();
        GetButtonStates();
    }


    private void GetButtonStates()
    {
        bool dummy = false;

        if (headSet.TryGetFeatureValue(CommonUsages.devicePosition, out var tmpHeadSetPosition))
        {
            headSetPosition = tmpHeadSetPosition;
        }
        if (headSet.TryGetFeatureValue(CommonUsages.deviceRotation, out var tmpHeadSetRotation))
        {
            headSetRotation = tmpHeadSetRotation;
        }

        if (deviceLeft.TryGetFeatureValue(CommonUsages.deviceRotation, out var tmpLeftRotation))
        {
            leftHandRotation = xrrig.transform.rotation * tmpLeftRotation;
        }
        if (deviceRight.TryGetFeatureValue(CommonUsages.deviceRotation, out var tmpRightRotation))
        {
            rightHandRotation = xrrig.transform.rotation * tmpRightRotation;
        }

        if (deviceLeft.TryGetFeatureValue(CommonUsages.devicePosition, out var tmpLeftPosition))
        {
            tmpLeftPosition = xrrig.transform.rotation * tmpLeftPosition;
            leftHandPosition = xrrig.transform.position + tmpLeftPosition + Vector3.up * xrrig.GetComponent<XROrigin>().CameraYOffset;
        }
        if (deviceRight.TryGetFeatureValue(CommonUsages.devicePosition, out var tmpRightPosition))
        {
            tmpRightPosition = xrrig.transform.rotation * tmpRightPosition;
            rightHandPosition = xrrig.transform.position + tmpRightPosition + Vector3.up * xrrig.GetComponent<XROrigin>().CameraYOffset;

        }


        UpdateState(ref leftButtonA, deviceLeft.TryGetFeatureValue(CommonUsages.primaryButton, out dummy), ref dummy);
        UpdateState(ref leftButtonB, deviceLeft.TryGetFeatureValue(CommonUsages.secondaryButton, out dummy), ref dummy);
        UpdateState(ref leftButtonTrigger, deviceLeft.TryGetFeatureValue(CommonUsages.triggerButton, out dummy), ref dummy);
        UpdateState(ref leftButtonGrip, deviceLeft.TryGetFeatureValue(CommonUsages.gripButton, out dummy), ref dummy);
        UpdateState(ref leftJoyButtonPress, deviceLeft.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out dummy), ref dummy);


        UpdateState(ref rightButtonA, deviceRight.TryGetFeatureValue(CommonUsages.primaryButton, out dummy), ref dummy);
        UpdateState(ref rightButtonB, deviceRight.TryGetFeatureValue(CommonUsages.secondaryButton, out dummy), ref dummy);
        UpdateState(ref rightButtonTrigger, deviceRight.TryGetFeatureValue(CommonUsages.triggerButton, out dummy), ref dummy);
        UpdateState(ref rightButtonGrip, deviceRight.TryGetFeatureValue(CommonUsages.gripButton, out dummy), ref dummy);
        UpdateState(ref rightJoyButtonPress, deviceRight.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out dummy), ref dummy);

        UpdateState(ref leftMenuButtonPress, deviceLeft.TryGetFeatureValue(CommonUsages.menuButton, out dummy), ref dummy);


        deviceLeft.TryGetFeatureValue(CommonUsages.trigger, out leftTriggerValue);
        deviceRight.TryGetFeatureValue(CommonUsages.trigger, out rightTriggerValue);

        deviceLeft.TryGetFeatureValue(CommonUsages.primary2DAxis, out leftPrimaryAxisValue);
        deviceRight.TryGetFeatureValue(CommonUsages.primary2DAxis, out rightPrimaryAxisValue);

        deviceLeft.TryGetFeatureValue(CommonUsages.grip, out leftGripValue);
        deviceRight.TryGetFeatureValue(CommonUsages.grip, out rightGripValue);

        bool ret = headSet.TryGetFeatureValue(CommonUsages.userPresence, out dummy);
        dummy = !dummy;
        UpdateState(ref headSetMounted, ret, ref dummy);

        dummy = leftTriggerValue > 0.1f;
        UpdateState(ref leftButtonTriggerSense, dummy, ref dummy);
        dummy = rightTriggerValue > 0.1f;
        UpdateState(ref rightButtonTriggerSense, dummy, ref dummy);


        deviceLeft.TryGetFeatureValue(CommonUsages.primary2DAxis, out leftJoystick);
        deviceRight.TryGetFeatureValue(CommonUsages.primary2DAxis, out rightJoystick);

        dummy = leftJoystick.x > 0.7f;
        UpdateState(ref leftJoyButtonRight, dummy, ref dummy);
        dummy = leftJoystick.x < -0.7f;
        UpdateState(ref leftJoyButtonLeft, dummy, ref dummy);
        dummy = leftJoystick.y > 0.7f;
        UpdateState(ref leftJoyButtonUp, dummy, ref dummy);
        dummy = leftJoystick.y < -0.7f;
        UpdateState(ref leftJoyButtonDown, dummy, ref dummy);

        dummy = rightJoystick.x > 0.7f;
        UpdateState(ref rightJoyButtonRight, dummy, ref dummy);
        dummy = rightJoystick.x < -0.7f;
        UpdateState(ref rightJoyButtonLeft, dummy, ref dummy);
        dummy = rightJoystick.y > 0.7f;
        UpdateState(ref rightJoyButtonUp, dummy, ref dummy);
        dummy = rightJoystick.y < -0.7f;
        UpdateState(ref rightJoyButtonDown, dummy, ref dummy);


    }
    private void UpdateState(ref ButtonState button, bool ret, ref bool state)
    {
        if (button.lastState != state)
        {
            if (button.lastState == false)
            {
                button.Down = true;
                button.Hold = false;
                button.Up = false;
            }
            else
            {
                button.Up = true;
                button.Down = false;
                button.Hold = false;
            }

        }
        else if (state == true)
        {
            button.Hold = true;
            button.Up = false;
            button.Down = false;
        }
        button.lastState = state;
    }
    private void ClearStates()
    {
        ClearState(ref leftButtonA);
        ClearState(ref leftButtonB);
        ClearState(ref leftButtonTrigger);
        ClearState(ref leftButtonTriggerSense);
        ClearState(ref leftButtonGrip);

        ClearState(ref rightButtonA);
        ClearState(ref rightButtonB);
        ClearState(ref rightButtonTrigger);
        ClearState(ref rightButtonTriggerSense);
        ClearState(ref rightButtonGrip);

        ClearState(ref rightJoyButtonRight);
        ClearState(ref rightJoyButtonLeft);
        ClearState(ref rightJoyButtonUp);
        ClearState(ref rightJoyButtonDown);

        ClearState(ref leftJoyButtonRight);
        ClearState(ref leftJoyButtonLeft);
        ClearState(ref leftJoyButtonUp);
        ClearState(ref leftJoyButtonDown);

        ClearState(ref headSetMounted);

    }
    private void ClearState(ref ButtonState button)
    {
        button.Up = false;
        button.Down = false;
        button.Hold = false;
    }

}

