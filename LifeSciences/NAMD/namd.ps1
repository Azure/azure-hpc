Param(
  [string]$namdConfFilePath,
  [string]$namdArgs = "",
  [string]$recipe = "NAMD-TCP",
  [string]$poolId = $null,
  [switch]$poolKeepAlive
)

if (-Not (Test-Path $namdConfFilePath))
{
    Write-Host "No such file $namdConfFilePath"
    exit 1
}

$id = [guid]::NewGuid().ToString()
$jobId = "namd-$id"
$rootDir = "$PSScriptRoot"
$shipyard = "$rootDir\\shipyard\\shipyard.py"
$tmpDir = "$rootDir\\.shipyard_tmp\\$jobId"
$tmpInputsDir = "$tmpDir\\inputs"
$shipyardDir = "$rootDir\\shipyard-recipes\\$recipe"

if ($poolId)
{
    # Pool spcified, no auto pool
    $autoPool = $false
}
else
{
    # No pool, lets create and delete one for the job
    $poolId = $id
    $autoPool = $true
}

$namdConfFile = Split-Path $namdConfFilePath -leaf
$namdInputDir = (get-item $namdConfFilePath).Directory.FullName

mkdir "$tmpInputsDir" | out-null

# Copy the input files
Copy-Item -Path "$namdInputDir\\*" -Destination "$tmpInputsDir" -Recurse

# Copy the helper scripts
Copy-Item -Path "$rootDir\\scripts\\*" -Destination "$tmpInputsDir" -Recurse

# Copy the shipyard configs and replace and variables needed
$shipyardFilesToCopy = "$rootDir\\credentials.json","$shipyardDir\\config.json","$shipyardDir\\jobs.json","$shipyardDir\\pool.json"
foreach ($file in $shipyardFilesToCopy)
{
    $filename = Split-Path $file -leaf
    $destination = "$tmpDir\\$filename"
    $sourcePath = $tmpInputsDir -replace "\\", "/"
    get-content $file | foreach-object {
		$_ -replace "@JOB_ID@", "$jobId" `
           -replace "@POOL_ID@", "$poolId" `
		   -replace "@SOURCE_PATH@", "$sourcePath" `
		   -replace "@NAMD_INPUT_FILE@", "$namdConfFile" `
           -replace "@NAMD_ARGS@", "$namdArgs" } | set-content $destination
}

Write-Host "Uploading job inputs..."
$output = & $env:python $shipyard data ingress --configdir "$tmpDir" 2>&1
if ($lastexitcode -ne 0)
{
    Write-Host "Failed to upload input data"
    Write-Host $output
    exit 1
}

if ($autoPool)
{
    Write-Host "Creating pool..."
    & $env:python $shipyard pool add --configdir "$tmpDir" --yes
    if ($lastexitcode -ne 0)
    {
        Write-Host "Failed to create pool"
        exit 1
    }
}

Write-Host "Submitting job $jobId..."
& $env:python $shipyard jobs add --configdir "$tmpDir" --yes --tail stdout.txt
if ($lastexitcode -ne 0)
{
    Write-Host "Failed to submit job"
    exit 1
}

Write-Host "Downloading job outputs..."
$output = & $env:python $shipyard data getfile --configdir "$tmpDir" --all --filespec "$jobId,dockertask-00000,std*.txt" 2>&1
if ($lastexitcode -ne 0)
{
	Write-Host $output
    Write-Host "Failed to retrieve outputs"
}

$output = & $env:python $shipyard data getfile --configdir "$tmpDir" --all --filespec "$jobId,dockertask-00000,wd/*" 2>&1
if ($lastexitcode -ne 0)
{
    Write-Host $output
	Write-Host "Failed to retrieve outputs"
    exit 1
}

if ($autoPool -and $poolKeepAlive -eq $false)
{
    Write-Host "Deleting pool..."
    & $env:python $shipyard pool del --configdir "$tmpDir" --yes
    if ($lastexitcode -ne 0)
    {
        Write-Host "Failed to delete pool"
        exit 1
    }
}

Remove-Item $tmpInputsDir -Force -Recurse | out-null

exit 0
