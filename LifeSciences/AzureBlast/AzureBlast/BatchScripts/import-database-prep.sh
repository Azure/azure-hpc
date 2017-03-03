#!/bin/bash

set -x
set -e

chmod 777 *.sh
sudo apt-get update
sudo apt-get install -y build-essential libssl-dev libffi-dev libpython3-dev python3-dev python3-pip wget curl
sudo -H pip3 install --upgrade pip
sudo -H pip3 install --upgrade blobxfer
sudo -H pip3 install --upgrade azure-storage
sudo -H pip3 install --upgrade azure-batch
