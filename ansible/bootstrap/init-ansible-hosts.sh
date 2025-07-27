#!/bin/bash

# Usage: ./init-ansible-hosts.sh user@target_host

# Run from Ansible control node
# This script sets up passwordless SSH access to a target host.
# Also creates ansible user on target machine and configure it as passwordless sudoer
# TODO: Can check git installation on control host


if [ -z "$1" ]; then
  echo "Usage: $0 <user@target_host>"
  exit 1
fi
TARGET="$1" # user: A privileged user can SSH to target host

# Generate SSH key if it doesn't exist
if [ ! -f "$HOME/.ssh/id_rsa.pub" ]; then
  echo "SSH key not found, generating one..."
  ssh-keygen -t rsa -b 4096 -N "" -f "$HOME/.ssh/id_rsa"
fi

PUBKEY=$(cat ~/.ssh/id_rsa.pub)

ssh "$TARGET" "sudo env PUBKEY='$PUBKEY' bash -s" < ./target-setup.sh
