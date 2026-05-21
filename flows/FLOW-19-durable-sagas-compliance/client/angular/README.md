# FLOW-19 durable-sagas-compliance Publishing & Distribution Angular Shell

This Angular application renders the durable-sagas-compliance questionnaire, matching, and personalization evidence surfaces for the .NET/Angular portability gate.

## Commands

```powershell
npm test -- --watch=false
npm run build
npm run serve:evidence
npm run capture:evidence
```

The evidence server listens on `http://127.0.0.1:4306`.
