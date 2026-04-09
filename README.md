# Polycomp Project 1 — Sappi production display (WinForms)

A Windows desktop app that recreates a **“PAPER - SAFE PRODUCTION”** factory-style board (PM1 / PM2) from the provided reference screenshot. It is built for a Polycomp evaluation brief: editable LED-style metrics, validation, status lights, and a live combined hexadecimal output stream.

**Repository:** [github.com/Philadelphia01/Polycomp_Project1](https://github.com/Philadelphia01/Polycomp_Project1)

---

## Languages & technologies

| Item | Details |
|------|---------|
| **Language** | C# |
| **UI** | Windows Forms (.NET) |
| **Runtime** | .NET 10 (`net10.0-windows`) |
| **IDE** | Visual Studio 2022 / VS Code / Rider (any editor with .NET SDK) |

---

## What this project does

The app shows two side-by-side sections (**PM1** and **PM2**) with production metrics (e.g. net production, downtime, shrinkage). Each metric can have **Target** and **Actual** values entered as text. The layout and labels follow the reference display; the bottom date/time and red ticker from the photo are intentionally omitted, as specified in the brief.

---

## Features

- **Board layout** — Title, PM1/PM2 headers, row labels, TARGET / ACTUAL columns, and units (TONS, %, DAYS, M/DAY).
- **LED-style inputs** — Dark input fields with coloured numeric text; each field has a small **colour dropdown** (Red, Green, Yellow).
- **Numeric validation**
  - **Integer-only** fields (e.g. budget tons, injury-free days): digits only.
  - **Decimal** fields: valid numbers with **at most two** digits after the decimal point; paste is sanitised as well as typing.
- **Status indicators** — Circular lights on applicable rows:
  - **Green** when `actual > target`
  - **Yellow** when `target > actual > 0.8 × target`
  - **Red** when `actual < 0.8 × target`
- **Live combined output** — On every change, all LED field values are concatenated **left to right, top to bottom**, each prefixed by a colour token (`FA` = red, `FB` = green, `FC` = yellow), then the whole string is shown as **ASCII hex** in the bottom panel.
- **Resizable output area** — A horizontal splitter lets you drag to give more room to the board or to the hex output (useful for screen recordings).

---

## How to run

**Prerequisites**

- [**.NET 10 SDK**](https://dotnet.microsoft.com/download) (or the SDK version matching `TargetFramework` in `SappiDisplayWinForms.csproj`)
- **Windows** (WinForms requires `net10.0-windows`)

**From the repository root** (`polycomp_Project`):

```powershell
dotnet run --project .\SappiDisplayWinForms\SappiDisplayWinForms.csproj
```

**Alternative** — run from the project folder:

```powershell
cd .\SappiDisplayWinForms
dotnet run
```

**Build only (Release):**

```powershell
dotnet build .\SappiDisplayWinForms\SappiDisplayWinForms.csproj -c Release
```

The executable is produced under `SappiDisplayWinForms\bin\Release\net10.0-windows\` (or `Debug` if you build without `-c Release`).

---

## Project structure

```
polycomp_Project/
├── README.md
├── .gitignore
└── SappiDisplayWinForms/
    ├── SappiDisplayWinForms.csproj
    ├── Program.cs          # Application entry point
    ├── Form1.cs            # Main UI and behaviour
    └── Form1.Designer.cs   # Form shell (size, title)
```

---

## Requirement mapping (brief checklist)

| Requirement | Implementation |
|-------------|----------------|
| UI from screenshot | WinForms layout in `Form1.cs` |
| Labels as static text | Row and column headers as labels |
| LED areas as inputs | `TextBox` per target/actual |
| 3 text colours + dropdown | `ComboBox` per field; `FA`/`FB`/`FC` in output |
| Integer / decimal rules | Per-field modes + `KeyPress` + `TextChanged` sanitisation |
| Status lights (3 colours + rules) | Custom painted control + threshold logic |
| Ignore bottom clock / ticker | Not shown |
| Combine + hex on change | `RecomputeAll()` updates bottom output |

---

## Licence / attribution

Code submitted for Polycomp Project 1. Windows and .NET are trademarks of Microsoft Corporation.
