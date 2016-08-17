@echo off

Title Creating Helsenorge.Messaging package

SET PATH=%PATH%;%WinDir%\Microsoft.NET\Framework64\v4.0.30319

SET VisualStudioVersion=14.0
SET PRE_RELEASE=%1

powershell.exe "& '.\DownloadNuget.ps1'"

MSBUILD build.proj /fl /nologo /p:Config=Release /p:RunCodeAnalysis=Never /p:SignAssemblies=True

rem pause
