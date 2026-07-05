# VSIP Logging — discover Visual Studio command GUIDs & IDs

`VSIP Logging` is a built-in Visual Studio diagnostic. When enabled, **Ctrl+Shift+clicking** any menu, menu item, or command pops up a dialog showing that element's **command set GUID**, **command ID**, type, and canonical name.

It's the fastest way for an extension author to find the (often undocumented) GUID/ID pair needed to anchor a command under an existing VS menu in a `.vsct` file.

## Enable

Set a `DWORD` value `EnableVSIPLogging = 1` under:

```
HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\<version>\General
```

Then **fully restart Visual Studio**.

Machines often have several VS version keys (`18.0`, `18.0_<hash>`, `18.4`, `20.0`, …) and it's not always obvious which one the running instance reads — the hash-suffixed one is the actual per-instance config. Easiest is to set it on **all** of them at once, in a normal PowerShell window:

```powershell
Get-ChildItem 'HKCU:\Software\Microsoft\VisualStudio' -ErrorAction SilentlyContinue |
  Where-Object { $_.PSChildName -match '^\d+\.\d' } |
  ForEach-Object {
    $g = "$($_.PSPath)\General"
    if (-not (Test-Path $g)) { New-Item -Path $g -Force | Out-Null }
    Set-ItemProperty -Path $g -Name EnableVSIPLogging -Value 1 -Type DWord
    "set: $($_.PSChildName)"
  }
```

## Use

1. Hold **Ctrl+Shift** and click a menu item (release the mouse **over** the item).
2. A dialog appears with the command/menu data.

Verify it's actually on by first Ctrl+Shift+clicking a known command such as **Tools → Options** — you should get a dialog with `NameLoc = &Tools`.

## Reading the dialog

| Field | Meaning |
|-------|---------|
| **Guid** | The command set GUID — use as the `guid` of your `<Parent>` / `GuidSymbol`. |
| GuidID | VSIP-internal index — ignore. |
| **CmdID** | The command/menu ID (shown in **decimal** — convert to hex for `.vsct`, e.g. `61504` → `0xF040`). |
| **Type** | `0x100` = menu, `0x400` = context menu, `0x100`-ish button, etc. |
| Flags | Command flags — usually not needed. |
| **NameLoc / Canonical name** | Human-readable name. For a **menu** this names the container itself. |

> **Key trick:** if the dialog's `NameLoc` reads like *"… Context Menu"* (or `Type` is a menu), you've captured the **container menu's** id — which is exactly what you parent a `<Group>` to. Clicking a plain command instead gives you that *command's* id, not its container.

## Disable

Set `EnableVSIPLogging` back to `0` (or delete the value) and restart VS.

## Gotchas

- **Test in the normal instance, not the experimental (`Exp`) one.** Built-in menus exist in every instance, and the exp instance uses a *private registry* (`privateregistry.bin`) that does **not** read from `HKCU` — so the key above won't take effect there.
- **Fully exit `devenv.exe`** (check Task Manager) before relaunching, or the setting may not load.
- **Modern WPF flyout menus** (e.g. the status-bar branch picker) may report a command GUID via VSIP, but are still **not `.vsct`-extensible** — the menu is registered in code, not the command table. VSIP tells you the GUID exists; it doesn't guarantee you can place items there.
- CmdID is decimal in the dialog; `.vsct` uses hex.

## Reference — IDs discovered for this project

Visual Studio's built-in **Git command set** `{57735D06-C920-4415-A2E0-7D6E6FBDFA99}`:

| ID | Element |
|----|---------|
| `0xF000` | Git top-level (root) menu — anchor for the Git menu items |
| `0xF040` | Git History commit context menu (Git Repository window) |
| `0xf032` | Branches context menu |
| `0x1005` | `Git.ShowHistory` |
| `0x1026` | Open |
| `0x1027` | Share |

Status-bar **branch flyout** menu — command set `{24FC9963-73A7-4B4D-AAA2-87D424E87BC7}` (e.g. `View.Branch` = `1280`). Runtime-registered; **not** `.vsct`-extensible.
