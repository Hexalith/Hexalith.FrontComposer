#!/usr/bin/env bash
set -euo pipefail

# Use Ubuntu's main HTTPS archive for browser dependency packages.
# The hosted Azure HTTP mirror stalled media package downloads for over 20 minutes.
for apt_source in /etc/apt/sources.list /etc/apt/sources.list.d/*.list /etc/apt/sources.list.d/*.sources; do
    if [ -f "$apt_source" ]; then
        sudo sed -i \
            -e 's|http://azure\.archive\.ubuntu\.com/ubuntu|https://archive.ubuntu.com/ubuntu|g' \
            -e 's|https://azure\.archive\.ubuntu\.com/ubuntu|https://archive.ubuntu.com/ubuntu|g' \
            "$apt_source"
    fi
done
