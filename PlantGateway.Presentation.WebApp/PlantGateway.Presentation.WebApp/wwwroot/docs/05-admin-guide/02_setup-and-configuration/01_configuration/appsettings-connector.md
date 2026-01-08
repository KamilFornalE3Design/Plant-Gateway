# AppSettings: Connectors  
_Administrator Guide_

This document explains how to configure the **Connectors** section inside `appsettings.json`.  
Connectors define how PGedge generates local launcher scripts (`.cmd`) used by desktop users and the WebApp to locate and run the PGedge CLI.

---

## 1. Purpose of Connectors

A **Connector** is a small auto-generated launcher responsible for:

- Setting the PGedge CLI path (environment variable)
- Preparing the workstation for protocol installation
- Allowing the WebApp to trigger local actions
- Providing stable entry points for desktop automation

Connectors are configuration-driven and do not require code changes.

---

## 2. Connectors Configuration Structure

Connectors are defined under the top-level `Connectors` section:

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
