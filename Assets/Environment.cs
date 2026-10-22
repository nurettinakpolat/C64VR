using System.IO;
using System;
using C64Interfaces;

namespace Commodore64
{
	class File : C64Interfaces.IFile
	{
		private FileStream _stream;
		private ulong _size;

		public File(FileInfo fileInfo)
		{
			_stream = fileInfo.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
			_size = (ulong)fileInfo.Length;
		}

		public ulong Size { get { return _size; } }
		public ulong Pos { get { return (ulong)_stream.Position; } }


		public void Read(byte[] memory, int offset, ushort size)
		{
			_stream.Seek(offset, SeekOrigin.Begin);
			_stream.Read(memory, 0, (int)size);
		}

		public byte ReadByte() { return (byte)_stream.ReadByte(); }
		public ushort ReadWord()
		{
			byte[] arr = new byte[2];
			_stream.Read(arr, 0, arr.Length);
			return BitConverter.ToUInt16(arr, 0);
		}
		public uint ReadDWord()
		{
			byte[] arr = new byte[4];
			_stream.Read(arr, 0, arr.Length);
			return BitConverter.ToUInt32(arr, 0);
		}
		public ulong ReadQWord()
		{
			byte[] arr = new byte[8];
			_stream.Read(arr, 0, arr.Length);
			return BitConverter.ToUInt64(arr, 0);
		}
		public bool ReadBool() { return ReadByte() != 0; }

		public void ReadBytes(byte[] data) { _stream.Read(data, 0, data.Length); }
		public void ReadWords(ushort[] data)
		{
			byte[] arr = new byte[2];
			for (int i = 0; i < data.Length; i++)
			{
				_stream.Read(arr, 0, arr.Length);
				data[i] = BitConverter.ToUInt16(arr, 0);
			}
		}
		public void ReadDWords(uint[] data)
		{
			byte[] arr = new byte[4];
			for (int i = 0; i < data.Length; i++)
			{
				_stream.Read(arr, 0, arr.Length);
				data[i] = BitConverter.ToUInt32(arr, 0);
			}
		}
		public void ReadBools(bool[] data)
		{
			for (int i = 0; i < data.Length; i++)
				data[i] = _stream.ReadByte() != 0;
		}

		public void Write(byte data) { _stream.WriteByte(data); }
		public void Write(ushort data)
		{
			byte[] arr = BitConverter.GetBytes(data);
			_stream.Write(arr, 0, arr.Length);
		}
		public void Write(uint data)
		{
			byte[] arr = BitConverter.GetBytes(data);
			_stream.Write(arr, 0, arr.Length);
		}
		public void Write(ulong data)
		{
			byte[] arr = BitConverter.GetBytes(data);
			_stream.Write(arr, 0, arr.Length);
		}
		public void Write(bool data) { Write(data ? (byte)1 : (byte)0); }

		public void Write(byte[] data) { _stream.Write(data, 0, data.Length); }
		public void Write(ushort[] data)
		{
			for (int i = 0; i < data.Length; i++)
				Write(BitConverter.GetBytes(data[i]));
		}
		public void Write(uint[] data)
		{
			for (int i = 0; i < data.Length; i++)
				Write(BitConverter.GetBytes(data[i]));
		}
		public void Write(bool[] data)
		{
			for (int i = 0; i < data.Length; i++)
				Write(data[i] ? (byte)1 : (byte)0);
		}

		public void Close() { _stream.Close(); }
	}
	public unsafe class GdiVideo : C64Interfaces.IVideoOutput
	{
		public uint[] dispaybuffer;
		public unsafe GdiVideo()
		{
			dispaybuffer = new uint[Video.VIC.X_RESOLUTION* Video.VIC.Y_RESOLUTION];
			for (int i = 0; i < Video.Pallete.CvtColors.Length; i++)
			{
				byte[] c = Video.Pallete.Colors[i];
				Video.Pallete.CvtColors[i] = (uint)((c[0] << 16) | (c[1] << 8) | c[2]);
			}
		}
		public unsafe void OutputPixel(uint pos, uint color)
		{
			dispaybuffer[pos] = color;
		}

		public unsafe void Flush()
		{
			
		}
	}
}