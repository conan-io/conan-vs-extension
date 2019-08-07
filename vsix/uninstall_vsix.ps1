if (${env:APPVEYOR_BUILD_WORKER_IMAGE} -eq "Visual Studio 2015")
{
    $visualStudioInstallation = "C:\Program Files (x86)\Microsoft Visual Studio 14.0"
}
else
{
    $visualStudioInstallation = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.VisualStudio.Component.VSSDK -property installationPath
}
$vsixInstaller = Join-Path $visualStudioInstallation 'Common7\IDE\VSIXInstaller.exe'

Write-Host "visualStudioInstallation: $visualStudioInstallation"
Write-Host "vsixInstaller: $vsixInstaller"

. $vsixInstaller /u:VSConanPackage.4d0379e2-2698-4e66-89de-6ead71165e9f

"OK" | Write-Host -ForegroundColor Green
