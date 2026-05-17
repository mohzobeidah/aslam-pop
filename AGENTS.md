# AGENTS.md — Camp Registration Form (Google Apps Script)

## Overview
Google Apps Script web app for camp family registration. Two files:
- `Code.gs` — server-side: serves HTML, writes to Google Sheets, uploads PDFs to Drive
- `Index.html` — client-side: Arabic RTL form, Tailwind CSS via CDN

## How to run / deploy
- **No local dev server.** Deploy via Google Apps Script editor (Extensions > Apps Script > Deploy > New deployment).
- Test by opening the deployed web app URL or using the GAS editor's "Run" on `doGet()`.
- There is no package manager, build step, test framework, linter, or typechecker.

## Architecture
- `doGet()` in `Code.gs` serves `Index.html` as the web app entrypoint.
- Client calls server functions via `google.script.run` (e.g. `google.script.run.processForm(data)`).
- `processForm(data)` appends a row to sheet named `البيانات` in the bound spreadsheet.
- `uploadFileToDrive(base64Data, fileName)` saves PDFs to a Drive folder named `التقارير الطبية`.
- Record ID is the first 8 chars of `Utilities.getUuid()`.

## Key conventions
- All UI text is in Arabic; the form uses `dir="rtl"` and the Cairo font.
- Dark theme (`#121212` background, `#d4af37` gold accent).
- Dynamic family members are added client-side via `addMember()` which clones a `.member-card` template.
- Female members get extra fields: pregnancy month, nursing status + baby details.
- Health fields, tent fields, bathroom fields are toggled via radio buttons + `hidden` class.
- Checkboxes for diseases, tent types, and disabilities are collected by name attribute and joined with `'، '` (Arabic comma + space).

## Data flow
1. User fills form → `handleSubmit(e)` collects data into a plain object.
2. Files are uploaded via `uploadFileAsync(file)` (wraps `google.script.run` in a Promise for `await`-based sequencing):
   - Head of family medical report → `headReportLink`, ID image → `headIdImageLink`
   - Per member: medical report → `reportUrl`, ID image → `idImageUrl`
3. All files are base64-encoded and uploaded to Drive via `uploadFileToDrive`.
4. `processForm(data)` sends everything as a single row to Google Sheets.
5. On success, the page shows a record ID and reloads after 3 seconds.

## Limitations / gotchas
- No input validation beyond HTML `required` attributes.
- No error handling on the server side (no try/catch).
- The `getSheet_()` function appends a header row every time the sheet is missing (not ideal for production).
- The form does not sanitize user input before writing to sheets.
- `uploadFileToDrive` in `Code.gs` hardcodes `MimeType.PDF` — ID images uploaded as images will still be stored as PDF.
- Member health radio buttons use index-based naming (`m_health_${idx+1}`) which can break if members are removed.
