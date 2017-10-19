FROM ubuntu:16.04
RUN apt-get update 
RUN apt-get install -y --no-install-recommends \
build-essential \
wget \
make \
bzip2 \
bwa 	\ 
bcftools \
libncurses5-dev \
zlib1g-dev

RUN mkdir /home/fileshare
RUN cd /home/fileshare
RUN wget --no-check-certificate https://github.com/samtools/samtools/releases/download/1.3.1/samtools-1.3.1.tar.bz2
RUN bunzip2 samtools-1.3.1.tar.bz2
RUN tar -xvf samtools-1.3.1.tar
RUN cd samtools-1.3.1 ; ./configure ; make ; make install

