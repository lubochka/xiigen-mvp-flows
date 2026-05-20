# FLOW-11 schema-registry-dag Publishing & Distribution Angular Shell

This Angular application renders the schema-registry-dag questionnaire, matching, and personalization evidence surfaces for the .NET/Angular portability gate.

## Commands

```powershell
npm test -- --watch=false
npm run build
npm run serve:evidence
npm run capture:evidence
```

The evidence server listens on `http://127.0.0.1:4306`.
