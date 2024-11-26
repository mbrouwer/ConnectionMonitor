FROM mcr.microsoft.com/powershell:ubuntu-22.04

# ARG POWERSHELL_VERSION=7.4.5
# ENV POWERSHELL_VERSION=${POWERSHELL_VERSION}

RUN apt-get update && apt-get install -y --no-install-recommends \
    nmap \
    wget \
    apt-transport-https \
    software-properties-common \
    && rm -rf /var/lib/apt/lists/*

COPY monitor.json .