# BethKEY Russian Edition

Russian localization build of BethKEY by 2Stoned2Mod, rebuilt from the
author's publicly provided C# sources.

Original mod (with original sources): https://www.nexusmods.com/skyrimspecialedition/mods/195029

## What is in this repository

- `src/` — original C# sources by the author (unmodified)
- `src_ru/` — sources with Russian localization applied
- `licenses/` — original license files
- `build_bethkey_ru.bat` — one-click build script

## Build instructions

Requirements: .NET Framework 4.x (shipped with Windows 10/11, no additional
runtime or third-party dependencies needed).

Run `build_bethkey_ru.bat`, or compile manually:
```text
csc /target:winexe /out:BethKEY_ru.exe /win32icon:BethKEY.ico MainForm.cs Core.cs Dialogs.cs ActionHelp.cs ControlMapBinary.cs /r:System.Windows.Forms.dll /r:System.Drawing.dll
```

