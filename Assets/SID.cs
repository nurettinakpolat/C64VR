// MIT License
//
// Copyright (c) 2018-2024 Lasse Oorni
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

// Waveform generation and filter implementation based on jsSID
//
// jsSID by Hermit (Mihaly Horvath) : a javascript SID emulator and player for the Web Audio API
// (Year 2016) http://hermit.sidrip.com

using Memory;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Commodore64
{
    public class SIDChannel
    {
        public enum ADSRState
        {
            Attack = 0,
            Decay,
            Release
        }
        public SIDChannel syncTarget;
        public SIDChannel syncSource;

        public ushort frequency;
        public byte ad;
        public byte sr;
        public ushort pulse;
        public byte waveform;
        public bool doSync = false;

        ADSRState state = ADSRState.Release;
        uint accumulator;
        uint noiseGenerator = 0x7ffff8;
        ushort adsrCounter = 0;
        byte adsrExpCounter = 0;
        byte volumeLevel;

        static readonly ushort[] adsrRateTable = { 9, 32, 63, 95, 149, 220, 267, 313, 392, 977, 1954, 3126, 3907, 11720, 19532, 31251 };
        static readonly byte[] sustainLevels = { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff };
        static readonly byte[] expTargetTable = {
        1,30,30,30,30,30,16,16,16,16,16,16,16,16,8,8,8,8,8,8,8,8,8,8,8,8,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,
        2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2
    };

        public void Clock(int cycles)
        {
            if ((waveform & 0x1) != 0)
            {
                if (state == ADSRState.Release)
                    state = ADSRState.Attack;
            }
            else
                state = ADSRState.Release;

            int adsrCycles = cycles;

            while (adsrCycles > 0)
            {
                // Calculate how long can run until ADSR counter reaches target
                ushort adsrTarget = (state == ADSRState.Attack) ? adsrRateTable[ad >> 4] : ((state == ADSRState.Decay) ? adsrRateTable[ad & 0xf] : adsrRateTable[sr & 0xf]);
                int adsrCyclesNow = Mathf.Min(adsrCycles, adsrCounter < adsrTarget ? adsrTarget - adsrCounter : 0x8000 + adsrTarget - adsrCounter);

                adsrCounter += (ushort)adsrCyclesNow;
                adsrCounter &= 0x7fff;

                if (adsrCounter == adsrTarget)
                {
                    adsrCounter = 0;

                    switch (state)
                    {
                        case ADSRState.Attack:
                            {
                                adsrExpCounter = 0;
                                ++volumeLevel;
                                if (volumeLevel == 0xff)
                                    state = ADSRState.Decay;
                            }
                            break;

                        case ADSRState.Decay:
                            {
                                byte adsrExpTarget = volumeLevel < 0x5d ? expTargetTable[volumeLevel] : (byte)1;
                                ++adsrExpCounter;
                                if (adsrExpCounter >= adsrExpTarget)
                                {
                                    adsrExpCounter = 0;
                                    if (volumeLevel > sustainLevels[sr >> 4])
                                        --volumeLevel;
                                }
                            }
                            break;

                        case ADSRState.Release:
                            {
                                if (volumeLevel > 0)
                                {
                                    byte adsrExpTarget = volumeLevel < 0x5d ? expTargetTable[volumeLevel] : (byte)1;
                                    ++adsrExpCounter;
                                    if (adsrExpCounter >= adsrExpTarget)
                                    {
                                        adsrExpCounter = 0;
                                        --volumeLevel;
                                    }
                                }
                            }
                            break;
                    }
                }

                adsrCycles -= adsrCyclesNow;
            }

            // Testbit
            if ((waveform & 0x8) != 0)
            {
                accumulator = 0;
                // Hack for testbit noise sounding mechanical under this crude emulation
                //noiseGenerator = 0x7ffff8;
                return;
            }

            // If frequency 0, no-op
            if (frequency == 0)
                return;

            // If no noise and no sync target, can use fast clocking
            if ((waveform & 0x80) == 0 && (syncTarget.waveform & 0x2) == 0)
            {
                accumulator += frequency * (uint)cycles;
                accumulator &= 0xffffff;
            }
            else
            {
                // Else calculate how long until next noise generator step or sync
                int accumulatorCycles = cycles;

                while (accumulatorCycles > 0)
                {
                    int accumulatorCyclesNow = accumulatorCycles;

                    if ((waveform & 0x80) != 0)
                    {
                        if ((accumulator & 0xfffff) < 0x80000)
                            accumulatorCyclesNow = Mathf.Min(accumulatorCyclesNow, (int)(0x80000 - (accumulator & 0xfffff)) / frequency + 1);
                        else
                            accumulatorCyclesNow = Mathf.Min(accumulatorCyclesNow, (int)(0x180000 - (accumulator & 0xfffff)) / frequency + 1);
                    }

                    if ((syncTarget.waveform & 0x2) != 0)
                    {
                        if (accumulator < 0x800000)
                            accumulatorCyclesNow = Mathf.Min(accumulatorCyclesNow, (int)(0x800000 - accumulator) / frequency + 1);
                        else
                            accumulatorCyclesNow = Mathf.Min(accumulatorCyclesNow, (int)(0x1800000 - accumulator) / frequency + 1);
                    }

                    uint lastAccumulator = accumulator;

                    accumulator += frequency * (uint)accumulatorCyclesNow;
                    accumulator &= 0xffffff;

                    if ((waveform & 0x80) != 0 && (lastAccumulator & 0x80000) == 0 && (accumulator & 0x80000) != 0)
                    {
                        uint temp = noiseGenerator;
                        uint step = (temp & 0x400000) ^ ((temp & 0x20000) << 5);
                        temp <<= 1;
                        if (step > 0)
                            temp |= 1;
                        noiseGenerator = temp & 0x7fffff;
                    }

                    // Sync
                    doSync = ((lastAccumulator & 0x800000) == 0 && (accumulator & 0x800000) != 0);

                    accumulatorCycles -= accumulatorCyclesNow;
                }
            }
        }

        public void ResetAccumulator()
        {
            accumulator = 0;
        }

        public float GetOutput()
        {
            if (volumeLevel == 0)
                return 0f;

            uint waveOutput = 0;

            switch (waveform & 0xf0)
            {
                case 0x10:
                    waveOutput = Triangle();
                    break;
                case 0x20:
                    waveOutput = Sawtooth();
                    break;
                case 0x40:
                    waveOutput = Pulse();
                    break;
                case 0x50:
                    {
                        uint triangle = Triangle();
                        uint pulse = Pulse();
                        waveOutput = ((pulse & triangle & (triangle >> 1)) & (triangle << 1)) << 1;
                        if (waveOutput > 0xffff)
                            waveOutput = 0xffff;
                    }
                    break;
                case 0x60:
                    {
                        uint saw = Sawtooth();
                        uint pulse = Pulse();
                        waveOutput = ((pulse & saw & (saw >> 1)) & (saw << 1)) << 1;
                        if (waveOutput > 0xffff)
                            waveOutput = 0xffff;
                    }
                    break;
                case 0x70:
                    {
                        uint triSaw = Triangle() & Sawtooth();
                        uint pulse = Pulse();
                        waveOutput = ((pulse & triSaw & (triSaw >> 1)) & (triSaw << 1)) << 1;
                        if (waveOutput > 0xffff)
                            waveOutput = 0xffff;
                    }
                    break;
                case 0x80:
                    waveOutput = Noise();
                    break;
            }

            return ((int)waveOutput - 0x8000) * volumeLevel / 16777216f;
        }

        uint Triangle()
        {
            uint temp = accumulator ^ ((waveform & 0x4) != 0 ? syncSource.accumulator : 0);
            return ((temp >= 0x800000 ? (accumulator ^ 0xffffff) : accumulator) >> 7) & 0xffff;
        }

        uint Sawtooth()
        {
            return accumulator >> 8;
        }

        uint Pulse()
        {
            return (uint)((accumulator >> 12) >= (pulse & 0xfff) ? 0xffff : 0x0);
        }

        uint Noise()
        {
            return ((noiseGenerator & 0x100000) >> 5) + ((noiseGenerator & 0x40000) >> 4) + ((noiseGenerator & 0x4000) >> 1) +
                   ((noiseGenerator & 0x800) << 1) + ((noiseGenerator & 0x200) << 2) + ((noiseGenerator & 0x20) << 5) +
                   ((noiseGenerator & 0x04) << 7) + ((noiseGenerator & 0x01) << 8);
        }
    }

    public class SID
    {

        public int CYCLES_PER_LINE = 63;
        public int NUM_LINES = 312;

        public List<float> samples = new List<float>();
        public List<float> newSamples = new List<float>();
        public object samplesLock = new Object();

        MemoryMappedDevice _ram;
        List<SIDChannel> _channels = new List<SIDChannel>();
        float _cutoffRatio;
       public  float _cyclesPerSample;
        float _cycleAccumulator;
        float _prevBandPass = 0f;
        float _prevLowPass = 0f;

        public SID(MemoryMappedDevice ram)
        {
            _ram = ram;
            for (int i = 0; i < 3; ++i)
                _channels.Add(new SIDChannel());
            _channels[0].syncTarget = _channels[1];
            _channels[1].syncTarget = _channels[2];
            _channels[2].syncTarget = _channels[0];
            _channels[0].syncSource = _channels[2];
            _channels[1].syncSource = _channels[0];
            _channels[2].syncSource = _channels[1];

            _cyclesPerSample = (63f * 312f * 50f) / AudioSettings.outputSampleRate;
            _cutoffRatio = -2f * Mathf.PI * (18000f / 256f) / AudioSettings.outputSampleRate; // 6581 filter
        }

        
        public void BufferSamples(int cpuCycles)
        {
            if (cpuCycles == 0)
                return;

            // Adjust amount of cycles to render based on buffer fill
            float multiplier = 1f + (1764 - samples.Count) / 8192f;
            // Let multiplier remain at 1 when we're executing the playroutine, to make sure ADSR behavior is accurate
            if (cpuCycles <= CYCLES_PER_LINE * 2)
                multiplier = 1f;
            cpuCycles = (int)(multiplier * cpuCycles);

            for (int i = 0; i < 3; ++i)
            {
                ushort ioBase = (ushort)(0xd400 + i * 7);//
                _channels[i].frequency = (ushort)(_ram.Read(ioBase) | (_ram.Read((ushort)(ioBase + 1)) << 8));
                _channels[i].pulse = (ushort)(_ram.Read((ushort)(ioBase + 2)) | (_ram.Read((ushort)(ioBase + 3)) << 8));
                _channels[i].waveform = _ram.Read((ushort)(ioBase + 4));
                _channels[i].ad = _ram.Read((ushort)(ioBase + 5));
                _channels[i].sr = _ram.Read((ushort)(ioBase + 6));
            }

            float masterVol = (_ram.Read(0xd418) & 0xf) / 22.5f;
            byte filterSelect = (byte)(_ram.Read(0xd418) & 0x70);
            byte filterCtrl = _ram.Read(0xd417);

            // Filter cutoff & resonance
            // Adjusted to be slightly darker than jsSID
            float cutoff = 0.05f + 0.85f * (Mathf.Sin((_ram.Read(0xd416) / 255.0f - 0.5f) * Mathf.PI) * 0.5f + 0.5f);
            cutoff = Mathf.Pow(cutoff, 1.3f);
            float resonance = (_ram.Read(0xd417) > 0x3f) ? 7.0f / (_ram.Read(0xd417) >> 4) : 1.75f;

            while (cpuCycles > 0)
            {
                int cyclesToRun = Mathf.Min(cpuCycles, (int)Mathf.Ceil(_cyclesPerSample - _cycleAccumulator));

                for (int j = 0; j < 3; ++j)
                    _channels[j].Clock(cyclesToRun);
                for (int j = 0; j < 3; ++j)
                {
                    if (_channels[j].doSync && (_channels[j].syncTarget.waveform & 0x2) != 0)
                        _channels[j].syncTarget.ResetAccumulator();
                }

                _cycleAccumulator += cyclesToRun;

                if (_cycleAccumulator >= _cyclesPerSample)
                {
                    _cycleAccumulator -= _cyclesPerSample;

                    float output = 0f;
                    float filterInput = 0f;
                    
                    if ((filterCtrl & 1) == 0)
                        output += _channels[0].GetOutput();
                    else
                        filterInput += _channels[0].GetOutput();

                    if ((filterCtrl & 2) == 0)
                        output += _channels[1].GetOutput();
                    else
                        filterInput += _channels[1].GetOutput();

                    if ((filterCtrl & 4) == 0)
                        output += _channels[2].GetOutput();
                    else
                        filterInput += _channels[2].GetOutput();

                    
                    // Highpass
                    float temp = filterInput + _prevBandPass * resonance + _prevLowPass;
                    if ((filterSelect & 0x40) != 0)
                        output -= temp;
                    // Bandpass
                    temp = _prevBandPass - temp * cutoff;
                    _prevBandPass = temp;
                    if ((filterSelect & 0x20) != 0)
                        output -= temp;
                    // Lowpass
                    temp = _prevLowPass + temp * cutoff;
                    _prevLowPass = temp;
                    if ((filterSelect & 0x10) != 0)
                        output += temp;
                    
                    output *= masterVol;
                    newSamples.Add(output);
                }

                cpuCycles -= cyclesToRun;
            }

            lock (samplesLock)
            {
                samples.AddRange(newSamples);
                newSamples.Clear();
            }
        }
    }
}