[Home](../README.md) | [Example](example.md) | [Template](template.md) | [Installation](installation.md) 

# Setting up a new Snakemake #

## Create a new folder on the fileshare ##

Create a new folder on the fileshare for your Snakemake workflow and create a folder for the Batch-Shipyard configuration files:

~~~~
cd $FILESHARE
mkdir <yourfolder>
cd <yourfolder>
mkdir azurebatch
cd azurebatch
~~~~

Batch-shipyard needs 4 configuration files to be able to create pools and run jobs. These can be cloned and then modified to match your 
account settings: https://github.com/ausdjt/bizdata/tree/master/docs/templates . Copy these into the 'azurebatch' folder.

Batch-shipyard documentation: https://github.com/Azure/batch-shipyard/tree/master/docs . There are also examples of the full settings available: https://github.com/Azure/batch-shipyard/tree/master/config_templates


## config.json ##

~~~~
{
    "batch_shipyard": {
        "storage_account_settings": "mystorageaccount"
    },
    "global_resources": {
        "docker_images": [
            "<dockerimages>"
        ],
    	"docker_volumes": {
			"shared_data_volumes": {
				"azurefilevol": {
					"volume_driver": "azurefile",
					"storage_account_settings": "mystorageaccount",
					"azure_file_share_name": "fileshare",
					"container_path": "/home/<youruser>/fileshare",
					"mount_options": [
						"filemode=0777",
						"dirmode=0777"
					]
				}
            }
        }
	}
}
~~~~

## credentials.json ##

~~~~
{
    "credentials": {
        "batch": {
            "account_key": "batchaccountkeyendingin==",
            "account_service_url": "https://<yourbatchaccount>.batch.azure.com"
        },
        "storage": {
            "mystorageaccount": {
                "account": "<yourazurestorageaccount>",
                "account_key": "keyfromtheazureportalendningin==",
                "endpoint": "core.windows.net"
            }
        }
    }
}
~~~~

To use a Azure Container registry add the following configuration: 

~~~~
"docker_registry": {
            "<yourdockercontainer>.azurecr.io": {
                "username": "<yourdockername>",
                "password": "<yourdockerkey>"
            }
        }
~~~~
## jobs.json ##

~~~~
{
    "job_specifications": [
        {
            "id": "<jobname>",
			"tasks": [
                {
                    "image": "dockerimage/touse",
                    "shared_data_volumes": ["azurefilevol"],
					"remove_container_after_exit": true,
                    "command": "/home/<youraccount>/fileshare/<yourfolder>/jobrun.sh"
                }
            ]
        }
    ]
}
~~~~


## pool.json ##

The vm_size can be modified here. For a full list go to: https://docs.microsoft.com/en-us/azure/virtual-machines/windows/sizes
~~~~
{
    "pool_specification": {
        "id": "combinedpool",
        "vm_size": "Basic_A1",
        "vm_count": {
            "dedicated": 1
        },
        "publisher": "Canonical",
        "offer": "UbuntuServer",
        "sku": "16.04-LTS",
        "ssh": {
            "username": "docker"
        },
        "reboot_on_start_task_failed": false,
        "block_until_all_global_resources_loaded": true
    }
}
~~~~

## Snakemake Shell command ##

For the Snakemake steps use this basic template:

 shell:
         "echo -e \"#!/usr/bin/env bash\ncd $FILESHARE/yourfolder;\n shellcommandtorun}\" > $FILESHARE/<yourfolder>/jobrun.sh ;\n $SHIPYARD/shipyard jobs add --configdir $FILESHARE/yourfolder/azurebatch --tail stderr.txt\n"

		 
		 
		 
