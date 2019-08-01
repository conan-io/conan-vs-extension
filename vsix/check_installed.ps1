

if (${env:APPVEYOR_BUILD_WORKER_IMAGE} -eq "Visual Studio 2015")
{
    $devenv = "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\devenv.com"
}
else
{
    $visualStudioInstallation = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.VisualStudio.Component.VSSDK -property installationPath
    # $vcvars64 = Join-Path $visualStudioInstallation 'VC\Auxiliary\Build\vcvars64.bat'
    $devenv = Join-Path $visualStudioInstallation 'Common7\IDE\devenv.com'
}

# Write-Host "vcvars64: $vcvars64"
Write-Host "devenv: $devenv"

# . "$vcvars64"

$ntimes = 5
For ($i=1; $i -le $ntimes; $i++) {  # Run 10 times
    $output = . "$devenv" /ConanVisualStudioVersion /?

    $pattern = "^${env:APPVEYOR_BUILD_VERSION}\s+Microsoft Visual Studio"  # Version + output from /? command
    $regex = New-Object System.Text.RegularExpressions.Regex $pattern
    $result = $regex.Matches($output)

    if ([string]::IsNullOrEmpty($result))
    {
        "FAILURE $i" | Write-Host -ForegroundColor Red
        if ($i -eq $ntimes) {
            Write-Host "Expected version: ${env:APPVEYOR_BUILD_VERSION}"
            Write-Host "Output was: $output"
            $host.SetShouldExit(-1)
            exit
        }
    }
    else {
        "OK" | Write-Host -ForegroundColor Green
        $host.SetShouldExit(0)
        exit
    }
    Start-Sleep -s 20
}

"FAILURE" | Write-Host -ForegroundColor Red
$host.SetShouldExit(-1)
exit
