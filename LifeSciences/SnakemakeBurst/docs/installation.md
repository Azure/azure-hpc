[Home](../README.md) | [Example](example.md) | [Template](template.md) | [Installation](installation.md) 

<a name="Setup"></a>
# Installation Guide #


<a name="SetupPrerequisites"></a>
### Prerequisites ###

- An active Microsoft Azure subscription. 

<a name="ServicesRequired"></a>
### Services Required ###

The 4 Azure Services required are a [Virtual Machine](#SetupVM), [Azure File Share](#SetupFile), [Azure Batch Account](#SetupAzureBatch) and optionally an [Azure Container Registry ](#SetupShipyard) 


<a id="SetupVM"/></a>
##  Create an Azure VM ##

An Azure Virtual Machine (VM) is used as the 'control node' for running the Snakemake workflows.

1. Login to [Azure Portal](https://portal.azure.com). If you are asked to log in, do so using your Microsoft account.
2. Click the New button found on the upper left-hand corner of the Azure portal.
3. Select Compute, then filter in the Search for 'ubuntu'. Select Ubuntu Server 16.04 LTS, and ensure that Resource Manager is the selected deployment model. Click the Create button.

![1](../images/install1.png)


4. Enter the virtual machine information. For Authentication type, select Password and make sure the Resource Group used here is same one for all of the Azure 
services in the next steps. If you dont have a Resource Group setup click on the 'Add New'. When the details are complete, click Ok

![1](../images/install2.png)


5. Select a size for the VM. To see more sizes, select View all or change the Supported disk type filter. The VM does not need a high specification as it is only a control node.
The actual compute will be handled by Azure Batch. Changing the 'Supported Disk Type' to HDD will provide some cheaper options eg. A1 Basic or A1 Standard.

6. Complete the next 2 steps in the Azure Wizardd by clicking 'Select' for the VM size then OK

7. Once the VM setup is complete we need the IP address to enable us to login to the machine. Copy the IP and save it to a temporary text file.

![1](../images/install3.png)


<a id="SetupFile"/></a>
##  Create an Azure Storage Fileshare ##

An Azure Storage Fileshare is used as the central location for the input/output and Snakemake control files.

1. Login to [Azure Portal](https://portal.azure.com). If you are asked to log in, do so using your Microsoft account.
2. Click the New button found on the upper left-hand corner of the Azure portal.
3. Select Storage then Storage Account. Click the Create button.

![1](../images/install4.png)

4. Enter the Storage Account information and ensure that Resource Group is same one created for the VM. When the details are complete, click Create

![1](../images/install5.png)

5. Once the Storage Account creation is complete a new fileshare needs to be created. The accesskey also needs to be saved for VM configuration section. Click on Files ![1](../images/install6.png) and then 'File Share' ![1](../images/install6a.png)

6. Once the File Share has been created click on 'Access Keys'. We will need these to login to the fileshare in a later step. Copy the user and access key into a Temporary
file.

![1](../images/install7.png)

<a id="SetupAzureBatch"/></a>
##  Create an Azure Batch Account ##

[Azure Batch](https://azure.microsoft.com/en-au/services/batch/) is used as the compute engine to execute the Snakemake commands in the workflow.

1. Login to [Azure Portal](https://portal.azure.com). If you are asked to log in, do so using your Microsoft account.

2. Click New > Compute > Batch Service.

![1](../images/marketplace_batch.png)

3. The New Batch Account blade is displayed. Enter the required values and mkae sure you use the Resource Group created for the VM.

![1](../images/batch_acct_portal.png)

Click Create to create the account.

4. View Batch account properties - We need the Batch account URL to add to the Batch-Shipyard configuration files. Click on overview and copy the URL into a Temporary
file. A Batch account URL has the following format: https://<account_name>.<region>.batch.azure.com

![1](../images/batchurl.png)

5. Access keys - To authenticate access to your Batch account from Batch-Shipyard we will need the account access key. Click on 'Key's and copy the Primary Access Key into a Temporary file.

![1](../images/batchkey.png)


<a id="SetupShipyard"/></a>
##  Install Batch-Shipyard and Snakemake ##

https://github.com/Azure/batch-shipyard/blob/master/docs/01-batch-shipyard-installation.md


For the installation you will install PiPy, the Python package manager, and then use it to install the dependencies for Batch Shipyard. 

1. Begin by launching a terminal. If you're using a desktop version of Linux, the terminal is usually in the Applications menu. It can also be launched by pressing **Ctrl+Alt+F1**. Once the terminal is started, use the following command to install Python, PiPy, and git using apt-get:

	````
	sudo apt-get install python-pip python git
	````

2. Execute the following command to clone Batch Shipyard on the local machine. This will create a folder named "batch-shipyard" and download all the files to that directory.

	````
	git clone https://github.com/Azure/batch-shipyard.git
	````

3. Use a ```cd``` command to change to the "batch-shipyard" folder:

	````
	cd batch-shipyard
	````

4. Use the following command to finish the installation by running the included setup script. The script invokes PiPy to install the dependencies needed by Batch Shipyard.

	````
	./install.sh
	````
To install Snake make run the following apt-get:
~~~~
sudo apt-get install snakemake
~~~~

<a id="SetupDocker"/></a>
##  Create an Azure Docker Container Hub ##

The following link has a very detailed explaination on setting up an Azure Docker Container: https://blogs.msdn.microsoft.com/uk_faculty_connection/2016/09/23/getting-started-with-docker-and-container-services/

The standard docker deployment and configuration is used in the Azure Docker Containers: https://docs.docker.com/get-started/#setup

## Setup the fileshare ##

Setup the new share filesystem. For this step we install CIFS, create a local folder for the share and then will map it to the Azure Fileshare

~~~~
sudo apt-get install cifs-utils
sudo mkdir /home/bizdata/fileshare
~~~~

Mount the Azure File Share. The folder needs to match the one created above and replace the name and key eg: '//genomicsazure.file.core.windows.net/fileshare' with your file
share tne the password with your key.

~~~~
sudo mount -t cifs //genomicsazure.file.core.windows.net/fileshare /home/bizdata/fileshare -o vers=3.0,user=genomicsazure,password=keyfromtheazureportalendningin==,dir_mode=0777,file_mode=0777
~~~~

## Setup Environment Variables ##

You can setup environment variables to be used in the Snakemake rules:

~~~~
export FILESHARE="/home/bizdata/fileshare" 
export SHIPYARD="/home/bizdata/batch-shipyard" 
~~~~

To make these persistant they need to be added to /etc/environment and /etc/fstab

<a id="Config"/></a>
##  Configuration of Batch-Shipyard ##

Once the setup is complete go to the [Template](template.md) section to enter the required keys and configuration from the install.


