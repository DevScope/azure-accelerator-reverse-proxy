@ECHO OFF

SETLOCAL ENABLEDELAYEDEXPANSION
%~d0
CD "%~dp0"

Setup\ContentInstallerClient\ContentInstallerClient.exe /depi:Setup\WindowsAzureAcceleratorForWebRoles.depi

SET /P SuccessfullyFinished="Did the Content Installer finish installing the required dependencies? (Y / N): "

IF /I "%SuccessfullyFinished%"=="Y" (
   ECHO.
   SET /P InstallVsix="Do you want to run the VSIX installer to install the 'Windows Azure Accelerator for Web Roles' extension? (Y / N): "
   
   IF /I "!InstallVsix!"=="Y"  (
      CALL Setup\Scripts\Tasks\InstallVSIX.cmd
   ) ELSE (
      ECHO.
      ECHO The 'Windows Azure Accelerator for Web Roles' extension will not be installed.
   )
   
) ELSE (
   ECHO.
   ECHO Please, run the Content Installer again and install all the required dependencies so you can continue with the setup.
)

ECHO.
PAUSE