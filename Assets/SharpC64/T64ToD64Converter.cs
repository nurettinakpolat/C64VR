using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace C64DiskUtilities
{
    public class T64Header
    {
        public string Description { get; private set; }
        public string ContainerName { get; private set; }
        public ushort Version { get; private set; }
        public ushort MaxEntries { get; private set; }
        public ushort UsedEntries { get; private set; }

        public T64Header(BinaryReader reader)
        {
            byte[] descBytes = reader.ReadBytes(32);
            Description = Encoding.ASCII.GetString(descBytes).TrimEnd('\0');
            Version = reader.ReadUInt16();
            MaxEntries = reader.ReadUInt16();
            UsedEntries = reader.ReadUInt16();
            reader.ReadUInt16(); //Not Used
            byte[] tapeContainer = reader.ReadBytes(24);
            ContainerName = Encoding.ASCII.GetString(tapeContainer).TrimEnd('\0');
        }
    }

    public class T64Entry
    {
        public byte EntryType { get; private set; }
        public byte FileType { get; private set; }
        public ushort StartAddress { get; private set; }
        public ushort EndAddress { get; private set; }
        public uint DataOffset { get; private set; }
        public string Filename { get; private set; }


        public T64Entry(BinaryReader reader)
        {
            EntryType = reader.ReadByte();
            FileType = reader.ReadByte();
            StartAddress = reader.ReadUInt16();
            EndAddress = reader.ReadUInt16();
            reader.ReadUInt16(); // Skip unused bytes
            DataOffset = reader.ReadUInt32();
            reader.ReadByte();
            reader.ReadByte();
            reader.ReadByte();
            reader.ReadByte();
            byte[] nameBytes = reader.ReadBytes(16);
            Filename = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0');
        }
    }

    public class D64Image
    {
        private byte[] diskImage;
        private const int D64_SIZE = 174848;
        private const int SECTOR_SIZE = 256;
        private const int DIR_TRACK = 18;
        private const int DIR_SECTOR = 1;
        private const int BAM_TRACK = 18;
        private const int BAM_SECTOR = 0;
        private bool[,] usedSectors;
        private int nextDirEntry = 0;

        public D64Image(string name)
        {
            diskImage = new byte[D64_SIZE];
            usedSectors = new bool[36, 21]; // tracks 1-35, max 21 sectors
            InitializeEmptyDisk(name);
        }

        private void InitializeEmptyDisk(string name)
        {
            // Fill disk with zeros
            Array.Fill(diskImage, (byte)0);

            // Initialize BAM and directory
            InitializeBAM(name);
            InitializeDirectory();
        }

        private void InitializeBAM(string name)
        {
            int bamOffset = GetSectorOffset(BAM_TRACK, BAM_SECTOR);

            // Set first directory sector pointer
            diskImage[bamOffset + 0] = DIR_TRACK;
            diskImage[bamOffset + 1] = DIR_SECTOR;

            // Set DOS version ('A' and $00)
            diskImage[bamOffset + 2] = 0x41;
            diskImage[bamOffset + 3] = 0x00;

            // Initialize BAM entries for each track
            for (int track = 1; track <= 35; track++)
            {
                int trackOffset = bamOffset + 4 + (track - 1) * 4;
                byte sectors = GetFreeSectorsForTrack(track);

                // Set number of free sectors
                diskImage[trackOffset] = sectors;

                // Set sector bitmap (all sectors free initially)
                uint bitmap = (1u << sectors) - 1;
                diskImage[trackOffset + 1] = (byte)(bitmap & 0xFF);
                diskImage[trackOffset + 2] = (byte)((bitmap >> 8) & 0xFF);
                diskImage[trackOffset + 3] = (byte)((bitmap >> 16) & 0xFF);
            }

            // Mark BAM sector and first directory sector as used
            UpdateBAMEntry(BAM_TRACK, BAM_SECTOR, false);
            UpdateBAMEntry(DIR_TRACK, DIR_SECTOR, false);

            // Write disk name (padded with A0)
            string diskName = name;// "NEWDISK";
            byte[] nameBytes = Encoding.ASCII.GetBytes(diskName);
            int nameOffset = bamOffset + 0x90;
            for (int i = 0; i < 16; i++)
            {
                diskImage[nameOffset + i] = (byte)(i < nameBytes.Length ? nameBytes[i] : 0xA0);
            }

            // Write disk ID and DOS type
            diskImage[bamOffset + 0xA2] = 0x32; // '2'
            diskImage[bamOffset + 0xA3] = 0x41; // 'A'
            diskImage[bamOffset + 0xA4] = 0xA0;
            diskImage[bamOffset + 0xA5] = 0xA0;
        }

        private int GetSectorOffset(int track, int sector)
        {
            if (track < 1 || track > 35)
                throw new ArgumentException("Invalid track number");

            int offset = 0;
            for (int t = 1; t < track; t++)
            {
                offset += GetFreeSectorsForTrack(t) * SECTOR_SIZE;
            }
            return offset + (sector * SECTOR_SIZE);
        }
        private void InitializeDirectory()
        {
            int dirOffset = GetSectorOffset(DIR_TRACK, DIR_SECTOR);

            // Mark directory track as used
            usedSectors[DIR_TRACK, DIR_SECTOR] = true;

            // Set up directory header
            diskImage[dirOffset + 0] = 0x00;     // Next track (end of chain)
            diskImage[dirOffset + 1] = 0xFF;     // Next sector (end of chain)

            // Clear directory entries
            for (int i = 2; i < SECTOR_SIZE; i++)
            {
                diskImage[dirOffset + i] = 0x00;
            }

            nextDirEntry = 2; // Start after header
        }
        private byte GetFreeSectorsForTrack(int track)
        {
            if (track <= 17) return 21;
            if (track <= 24) return 19;
            if (track <= 30) return 18;
            return 17;
        }
        private (int track, int sector) FindFreeSector(int startTrack = 1)
        {
            for (int track = startTrack; track <= 35; track++)
            {
                if (track == DIR_TRACK) continue;

                int maxSector = GetFreeSectorsForTrack(track);
                for (int sector = 0; sector < maxSector; sector++)
                {
                    if (!usedSectors[track, sector])
                    {
                        usedSectors[track, sector] = true;
                        UpdateBAMEntry(track, sector, false);
                        return (track, sector);
                    }
                }
            }
            throw new Exception("Disk full");
        }

        public void WriteFile(string filename, byte[] data, ushort startAddress)
        {
            Console.WriteLine($"Writing file: {filename}, Length: {data.Length}, Start Address: ${startAddress:X4}");

            // Find space in directory sector
            int dirOffset = GetSectorOffset(DIR_TRACK, DIR_SECTOR);
            int entryOffset = dirOffset + nextDirEntry;

            if (nextDirEntry >= SECTOR_SIZE)
            {
                throw new Exception("Directory full");
            }

            // Allocate first data sector
            var (firstTrack, firstSector) = FindFreeSector();
            Console.WriteLine($"First data sector: Track {firstTrack}, Sector {firstSector}");

            // Create directory entry
            // File type ($82 = PRG + Closed)
            diskImage[entryOffset + 0] = 0x82;

            // First data sector location
            diskImage[entryOffset + 1] = (byte)firstTrack;
            diskImage[entryOffset + 2] = (byte)firstSector;

            // Filename (padded with $A0)
            byte[] nameBytes = Encoding.ASCII.GetBytes(filename.ToUpper());
            for (int i = 0; i < 16; i++)
            {
                diskImage[entryOffset + 3 + i] =(byte) ((i < nameBytes.Length) ? nameBytes[i] : 0xA0);
            }

            // Side sector track/sector (unused for PRG)
            diskImage[entryOffset + 19] = 0;
            diskImage[entryOffset + 20] = 0;

            // File size in sectors (will update later)
            diskImage[entryOffset + 28] = 0;
            diskImage[entryOffset + 29] = 0;

            nextDirEntry += 32; // Move to next directory entry

            // Prepare data with load address
            byte[] fullData = new byte[data.Length + 2];
            fullData[0] = (byte)(startAddress & 0xFF);
            fullData[1] = (byte)(startAddress >> 8);
            Array.Copy(data, 0, fullData, 2, data.Length);

            // Write file data
            int currentTrack = firstTrack;
            int currentSector = firstSector;
            int dataIndex = 0;
            int sectorCount = 0;

            while (dataIndex < fullData.Length)
            {
                int sectorOffset = GetSectorOffset(currentTrack, currentSector);
                int bytesThisSector = Math.Min(254, fullData.Length - dataIndex);
                sectorCount++;

                if (dataIndex + bytesThisSector < fullData.Length)
                {
                    // Need another sector - find it first
                    var (nextTrack, nextSector) = FindFreeSector(currentTrack);

                    // Write chain pointers
                    diskImage[sectorOffset + 0] = (byte)nextTrack;
                    diskImage[sectorOffset + 1] = (byte)nextSector;

                    // Write data
                    Array.Copy(fullData, dataIndex, diskImage, sectorOffset + 2, bytesThisSector);

                    // Move to next sector
                    currentTrack = nextTrack;
                    currentSector = nextSector;
                }
                else
                {
                    // Last sector
                    diskImage[sectorOffset + 0] = 0x00;  // End of chain
                    diskImage[sectorOffset + 1] = (byte)(bytesThisSector + 1);
                    Array.Copy(fullData, dataIndex, diskImage, sectorOffset + 2, bytesThisSector);
                }

                dataIndex += bytesThisSector;
            }

            // Update file size in directory entry
            diskImage[entryOffset + 28] = (byte)(sectorCount & 0xFF);
            diskImage[entryOffset + 29] = (byte)((sectorCount >> 8) & 0xFF);

            Console.WriteLine($"File written using {sectorCount} sectors");
        }

        private void UpdateBAMEntry(int track, int sector, bool isFree)
        {
            int bamOffset = GetSectorOffset(BAM_TRACK, BAM_SECTOR);
            int trackOffset = bamOffset + 4 + (track - 1) * 4;

            // Get current bitmap byte
            int byteIndex = sector / 8;
            int bitIndex = sector % 8;
            int mapOffset = trackOffset + 1 + byteIndex;

            byte currentByte = diskImage[mapOffset];

            if (isFree)
            {
                // Set bit to 1 (free)
                currentByte |= (byte)(1 << bitIndex);
                diskImage[trackOffset]++; // Increment free sector count
            }
            else
            {
                // Set bit to 0 (used)
                currentByte &= (byte)~(1 << bitIndex);
                diskImage[trackOffset]--; // Decrement free sector count
            }

            diskImage[mapOffset] = currentByte;
        }

        // [Other utility methods remain the same]

        public void SaveToFile(string filename)
        {
            File.WriteAllBytes(filename, diskImage);
            Console.WriteLine($"D64 image saved to {filename}");
        }
    }

    public class T64ToD64Converter
    {
        public void ConvertFile(string t64Path, string d64Path, bool temp=false)
        {
            using (var fileStream = File.OpenRead(t64Path))
            using (var reader = new BinaryReader(fileStream))
            {
                var header = new T64Header(reader);
                D64Image d64 = null;// new D64Image();

                Console.WriteLine($"Converting {header.UsedEntries} files from {header.Description}");

                for (int i = 0; i < header.UsedEntries; i++)
                {
                    var entry = new T64Entry(reader);
                    if (d64 == null)
                        d64 =  new D64Image(entry.Filename);
                    if (entry.EntryType == 1) // Normal file
                    {
                        Console.WriteLine($"Processing file: {entry.Filename}");
                        long currentPos = fileStream.Position;
                        fileStream.Seek(entry.DataOffset, SeekOrigin.Begin);

                        int length = entry.EndAddress - entry.StartAddress + 1;
                        byte[] fileData = reader.ReadBytes(length);

                        Console.WriteLine($"Converting {entry.Filename} (Start: ${entry.StartAddress:X4}, Length: {length} bytes)");
                        d64.WriteFile(entry.Filename, fileData, entry.StartAddress);

                        fileStream.Seek(currentPos, SeekOrigin.Begin);
                    }
                    else
                    {
                        Console.WriteLine($"Skipping entry {i} (type {entry.EntryType})");
                    }
                }

                d64.SaveToFile(d64Path);
            }
        }
    }
}