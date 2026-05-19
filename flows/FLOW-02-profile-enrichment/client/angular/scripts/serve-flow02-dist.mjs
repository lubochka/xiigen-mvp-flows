import { createServer } from 'node:http';
import { createReadStream, existsSync } from 'node:fs';
import { extname, join, resolve } from 'node:path';

const port = Number(process.env.PORT ?? '4302');
const root = resolve(process.cwd(), 'dist', 'angular', 'browser');
const mimeTypes = new Map([
  ['.html', 'text/html; charset=utf-8'],
  ['.js', 'text/javascript; charset=utf-8'],
  ['.css', 'text/css; charset=utf-8'],
  ['.ico', 'image/x-icon'],
  ['.png', 'image/png'],
  ['.svg', 'image/svg+xml'],
]);

createServer((request, response) => {
  const url = new URL(request.url ?? '/', `http://127.0.0.1:${port}`);
  const requestedPath = url.pathname === '/' ? '/index.html' : url.pathname;
  let filePath = join(root, requestedPath);
  if (!existsSync(filePath)) {
    filePath = join(root, 'index.html');
  }

  response.setHeader('Content-Type', mimeTypes.get(extname(filePath)) ?? 'application/octet-stream');
  createReadStream(filePath)
    .on('error', () => {
      response.statusCode = 500;
      response.end('static server error');
    })
    .pipe(response);
}).listen(port, '127.0.0.1', () => {
  console.log(`FLOW-02 static server listening on http://127.0.0.1:${port}`);
});
