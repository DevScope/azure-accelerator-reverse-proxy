md c:\cache
echo y|cacls c:\cache /G Everyone:f

"%~dp0Webpicmdline.exe" /products: ARR /log:ARR.log /AcceptEula >> startup.log 2>> starterror.log
ping www.microsoft.com

%windir%\system32\inetsrv\appcmd.exe set config -section:system.webServer/proxy /enabled:"True" /reverseRewriteHostInResponseHeaders:"False" /preserveHostHeader:"True" /commit:apphost 	>> startup.log 2>> starterror.log

%windir%\system32\inetsrv\appcmd.exe set config -section:system.webServer/proxy /cache.requestConsolidationEnabled:"True" /cache.queryStringHandling:"Accept" /commit:apphost >> startup.log 2>> starterror.log

%windir%\system32\inetsrv\appcmd.exe set config -section:system.webServer/diskCache /+[path='c:\cache',maxUsage='0'] /commit:apphost >> startup.log 2>> starterror.log

%windir%\system32\inetsrv\appcmd.exe set config	-section:applicationPools -applicationPoolDefaults.processModel.idleTimeout:00:00:00 >> startup.log 2>> starterror.log

exit /b 0