#!/usr/bin/env bash

set -x

namd_conf=$1
echo Executing $namd_conf...

GFS_DIR=$AZ_BATCH_NODE_SHARED_DIR/gfs/$AZ_BATCH_JOB_ID
cd $GFS_DIR

# set PEs
ppn=$3
if [ -z $ppn ]; then
    ppn=`nproc`
fi

# calculate total number of processors
IFS=',' read -ra HOSTS <<< "$AZ_BATCH_HOST_LIST"
nodes=${#HOSTS[@]}
np=$(($nodes * $ppn))

# create node list
nodelist=.nodelist.charm
rm -f $nodelist
touch $nodelist
for node in "${HOSTS[@]}"; do
    echo host $node >> $nodelist
done

# execute NAMD
echo "Executing namd on $np processors (ppn=$ppn)..."
$NAMD_DIR/charmrun ++verbose ++timeout 300 ++batch $ppn ++remote-shell ssh ++p $np ++ppn $ppn ++nodelist $nodelist $NAMD_DIR/namd2 $namd_conf

namd_conf_prefix=${namd_conf%.*}
cp $namd_conf_prefix*.* $AZ_BATCH_TASK_WORKING_DIR
