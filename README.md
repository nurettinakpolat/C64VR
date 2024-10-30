<h1>Unity C64 Emulator</h1>
<p>This project is a Unity adaptation of sharp-c64 by Stuart Carnie https://github.com/stuartcarnie/sharp-c64 , with additional features and improvements. <br /><br />The original project lacked sound support, so a SID player has been added. (Thx to Lasse Oorni https://github.com/cadaver/oldschoolengine2) <br /><br />It also couldn&rsquo;t load .t64 files, so a .t64 to .d64 converter has been implemented. <br /><br />Load T64 files as if it were a disk file using LOAD"*",8,1.</p>

<h3>Controls</h3>
<p>Use your index fingers on the hand controllers to operate the C64 keyboard or the controllers. To use your hands, place the controllers on a flat surface. Your hands will then become visible. <br /><br />To load a disk, simply touch the handle on the 1541 drive, which will open all available files in the C64 emulator. <br /><br />The controllers also function as joysticks for gameplay. When using the left joystick to control, the fire button is on the right controller, and vice versa.</p>

<h3>File Setup</h3>
<p>At build time add your .d64 files to the Resources/D64/ folder. Rename each .d64 file to filename.d64.bytes (make sure to add .bytes at the end of each filename). Place your .t64 files in the Resources/T64/ folder.</p>
<p><strong>Upload D64 or T64 using SideQuest</strong></p>
<p>Simply drop your D64 or T64 files as it is, without adding <code>.bytes</code> at the end to the quest device folder: <code>sdcard/Android/data/com.DefaultCompany.c64/files/myData/</code>.<br /><br />Download the necessary ROM files:<br /><br />- kernal.rom<br />- basic.rom<br />- d1541.rom<br />- char.rom <br /><br />Move these ROM files into the Resources/Roms/ folder, and rename each file to include .bytes at the end:<br /><br />- kernal.rom.bytes<br />- basic.rom.bytes<br />- d1541.rom.bytes<br />- char.rom.bytes</p>

<h3>Getting Started</h3>
<p>Once all files are set up, build the project in Unity and enjoy your C64 emulator!</p>