# Info: http://www.visualstudioextensibility.com/2018/01/29/code-signing-a-vsix-package-targeting-multiple-visual-studio-versions/
# vsixsigntool.exe sign /f CodeSigningCertificate.pfx /sha1 "<sha1 bytes>" /p MyPassword /fd sha256 MyVSIXProject.vsix

$cert = (Get-Item .\vsix\conan_vs_extension.pfx).FullName
$vsix = (Get-Item .\Conan.VisualStudio\bin\Release\Conan.VisualStudio.vsix).FullName
Write-Host "vsix: $vsix"
Write-Host "cert: $cert"

$visualStudioInstallation = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.VisualStudio.Component.VSSDK -property installationPath
Write-Host "visualStudioInstallation: $visualStudioInstallation"

$vsixSignTool = Join-Path $visualStudioInstallation 'VSSDK\VisualStudioIntegration\Tools\Bin\VSIXSignTool.exe'
Write-Host "vsixSignTool: $vsixSignTool"

. $vsixSignTool sign /?

# . $vsixSignTool sign /f "$cert" /sha1 c7719dfd6dc759ac9de0a944a76ada5863e2bd85 /p $env:vsix_sign /fd sha256 "$vsix"

