import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';

const root = path.resolve('dist/angular/browser');
const port = Number(process.env.flow46_STATIC_PORT ?? 4342);
const mime = new Map([['.html', 'text/html'], ['.js', 'text/javascript'], ['.css', 'text/css'], ['.svg', 'image/svg+xml'], ['.json', 'application/json']]);

http.createServer((request, response) => {
  const url = new URL(request.url ?? '/', `http://127.0.0.1:${port}`);
  const pathname = url.pathname === '/' ? '/index.html' : url.pathname;
  const target = path.join(root, pathname);
  const safe = target.startsWith(root);
  const file = safe && fs.existsSync(target) && fs.statSync(target).isFile() ? target : path.join(root, 'index.html');
  response.writeHead(200, { 'content-type': mime.get(path.extname(file)) ?? 'application/octet-stream' });
  fs.createReadStream(file).pipe(response);
}).listen(port, '127.0.0.1', () => console.log(`flow46 static server http://127.0.0.1:${port}`));
