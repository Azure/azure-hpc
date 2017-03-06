
# Azure NAMD

The Azure NAMD project aims to make it simple to run NAMD models on Azure.  Models can be executed on single node or multi-node using TCP or RDMA.

Azure NAMD is based on Azure Batch and Shipyard technologies which utilise Docker under the covers.

## Installation

### Prerequisites

#### Python 3.5 and Pip

You'll need to have Python 3.5+ installed on the system and available in the PATH.  You can tests this by opening a command prompt (cmd.exe) and typing 'python.exe'.  If python is installed you'll end up in a python console.  Check the Python version is at least 3.5.

If Python isn't installed you can download and install it [here](https://www.python.org/downloads/windows/).

When installing ensure you check 'add python to the PATH'.

#### Git

You'll need Git to clone the repository, if you don't have this available you can download a release archive.

#### Azure Storage and Batch Account

You'll need a Azure Storage and Batch account created in the same region.  You can create them using the Azure portal [here](https://portal.azure.com).

### Setup

* Open a command prompt (Windows-R) and run cmd.exe
* Clone the azure-hpc repository into a local drive, we'll use C:\azure-hpc for this example
  * e.g. git clone https://github.com/Azure/azure-hpc.git
* Change directory to NAMD within the repository, 'cd C:\azure-hpc\LifeSciences\NAMD'.
* Initialize the directory by running the init.cmd script - this will download and install Azure Batch Shipyard.
* Update credentials.json with your Azure Storage and Batch account.

## Running Azure NAMD

* Open a command prompt (Windows-R) and run cmd.exe
* Ensure you're in the 'C:\azure-hpc\LifeSciences\NAMD' directory, or where ever you placed Azure NAMD.
* Execute 'namd <path to NAMD conf>'

Note that all files within the NAMD configuration file directory will be uploaded to the virtual machines so ensure all required files exist there.
