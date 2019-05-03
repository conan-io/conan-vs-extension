# Inspired in
#   https://docs.microsoft.com/en-us/visualstudio/extensibility/walkthrough-publishing-a-visual-studio-extension-via-command-line?view=vs-2017
#   https://www.alexdresko.com/2018/10/29/publishing-a-visual-studio-extension-via-command-line/

$visualStudioInstallation = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.VisualStudio.Component.VSSDK -property installationPath
$vsixPublisher = Join-Path $visualStudioInstallation 'VSSDK\VisualStudioIntegration\Tools\Bin\VsixPublisher.exe'
#Write-Host "vsixPublisher: $vsixPublisher"

. $vsixPublisher login -publisherName conan-io -personalAccessToken $env:vsmarketplacetoken

$manifest = (Get-Item .\vsix\publish-manifest.json).FullName
$vsix = (Get-Item .\Conan.VisualStudio\bin\Release\Conan.VisualStudio.vsix).FullName
Write-Host "vsix: $vsix"
Write-Host "manifest: $manifest"

. $vsixPublisher publish -payload "$vsix" -publishManifest "$manifest"
