

if (${env:APPVEYOR_BUILD_WORKER_IMAGE} -eq "Visual Studio 2015")
{
    $devenv = "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\devenv.com"
}
else
{
    $visualStudioInstallation = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.VisualStudio.Component.VSSDK -property installationPath
    $devenv = Join-Path $visualStudioInstallation 'Common7\IDE\devenv.com'
    $vcvars64 = Join-Path $visualStudioInstallation 'VC\Auxiliary\Build\vcvars64.bat'
}

Write-Host "devenv: $devenv"

$ntimes = 1
For ($i=1; $i -le $ntimes; $i++) {  # Run 10 times
    $output = . "$devenv" /ConanVisualStudioVersion /?

    $pattern = "^${env:APPVEYOR_BUILD_VERSION}"  # Version + output from /? command
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
    }
}

. "$vcvars64"

$pathConan = Get-Command conan.exe | Select-Object -ExpandProperty Definition
Write-Host "path to Conan: $pathConan"

$sln_file = "C:\projects\conan-vs-extension\Conan.VisualStudio.Examples\ExampleCLI\ExampleCLI.sln"
Write-Host "sln_file: $sln_file"

$process = (Start-Process -FilePath "$devenv" -ArgumentList "$sln_file /ConanVisualStudioVersion /ConanRunInstall conan /Build Release|x64" -Wait -PassThru)
Write-Host "ConanRunInstall finished with return code: " $process.ExitCode


#$output = . "$devenv" /ConanRunInstall "conan" /Build "Release|x64" "$sln_file"
#Write-Host "Output from 'test': $output"
