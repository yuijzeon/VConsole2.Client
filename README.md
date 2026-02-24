# Babyduck.VConsole2.Client

A lightweight C# client library for interacting with VConsole2 used in Source 2 games (eg. Dota 2). This library allows you to connect to a running VConsole2 server, receive structured message chunks, parse common packages (such as PRNT), and send console commands.

## Features

- Connect to a VConsole2 TCP endpoint and stream message chunks.
- Parse message chunks and convert payloads into typed packages that implement `IPackage` (includes `Prnt` package parser).
- Send commands to the VConsole2 server.
- Small, single-file API surface that's easy to integrate.

## Supported frameworks

- Library: .NET Standard 2.1
- Sample project: .NET 10 (net10.0)

## Requirements

Before using this library, make sure you have the following:

- A Source 2 game installed (for example, Dota 2).
- The game's Workshop Tools DLC installed and enabled. See Valve's documentation:
  https://developer.valvesoftware.com/wiki/Dota_2_Workshop_Tools
- The in-game developer console / VConsole2 enabled and configured to listen on a TCP port.
- The correct TCP port is open/allowed by any local firewall (the sample uses port 29000).

Quick checks:

- Verify the game and Workshop Tools are installed.
- Confirm VConsole2 is listening on the expected port (e.g. with `netstat`).
- Run the sample project (`Babyduck.VConsole2.Client.Sample`) to confirm connectivity.

## Installation

You can install it from NuGet.

```powershell
# using dotnet CLI
dotnet add package Babyduck.VConsole2.Client
```

## Quick start & usage

- See the `Babyduck.VConsole2.Client.Sample` project (targets .NET 10) for a runnable example and full usage.
- The sample shows how to create a `VConsole2Client`, subscribe to incoming messages, parse `PRNT` packages, and send console commands.
- For up-to-date API and implementation details, consult the library source files in `Babyduck.VConsole2.Client` (notably `VConsole2Client.cs`, `MessageChunk.cs`, `Prnt.cs`, and `IPackage.cs`).

The sample project demonstrates typical usage and is the best starting point for integration.

## Contributing

Contributions and fixes are welcome. Please open issues or pull requests on the upstream repository: https://github.com/yuijzeon/VConsole2.Client
