
param (
  [parameter(Mandatory)]
  [string]$ProjectDir
)

$zipPath = Join-Path "$ProjectDir" "versions.zip"

if (Test-Path $zipPath) 
{
    $sourcePath = Join-Path "$ProjectDir" "versions.json"
    $objDir = Join-Path "$ProjectDir" "obj"

    do
    {
        $tempDir = Join-Path "$objDir" ([System.IO.Path]::GetRandomFileName())
    }
    while (Test-Path $tempDir)

    $tempPath = Join-Path "$tempDir" "versions.json"

    Expand-Archive $zipPath $tempDir

    $source = Get-Content $sourcePath -raw
    $zipped = Get-Content $tempPath -raw

    # Because the data comes from a trusted source it is sufficient to just
    # compare the file contents rather than the json objects that they contain
    $diff = Compare-Object $source $zipped

    if ($diff)
    {
        Compress-Archive $sourcePath $zipPath -Update
    }

    Remove-Item $tempDir -Recurse
} 
else 
{
    # If the unchanged json is simply zipped again, the binary zip file will most likely 
    # differ because the zip archive contains the file's metadata as well as the file's contents.
    # The user must revert the zip file deletion unless it's intensional, then this script needs updating
    Write-Host "ERROR: post build event failed, versions.zip cannot be found"
    exit 1
}