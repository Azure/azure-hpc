# 
# Download and installs Batch Shipyard
#
$shipyardVersion = "2.5.3"

# Download shipyard
$shipyardZip = "$PSScriptRoot\shipyard-${shipyardVersion}.zip"
wget "https://github.com/Azure/batch-shipyard/archive/${shipyardVersion}.zip" -OutFile $shipyardZip

# Unzip to current dir
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::ExtractToDirectory($shipyardZip, $PSScriptRoot)

# Rename dir to 'shipyard'
move "$PSScriptRoot\batch-shipyard-$shipyardVersion" "$PSScriptRoot\shipyard" 

# Install shipyard and any dependencies
cd "$PSScriptRoot\shipyard"
pip3.exe install --upgrade -r requirements.txt
cd $PSScriptRoot
