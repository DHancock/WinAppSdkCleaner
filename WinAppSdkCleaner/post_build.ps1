
param (
  [parameter(Mandatory)]
  [string]$ProjectDir
)

$zipPath = Join-Path "$ProjectDir" "versions.zip"

if (Test-Path $zipPath) 
{
    $sourcePath = Join-Path "$ProjectDir" "versions.json"
    $tempPath = Join-Path "$env:temp" "D27A648C-896F-47CB-90E1-6D70E728C5FB.zip"

    Compress-Archive $sourcePath $tempPath -Update

    $zipHash = (Get-FileHash $zipPath).Hash
    $tempHash = (Get-FileHash $tempPath).Hash

    if ($zipHash -ne $tempHash)
    {
        Move-Item $tempPath $zipPath -Force
    }
    else
    {
        Remove-Item $tempPath
    }
} 
else 
{
    Write-Host "ERROR: post build event failed, versions.zip cannot be found"
    exit 1
}