
$visualStudioInstallation = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.VisualStudio.Component.VSSDK -property installationPath
$vcvars64 = Join-Path $visualStudioInstallation 'VC\Auxiliary\Build\vcvars64.bat'
$devenv = Join-Path $visualStudioInstallation 'Common7\IDE\devenv.com'

Write-Host "vcvars64: $vcvars64"
Write-Host "devenv: $devenv"

Set-AppveyorBuildVariable "vcvars64" $vcvars64

. "$vcvars64"
$output = . "$devenv" /ConanVisualStudioVersion /?

Write-Host "output: $ss2"
Write-Host "APPVEYOR_JOB_ID: ${env:APPVEYOR_JOB_ID}"
Write-Host "APPVEYOR_BUILD_NUMBER: ${env:APPVEYOR_BUILD_NUMBER}"
Write-Host "APPVEYOR_BUILD_VERSION: ${env:APPVEYOR_BUILD_VERSION}"

$pattern = "^${env:APPVEYOR_BUILD_VERSION}\s+aMicrosoft Visual Studio"
$regex = New-Object System.Text.RegularExpressions.Regex $pattern
$result = $regex.Matches($output)
Write-Host "Matching string ${pattern}: $result"

if ([string]::IsNullOrEmpty($result))
{
    "FAILURE" | Write-Host -ForegroundColor Red
    exit -1
}
"OK" | Write-Host -ForegroundColor Green
