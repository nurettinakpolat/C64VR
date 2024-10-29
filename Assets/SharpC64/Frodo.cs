using System;
using System.IO;

namespace SharpC64
{
    public class Frodo
    {
        #region Private Constants
        string BASIC_ROM_FILE	= "basic.rom";
        string KERNAL_ROM_FILE	= "kernal.rom";
        string CHAR_ROM_FILE	= "char.rom";
        string FLOPPY_ROM_FILE  = "d1541.rom";
        #endregion

        #region Public methods
        public string path = "";
        public Frodo(string _path)
        {
            path = _path;
            GlobalPrefs.ThePrefs.DrivePath[0] = path+ GlobalPrefs.ThePrefs.DrivePath[0];

            _TheC64 = new C64();
            _TheC64.Initialize();
        }

        public void ReadyToRun()
        {
            load_rom_files();

            _TheC64.Run();
        }

        #endregion

        #region Public Properties

        public C64 TheC64
        {
            get { return _TheC64; }
            set { _TheC64 = value; }
        }

        #endregion

        private bool load_rom_files()
        {
            Stream file;
                       
            // Load Basic ROM
            try
            {
                using (file = new FileStream(path+BASIC_ROM_FILE, FileMode.Open))
                {
                    BinaryReader br = new BinaryReader(file);
                    br.Read(TheC64.Basic, 0, 0x2000);
                }
            }
            catch (IOException)
            {
                TheC64.TheDisplay.ShowRequester("Can't read 'Basic ROM'.", "Quit");
                return false;
            }

            // Load Kernal ROM
            try
            {
                using (file = new FileStream(path+KERNAL_ROM_FILE, FileMode.Open))
                {
                    BinaryReader br = new BinaryReader(file);
                    br.Read(TheC64.Kernal, 0, 0x2000);
                }
            }
            catch (IOException)
            {
                TheC64.TheDisplay.ShowRequester("Can't read 'Kernal ROM'.", "Quit");
                return false;
            }
                      

            // Load Char ROM
            try
            {
                using (file = new FileStream(path+CHAR_ROM_FILE, FileMode.Open))
                {
                    BinaryReader br = new BinaryReader(file);
                    br.Read(TheC64.Char, 0, 0x1000);
                }
            }
            catch (IOException)
            {
                TheC64.TheDisplay.ShowRequester("Can't read 'Char ROM'.", "Quit");
                return false;
            }          

            // Load 1541 ROM
            try
            {
                using (file = new FileStream(path+FLOPPY_ROM_FILE, FileMode.Open))
                {
                    BinaryReader br = new BinaryReader(file);
                    br.Read(TheC64.ROM1541, 0, 0x4000);
                }
            }
            catch (IOException)
            {
                TheC64.TheDisplay.ShowRequester("Can't read '1541 ROM'.", "Quit");
                return false;
            }          

            return true;
        }

        C64 _TheC64;

        public void Shutdown()
        {
            Console.Out.WriteLine("Fordo: Shutdown");

            //Video.Close();
            //Events.QuitApplication();
        }
    }
}
