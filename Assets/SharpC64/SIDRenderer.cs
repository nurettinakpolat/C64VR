using System;
using System.Collections.Generic;
using System.Text;

namespace SharpC64
{
    public abstract class SIDRenderer
    {
        public abstract void Reset();
        public abstract void EmulateLine();
        public abstract void VBlank();
	    public abstract void WriteRegister(UInt16 adr, Byte abyte);
	    public abstract void NewPrefs(Prefs prefs);
	    public abstract void Pause();
	    public abstract void Resume();
        public Action<short[], int> AudioBufferCallback = null;
        public int RemainingMilliseconds = 0;
        public abstract short[] GetAudioBuffer();
    }
}
