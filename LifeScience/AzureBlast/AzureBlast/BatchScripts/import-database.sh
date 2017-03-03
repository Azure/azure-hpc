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

DATABASE_SEGMENT=$5
if [ -n "$AZ_BLAST_DATABASE_SEGMENT" ]; then
    DATABASE_SEGMENT=$AZ_BLAST_DATABASE_SEGMENT
fi

NCBI_URL="ftp://ftp.ncbi.nlm.nih.gov/blast/db/"

function download_db_file()
{
    file=$1

    # Fetch the database segment file and md5
    wget $NCBI_URL/$file
    wget $NCBI_URL/$file.md5

    # Validate md5
    md5sum=`md5sum $file | awk -F' ' '{print $1}'`
    grep -q $md5sum $file.md5
    if [ $? -ne 0 ]; then
        echo "MD5 checksum failed for file $file"
        return 1
    fi

    return 0
}

function import_db_file()
{
    file=$1
    unpack_dir="$file.extracted"

    # Create a tmp dir and unpack the database segment
    mkdir $unpack_dir
    cd $unpack_dir

    tar xvfz ../$file
    if [ $? -ne 0 ]; then
        echo "Failed to untar archive $file"
    else
        attempts=0
        while [ $attempts -lt 3 ]; do
            retries=$(($attempts+1))
            # Upload the database segment files to blob
            blobxfer $STORAGE_ACCOUNT $DATABASE_CONTAINER . --upload --include '*.*'
            if [ $? -ne 0 ]; then
                echo "Blob upload failed"
                sleep 30 # back off for a bit
                continue
            fi
            break
        done
    fi

    # Cleanup tmp files
    cd ..
    rm -rf $unpack_dir
    rm -rf $file*

    return 0
}

if [ -n "$DATABASE_SEGMENT" ]; then
    db_files=$DATABASE_SEGMENT
else
    # List database files
    db_files=`curl -s -l $NCBI_URL | grep -v .md5$ | grep ^$DATABASE_NAME`
    if [ $? -ne 0 ]; then
        echo "Error listing databases"
        exit 1
    fi
fi

for file in $db_files; do
    download_db_file $file &
done

failures=0

for job in `jobs -p`; do
    wait $job || let "failures+=1"
done

if [ $failures -ne 0 ]; then
    echo "One or more database segments failed to download"
    exit 1
fi

for file in $db_files; do
    import_db_file $file
done
