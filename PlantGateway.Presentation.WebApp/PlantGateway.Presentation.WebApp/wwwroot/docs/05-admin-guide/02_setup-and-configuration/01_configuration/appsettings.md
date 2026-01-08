# AppSettings: Connectors & Protocol Configuration  
_Administrator Guide_

This document explains how to configure **Connectors** and **Protocols** inside `appsettings.json`.  
These sections control how PGedge integrates with the workstation, WebApp, and custom URL protocols.

Proper configuration is required for:

- Generating connector `.cmd` launchers  
- Registering the `pgedge://` URL protocol  
- Making the WebApp able to launch the local PGedge CLI  
- Running pipelines via protocol calls  

---

## 1. Overview

Two configuration blocks work together:

1. **Connectors** → define connector launchers & publish locations  
2. **Protocols** → define the URL scheme and verbs  

Connectors reference protocols using `ProtocolKey`:

