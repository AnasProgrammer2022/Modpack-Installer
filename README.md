# Modpack Installer
## What is this?
This is a lightweight, terminal-like tool that allows you to download Minecraft Java mods, modpacks and resourcepacks, and updates them.
So you will just play without the headache of updating mods everytime you open the game.

## How to use:
**NOTE:** To use the program, the program has to wipe the config, resourcepacks and mods folders so it can save data for your new installed mods.

Because the program is built on terminal (no UI), you navigate it with commands. But don't worry, the program is intuitive, just follow what the tool is asking for.

Depending on the OS (whether it's Windows 32bit or 64bit, Linux or Mac-OS) you download its executable at the latest release [here](https://github.com/AnasProgrammer2022/Modpack-Installer/releases), then you follow the instructions:
### For Windows:
After you download the Modpack-Installer-Windowsx86.exe or Modpack-Installer-Windowsx64.exe file, you can drop it anywhere. Just run it and run through the steps to start using the program.
### For Linux:
After you download the Modpack-Installer-Linux file, open the terminal in the location of the file, and run this:
```
chmod +x Modpack-Installer
./Modpack-Installer
```

If you're using Linux Mint you can create a launcher with "Launch in terminal" toggled, so it immediately launches the program.

## How to build:
[.NET SDK](https://dotnet.microsoft.com/download) is required (version 9 or higher). After you install it on your operating system, follow the instructions:
### For Windows:
1. Download Visual Studio Installer, open it then install Visual Studio. Make sure you choose **.NET Desktop Development**.
2. Download the zip archive of the source code end extract it in your default Visual Studio projects folder.
3. Open Visual Studio and click on **Open a project or solution** and navigate to the project's root folder, then open the `Modpack-Installer.sln` file.
4. Click on **Build** located in the toolbar up the window, then click on **Publish Selection**.
5. Choose your desired system architecture and operating system. Then click on **Show more settings** then **File publish options** and make sure that:
   - **Enable ReadyToRun Compilation** is disabled.
   - **Trim unused code** is disabled.
   - **Produce single file** is enabled.
### For Linux:
Open the terminal then run the following: (make sure git is installed)
```
# Clone the repo
git clone https://github.com/AnasProgrammer2022/Modpack-Installer.git
cd Modpack-Installer
# Then build the project
dotnet build -c Release
```

Now congrats! You should be having a build of the project.


I took inspiration from [Jamie's Modrinth Pack to Zip Converter](https://jamie.codeberg.page/mrpack-to-zip/@master/).

Made with love by AnasProgrammer2022. No AI is used (except when I say I did).
