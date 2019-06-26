# Info: http://www.visualstudioextensibility.com/2018/01/29/code-signing-a-vsix-package-targeting-multiple-visual-studio-versions/
# vsixsigntool.exe sign /f CodeSigningCertificate.pfx /sha1 "<sha1 bytes>" /p MyPassword /fd sha256 MyVSIXProject.vsix



$cert = (Get-Item .\vsix\conan_vs_extension.pfx).FullName
$vsix = (Get-Item .\Conan.VisualStudio\bin\Release\Conan.VisualStudio.vsix).FullName
Write-Host "vsix: $vsix"
Write-Host "cert: $cert"

$vsixSignTool = Join-Path (Get-Item -Path ".\").FullName "packages\Microsoft.VSSDK.VsixSignTool.16.1.28916.169\tools\vssdk\vsixsigntool.exe"
# Write-Host "vsixSignTool: $vsixSignTool"

. $vsixSignTool sign /f "$cert" /p $env:vsix_sign /fd sha256 "$vsix"
