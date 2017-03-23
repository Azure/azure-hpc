#!/bin/bash

set -x
set -e

DATABASE_NAME=$1
if [ -n "$AZ_BLAST_DATABASE_NAME" ]; then
    DATABASE_NAME=$AZ_BLAST_DATABASE_NAME
fi

STORAGE_ACCOUNT=$2
if [ -n "$AZ_BLAST_STORAGE_ACCOUNT" ]; then
    STORAGE_ACCOUNT=$AZ_BLAST_STORAGE_ACCOUNT
fi

STORAGE_KEY=$3
if [ -n "$AZ_BLAST_STORAGE_KEY" ]; then
    STORAGE_KEY=$AZ_BLAST_STORAGE_KEY
fi

export BLOBXFER_STORAGEACCOUNTKEY=$STORAGE_KEY

DATABASE_CONTAINER=$4
if [ -n "$AZ_BLAST_DATABASE_CONTAINER" ]; then
    DATABASE_CONTAINER=$AZ_BLAST_DATABASE_CONTAINER
fi

DATABASE_LOCATION=/dev/shm
if [ -n "$AZ_BLAST_DATABASE_LOCATION" ]; then
    DATABASE_LOCATION=$AZ_BLAST_DATABASE_LOCATION
fi

INCLUDE_PATTERN="${DATABASE_NAME}.*"
if [ "$DATABASE_CONTAINER" != "blast-databases" ]; then
    # We have a dedicated DB container
    INCLUDE_PATTERN="*"
fi

sudo apt-get update
sudo apt-get install -y build-essential libssl-dev libffi-dev libpython3-dev python3-dev python3-pip wget curl ncbi-blast+
sudo -H pip3 install --upgrade pip
sudo -H pip3 install --upgrade blobxfer
sudo -H pip3 install --upgrade azure-storage
sudo -H pip3 install --upgrade azure-batch

blobxfer $STORAGE_ACCOUNT $DATABASE_CONTAINER "$DATABASE_LOCATION/$DATABASE_NAME" --download --remoteresource . --include "$INCLUDE_PATTERN"
exit $?
