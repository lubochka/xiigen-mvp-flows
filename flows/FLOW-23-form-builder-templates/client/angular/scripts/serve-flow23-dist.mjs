import express from 'express';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(__dirname, '..');
const dist = path.join(root, 'dist', 'angular', 'browser');
const app = express();
const port = Number(process.env.PORT ?? process.env.XIIGEN_flow23_PORT ?? 4321);

app.use(express.static(dist));
app.get(/.*/, (_req, res) => res.sendFile(path.join(dist, 'index.html')));
app.listen(port, '127.0.0.1', () => console.log(`FLOW-23 Angular evidence server listening on http://127.0.0.1:${port}`));
