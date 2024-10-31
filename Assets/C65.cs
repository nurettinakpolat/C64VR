using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SharpC64;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine.XR;
using System.IO;
using UnityEngine.XR.ARFoundation;
using System;
using C64DiskUtilities;
using UnityEngine.XR.Interaction.Toolkit;

public class C65 : MonoBehaviour
{
    public AudioSource audio;
    public AudioClip clipClick;
    public Material displayMaterial;
    public ARCameraManager passthrough;
    public GameObject ComputerModel;
    public GameObject leftController;
    public GameObject rightController;

    public XRBaseInteractor rightInteractor;
    public XRBaseInteractor leftInteractor;


    // Start is called before the first frame update
    public Frodo frodo;
    Texture2D texture;

    Thread emuthread = null;
    DoOnMainThread runonmainthread;    

    bool initDone = false;
    int _audioCycles;
    int frameCycles;

    XRInputController inputcontroller;
    T64ToD64Converter converter;

    public GameObject listPrefab;
    public GameObject listParent;
    public static void BeforeSplashScreen()
    {

        Application.runInBackground = true;

        int frameSync = 90;
        SetFrameSync(frameSync);

        //OVRPlugin.foveatedRenderingLevel = OVRPlugin.FoveatedRenderingLevel.Off;
      //  OVRPlugin.suggestedGpuPerfLevel = OVRPlugin.ProcessorPerformanceLevel.Boost;
       // OVRPlugin.suggestedCpuPerfLevel = OVRPlugin.ProcessorPerformanceLevel.Boost;
        Unity.XR.Oculus.Performance.TrySetCPULevel(4);
        Unity.XR.Oculus.Performance.TrySetGPULevel(4);
        Unity.XR.Oculus.Performance.TrySetDisplayRefreshRate(frameSync);

        XRSettings.eyeTextureResolutionScale = 2.0f;
        XRSettings.eyeTextureResolutionScale = 2.0f;

        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }
    public static void SetFrameSync(int frameSync)
    {
        Unity.XR.Oculus.Performance.TrySetDisplayRefreshRate(frameSync);
    }

   
    
    void Start()
    {
        inputcontroller = GameObject.Find("XR Interaction Hands Setup").GetComponent<XRInputController>();
        InitFrodo();
        converter = new T64ToD64Converter();
        rightInteractor = rightController.transform.Find("Ray Interactor").GetComponent<XRRayInteractor>();
        leftInteractor = rightController.transform.Find("Ray Interactor").GetComponent<XRRayInteractor>();
        rightInteractor.hoverEntered.AddListener(HandleInteractorHoverEnterRight);
        rightInteractor.hoverExited.AddListener(HandleInteractorHoverExitRight);

        leftInteractor.gameObject.GetComponent<LineRenderer>().enabled = false;
        leftInteractor.gameObject.GetComponent<XRInteractorLineVisual>().enabled = false;
        rightInteractor.gameObject.GetComponent<LineRenderer>().enabled = false;
        rightInteractor.gameObject.GetComponent<XRInteractorLineVisual>().enabled = false;


    }

    void HandleInteractorHoverEnterRight(HoverEnterEventArgs args)
    {
        if (args.interactableObject.colliders[0].gameObject.GetInstanceID() == listParent.GetInstanceID()||
            args.interactableObject.colliders[0].gameObject.transform.parent.GetInstanceID() == listParent.GetInstanceID())          
        {
            rightInteractor.gameObject.GetComponent<LineRenderer>().enabled = true;
            rightInteractor.gameObject.GetComponent<XRInteractorLineVisual>().enabled = true;
        }
    }

    void HandleInteractorHoverExitRight(HoverExitEventArgs args)
    {
        if (args.interactableObject.colliders[0].gameObject.GetInstanceID() == listParent.GetInstanceID() ||
            args.interactableObject.colliders[0].gameObject.transform.parent.GetInstanceID() == listParent.GetInstanceID())
        {
            rightInteractor.gameObject.GetComponent<LineRenderer>().enabled = false;
            rightInteractor.gameObject.GetComponent<XRInteractorLineVisual>().enabled = false;
        }
    }


