
$visualStudioInstallation = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.VisualStudio.Component.VSSDK -property installationPath
$vcvars64 = Join-Path $visualStudioInstallation 'VC\Auxiliary\Build\vcvars64.bat'

Write-Host "vcvars64: $vcvars64"

. $vcvars64 "&& devenv /ConanVisualStudioVersion /?";