# Inspired in
#   https://docs.microsoft.com/en-us/visualstudio/extensibility/walkthrough-publishing-a-visual-studio-extension-via-command-line?view=vs-2017
#   https://www.alexdresko.com/2018/10/29/publishing-a-visual-studio-extension-via-command-line/

$visualStudioInstallation = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.VisualStudio.Component.VSSDK -property installationPath
$vsixPublisher = Join-Path $visualStudioInstallation 'VSSDK\VisualStudioIntegration\Tools\Bin\VsixPublisher.exe'

$manifest = (Get-Item .\vsix\publish-manifest.json).FullName
# $vsix = (Get-Item .\Conan.VisualStudio\bin\Release\Conan.VisualStudio.vsix).FullName
Write-Host "vsix: ${env:localArtifactPath}"
Write-Host "manifest: $manifest"

. $vsixPublisher login -publisherName conan-io -personalAccessToken $env:vsmarketplacetoken
. $vsixPublisher publish -payload "${env:localArtifactPath}" -publishManifest "$manifest"
