using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Commodore64;
using UnityEngine.UI;

public class C64 : MonoBehaviour
{
    // Start is called before the first frame update
    C64Emulator c64;
    Texture2D texture;

    int _audioCycles;
    int frameCycles;
    void Start()
    {
        texture = new Texture2D(Video.VIC.X_RESOLUTION , Video.VIC.Y_RESOLUTION);
        c64 = new C64Emulator();
        c64.Start();
        RawImage rawimage = GetComponent<RawImage>();
        rawimage.texture = texture;
        texture.filterMode = FilterMode.Point;
        frameCycles = c64.sid.CYCLES_PER_LINE * c64.sid.NUM_LINES;
    }

    // Update is called once per frame
    void Update()
    {
        var pixels = texture.GetPixels32();
        for (int i = 0; i < c64.videobuffer.dispaybuffer.Length; i++) {
            Color32 c = new Color32();
            c.b = (byte)((c64.videobuffer.dispaybuffer[i]) & 0xFF);
            c.g = (byte)((c64.videobuffer.dispaybuffer[i] >> 8) & 0xFF);
            c.r = (byte)((c64.videobuffer.dispaybuffer[i] >> 16) & 0xFF);
            c.a = 255;/// (byte)((c64.videobuffer.dispaybuffer[i] >> 24) & 0xFF) / 255.0f;
            pixels[i] = c;
            /*
            int x = i % Video.VIC.X_RESOLUTION;
            int y = i / Video.VIC.Y_RESOLUTION;
            Color c;
            c.b =  (byte)((c64.videobuffer.dispaybuffer[i]) & 0xFF) / 255.0f;
            c.g =  (byte)((c64.videobuffer.dispaybuffer[i] >> 8) & 0xFF) / 255.0f;
            c.r =  (byte)((c64.videobuffer.dispaybuffer[i] >> 16) & 0xFF) / 255.0f;
            c.a = 1.0f;/// (byte)((c64.videobuffer.dispaybuffer[i] >> 24) & 0xFF) / 255.0f;
            texture.SetPixel(x , y, c );
            */
        }
        texture.SetPixels32(pixels);
        texture.Apply();

      
    }

    bool _underRun = false;
    float _lastSidSample = 0;
    void OnAudioFilterRead(float[] data, int channels)
    {
        //c64.sid.BufferSamples(32768);
        
        lock (c64.sid.samplesLock)
        {
            //c64.sid.BufferSamples(32768);
            int j = 0;
            for (int i = 0; i < data.Length; ++i)
            {
                if (j >= c64.sid.samples.Count)
                    _underRun = true;

                if (j < c64.sid.samples.Count && (i % channels == 0))
                    _lastSidSample = c64.sid.samples[j++];

                data[i] = _lastSidSample;
            }

            c64.sid.samples.RemoveRange(0, j);
            
        }
    }


    private void OnGUI()
    {
        Event e = Event.current;
        if (e.type == EventType.KeyDown)
        {
            int k = (int)(e.keyCode);// - KeyCode.Keypad0);
            c64.KeyPressed(k);
            //if (k >= 0 && k <= 9)
            //{
            //    Debug.Log("pressed num pad key " + k);
            //    // Do something here
            //}
        }
        if (e.type == EventType.KeyUp)
        {
            int k = (int)(e.keyCode);// - KeyCode.Keypad0);
            c64.KeyReleased(k);
            //if (k >= 0 && k <= 9)
            //{
            //    Debug.Log("pressed num pad key " + k);
            //    // Do something here
            //}
        }
    }
}
