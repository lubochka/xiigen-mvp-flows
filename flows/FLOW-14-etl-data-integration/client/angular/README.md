# FLOW-14 etl-data-integration Publishing & Distribution Angular Shell

This Angular application renders the etl-data-integration questionnaire, matching, and personalization evidence surfaces for the .NET/Angular portability gate.

## Commands

```powershell
npm test -- --watch=false
npm run build
npm run serve:evidence
npm run capture:evidence
```

The evidence server listens on `http://127.0.0.1:4306`.
