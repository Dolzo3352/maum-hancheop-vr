"""Serve only the public output directory on localhost for visual review."""
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

root = Path(__file__).resolve().parents[1] / 'output'
handler = partial(SimpleHTTPRequestHandler, directory=str(root))
server = ThreadingHTTPServer(('127.0.0.1', 4173), handler)
print('Local preview: http://127.0.0.1:4173', flush=True)
server.serve_forever()
