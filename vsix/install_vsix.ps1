
$visualStudioInstallation = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.VisualStudio.Component.VSSDK -property installationPath
$vsixInstaller = Join-Path $visualStudioInstallation 'Common7\IDE\VSIXInstaller.exe'

Write-Host "visualStudioInstallation: $visualStudioInstallation"
Write-Host "vsixInstaller: $vsixInstaller"
Write-Host "localArtifactPath: ${env:localArtifactPath}"

$process = (Start-Process -FilePath "$vsixInstaller" -ArgumentList "/q /a /sp ${env:localArtifactPath}" -Wait -PassThru)
Write-Host "Install process finished with return code: " $process.ExitCode

"OK" | Write-Host -ForegroundColor Green
