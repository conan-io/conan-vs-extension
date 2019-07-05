
$vsixPath = "C:\projects\Conan.VisualStudio.vsix"

$visualStudioInstallation = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.VisualStudio.Component.VSSDK -property installationPath
$vsixInstaller = Join-Path $visualStudioInstallation 'Common7\IDE\VSIXInstaller.exe'

Write-Host "visualStudioInstallation: $visualStudioInstallation"
Write-Host "vsixInstaller: $vsixInstaller"

Start-Process -FilePath "$vsixInstaller" -ArgumentList "/q /a $vsixPath" -Wait -PassThru;
