param($projectDir, $projectName)

$rdpEnabledSetting = "Microsoft.WindowsAzure.Plugins.RemoteAccess.Enabled";
$cscfgFile = "$projectDir\ServiceConfiguration.cscfg";
$xml = [xml]$(Get-Content $cscfgFile);

try
{
    $acceleratorRole = $xml.ServiceConfiguration.Role | Where-Object { $_.Name -match $projectName.Replace(".Cloud", ".Web") };
    $remoteAccessImport = $acceleratorRole.ConfigurationSettings.Setting | Where-Object { ($_.name -match $rdpEnabledSetting) -and ($_.value -match "true") };
}
catch { }

if ($remoteAccessImport -eq $null)
{
    $message = "Windows Azure Accelerator for Web Roles requires Remote Desktop. Once remote desktop is enabled, you can publish the web role.`n`rHow to enable remote desktop: http://go.microsoft.com/fwlink/?LinkID=221215";
    
    [System.Reflection.Assembly]::LoadWithPartialName("System.Windows.Forms") | Out-Null;
    [Windows.Forms.MessageBox]::Show($message, "Windows Azure Accelerator for Web Roles", [Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Error) | Out-Null;
    
    throw (New-Object Exception $message);
}