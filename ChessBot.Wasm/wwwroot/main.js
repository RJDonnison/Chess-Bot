// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet } from './_framework/dotnet.js'

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