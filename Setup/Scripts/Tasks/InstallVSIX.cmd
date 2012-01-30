@ECHO OFF

SETLOCAL
%~d0
CD "%~dp0"

ECHO.
ECHO ==================================
ECHO Environment Setup Started
ECHO ==================================
ECHO.

SET VsixInstallerPath=%vs100comntools%

IF NOT "%VsixInstallerPath%"=="" GOTO INSTALL

IF EXIST %WINDIR%\SysWow64 SET VsixInstallerPath=%programfiles(x86)%\Microsoft Visual Studio 10.0\Common7\Tools\
IF NOT EXIST %WINDIR%\SysWow64 SET VsixInstallerPath=%programfiles%\Microsoft Visual Studio 10.0\Common7\Tools\

:INSTALL

ECHO Trying to remove 'Windows Azure Accelerator for Web Roles' extension...
CALL "%VsixInstallerPath%..\IDE\VSIXInstaller.exe" /Q /U:WindowsAzureWebHost
CALL "%VsixInstallerPath%..\IDE\VSIXInstaller.exe" /Q /U:WindowsAzureWebDeployHost
ECHO Installing 'Windows Azure Accelerator for Web Roles' extension...
CALL "%VsixInstallerPath%..\IDE\VSIXInstaller.exe" .\..\..\WindowsAzureWebDeployHost.Templates.vsix

ECHO.
ECHO ==================================
ECHO Environment Setup Completed
ECHO ==================================