#define NSCANLINE
//#define SCANLINE

using System;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace SharpC64
{
    public class C64Display
    {
        #region Public constants
        public const int DISPLAY_X = 0x180;
        public const int DISPLAY_Y = 0x110;
        #endregion

        public Action UpdateCallback = null;

        
        #region Private Constants
        const byte joystate = 0xff;
        #endregion

        #region Public methods
        public struct DisplayBuffer
        {
            public IntPtr Pixels;
            public short Pitch;
        }
        public C64Display(C64 c64)
        {
            _TheC64 = c64;
        }

        internal void Update()
        {
            /*
            // Draw speedometer/LEDs
            Rectangle r = new Rectangle(0, DISPLAY_Y, DISPLAY_X, 15);
            _c64Screen.DrawFilledBox(r, Color.Gray);

            r.Width = DISPLAY_X; r.Height = 1;
            _c64Screen.DrawFilledBox(r, Color.LightGray);

            r.Y = DISPLAY_Y + 14;
            _c64Screen.DrawFilledBox(r, Color.DarkGray);
            r.Width = 16;

            for (int i = 2; i < 6; i++)
            {
                r.X = DISPLAY_X * i / 5 - 24; r.Y = DISPLAY_Y + 4;
                _c64Screen.DrawFilledBox(r, Color.DarkGray);
                r.Y = DISPLAY_Y + 10;
                _c64Screen.DrawFilledBox(r, Color.LightGray);
            }

            r.Y = DISPLAY_Y; r.Width = 1; r.Height = 15;
            for (int i = 0; i < 5; i++)
            {
                r.X = DISPLAY_X * i / 5;
                _c64Screen.DrawFilledBox(r, Color.LightGray);
                r.X = DISPLAY_X * (i + 1) / 5 - 1;
                _c64Screen.DrawFilledBox(r, Color.DarkGray);
            }

            r.Y = DISPLAY_Y + 4; r.Height = 7;
            for (int i = 2; i < 6; i++)
            {
                r.X = DISPLAY_X * i / 5 - 24;
                _c64Screen.DrawFilledBox(r, Color.DarkGray);
                r.X = DISPLAY_X * i / 5 - 9;
                _c64Screen.DrawFilledBox(r, Color.LightGray);
            }
            r.Y = DISPLAY_Y + 5; r.Width = 14; r.Height = 5;
            for (int i = 0; i < 4; i++)
            {
                r.X = DISPLAY_X * (i + 2) / 5 - 23;
                Color c;
                switch (led_state[i])
                {
                    case DriveLEDState.DRVLED_ON:
                        c = Color.Green;
                        break;
                    case DriveLEDState.DRVLED_ERROR:
                        c = Color.Red;
                        break;
                    default:
                        c = Color.Black;
                        break;
                }
                _c64Screen.DrawFilledBox(r, c);
            }
            */
            /*
            draw_string(DISPLAY_X * 1 / 5 + 8, DISPLAY_Y + 4, "D\x12 8", (byte)Color.Black.ToArgb(), (byte)Color.Gray.ToArgb());
            draw_string(DISPLAY_X * 2 / 5 + 8, DISPLAY_Y + 4, "D\x12 9", (byte)Color.Black.ToArgb(), (byte)Color.Gray.ToArgb());
            draw_string(DISPLAY_X * 3 / 5 + 8, DISPLAY_Y + 4, "D\x12 10", (byte)Color.Black.ToArgb(), (byte)Color.Gray.ToArgb());
            draw_string(DISPLAY_X * 4 / 5 + 8, DISPLAY_Y + 4, "D\x12 11", (byte)Color.Black.ToArgb(), (byte)Color.Gray.ToArgb());
            draw_string(24, DISPLAY_Y + 4, speedometer_string, (byte)Color.Black.ToArgb(), (byte)Color.Gray.ToArgb());
            */
#if SCANLINE
            unsafe
            {
                byte* srcscanline = (byte*)_c64Screen.Pixels;
                byte* destscanline = (byte*)_videoDisplay.Pixels;

                short srcstride = _c64Screen.Pitch;
                short deststride = (short)(_videoDisplay.Pitch * 2);

                for (int y = 0; y < DISPLAY_Y + 17; y++)
                {
                    for (int x = 0; x < DISPLAY_X; x++)
                    {
                        destscanline[x*2] = srcscanline[x];
                        destscanline[x*2+1] = srcscanline[x];
                    }

                    srcscanline += srcstride;
                    destscanline += deststride;
                }
            }
           // _videoDisplay.Update();
#else
            //_c64Screen.Update();
#endif
            if (UpdateCallback != null)
            {
                UpdateCallback();
            }
            //Thread.Sleep(10);
        }

        public void UpdateLEDs(DriveLEDState l0, DriveLEDState l1, DriveLEDState l2, DriveLEDState l3)
        {
            led_state[0] = l0;
            led_state[1] = l1;
            led_state[2] = l2;
            led_state[3] = l3;
        }

        internal void Speedometer(int speed)
        {
            speedometer_string = String.Format("{0}%", speed);
        }

        unsafe internal byte* BitmapBase
        {
            get
            {
#if SCANLINE
                return (byte*)_videoDisplay.Pixels;
#else
             return (byte*)_c64Screen.Pixels;
#endif
            }
        }

        public int BitmapXMod
        {
            get
            {
                return (int)_c64Screen.Pitch;
            }
        }

        bool lshift = false;
        bool rshift = false;
        public void PollKeyboard(int Key, bool KeyDown, bool KeyUp, byte[] key_matrix, byte[] rev_matrix, ref byte joystick, bool direct= false)
        {
            
            if (KeyDown)
            {
               
                switch (Key)
                {
                    case 293: //F12
                        TheC64.Reset();
                        break;
                    case 292: //F11
                        _TheC64.NMI();
                        break;
                    case 291: //F10
                        quit_requested = true;
                        break;
                    case 290: //F9
                        #if DEBUG_INSTRUCTIONS  
                            TheC64.TheCPU.debugLogger.Enabled = !TheC64.TheCPU.debugLogger.Enabled; 
                        #endif
                        break;
                    case 289: //F8
                        break;
                    case 288: //F7
                        break;
                    case 287: //F6
                        break;
                    case 286: //F5
                        break;
                    case 285: //F4
                        break;
                    case 284: //F3
                        break;
                    case 283: //F2
                        break;
                    case 282: //F1
                        break;

                    case 301:   // CapsLock:
                        swapjoysticks = true;
                        break;

                    case 304:   // LeftShift:
                        lshift = true;
                        break;
                    case 303:   // RightShift:
                        rshift = true;
                        break;

                   // case 61:   // +: Increase SkipFrames
                   //     GlobalPrefs.ThePrefs.SkipFrames++;
                   //     break;

                   // case 45: // '-' : Decrease SkipFrames
                   //     if (GlobalPrefs.ThePrefs.SkipFrames > 1)
                    //        GlobalPrefs.ThePrefs.SkipFrames--;
                    //    break;

                    case 56:    // '*' : Toggle speed limiter                         
                        translate_key(56, false, key_matrix, rev_matrix, ref joystick,  direct); //Asterix
                        break;

                    default:
                        translate_key(Key, false, key_matrix, rev_matrix, ref joystick,  direct);
                        break;
                }
            } else if (KeyUp)  {

               
                    switch (Key)
                    {
                        case 304:   // LeftShift:
                            lshift = false;
                            break;
                        case 303:   // RightShift:
                            rshift = false;
                            break;
                    }
                    if (Key == 301)    //Key.CapsLock
                        swapjoysticks = false;
                    else
                        translate_key(Key, true, key_matrix, rev_matrix, ref joystick,  direct);
                

                // Quit Frodo
                //case EventTypes.Quit:
                //    quit_requested = true;
                //    break;
                }
               
        }

        int MATRIX(int a, int b)
        {
            return (((a) << 3) | (b));
        }

        private void translate_key(int key, bool key_up, byte[] key_matrix, byte[] rev_matrix, ref byte joystick, bool direct = false)
        {
            int c64_key = -1;
            bool shifted = (lshift || rshift);// (c64_key & 0x80) != 0;

            if (direct == false)
            {
                switch (key)
                {
                    case 27: c64_key = MATRIX(7, 7); break;    //Key.Escape
                    case 13: c64_key = MATRIX(0, 1); break;    //Key.Return
                    case 8: c64_key = MATRIX(0, 0); break;    //Key.Delete
                    case 127: c64_key = MATRIX(6, 3); break;    //Key.Insert
                    case 278: c64_key = MATRIX(6, 3); break;    //Key.Home
                    case 279: c64_key = MATRIX(6, 0); break;    //Key.End
                    case 280: c64_key = MATRIX(6, 0); break;    //Key.PageUp
                    case 281: c64_key = MATRIX(6, 5); break;    //Key.PageDown

                    case 32: c64_key = MATRIX(7, 4); break;     //Key.Space
                    case 96: c64_key = MATRIX(7, 1); break;     //Key.BackQuote
                    case 92: c64_key = MATRIX(6, 6); break;     //Key.Backslash
                    case 44: c64_key = MATRIX(5, 7); break;     //Key.Comma
                    case 46: c64_key = MATRIX(5, 4); break;     //Key.Period
                    case 45: c64_key = MATRIX(5, 0); break;     //Key.Minus
                    case 81: c64_key = MATRIX(5, 3); break;     //Key.Equals
                    case 91: c64_key = MATRIX(5, 6); break;     //Key.LeftBracket
                    case 93: c64_key = MATRIX(6, 1); break;     //Key.RightBracket
                    case 59: c64_key = MATRIX(5, 5); break;     //Key.Semicolon
                    case 39: c64_key = MATRIX(6, 2); break;     //Key.Quote
                    case 47: c64_key = MATRIX(6, 7); break;     //Key.Slash


                    case 97: c64_key = MATRIX(1, 2); break;     //A 
                    case 98: c64_key = MATRIX(3, 4); break;     //B
                    case 99: c64_key = MATRIX(2, 4); break;     //C
                    case 100: c64_key = MATRIX(2, 2); break;    //Key.D
                    case 101: c64_key = MATRIX(1, 6); break;    //Key.E
                    case 102: c64_key = MATRIX(2, 5); break;    //Key.F
                    case 103: c64_key = MATRIX(3, 2); break;    //Key.Gx
                    case 104: c64_key = MATRIX(3, 5); break;    //Key.H
                    case 105: c64_key = MATRIX(4, 1); break;    //Key.I
                    case 106: c64_key = MATRIX(4, 2); break;    //Key.J
                    case 107: c64_key = MATRIX(4, 5); break;    //Key.K
                    case 108: c64_key = MATRIX(5, 2); break;    //Key.L
                    case 109: c64_key = MATRIX(4, 4); break;    //Key.M
                    case 110: c64_key = MATRIX(4, 7); break;    //Key.N
                    case 111: c64_key = MATRIX(4, 6); break;    //Key.O
                    case 112: c64_key = MATRIX(5, 1); break;    //Key.P
                    case 113: c64_key = MATRIX(7, 6); break;    //Key.Q
                    case 114: c64_key = MATRIX(2, 1); break;    //Key.R
                    case 115: c64_key = MATRIX(1, 5); break;    //Key.S
                    case 116: c64_key = MATRIX(2, 6); break;    //Key.T
                    case 117: c64_key = MATRIX(3, 6); break;    //Key.U
                    case 118: c64_key = MATRIX(3, 7); break;    //Key.V
                    case 119: c64_key = MATRIX(1, 1); break;    //Key.W
                    case 120: c64_key = MATRIX(2, 7); break;    //Key.X
                    case 121: c64_key = MATRIX(3, 1); break;    //Key.Y
                    case 122: c64_key = MATRIX(1, 4); break;    //Key.Z

                    case 48: c64_key = MATRIX(4, 3); break;     //Key.Zero
                    case 49: c64_key = MATRIX(7, 0); break;     //Key.One
                    case 50: c64_key = MATRIX(7, 3); break;     //Key.Two
                    case 51: c64_key = MATRIX(1, 0); break;     //Key.Three
                    case 52: c64_key = MATRIX(1, 3); break;     //Key.Four
                    case 53: c64_key = MATRIX(2, 0); break;     //Key.Five
                    case 54: c64_key = MATRIX(2, 3); break;     //Key.Six
                    case 55: c64_key = MATRIX(3, 0); break;     //Key.Seven
                    case 56: c64_key = MATRIX(3, 3); break;     //Key.Eight
                    case 57: c64_key = MATRIX(4, 0); break;     //Key.Nine

                    case 282: c64_key = MATRIX(0, 4); break;                            //Key.F1
                    case 283: c64_key = MATRIX(0, 4) | 0x80; shifted = true; break;     //Key.F2
                    case 284: c64_key = MATRIX(0, 5); break;                            //Key.F3
                    case 285: c64_key = MATRIX(0, 5) | 0x80; shifted = true; break;     //Key.F4
                    case 286: c64_key = MATRIX(0, 6); break;                            //Key.F5
                    case 287: c64_key = MATRIX(0, 6) | 0x80; shifted = true; break;     //Key.F6
                    case 288: c64_key = MATRIX(0, 3); break;                            //Key.F7
                    case 289: c64_key = MATRIX(0, 3) | 0x80; shifted = true; break;     //Key.F8

                    case 273: c64_key = MATRIX(0, 7) | 0x80; shifted = true; break;     //Key.UpArrow
                    case 274: c64_key = MATRIX(0, 7); break;                            //Key.DownArrow
                    case 276: c64_key = MATRIX(0, 2) | 0x80; shifted = true; break;     //Key.LeftArrow
                    case 275: c64_key = MATRIX(0, 2); break;            //Key.RightArrow

                    case 9: c64_key = MATRIX(7, 2); break;              //Key.Tab
                    case 305: c64_key = MATRIX(7, 5); break;            //Key.RightControl
                    case 304: c64_key = MATRIX(1, 7); break;            //Key.LeftShift
                    case 303: c64_key = MATRIX(6, 4); break;            //Key.RightShift



                        /*
                        case 480: c64_key = 41; break;                      //Key.Asterix
                        case 490: c64_key = 33; break;                      //Key.Asterix
                        case 500: c64_key = 64; break;                      //Key.Asterix
                        case 510: c64_key = 35; break;                      //Key.Asterix
                        case 520: c64_key = 36; break;                      //Key.Asterix
                        case 530: c64_key = 37; break;                      //Key.Asterix
                        case 540: c64_key = 38; break;                      //Key.Asterix
                        case 550: c64_key = 39; break;                      //Key.Asterix
                        case 560: c64_key = 42; break;                      //Key.Asterix
                        case 570: c64_key = 40; break;                      //Key.Asterix
                        */

                        /*




                        case Key.LeftControl: 


                        case Key.LeftAlt:
                        case Key.LeftMeta: c64_key = MATRIX(7, 5); break;
                        case Key.RightAlt:
                        case Key.RightMeta: c64_key = MATRIX(7, 5); break;



                        case Key.Keypad0:
                        case Key.Keypad5: c64_key = 0x10 | 0x40; break;
                        case Key.Keypad1: c64_key = 0x06 | 0x40; break;
                        case Key.Keypad2: c64_key = 0x02 | 0x40; break;
                        case Key.Keypad3: c64_key = 0x0a | 0x40; break;
                        case Key.Keypad4: c64_key = 0x04 | 0x40; break;
                        case Key.Keypad6: c64_key = 0x08 | 0x40; break;
                        case Key.Keypad7: c64_key = 0x05 | 0x40; break;
                        case Key.Keypad8: c64_key = 0x01 | 0x40; break;
                        case Key.Keypad9: c64_key = 0x09 | 0x40; break;

                        case Key.KeypadDivide: c64_key = MATRIX(6, 7); break;
                        case Key.KeypadEnter: c64_key = MATRIX(0, 1); break;
                        */
                }
            }
            else
            {
                c64_key = key;
            }

            if (c64_key < 0)
                return;

            // Handle joystick emulation
            if ((c64_key & 0x40) != 0)
            {
                c64_key &= 0x1f;
                if (key_up)
                    joystick |= (byte)c64_key;
                else
                    joystick &= (byte)~c64_key;
                return;
            }
            
            // Handle other keys
            
            int c64_byte = (c64_key >> 3) & 7;
            int c64_bit = c64_key & 7;
            if (key_up)
            {
                
                if (shifted)
                {
                    key_matrix[6] |= 0x10;
                    rev_matrix[4] |= 0x40;
                }
                
                key_matrix[c64_byte] |= (byte)(1 << c64_bit);
                rev_matrix[c64_bit] |= (byte)(1 << c64_byte);
            }
            else
            {
                
                if (shifted)
                {
                    key_matrix[6] &= 0xef;
                    rev_matrix[4] &= 0xbf;
                }
                
                key_matrix[c64_byte] &= (byte)~(1 << c64_bit);
                rev_matrix[c64_bit] &= (byte)~(1 << c64_byte);
            }
            
        }

        public void InitColors(byte[] colors)
        {
            /*
            Sdl.SDL_Color[] palette = new Sdl.SDL_Color[21];
            for (int i = 0; i < 16; i++)
            {
                palette[i].r = palette_red[i];
                palette[i].g = palette_green[i];
                palette[i].b = palette_blue[i];
            }

            IntPtr current = Sdl.SDL_GetVideoSurface();
            Sdl.SDL_SetColors(current, palette, 0, 21);
            */
            for (int i = 0; i < 256; i++)
                colors[i] = (byte)(i & 0x0f);
        }

        internal void NewPrefs(Prefs prefs)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        internal void WaitUntilActive()
        {

        }

        internal void Initialize()
        {
            init_graphics();

        }

        internal void ShowRequester(string a, string button1, string button2)
        {
            ShowRequester(a, button1);
        }

        internal void ShowRequester(string a, string button1)
        {
            Console.WriteLine("{0}: {1}", a, button1);
        }

#endregion Public methods

        #region Public properties

        internal bool SwapJoysticks
        {
            [DebuggerStepThrough]
            get { return swapjoysticks; }
            [DebuggerStepThrough]
            set { swapjoysticks = value; }
        }

        public C64 TheC64
        {
            [DebuggerStepThrough]
            get { return _TheC64; }
            [DebuggerStepThrough]
            set { _TheC64 = value; }
        }

        public bool QuitRequested
        {
            [DebuggerStepThrough]
            get { return quit_requested; }
            [DebuggerStepThrough]
            set { quit_requested = value; }
        }

        public DisplayBuffer C64Screen
        {
            get { return _c64Screen; }
        }

        #endregion

        #region Private methods

        //Surface _c64Screen;
        DisplayBuffer _c64Screen;
        DisplayBuffer _c64Screen2;
#if SCANLINE
        //Surface _videoDisplay;
        DisplayBuffer _videoDisplay; 
#endif

        /*
        int init_graphics()
        {
            // Init SDL
            Video.Initialize();

#if SCANLINE
            _c64Screen = Video.CreateRgbSurface(DISPLAY_X, DISPLAY_Y + 17, 8, 0xff, 0xff, 0xff, 0x00, false);
            _videoDisplay = Video.SetVideoMode(DISPLAY_X * 2, (DISPLAY_Y + 17) * 2, 8);
#else
            _c64Screen = Video.SetVideoMode(DISPLAY_X, DISPLAY_Y + 17, 8);
#endif

            // Open window
            Video.WindowCaption = "Sharp-C64";

            return 1;
        }
        */

        int init_graphics()
        {

#if SCANLINE
            _c64Screen = new DisplayBuffer();
            _c64Screen.Pixels = Marshal.AllocHGlobal(DISPLAY_X * (DISPLAY_Y ));
            _c64Screen.Pitch = DISPLAY_X;
            _videoDisplay = new DisplayBuffer();
            _videoDisplay.Pixels = Marshal.AllocHGlobal(DISPLAY_X * 2 * (DISPLAY_Y )*2);
            _videoDisplay.Pitch = DISPLAY_X;
#else
            _c64Screen = new DisplayBuffer();
            _c64Screen.Pixels = Marshal.AllocHGlobal(DISPLAY_X * (DISPLAY_Y ));
            _c64Screen.Pitch = DISPLAY_X;

            _c64Screen2 = new DisplayBuffer();
            _c64Screen2.Pixels = Marshal.AllocHGlobal(DISPLAY_X * (DISPLAY_Y ));
            _c64Screen2.Pitch = DISPLAY_X;

#endif

            // Open window
            //Video.WindowCaption = "Sharp-C64";

            return 1;
        }
        
        /*
        unsafe private void draw_string(int x, int y, string str, byte front_color, byte back_color)
        {
            byte* pb = (byte*)_c64Screen.Pixels + _c64Screen.Pitch * y + x;
            char c;
            fixed (byte* qq = TheC64.Char)
            {
                for (int i = 0; i < str.Length; i++)
                {
                    c = str[i];
                    byte* q = qq + c * 8 + 0x800;
                    byte* p = pb;
                    for (int j = 0; j < 8; j++)
                    {
                        byte v = *q++;
                        p[0] = (v & 0x80) != 0 ? front_color : back_color;
                        p[1] = (v & 0x40) != 0 ? front_color : back_color;
                        p[2] = (v & 0x20) != 0 ? front_color : back_color;
                        p[3] = (v & 0x10) != 0 ? front_color : back_color;
                        p[4] = (v & 0x08) != 0 ? front_color : back_color;
                        p[5] = (v & 0x04) != 0 ? front_color : back_color;
                        p[6] = (v & 0x02) != 0 ? front_color : back_color;
                        p[7] = (v & 0x01) != 0 ? front_color : back_color;
                        p += _c64Screen.Pitch;
                    }
                    pb += 8;
                }
            }
        }
        */
        unsafe private void draw_string(int x, int y, string str, byte front_color, byte back_color)
        {
            byte* pb = (byte*)_c64Screen.Pixels + _c64Screen.Pitch * y + x;
            char c;
            fixed (byte* qq = TheC64.Char)
            {
                for (int i = 0; i < str.Length; i++)
                {
                    c = str[i];
                    byte* q = qq + c * 8 + 0x800;
                    byte* p = pb;
                    for (int j = 0; j < 8; j++)
                    {
                        byte v = *q++;
                        p[0] = (v & 0x80) != 0 ? front_color : back_color;
                        p[1] = (v & 0x40) != 0 ? front_color : back_color;
                        p[2] = (v & 0x20) != 0 ? front_color : back_color;
                        p[3] = (v & 0x10) != 0 ? front_color : back_color;
                        p[4] = (v & 0x08) != 0 ? front_color : back_color;
                        p[5] = (v & 0x04) != 0 ? front_color : back_color;
                        p[6] = (v & 0x02) != 0 ? front_color : back_color;
                        p[7] = (v & 0x01) != 0 ? front_color : back_color;
                        p += _c64Screen.Pitch;
                    }
                    pb += 8;
                }
            }
        }

        #endregion

        #region Private fields

        C64 _TheC64;

        DriveLEDState[] led_state = new DriveLEDState[4];
        DriveLEDState[] old_led_state = new DriveLEDState[4];

        bool swapjoysticks;

        string speedometer_string = String.Empty;

        public bool quit_requested = false;

        #endregion Private fields

#if USE_THEORETICAL_COLORS

        // C64 color palette (theoretical values)
        static readonly byte[] palette_red = {
	        0x00, 0xff, 0xff, 0x00, 0xff, 0x00, 0x00, 0xff, 0xff, 0x80, 0xff, 0x40, 0x80, 0x80, 0x80, 0xc0
        };

        static readonly byte[] palette_green = {
	        0x00, 0xff, 0x00, 0xff, 0x00, 0xff, 0x00, 0xff, 0x80, 0x40, 0x80, 0x40, 0x80, 0xff, 0x80, 0xc0
        };

        static readonly byte[] palette_blue = {
	        0x00, 0xff, 0x00, 0xff, 0xff, 0x00, 0xff, 0x00, 0x00, 0x00, 0x80, 0x40, 0x80, 0x80, 0xff, 0xc0
        };

#else

        // C64 color palette (more realistic looking colors)
        public static readonly byte[] palette_red = {
	        0x00, 0xff, 0x99, 0x00, 0xcc, 0x44, 0x11, 0xff, 0xaa, 0x66, 0xff, 0x40, 0x80, 0x66, 0x77, 0xc0
        };

        public static readonly byte[] palette_green = {
	        0x00, 0xff, 0x00, 0xff, 0x00, 0xcc, 0x00, 0xff, 0x55, 0x33, 0x66, 0x40, 0x80, 0xff, 0x77, 0xc0
        };

        public static readonly byte[] palette_blue = {
	        0x00, 0xff, 0x00, 0xcc, 0xcc, 0x44, 0x99, 0x00, 0x00, 0x00, 0x66, 0x40, 0x80, 0x66, 0xff, 0xc0
        };

#endif

    }

    
}
