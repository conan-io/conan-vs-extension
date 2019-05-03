# Inspired in
#   https://docs.microsoft.com/en-us/visualstudio/extensibility/walkthrough-publishing-a-visual-studio-extension-via-command-line?view=vs-2017
#   https://www.alexdresko.com/2018/10/29/publishing-a-visual-studio-extension-via-command-line/

$vsixpublish = Get-ChildItem -File .\packages -recurse | 
    Where-Object { $_.Name -eq "VsixPublisher.exe" } | 
    Sort-Object -Descending -Property CreationTime | 
    Select-Object -First 1 -ExpandProperty FullName
Write-Host "vsixpublish: $vsixpublish"

$visualStudioInstallation = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.VisualStudio.Component.VSSDK -property installationPath
$vsixPublisher = Join-Path $visualStudioInstallation 'VSSDK\VisualStudioIntegration\Tools\Bin\VsixPublisher.exe'
Write-Host "vsixPublisher: $vsixPublisher"


Write-Host "vsixpublish: $vsixpublish"
.\VsixPublisher.exe --version
. $vsixpublish login -publisherName conan-io -personalAccessToken $env:vsmarketplacetoken

$manifest = (Get-Item .\vsix\publish-manifest.json).FullName
$vsix = (Get-Item .\Conan.VisualStudio\bin\Release\Conan.VisualStudio.vsix).FullName
Write-Host "vsix: $vsix"
Write-Host "manifest: $manifest"

. $vsixpublish publish -payload "$vsix" -publishManifest "$manifest"
