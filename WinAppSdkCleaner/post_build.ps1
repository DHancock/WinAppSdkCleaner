
param (
  [parameter(Mandatory)]
  [string]$ProjectDir
)

$zipPath = Join-Path "$ProjectDir" "versions.zip"

if (Test-Path $zipPath) 
{
    $sourcePath = Join-Path "$ProjectDir" "versions.json"
    $tempDir = Join-Path "$env:temp" ([System.IO.Path]::GetRandomFileName())
    $tempPath = Join-Path "$tempDir" "versions.json"

    Expand-Archive $zipPath $tempDir

    $source = Get-Content $sourcePath -raw
    $zipped = Get-Content $tempPath -raw

    # Because the data comes from a trusted source it is sufficient to
    # just compare the file contents, not the json objects that they contain
    $diff = Compare-Object $source $zipped

    if ($diff)
    {
        Compress-Archive $sourcePath $zipPath -Update
    }

    Remove-Item $tempDir -Recurse
} 
else 
{
    # The zip file contains file metadata as well as the file's contents.
    # If the unchanged json is simply zipped again, the binary zip file will most likely differ
    # The user has to revert deleted zip file, unless it's intensional
    Write-Host "ERROR: post build event failed, versions.zip cannot be found"
    exit 1
}