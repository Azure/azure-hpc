# Azure BLAST+

BLAST (Basic Local Alignment Search Tool) is an algorithm for comparing primary biological sequence information, such as the amino-acid sequences of proteins or the nucleotides of DNA sequences. A BLAST search enables a researcher to compare a query sequence with a library or database of sequences, and identify library sequences that resemble the query sequence above a certain threshold.

The Azure BLAST+ portal allows you to execute one or more BLAST queries across a number of virtual machines.  Features include

* Azure Web App for the portal
* Azure AAD for authentication
* Azure Storage for persisting inputs and outputs
* Azure Batch for provisioning VMs and scheduling queries
* Large scale - run queries across 10s or even thousands of virtual machines

Deploy the Azure BLAST+ portal to your Azure account.

See the NCBI BLAST+ [home page](http://blast.ncbi.nlm.nih.gov/Blast.cgi) for more information.

# Installation

## Create a AAD application

You'll need to start by creating a AAD application via the Azure Portal or the Azure Xplat CLI.

The first thing you'll need to do is decide on an available web site DNS name in the form of mywebsitename.azurewebsites.net.  For this example we'll use a site name of 'MyBlastPortal', i.e. myblastportal.azurewebsites.net.

Once you've created and configured your application you'll need the following info:

* Application ID
* AAD Tenant Name (something like contoso.onmicrosoft.com)

### AAD - Azure Portal

* Login to the [Azure Portal](https://portal.azure.com)
* Navigate to Azure Active Directory -> App registrations
* Click '+' to create a new application and fill in the following
  * Name - MyBlastPortal
  * Application Type - Web app / API
  * Sign-on URL - https://myblastportal.azurewebsites.net
* Click Create

Navigate to App registrations -> MyBlastPortal and take note of the 'Application ID'.

### AAD - Azure Xplat CLI

When creating the application via the Xplat CLI you'll need to modify the permissions in the portal to grant sign on and profile permissions for the application.

You'll need to have the Azure Xplat CLI installed which is available via the Microsoft Web Platform Installer or [here](http://aka.ms/webpi-azure-cli).

* Open command prompt (cmd.exe)
* Create AAD application with the following command:

`azure ad app create --name MyBlastPortal --home-page https://myblastportal.azurewebsites.net --identifier-uris https://<MyAADTenant>/MyBlastPortal --reply-urls https://myblastportal.azurewebsites.net`

The suggested identifier-uris can be any URL providing it's unique.

Keep note of the 'Application ID' that's output from the above command.

* Login to the Azure Portal https://portal.azure.com
* Navigate to Azure Active Directory -> App registrations -> MyBlastPortal -> Required permissions
* Click '+' to add a new permission
* Click 'Select an API' -> 'Windows Azure Active Directory (Microsoft.Azure.ActiveDirectory)'
* Click 'Select permissions' -> 'Delegated Permissions'
  * Check 'Sign in and read user profile'
  * Click Select
* Click Done to save

## Deploy the Portal

The portal deployment is automated using the ARM template below which will create the Azure storage, batch and web application.

Most of the parameters should be self explanatory and defaults shouldn't need to be changed.  Details on Azure Web App instance sizes and pricing can be found [here](https://azure.microsoft.com/en-au/pricing/details/app-service/).

The deployment will ask for the following information

* Resource group - A friendly name for the web app, batch and storage accounts
* Location - the region to deploy in
* Site Name - the unique DNS name for the website.  This will be suffixed with .azurewebsites.net
* Sku Tier - the web app tier as described in the link above
* Sku Size - the pricing tier size
* Worker Size - Hosting plan instance size
* Number of Workers - The number of workers in the hosting plan
* AAD Tenant - The AAD tenant where you AAD application lives, e.g. contoso.onmicrosoft.com
* AAD Instance - AAD Login URL, you shouldn't need to change this
* AAD Application (client) ID - The AAD application ID from above

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAzure%2Fazure-hpc%2Fmaster%2FLifeSciences%2FAzureBlast%2FTemplates%2Fazuredeploy.json" target="_blank"><img alt="Deploy to Azure" src="http://azuredeploy.net/deploybutton.png"/></a>

Once the web site is deployed you can navigate to https://myblastportal.azurewebsites.net to login.
