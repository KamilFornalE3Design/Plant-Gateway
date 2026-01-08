# Connector Setup and Verification

This document describes how an administrator installs and verifies **PGedge connectors** on a workstation.

Connectors are small launchers that configure the local environment (for example, set `PGEDGE_CLI_LAUNCHER`) and prepare the machine to be used from the WebApp or other tools.

---

## 1. Connector configuration in `appsettings`

Connectors are defined in configuration under the `Connectors` section:

```jsonc
"Connectors": {
  "PgedgeCli": {
    "ProtocolKey": "Cli",
    "Connector": "PgedgeCliConnector",
    "PublishFolder": "C:\\PGedge\\Connector"
  },
  "PgedgeAcc": {
    "ProtocolKey": "Acc",
    "Connector": "PgedgeAccConnector",
    "PublishFolder": "C:\\PGedge\\Connector"
  }
}
