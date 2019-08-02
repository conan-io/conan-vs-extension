$apiUrl = 'https://ci.appveyor.com/api'
$token = '<your-api-token>'
$headers = @{
  "Authorization" = "Bearer $token"
  "Content-type" = "application/json"
}
$accountName = $env:APPVEYOR_ACCOUNT_NAME
$projectSlug = $env:APPVEYOR_PROJECT_SLUG
$buildId = $env:APPVEYOR_BUILD_ID

$downloadLocation = 'C:\projects'

# get project with last build details
$project = Invoke-RestMethod -Method Get -Uri "$apiUrl/projects/$accountName/$projectSlug/builds/$buildId" -Headers $headers

# we assume here that build has a single job
# get this job id
$jobId = $project.build.jobs[0].jobId

# get job artifacts (just to see what we've got)
$artifacts = Invoke-RestMethod -Method Get -Uri "$apiUrl/buildjobs/$jobId/artifacts" -Headers $headers

# here we just take the first artifact, but you could specify its file name
# $artifactFileName = 'Conan.VisualStudio\bin\Release\Conan.VisualStudio.vsix'
$artifactFileName = [uri]::EscapeDataString($artifacts[0].fileName)

# artifact will be downloaded as
$localArtifactPath = "$downloadLocation\Conan.VisualStudio.vsix"
Set-AppveyorBuildVariable "localArtifactPath" $localArtifactPath

# download artifact
# -OutFile - is local file name where artifact will be downloaded into
# the Headers in this call should only contain the bearer token, and no Content-type, otherwise it will fail!
Invoke-RestMethod -Method Get -Uri "$apiUrl/buildjobs/$jobId/artifacts/$artifactFileName" `
-OutFile $localArtifactPath -Headers @{ "Authorization" = "Bearer $token" }

"OK" | Write-Host -ForegroundColor Green
