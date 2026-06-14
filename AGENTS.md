# AGENTS.md

## Build (WSL → Windows MSBuild)

This is a .NET Framework 4.8 WPF solution; build with Visual Studio MSBuild on Windows. From WSL, invoke it via Windows PowerShell.

MSBuild path:
`C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe`

Restore (run once after pulling):
```
/mnt/c/Windows/System32/WindowsPowerShell/v1.0/powershell.exe -NoProfile -Command "& 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe' 'D:\projects\Monitorian\Source\Monitorian.sln' /t:Restore /p:Configuration=Release /v:minimal /nologo"
```

Build the app (produces `Source/Monitorian/bin/Release/Monitorian.exe`):
```
/mnt/c/Windows/System32/WindowsPowerShell/v1.0/powershell.exe -NoProfile -Command "& 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe' 'D:\projects\Monitorian\Source\Monitorian\Monitorian.csproj' /p:Configuration=Release /v:minimal /nologo"
```

Building `Monitorian.csproj` transitively builds `Monitorian.Core`, `ScreenFrame`, `StartupAgency`. Use `/p:Configuration=Debug` for a debug build.

Note: `dotnet` SDK is not installed in this environment, only the runtime — do not use `dotnet build`.
