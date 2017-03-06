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

# execute NAMD
source /opt/intel/compilers_and_libraries/linux/mpi/bin64/mpivars.sh
echo "Executing namd on $np processors (ppn=$ppn)..."
mpirun -np $np -ppn $ppn -hosts $AZ_BATCH_HOST_LIST $NAMD_DIR/namd2 $namd_conf

# move output file to task working directory
namd_conf_prefix=${namd_conf%.*}
cp $namd_conf_prefix*.* $AZ_BATCH_TASK_WORKING_DIR
