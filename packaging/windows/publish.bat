@echo off
rem Publishes Enigma.Msi (the Enigma.Msi.Desktop app) for Windows x64. Runs, from wherever it is
rem started:
rem
rem     dotnet publish src\Enigma.Msi.Desktop -c Release -r win-x64 -p:PublishReadyToRun=true
rem
rem A Release build for win-x64, compiled ReadyToRun so it starts faster, which runs on an installed
rem .NET 10 runtime. The result lands in src\Enigma.Msi.Desktop\bin\Release\net10.0\win-x64\publish\,
rem with the MSI worker in its worker\ subfolder - the folder the MSI profiles under msiProfiles\
rem package. publish.ps1, beside this file, does the same from PowerShell; this one needs no execution
rem policy.

setlocal

rem Resolved from this script (%~dp0 ends with a backslash) rather than the current directory, so it
rem runs from anywhere.
dotnet publish "%~dp0..\..\src\Enigma.Msi.Desktop" -c Release -r win-x64 -p:PublishReadyToRun=true

rem Passed on, so a failed publish fails the script.
exit /b %ERRORLEVEL%
