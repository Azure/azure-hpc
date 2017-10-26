#!/bin/bash

set -x
#set -xeuo pipefail

if [[ $(id -u) -ne 0 ]] ; then
    echo "Must be run as root"
    exit 1
fi

echo "Script arguments: $@"

if [ $# != 10 ]; then
    echo "Usage: $0 <MasterHostname> <WorkerHostnamePrefix> <WorkerNodeCount> <HPCUserName> <TemplateBaseUrl> <ClusterFilesystem> <ClusterFilesystemStorage> <ImageOffer> <Scheduler> <InstallEasybuild>"
    exit 1
fi

# Set user args
MASTER_HOSTNAME=$1
WORKER_HOSTNAME_PREFIX=$2
WORKER_COUNT=$3
TEMPLATE_BASE_URL="$5"
CFS="$6" # None, BeeGFS
CFS_STORAGE="$7" # None,Storage,SSD
CFS_STORAGE_LOCATION="/data/beegfs/storage"
IMAGE_OFFER="$8"
SCHEDULER="$9"
INSTALL_EASYBUILD="${10}"
LAST_WORKER_INDEX=$(($WORKER_COUNT - 1))

if [ "$CFS_STORAGE" == "Storage" ]; then
    CFS_STORAGE_LOCATION="/data/beegfs/storage"
elif [ "$CFS_STORAGE" == "SSD" ]; then
    CFS_STORAGE_LOCATION="/mnt/resource/storage"
fi

# Shares
SHARE_NFS=/share/nfs
SHARE_HOME=$SHARE_NFS/home
SHARE_DATA=$SHARE_NFS/data
SHARE_CFS=/share/cfs
BEEGFS_METADATA=/data/beegfs/meta

# Munged
MUNGE_USER=munge
MUNGE_GROUP=munge
MUNGE_VERSION=0.5.11

# SLURM
SLURM_USER=slurm
SLURM_UID=6006
SLURM_GROUP=slurm
SLURM_GID=6006
SLURM_VERSION=15-08-1-1
SLURM_CONF_DIR=$SHARE_DATA/conf

# Hpc User
HPC_USER=$4
HPC_UID=7007
HPC_GROUP=hpc
HPC_GID=7007


# Returns 0 if this node is the master node.
#
is_master()
{
    hostname | grep "$MASTER_HOSTNAME"
    return $?
}


# Installs all required packages.
#
install_pkgs()
{
    echo "$IMAGE_OFFER" | grep -q 'HPC$'
    if [ $? -eq 0 ]; then
        rpm --rebuilddb
        updatedb
        yum clean all
        yum -y install epel-release
        #yum --exclude WALinuxAgent,intel-*,kernel*,*microsoft-*,msft-* -y update

        sed -i.bak -e '28d' /etc/yum.conf
        sed -i '28i#exclude=kernel*' /etc/yum.conf

        yum -y install zlib zlib-devel bzip2 bzip2-devel bzip2-libs openssl openssl-devel openssl-libs \
            nfs-utils rpcbind git libicu libicu-devel make zip unzip mdadm wget gsl bc rpm-build  \
            readline-devel pam-devel libXtst.i686 libXtst.x86_64 make.x86_64 sysstat.x86_64 python-pip automake autoconf \
            binutils.x86_64 compat-libcap1.x86_64 glibc.i686 glibc.x86_64 \
            ksh compat-libstdc++-33 libaio.i686 libaio.x86_64 libaio-devel.i686 libaio-devel.x86_64 \
            libgcc.i686 libgcc.x86_64 libstdc++.i686 libstdc++.x86_64 libstdc++-devel.i686 libstdc++-devel.x86_64 \
            libXi.i686 libXi.x86_64 gcc gcc-c++ gcc.x86_64 gcc-c++.x86_64 glibc-devel.i686 glibc-devel.x86_64 libtool libxml2-devel mpich-3.2 mpich-3.2-devel

        sed -i.bak -e '28d' /etc/yum.conf
        sed -i '28iexclude=kernel*' /etc/yum.conf
    else
        yum -y install epel-release
        yum -y install zlib zlib-devel bzip2 bzip2-devel bzip2-libs openssl openssl-devel openssl-libs \
            gcc gcc-c++ nfs-utils rpcbind mdadm wget python-pip kernel kernel-devel \
            mpich-3.2 mpich-3.2-devel automake autoconf
    fi
}

# Partitions all data disks attached to the VM and creates
# a RAID-0 volume with them.
#
setup_data_disks()
{
    mountPoint="$1"
    filesystem="$2"
    createdPartitions=""

    # Loop through and partition disks until not found
    for disk in sdc sdd sde sdf sdg sdh sdi sdj sdk sdl sdm sdn sdo sdp sdq sdr; do
        fdisk -l /dev/$disk || break
        fdisk /dev/$disk << EOF
n
p
1


t
fd
w
EOF
        createdPartitions="$createdPartitions /dev/${disk}1"
    done
    
    sleep 30

    # Create RAID-0 volume
    if [ -n "$createdPartitions" ]; then
        devices=`echo $createdPartitions | wc -w`
        mdadm --create /dev/md10 --level 0 --raid-devices $devices $createdPartitions
        if [ "$filesystem" == "xfs" ]; then
            mkfs -t $filesystem /dev/md10
            echo "/dev/md10 $mountPoint $filesystem rw,noatime,attr2,inode64,nobarrier,sunit=1024,swidth=4096,nofail 0 2" >> /etc/fstab
        else
            mkfs -t $filesystem /dev/md10
            echo "/dev/md10 $mountPoint $filesystem defaults,nofail 0 2" >> /etc/fstab
        fi
        
        sleep 15
        
        mount /dev/md10
    fi
}

# Creates and exports two shares on the master nodes:
#
# /share/home (for HPC user)
# /share/data
#
# These shares are mounted on all worker nodes.
#
setup_shares()
{
    mkdir -p $SHARE_NFS
    mkdir -p $SHARE_CFS

    if is_master; then
        if [ "$CFS" == "BeeGFS" ]; then
            mkdir -p $BEEGFS_METADATA
            setup_data_disks $BEEGFS_METADATA "ext4"
        else
            setup_data_disks $SHARE_NFS "ext4"
        fi

        echo "$SHARE_NFS    *(rw,async)" >> /etc/exports
        systemctl enable rpcbind || echo "Already enabled"
        systemctl enable nfs-server || echo "Already enabled"
        systemctl start rpcbind || echo "Already enabled"
        systemctl start nfs-server || echo "Already enabled"

        mount -a
        mount
    else
        if [ "$CFS_STORAGE" == "Storage" ]; then
            # Format CFS mount point
            mkdir -p $CFS_STORAGE_LOCATION
            setup_data_disks $CFS_STORAGE_LOCATION "xfs"
        fi

        # Mount master NFS share
        echo "master:$SHARE_NFS $SHARE_NFS    nfs4    rw,auto,_netdev 0 0" >> /etc/fstab
        mount -a
        mount | grep "^master:$SHARE_HOME"
    fi
}

# Downloads/builds/installs munged on the node.
# The munge key is generated on the master node and placed
# in the data share.
# Worker nodes copy the existing key from the data share.
#
install_munge()
{
    groupadd $MUNGE_GROUP

    useradd -M -c "Munge service account" -g munge -s /usr/sbin/nologin munge

    wget https://github.com/dun/munge/archive/munge-${MUNGE_VERSION}.tar.gz

    tar xvfz munge-$MUNGE_VERSION.tar.gz

    cd munge-munge-$MUNGE_VERSION

    mkdir -m 700 /etc/munge
    mkdir -m 711 /var/lib/munge
    mkdir -m 700 /var/log/munge
    mkdir -m 755 /var/run/munge

    ./configure -libdir=/usr/lib64 --prefix=/usr --sysconfdir=/etc --localstatedir=/var && make && make install

    chown -R munge:munge /etc/munge /var/lib/munge /var/log/munge /var/run/munge

    if is_master; then
        dd if=/dev/urandom bs=1 count=1024 > /etc/munge/munge.key
        mkdir -p $SLURM_CONF_DIR
        cp /etc/munge/munge.key $SLURM_CONF_DIR
    else
        cp $SLURM_CONF_DIR/munge.key /etc/munge/munge.key
    fi

    chown munge:munge /etc/munge/munge.key
    chmod 0400 /etc/munge/munge.key

    /etc/init.d/munge start

    cd ..
}

# Installs and configures slurm.conf on the node.
# This is generated on the master node and placed in the data
# share.  All nodes create a sym link to the SLURM conf
# as all SLURM nodes must share a common config file.
#
install_slurm_config()
{
    if is_master; then

        mkdir -p $SLURM_CONF_DIR

        if [ -e "$TEMPLATE_BASE_URL/slurm.template.conf" ]; then
            cp "$TEMPLATE_BASE_URL/slurm.template.conf" .
        else
            wget "$TEMPLATE_BASE_URL/slurm.template.conf"
        fi

        cat slurm.template.conf |
        sed 's/__MASTER__/'"$MASTER_HOSTNAME"'/g' |
                sed 's/__WORKER_HOSTNAME_PREFIX__/'"$WORKER_HOSTNAME_PREFIX"'/g' |
                sed 's/__LAST_WORKER_INDEX__/'"$LAST_WORKER_INDEX"'/g' > $SLURM_CONF_DIR/slurm.conf
    fi

    ln -s $SLURM_CONF_DIR/slurm.conf /etc/slurm/slurm.conf
}

# Downloads, builds and installs SLURM on the node.
# Starts the SLURM control daemon on the master node and
# the agent on worker nodes.
#
install_slurm()
{
    groupadd -g $SLURM_GID $SLURM_GROUP

    useradd -M -u $SLURM_UID -c "SLURM service account" -g $SLURM_GROUP -s /usr/sbin/nologin $SLURM_USER

    mkdir -p /etc/slurm /var/spool/slurmd /var/run/slurmd /var/run/slurmctld /var/log/slurmd /var/log/slurmctld

    chown -R slurm:slurm /var/spool/slurmd /var/run/slurmd /var/run/slurmctld /var/log/slurmd /var/log/slurmctld

    wget https://github.com/SchedMD/slurm/archive/slurm-$SLURM_VERSION.tar.gz

    tar xvfz slurm-$SLURM_VERSION.tar.gz

    cd slurm-slurm-$SLURM_VERSION

    ./configure -libdir=/usr/lib64 --prefix=/usr --sysconfdir=/etc/slurm && make -j 4 && make install

    install_slurm_config

    if is_master; then
        wget $TEMPLATE_BASE_URL/slurmctld.service
        mv slurmctld.service /usr/lib/systemd/system
        systemctl daemon-reload
        systemctl enable slurmctld
        systemctl start slurmctld
        systemctl status slurmctld
    else
        wget $TEMPLATE_BASE_URL/slurmd.service
        mv slurmd.service /usr/lib/systemd/system
        systemctl daemon-reload
        systemctl enable slurmd
        systemctl start slurmd
        systemctl status slurmd
    fi

    cd ..
}

# Downloads and installs PBS Pro OSS on the node.
# Starts the PBS Pro control daemon on the master node and
# the mom agent on worker nodes.
#
install_pbsoss()
{
    yum install -y gcc make rpm-build libtool hwloc-devel \
      libX11-devel libXt-devel libedit-devel libical-devel \
      ncurses-devel perl postgresql-devel python-devel tcl-devel \
      tk-devel swig expat-devel openssl-devel libXext libXft \
      autoconf automake expat libedit postgresql-server python \
      sendmail tcl tk libical perl-Env perl-Switch

    # Required on 7.2 as the libical lib changed
    ln -s /usr/lib64/libical.so.1 /usr/lib64/libical.so.0

    wget http://wpc.23a7.iotacdn.net/8023A7/origin2/rl/PBS-Open/CentOS_7.zip
    unzip CentOS_7.zip
    cd CentOS_7
    rpm -ivh --nodeps pbspro-server-14.1.0-13.1.x86_64.rpm

    echo 'export PATH=/opt/pbs/default/bin:$PATH' >> /etc/profile.d/pbs.sh
    echo 'export PATH=/opt/pbs/default/sbin:$PATH' >> /etc/profile.d/pbs.sh

    if is_master; then
        cat > /etc/pbs.conf << EOF
PBS_SERVER=$MASTER_HOSTNAME
PBS_START_SERVER=1
PBS_START_SCHED=1
PBS_START_COMM=1
PBS_START_MOM=0
PBS_EXEC=/opt/pbs
PBS_HOME=/var/spool/pbs
PBS_CORE_LIMIT=unlimited
PBS_SCP=/bin/scp
EOF

        /etc/init.d/pbs start

        for i in $(seq 0 $LAST_WORKER_INDEX); do
            nodeName=${WORKER_HOSTNAME_PREFIX}${i}
            /opt/pbs/bin/qmgr -c "c n $nodeName"
        done

        # Enable job history
        /opt/pbs/bin/qmgr -c "s s job_history_enable = true"
        /opt/pbs/bin/qmgr -c "s s job_history_duration = 336:0:0"
    else
        cat > /etc/pbs.conf << EOF
PBS_SERVER=$MASTER_HOSTNAME
PBS_START_SERVER=0
PBS_START_SCHED=0
PBS_START_COMM=0
PBS_START_MOM=1
PBS_EXEC=/opt/pbs
PBS_HOME=/var/spool/pbs
PBS_CORE_LIMIT=unlimited
PBS_SCP=/bin/scp
EOF

        /etc/init.d/pbs start
    fi

    cd ..
}

install_scheduler()
{
    if [ "$SCHEDULER" == "Slurm" ]; then
        install_munge
        install_slurm
    elif [ "$SCHEDULER" == "PBSPro-OS" ]; then
        install_pbsoss
    else
        echo "Invalid scheduler specified: $SCHEDULER"
        exit 1
    fi
}

# Adds a common HPC user to the node and configures public key SSh auth.
# The HPC user has a shared home directory (NFS share on master) and access
# to the data share.
#
setup_hpc_user()
{
    # disable selinux
    sed -i 's/enforcing/disabled/g' /etc/selinux/config
    setenforce permissive

    groupadd -g $HPC_GID $HPC_GROUP

    # Don't require password for HPC user sudo
    echo "$HPC_USER ALL=(ALL) NOPASSWD: ALL" >> /etc/sudoers

    # Disable tty requirement for sudo
    sed -i 's/^Defaults[ ]*requiretty/# Defaults requiretty/g' /etc/sudoers

    if is_master; then
        mkdir -p $SHARE_HOME

        useradd -c "HPC User" -g $HPC_GROUP -m -d $SHARE_HOME/$HPC_USER -s /bin/bash -u $HPC_UID $HPC_USER

        mkdir -p $SHARE_HOME/$HPC_USER/.ssh

        # Configure public key auth for the HPC user
        ssh-keygen -t rsa -f $SHARE_HOME/$HPC_USER/.ssh/id_rsa -q -P ""
        cat $SHARE_HOME/$HPC_USER/.ssh/id_rsa.pub > $SHARE_HOME/$HPC_USER/.ssh/authorized_keys

        echo "Host *" > $SHARE_HOME/$HPC_USER/.ssh/config
        echo "    StrictHostKeyChecking no" >> $SHARE_HOME/$HPC_USER/.ssh/config
        echo "    UserKnownHostsFile /dev/null" >> $SHARE_HOME/$HPC_USER/.ssh/config
        echo "    PasswordAuthentication no" >> $SHARE_HOME/$HPC_USER/.ssh/config

        # Fix .ssh folder ownership
        chown -R $HPC_USER:$HPC_GROUP $SHARE_HOME/$HPC_USER

        # Fix permissions
        chmod 700 $SHARE_HOME/$HPC_USER/.ssh
        chmod 644 $SHARE_HOME/$HPC_USER/.ssh/config
        chmod 644 $SHARE_HOME/$HPC_USER/.ssh/authorized_keys
        chmod 600 $SHARE_HOME/$HPC_USER/.ssh/id_rsa
        chmod 644 $SHARE_HOME/$HPC_USER/.ssh/id_rsa.pub

    else
        useradd -c "HPC User" -g $HPC_GROUP -d $SHARE_HOME/$HPC_USER -s /bin/bash -u $HPC_UID $HPC_USER
    fi

    chown $HPC_USER:$HPC_GROUP $SHARE_CFS
}

# Sets all common environment variables and system parameters.
#
setup_env()
{
    # Set unlimited mem lock
    echo "$HPC_USER hard memlock unlimited" >> /etc/security/limits.conf
    echo "$HPC_USER soft memlock unlimited" >> /etc/security/limits.conf

    echo "$IMAGE_OFFER" | grep -q 'HPC$'
    if [ $? -eq 0 ]; then
        # Intel MPI config for IB
        echo "# IB Config for MPI" > /etc/profile.d/mpi.sh
        echo "export I_MPI_FABRICS=shm:dapl" >> /etc/profile.d/mpi.sh
        echo "export I_MPI_DAPL_PROVIDER=ofa-v2-ib0" >> /etc/profile.d/mpi.sh
        echo "export I_MPI_DYNAMIC_CONNECTION=0" >> /etc/profile.d/mpi.sh
    fi
}

install_easybuild()
{
    if [ "$INSTALL_EASYBUILD" != "Yes" ]; then
        echo "Skipping EasyBuild install..."
        return 0
    fi

    yum -y install Lmod python-devel python-pip gcc gcc-c++ patch unzip tcl tcl-devel libibverbs libibverbs-devel
    pip install vsc-base

    EASYBUILD_HOME=$SHARE_HOME/$HPC_USER/EasyBuild

    if is_master; then
        su - $HPC_USER -c "pip install --install-option --prefix=$EASYBUILD_HOME https://github.com/hpcugent/easybuild-framework/archive/easybuild-framework-v2.5.0.tar.gz"

        # Add Lmod to the HPC users path
        echo 'export PATH=/usr/lib64/openmpi/bin:/usr/share/lmod/6.0.15/libexec:$PATH' >> $SHARE_HOME/$HPC_USER/.bashrc

        # Setup Easybuild configuration and paths
        echo 'export PATH=$HOME/EasyBuild/bin:$PATH' >> $SHARE_HOME/$HPC_USER/.bashrc
        echo 'export PYTHONPATH=$HOME/EasyBuild/lib/python2.7/site-packages:$PYTHONPATH' >> $SHARE_HOME/$HPC_USER/.bashrc
        echo "export MODULEPATH=$EASYBUILD_HOME/modules/all" >> $SHARE_HOME/$HPC_USER/.bashrc
        echo "export EASYBUILD_MODULES_TOOL=Lmod" >> $SHARE_HOME/$HPC_USER/.bashrc
        echo "export EASYBUILD_INSTALLPATH=$EASYBUILD_HOME" >> $SHARE_HOME/$HPC_USER/.bashrc
        echo "export EASYBUILD_DEBUG=1" >> $SHARE_HOME/$HPC_USER/.bashrc
        echo "source /usr/share/lmod/6.0.15/init/bash" >> $SHARE_HOME/$HPC_USER/.bashrc
    fi
}

install_cfs()
{
    if [ "$CFS" == "BeeGFS" ]; then
        wget -O beegfs-rhel7.repo http://www.beegfs.com/release/latest-stable/dists/beegfs-rhel7.repo
        mv beegfs-rhel7.repo /etc/yum.repos.d/beegfs.repo
        rpm --import http://www.beegfs.com/release/latest-stable/gpg/RPM-GPG-KEY-beegfs

        yum install -y beegfs-client beegfs-helperd beegfs-utils

        sed -i 's/^sysMgmtdHost.*/sysMgmtdHost = '$MASTER_HOSTNAME'/g' /etc/beegfs/beegfs-client.conf
        sed -i  's/Type=oneshot.*/Type=oneshot\nRestart=always\nRestartSec=5/g' /etc/systemd/system/multi-user.target.wants/beegfs-client.service
        echo "$SHARE_CFS /etc/beegfs/beegfs-client.conf" > /etc/beegfs/beegfs-mounts.conf

        if is_master; then
            yum install -y beegfs-mgmtd beegfs-meta
            mkdir -p /data/beegfs/mgmtd
            sed -i 's|^storeMgmtdDirectory.*|storeMgmtdDirectory = /data/beegfs/mgmt|g' /etc/beegfs/beegfs-mgmtd.conf
            sed -i 's|^storeMetaDirectory.*|storeMetaDirectory = '$BEEGFS_METADATA'|g' /etc/beegfs/beegfs-meta.conf
            sed -i 's/^sysMgmtdHost.*/sysMgmtdHost = '$MASTER_HOSTNAME'/g' /etc/beegfs/beegfs-meta.conf
            /etc/init.d/beegfs-mgmtd start
            /etc/init.d/beegfs-meta start
        else
            yum install -y beegfs-storage
            sed -i 's|^storeStorageDirectory.*|storeStorageDirectory = '$CFS_STORAGE_LOCATION'|g' /etc/beegfs/beegfs-storage.conf
            sed -i 's/^sysMgmtdHost.*/sysMgmtdHost = '$MASTER_HOSTNAME'/g' /etc/beegfs/beegfs-storage.conf
            /etc/init.d/beegfs-storage start
        fi

        systemctl daemon-reload
    fi
}

install_pkgs
setup_shares
setup_hpc_user
install_cfs
install_scheduler
setup_env
install_easybuild
shutdown -r +1 &
exit 0
