#!/bin/bash
echo "Installing .NET 10 SDK..."
curl -sSL https://dot.net/v1/dotnet-install.sh > dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh -c 10.0 -InstallDir ./dotnet

echo "Publishing Blazor App..."
./dotnet/dotnet publish ZENIT.Blazor/ZENIT.Blazor.csproj -c Release -o release

echo "Applying Vercel Fix: Copying fingerprinted blazor.webassembly.js..."
# Find the exact fingerprinted file and copy it to the generic name
FINGERPRINTED_FILE=$(ls release/wwwroot/_framework/blazor.webassembly.*.js | head -n 1)

if [ -f "$FINGERPRINTED_FILE" ]; then
    echo "Found fingerprinted file: $FINGERPRINTED_FILE"
    cp "$FINGERPRINTED_FILE" release/wwwroot/_framework/blazor.webassembly.js
    echo "Successfully copied to blazor.webassembly.js"
else
    echo "WARNING: Could not find blazor.webassembly.*.js"
fi
