<h1>Unity C64 Emulator</h1>
This project is a Unity adaptation of sharp-c64 by Stuart Carnie https://github.com/stuartcarnie/sharp-c64 , with additional features and improvements.
<br/><br/>
-The original project lacked sound support, so a SID player has been added. (Thx to Lasse Oorni https://github.com/cadaver/oldschoolengine2)
<br/><br/>
-It also couldn’t load .t64 files, so a .t64 to .d64 converter has been implemented. <br/><br/>
To load a .t64 file, first convert it to .d64, then load it as if it were a disk file using LOAD"*",8,1.

<h3>Controls</h3>
Use your index fingers on the hand controllers to operate the C64 keyboard or the controllers. <br/><br/>To use your hands, set down the controllers.
To load a disk, simply touch the handle on the 1541 drive, which will open all available files in the C64 emulator.
<br/><br/>         
The controllers also function as joysticks for gameplay. When using the left joystick to control, the fire button is on the right controller, and vice versa.

<h3>File Setup</h3>
Add your .d64 files to the Resources/D64/ folder. Rename each .d64 file to filename.d64.bytes (make sure to add .bytes at the end of each filename).
Place your .t64 files in the Resources/T64/ folder.<br/><br/>
Download the necessary ROM files:<br/><br/>
- kernal.rom<br/>
- basic.rom<br/>
- d1541.rom<br/>
- char.rom <br/><br/>
Move these ROM files into the Resources/Roms/ folder, and rename each file to include .bytes at the end:<br/><br/>
- kernal.rom.bytes<br/>
- basic.rom.bytes<br/>
- d1541.rom.bytes<br/>
- char.rom.bytes.<br/>

<h3>Getting Started</h3>
Once all files are set up, build the project in Unity and enjoy your C64 emulator!