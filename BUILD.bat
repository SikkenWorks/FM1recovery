@echo off
setlocal
cd /d "%~dp0"
set "FM1_CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%FM1_CSC%" (
 echo .NET Framework 4.x CSharp compiler not found.
 pause
 exit /b 1
)
"%FM1_CSC%" /nologo /target:winexe /platform:x64 /optimize+ /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /win32manifest:app.manifest /win32icon:app.ico /resource:app.ico,branding.ico /resource:payload.zip,payload.zip /out:FM1-Recovery-by-SikkenWorks-1.0k.exe RecoveryApp.cs
if errorlevel 1 exit /b 1
echo Build complete.
pause
