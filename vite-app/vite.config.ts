import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import type { Plugin } from 'vite'
import fs from 'fs'
import path from 'path'

function unityBrotliPlugin(): Plugin {
  return {
    name: 'unity-brotli',
    configureServer(server) {
      server.middlewares.use((req, res, next) => {
        const url = (req.url ?? '').split('?')[0];
        if (!url.endsWith('.br')) return next();

        const filePath = path.join(process.cwd(), 'public', url);
        if (!fs.existsSync(filePath)) return next();

        let contentType = 'application/octet-stream';
        if (url.endsWith('.wasm.br'))      contentType = 'application/wasm';
        else if (url.endsWith('.js.br'))   contentType = 'application/javascript';

        res.setHeader('Content-Encoding', 'br');
        res.setHeader('Content-Type', contentType);
        res.setHeader('Access-Control-Allow-Origin', '*');
        fs.createReadStream(filePath).pipe(res);
      });
    },
  };
}

export default defineConfig({
  plugins: [react(), unityBrotliPlugin()],
  server: {
    port: 5173,
    host: true,
    cors: true,
    headers: {
      'Cross-Origin-Embedder-Policy': 'require-corp',
      'Cross-Origin-Opener-Policy': 'same-origin',
    },
  },
})
