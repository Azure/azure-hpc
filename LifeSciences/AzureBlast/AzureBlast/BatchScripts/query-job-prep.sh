#!/bin/bash

set -x

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

OS_MEMORY=4096
if [ -n "$AZ_BLAST_OS_MEMORY" ]; then
    OS_MEMORY=$$AZ_BLAST_OS_MEMORY
fi

# Install pre req packages
sudo apt-get update
sudo apt-get install -y build-essential libssl-dev libffi-dev libpython3-dev python3-dev python3-pip wget curl ncbi-blast+
sudo -H pip3 install --upgrade pip
sudo -H pip3 install --upgrade blobxfer
sudo -H pip3 install --upgrade azure-storage
sudo -H pip3 install --upgrade azure-batch

# Resize the tmpfs ram disk
total_mem=`free -m | awk '/Mem:/ {print $2}'`
tmpfs_mem=$((total_mem - OS_MEMORY))
if [ $tmpfs_mem -gt 1024 ]; then
    sudo mount -o remount,size=${tmpfs_mem}M /dev/shm
    if [ $? -ne 0 ]; then
        python3 updatestate.py "$STORAGE_ACCOUNT" "$STORAGE_KEY" "allusers" "$AZ_BATCH_JOB_ID" "Error" "Error updating tmpfs"
        exit 1
    fi
    df -h # debug
fi

python3 updatestate.py "$STORAGE_ACCOUNT" "$STORAGE_KEY" "allusers" "$AZ_BATCH_JOB_ID" "DownloadingDatabase"

blobxfer $STORAGE_ACCOUNT $DATABASE_CONTAINER "$DATABASE_LOCATION/$DATABASE_NAME" --download --remoteresource . --include "$INCLUDE_PATTERN"
result=$?

if [ $result -eq 0 ]; then
    python3 updatestate.py "$STORAGE_ACCOUNT" "$STORAGE_KEY" "allusers" "$AZ_BATCH_JOB_ID" "Running"
else
    python3 updatestate.py "$STORAGE_ACCOUNT" "$STORAGE_KEY" "allusers" "$AZ_BATCH_JOB_ID" "Error" "Error downloading database"
fi

exit $result
