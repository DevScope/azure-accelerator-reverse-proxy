@echo off
rem This is only for SDK 1.4 and not a best practice to detect DevFabric.
if ("%WA_CONTAINER_SID%") == ("") goto Exit

echo Initializing role instance... >> startup.log

if exist retries.txt goto start
echo Initializing retry counter... >> startup.log
echo 0 > retries.txt
:start

rem Increment retry count and bypass start up task after 3 consecutive failures
set /p count=< retries.txt
set /a count+=1
echo %count% > retries.txt
if %count% GEQ 3 goto Exit

echo Installing Web-Mgmt-Service... >> startup.log
if exist "%windir%\system32\ServerManagerCmd.exe" "%windir%\system32\ServerManagerCmd.exe" -install Web-Mgmt-Service  >> startup.log 2>> starterror.log

echo Configuring Web-Mgmt-Service... >> startup.log
sc config wmsvc start= auto >> startup.log 2>> starterror.log
net stop wmsvc >> startup.log 2>> starterror.log

echo Setting the registry key... >> startup.log
%windir%\regedit /s EnableRemoteManagement.reg >> startup.log 2>> starterror.log

md "%~dp0appdata" >> startup.log 2>> starterror.log

reg add "hku\.default\software\microsoft\windows\currentversion\explorer\user shell folders" /v "Local AppData" /t REG_EXPAND_SZ /d "%~dp0appdata" /f >> startup.log 2>> starterror.log

echo Installing WebDeploy... >> startup.log
"%~dp0Webpicmdline.exe" /products: WDeployNoSMO,PHP53,MVC3 /log:webpi.log /AcceptEula >> startup.log 2>> starterror.log

reg add "hku\.default\software\microsoft\windows\currentversion\explorer\user shell folders" /v "Local AppData" /t REG_EXPAND_SZ /d %%USERPROFILE%%\AppData\Local /f >> startup.log 2>> starterror.log

echo Configuring WebDeploy... >> startup.log
sc config msdepsvc start= auto >> startup.log 2>> starterror.log
net stop msdepsvc >> startup.log 2>> starterror.log

echo Starting required services... >> startup.log
net start wmsvc >> startup.log 2>> starterror.log
net start msdepsvc >> startup.log 2>> starterror.log

echo Reset retry count... >> startup.log
echo 0 > retries.txt

exit /b 0

:Exit
echo Running on DevFabric. No action taken.