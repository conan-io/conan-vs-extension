if (${env:APPVEYOR_BUILD_WORKER_IMAGE} -eq "Visual Studio 2015")
{
    $visualStudioInstallation = "C:\Program Files (x86)\Microsoft Visual Studio 14.0"
    $vsixOptions = "/q /a"
}
else
{
    $visualStudioInstallation = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.VisualStudio.Component.VSSDK -property installationPath
    $vsixOptions = "/q /a /sp"
}
$vsixInstaller = Join-Path $visualStudioInstallation 'Common7\IDE\VSIXInstaller.exe'

Write-Host "visualStudioInstallation: $visualStudioInstallation"
Write-Host "vsixInstaller: $vsixInstaller"
Write-Host "localArtifactPath: ${env:localArtifactPath}"

$process = (Start-Process -FilePath "$vsixInstaller" -ArgumentList "$vsixOptions ${env:localArtifactPath}" -Wait -PassThru)
Write-Host "Install process finished with return code: " $process.ExitCode

"OK" | Write-Host -ForegroundColor Green
