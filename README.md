# Polycomp Project 1 - Sappi Display (WinForms)

## Run

```bash
dotnet run --project .\SappiDisplayWinForms\SappiDisplayWinForms.csproj
```

## Requirement Mapping

- **Build screenshot as UI (WinForms):** Implemented in `SappiDisplayWinForms/Form1.cs` with PM1/PM2 layout and display labels.
- **Text labels as-is:** Includes `PAPER - SAFE PRODUCTION`, `PM1`, `PM2`, `TARGET`, `ACTUAL`, `NET PRODUCTION`, `DOWNTIME`, `SHRINKAGE`, `TONS TO MAKE BUDGET`, `SAFETY`, `INJURY FREE DAYS`, `EFFLUENT`, plus row units.
- **LED signage sections as text input:** Target/Actual fields are editable LED-style text inputs.
- **3 input text colours via dropdown:** Each LED input has `Red`, `Green`, `Yellow` selection.
- **Numeric validation:** Integer-only and decimal (max 2 decimals) modes are enforced for relevant fields.
- **Status indicators with 3 colours:** Right-side indicators use:
  - Green when `actual > target`
  - Yellow when `target > actual > 0.8 * target`
  - Red when `actual < 0.8 * target`
- **Ignore bottom time/date and red ticker:** Not implemented intentionally, per requirement.
- **Parse all input on any change:** Every input or colour change triggers recompute.
- **Combine text in reading order:** Left-to-right, top-to-bottom.
- **Prefix by selected colour:** `FA` (red), `FB` (green), `FC` (yellow).
- **Convert combined output to hex:** Displayed in the bottom output panel.

## Notes

- The app is built to satisfy functional requirements and resemble the provided display layout.
- The bottom output panel is resizable with a splitter for easier demonstration recording.