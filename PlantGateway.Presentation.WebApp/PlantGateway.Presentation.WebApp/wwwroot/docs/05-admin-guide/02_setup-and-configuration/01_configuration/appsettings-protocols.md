This allows the WebApp, browser, or desktop tools to trigger local PGedge CLI actions.

---

## 1. Purpose of Protocols

Protocols:

- Declare the URL scheme (`pgedge`)
- Define logical targets (e.g., `cli`, `acc`)
- Define verbs (`open`, `run`, `job`)
- Connect incoming protocol URLs to handlers in the PGedge CLI
- Enable WebApp → Desktop integration

Example usage:

```jsonc
"Protocols": {
  "Cli": {
    "Scheme": "pgedge",
    "Target": "cli",
    "OpenVerb": "open",
    "RunVerb": "run",
    "JobVerb": "job"
  },

  "Acc": {
    "Scheme": "pgedge",
    "Target": "acc",
    "OpenVerb": "open"
  }
}