    void CopyFileFromResources(string filename, string toFile)
    {
        if (System.IO.Directory.Exists(Application.persistentDataPath + "/myData/") == false)
            System.IO.Directory.CreateDirectory(Application.persistentDataPath + "/myData/");

        TextAsset textFile = Resources.Load<TextAsset>(filename);
        if (textFile != null)
            File.WriteAllBytes(Application.persistentDataPath + "/myData/" + toFile, textFile.bytes);
        else
            Debug.Log("Copy file from resources failed, "+ filename);
    }
    void InitFrodo()
    {
        runonmainthread = GetComponent<DoOnMainThread>();
        texture = new Texture2D(C64Display.DISPLAY_X, C64Display.DISPLAY_Y, TextureFormat.RGBA32, 1, false, true);
        texture.filterMode = FilterMode.Point;
        displayMaterial.mainTexture = texture;
        string path = Application.persistentDataPath + "/myData/";

        //Create Directory if it does not exist
        if (!System.IO.Directory.Exists(Path.GetDirectoryName(path)))
            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path));

        CopyFileFromResources("Roms/kernal.rom", "kernal.rom");
        CopyFileFromResources("Roms/basic.rom", "basic.rom");
        CopyFileFromResources("Roms/d1541.rom", "d1541.rom");
        CopyFileFromResources("Roms/char.rom", "char.rom");
        UnityEngine.Object[] filesD64 = Resources.LoadAll("D64", typeof(TextAsset));

        foreach (var t in filesD64)
        {
            string[] names = t.name.Split(".");
            if (names[names.Length - 1] == "d64" || names[names.Length - 1] == "D64")
            {
                CopyFileFromResources("D64/"+t.name, t.name );
            }
        }

        UnityEngine.Object[] filesT64 = Resources.LoadAll("T64", typeof(TextAsset));

        foreach (var t in filesT64)
        {
            string[] names = t.name.Split(".");
            if (names[names.Length - 1] == "t64" || names[names.Length - 1] == "T64")
            {
                CopyFileFromResources("T64/" + t.name, t.name);
            }
        }



        frodo = new Frodo(path);

        emuthread = new Thread(new ThreadStart(StartEmu));
        emuthread.Start();
        initDone = true;

        UpdateDiskList();
    }

    void UpdateDiskList()
    {
        string[] alllines = new string[1]; alllines[0] = "";
        if (File.Exists(Application.persistentDataPath + "/played.txt"))
            alllines = File.ReadAllLines(Application.persistentDataPath + "/played.txt");

        string[] files = System.IO.Directory.GetFiles(frodo.path);

        for (int i = 0; i < files.Length; i++) {
            Debug.Log(files[i]);
            string[] splitted =  files[i].Split(".");
            string[] extractedfilename = files[i].Split("/");
            if (splitted[splitted.Length - 1] == "d64" || splitted[splitted.Length - 1] == "D64" ||
                splitted[splitted.Length - 1] == "t64" || splitted[splitted.Length - 1] == "T64")
            {
                if (listPrefab != null)
                {
                    GameObject newobj = GameObject.Instantiate(listPrefab, listParent.transform);
                    newobj.SetActive(true);
                    newobj.transform.Find("Text").GetComponent<Text>().text =  extractedfilename[extractedfilename.Length-1];
                    bool found = false;
                    for (int z=0;z< alllines.Length; z++)
                    {
                        if (alllines[z] == extractedfilename[extractedfilename.Length - 1])
                        {
                            newobj.transform.Find("Image").gameObject.SetActive(true);
                            break;
                        }
                    }
                    if (!found) newobj.transform.Find("Image").gameObject.SetActive(false);
                }
            }
        }
    }
    private void OnApplicationQuit()
    {
        frodo.TheC64.Quit();
    }

    void StartEmu()
    {
        frodo.TheC64.TheDisplay.UpdateCallback = () =>
        {
            runonmainthread.ExecuteOnMainThread.Enqueue(() => {
                UpdateScreen();
                HandleInput();
            });
        };
        frodo.ReadyToRun();
    }

    // Update is called once per frame
    unsafe void UpdateScreen()
    {
            if (frodo == null) return;
            byte* buffer = frodo.TheC64.TheDisplay.BitmapBase;
            var pixels = texture.GetRawTextureData<Color32>();

            for (int i = 0; i < pixels.Length; i++)
            {
                byte c64Col = (byte)Mathf.Min(buffer[i], 15);
                Color32 c32 = Color.white;
                c32.b = SharpC64.C64Display.palette_blue[c64Col];
                c32.g = SharpC64.C64Display.palette_green[c64Col];
                c32.r = SharpC64.C64Display.palette_red[c64Col]; ;
                c32.a = 255;
                pixels[i] = c32;
            }

            texture.Apply();
    }

    bool _underRun = false;
    float _lastSidSample = 0;
    void OnAudioFilterRead(float[] data, int channels)        
    {
        if (initDone)
        {
            short[] soundbuffer = frodo.TheC64.TheSID.the_renderer.GetAudioBuffer();

            for (int i = 0; i < soundbuffer.Length; i++)
            {
                data[i] = (soundbuffer[i] / 32768.0f) * 0.3f;
            }
        }        
    }


    private void OnGUI()
    {
        Event e = Event.current;
        if (e.type == EventType.KeyDown)
        {
            int k = (int)(e.keyCode);// - KeyCode.Keypad0);
            if (frodo != null)
                frodo.TheC64.PollKeyboard((int)e.keyCode, true, false);
        }
        if (e.type == EventType.KeyUp)
        {
            if (frodo != null)
                frodo.TheC64.PollKeyboard((int)e.keyCode, false, true);
        }
    }


    Vector3 gripDownDiffVexctor;
    private void HandleInput()
    {
        //if (inputcontroller &&inputcontroller.leftMenuButtonPress.Down)
        //{
        //    passthrough.enabled = !passthrough.enabled;
        //}

        //if (inputcontroller.leftMenuButtonPress.Down)
        //    locomotiveControl.SetActive(!locomotiveControl.activeSelf);


        if (inputcontroller.rightButtonGrip.Down)
        {
            gripDownDiffVexctor = rightController.transform.position - ComputerModel.transform.position;
        }else if (inputcontroller.rightButtonGrip.Hold && gripDownDiffVexctor!=Vector3.zero)
        {
            if ( Vector3.Distance(ComputerModel.transform.position, rightController.transform.position) < 5.0f)
            {
                ComputerModel.transform.position =  rightController.transform.position - gripDownDiffVexctor;
            }
        }
        else
        {
            gripDownDiffVexctor = Vector3.zero;
        }

        frodo.TheC64.joykey = 255;
        //Joy Left button Right A
        byte buttonPressedLeft = 0;
        if (inputcontroller.rightButtonA.Down || inputcontroller.rightButtonA.Hold)
        {
            buttonPressedLeft = 16;
            frodo.TheC64.joykey = 239;
        }
        if ((inputcontroller.leftJoyButtonDown.Down || inputcontroller.leftJoyButtonDown.Hold) && (inputcontroller.leftJoyButtonLeft.Down || inputcontroller.leftJoyButtonLeft.Hold))
            frodo.TheC64.joykey = (byte)(249 - buttonPressedLeft);
        else if ((inputcontroller.leftJoyButtonDown.Down || inputcontroller.leftJoyButtonDown.Hold) && (inputcontroller.leftJoyButtonRight.Down || inputcontroller.leftJoyButtonRight.Hold))
            frodo.TheC64.joykey = (byte)(245 - buttonPressedLeft);
        else if ((inputcontroller.leftJoyButtonUp.Down || inputcontroller.leftJoyButtonUp.Hold) && (inputcontroller.leftJoyButtonRight.Down || inputcontroller.leftJoyButtonRight.Hold))
            frodo.TheC64.joykey = (byte)(246 - buttonPressedLeft);
        else if ((inputcontroller.leftJoyButtonUp.Down || inputcontroller.leftJoyButtonUp.Hold) && (inputcontroller.leftJoyButtonLeft.Down || inputcontroller.leftJoyButtonLeft.Hold))
            frodo.TheC64.joykey = (byte)(250 - buttonPressedLeft);
        else if (inputcontroller.leftJoyButtonLeft.Down || inputcontroller.leftJoyButtonLeft.Hold)
            frodo.TheC64.joykey = (byte)(251 - buttonPressedLeft);
        else if (inputcontroller.leftJoyButtonRight.Down || inputcontroller.leftJoyButtonRight.Hold)
            frodo.TheC64.joykey = (byte)(247 - buttonPressedLeft);
        else if (inputcontroller.leftJoyButtonDown.Down || inputcontroller.leftJoyButtonDown.Hold)
            frodo.TheC64.joykey = (byte)(253 - buttonPressedLeft);
        else if (inputcontroller.leftJoyButtonUp.Down || inputcontroller.leftJoyButtonUp.Hold)
            frodo.TheC64.joykey = (byte)(254 - buttonPressedLeft);
        
      
        


        //Joy Right button Left X
        byte buttonPressedRight = 0;
        if (inputcontroller.leftButtonA.Down || inputcontroller.leftButtonA.Hold)
        {
            buttonPressedRight = 111;
        }
        if (inputcontroller.rightJoyButtonLeft.Down || inputcontroller.rightJoyButtonLeft.Hold)
            frodo.TheC64.joykey = (byte)(123 - buttonPressedRight);
        else if (inputcontroller.rightJoyButtonRight.Down || inputcontroller.rightJoyButtonRight.Hold)
            frodo.TheC64.joykey = (byte)(119 - buttonPressedRight);
        else if (inputcontroller.rightJoyButtonUp.Down || inputcontroller.rightJoyButtonUp.Hold)
            frodo.TheC64.joykey = (byte)(126 - buttonPressedRight);
        else if (inputcontroller.rightJoyButtonDown.Down || inputcontroller.rightJoyButtonDown.Hold)
            frodo.TheC64.joykey = (byte)(125 - buttonPressedRight);
        else if ((inputcontroller.rightJoyButtonDown.Down || inputcontroller.rightJoyButtonDown.Hold) && (inputcontroller.rightJoyButtonLeft.Down || inputcontroller.rightJoyButtonLeft.Hold))
            frodo.TheC64.joykey = (byte)(121 - buttonPressedRight);
        else if ((inputcontroller.rightJoyButtonDown.Down || inputcontroller.rightJoyButtonDown.Hold) && (inputcontroller.rightJoyButtonRight.Down || inputcontroller.rightJoyButtonRight.Hold))
            frodo.TheC64.joykey = (byte)(117 - buttonPressedRight);
        else if ((inputcontroller.rightJoyButtonUp.Down || inputcontroller.rightJoyButtonUp.Hold) && (inputcontroller.rightJoyButtonRight.Down || inputcontroller.rightJoyButtonRight.Hold))
            frodo.TheC64.joykey = (byte)(118 - buttonPressedRight);
        else if ((inputcontroller.rightJoyButtonUp.Down || inputcontroller.rightJoyButtonUp.Hold) && (inputcontroller.rightJoyButtonLeft.Down || inputcontroller.rightJoyButtonLeft.Hold))
            frodo.TheC64.joykey = (byte)(122 - buttonPressedRight);
        
    }

    public void LoadD64(Toggle button)
    {
        string fname = button.transform.Find("Text").GetComponent<Text>().text;
        string[] filenames = fname.Split(".");
        string path = frodo.path;
        if (filenames[filenames.Length-1] == "t64" || filenames[filenames.Length - 1] == "T64")
        {
            if (!System.IO.Directory.Exists(frodo.path + "Temp"))
                System.IO.Directory.CreateDirectory(frodo.path + "Temp");
            
            converter.ConvertFile(frodo.path + fname, frodo.path+"Temp/"+filenames[0]+".d64");
            fname = filenames[0] + ".d64";
            path = frodo.path + "Temp/";
        }


        GlobalPrefs.ThePrefs.DrivePath[0] = path + fname;
        frodo.TheC64.TheJob1541.close_d64_file();
        frodo.TheC64.TheJob1541.open_d64_file(GlobalPrefs.ThePrefs.DrivePath[0]);
        frodo.TheC64.TheJob1541.disk_changed = true;

        if (File.Exists(Application.persistentDataPath + "/played.txt") == false)
            File.Create(Application.persistentDataPath + "/played.txt").Close();
        

       

        string[] alllines = File.ReadAllLines(Application.persistentDataPath + "/played.txt");

        bool found=false;
        for (int i = 0; i < alllines.Length; i++)
        {
            if (alllines[i] == button.transform.Find("Text").GetComponent<Text>().text)
            {
                found = true;
            }
        }
        if (found == false) {
            // Append text to file with a newline at the end
            using (StreamWriter sw = File.AppendText(Application.persistentDataPath + "/played.txt"))
            {
                sw.WriteLine(button.transform.Find("Text").GetComponent<Text>().text + Environment.NewLine); // WriteLine automatically adds a newline
                sw.Close();
                for (int i=0;i< listParent.transform.childCount; i++)
                {
                    if (listParent.transform.GetChild(i).transform.Find("Text").GetComponent<Text>().text == button.transform.Find("Text").GetComponent<Text>().text)
                    {
                        listParent.transform.GetChild(i).transform.Find("Image").gameObject.SetActive(true);
                        audio.PlayOneShot(clipClick);
                        break;
                    }
                }
            }
            
        }
    }


}
