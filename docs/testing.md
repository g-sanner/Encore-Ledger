# COBOL Report Testing Checklist

This checklist verifies both the COBOL execution path and the C# fallback path.

## 1 Build the COBOL executable

From MSYS2 UCRT64:

```bash
cd /c/Users/grace/repos/Encore-Ledger/cobol
cobc -x -o reportgen.exe reportgen.cbl
ls -l reportgen.exe
```

Expected: `reportgen.exe` exists and has a recent timestamp.

## 2 Configure EncoreLedger to use COBOL first

In `EncoreLedger/appsettings.Development.json`:

- `"UseCobolForReporting": true`
- `"CobolExecutablePath": "cobol\\reportgen.exe"` (relative path recommended for portability)

## 3 Run the web app

From PowerShell:

```powershell
cd c:\Users\grace\repos\Encore-Ledger\EncoreLedger
dotnet run
```

## 4 Test the COBOL success path

In the app:

1. Go to Reports
2. Generate a report for a date range with transactions

Expected:

- UI success message includes: `via COBOL`
- Logs include: `Report generation completed via COBOL.`

## 5 Test the C# fallback path

Temporarily break the executable path in `appsettings.Development.json`, for example:

- `"CobolExecutablePath": "c:\\Users\\grace\\repos\\Encore-Ledger\\cobol\\missing.exe"`

Generate another report.

Expected:

- UI success message includes: `via C# (fallback)`
- Logs include either:
  - `COBOL reporting is enabled but no executable was found ... using C#.`, or
  - `COBOL report generation failed; falling back to C#.`
- Logs include: `Report generation completed via C# fallback.`
- If fallback reason includes exit code `-1073741515` (`0xC0000135`), required runtime DLLs are missing; copy MSYS2 runtime DLLs next to `reportgen.exe` or add `C:\\msys64\\ucrt64\\bin` to PATH.

Restore the real executable path afterward.

## 6 Data parity check (recommended)

Run one report with COBOL enabled and one with COBOL disabled (`UseCobolForReporting: false`) for the same period.

Compare:

- Total Income
- Total Expenses
- Net Balance
- Category rows

Expected: values match.
