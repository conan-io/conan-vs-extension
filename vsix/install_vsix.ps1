
$visualStudioInstallation = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.VisualStudio.Component.VSSDK -property installationPath
$vsixInstaller = Join-Path $visualStudioInstallation 'Common7\IDE\VSIXInstaller.exe'

Write-Host "visualStudioInstallation: $visualStudioInstallation"
Write-Host "vsixInstaller: $vsixInstaller"
Write-Host "localArtifactPath: ${env:localArtifactPath}"

$logFile = "C:\projects\install.log"
New-Item $logFile -ItemType file
Start-Process -FilePath "$vsixInstaller" -ArgumentList "/q /a /sp /logFile:$logFile ${env:localArtifactPath}" -Wait -PassThru;

$content = Get-Content -Path $logFile
Write-Host "log output: $content"

$activityLog = "$env:APPDATA\Roaming\Microsoft\VisualStudio\11.0\ActivityLog.xml"
Write-Host "activityLog: $activityLog"
$content = Get-Content -Path $activityLog
Write-Host "activityLog output: $content"

Start-Sleep -s 60
"OK" | Write-Host -ForegroundColor Green
