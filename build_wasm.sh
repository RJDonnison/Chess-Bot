#!/bin/bash

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
VERSION=${1:-}
OUTPUT_DIR="wasm-dist"
SHARED_DIR="$OUTPUT_DIR/shared-dotnet"
BOTS_DIR="$OUTPUT_DIR/bots"

# Validate version argument
if [ -z "$VERSION" ]; then
    echo -e "${RED}Error: Version name required${NC}"
    echo "Usage: ./build_wasm.sh <version>"
    echo "Example: ./build_wasm.sh v1"
    exit 1
fi

echo -e "${BLUE}Building ChessBot WASM version: $VERSION${NC}"

# Clean previous build
echo "Cleaning previous build..."
rm -rf ChessBot.Wasm/bin/Release

# Publish the WASM project
echo "Publishing WASM project..."
dotnet publish ChessBot.Wasm -c Release -o ChessBot.Wasm/bin/Release/publish

# Create output directories
echo "Creating output directories..."
mkdir -p "$SHARED_DIR"
mkdir -p "$BOTS_DIR/$VERSION"

# Copy .NET framework files to shared directory (only if not already there)
echo "Setting up shared .NET framework..."
if [ ! -f "$SHARED_DIR/dotnet.js" ]; then
    echo "  Copying framework files..."
    cp -r ChessBot.Wasm/bin/Release/publish/wwwroot/_framework/* "$SHARED_DIR/"
else
    echo "  Shared framework already exists, skipping..."
fi

# Copy bot-specific files to versioned directory
echo "Copying bot version $VERSION..."
cp ChessBot.Wasm/bin/Release/publish/wwwroot/index.html "$BOTS_DIR/$VERSION/"

# Create a modified main.js that imports from the shared framework
echo "Generating main.js for version $VERSION..."
cat > "$BOTS_DIR/$VERSION/main.js" << 'EOF'
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet } from '../../shared-dotnet/dotnet.js'

const { getAssemblyExports, getConfig } = await dotnet
    .withDiagnosticTracing(false)
    .create();

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);

self.postMessage({ type: "ready" });

self.onmessage = (e) => {
    if (e.data?.type === "getmove") {
        const move = exports.Chess.GetBestMove(e.data.fen);
        self.postMessage({ type: "bestmove", move });
    }
};
EOF

# All assembly files stay in the shared directory
# The versioned directory only needs the custom main.js and index.html
echo "Bot-specific version directory configured."

# Create .gitignore entry in wasm-dist if it doesn't exist
if [ ! -f "$OUTPUT_DIR/.gitignore" ]; then
    echo "Creating .gitignore for $OUTPUT_DIR..."
    cat > "$OUTPUT_DIR/.gitignore" << 'EOF'
# WASM build artifacts
*
!.gitignore
!shared-dotnet/
!bots/
EOF
fi

echo -e "${GREEN}✓ Successfully built version: $VERSION${NC}"
echo -e "${BLUE}Output directory: $OUTPUT_DIR${NC}"
echo "  - Shared framework: $SHARED_DIR"
echo "  - Version $VERSION: $BOTS_DIR/$VERSION"
echo ""
echo -e "${GREEN}Next steps:${NC}"
echo "1. Copy the '$OUTPUT_DIR' directory to your React app's 'public' folder"
echo "2. Update your host configuration to point to: '/bots/$VERSION/main.js'"
echo "3. To add more versions, run: ./build_wasm.sh v2, ./build_wasm.sh v3, etc."
