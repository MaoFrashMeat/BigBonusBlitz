import http.server
import socketserver
import urllib.parse

class Handler(http.server.SimpleHTTPRequestHandler):
    def do_GET(self):
        query = urllib.parse.urlparse(self.path).query
        if 'error=' in query:
            msg = urllib.parse.unquote(query.split('error=')[1])
            with open('error.log', 'a', encoding='utf-8') as f:
                f.write(msg + '\n')
            print('Logged error:', msg)
        
        # Add CORS headers so browser fetch doesn't fail
        self.send_response(200)
        self.send_header('Access-Control-Allow-Origin', '*')
        self.end_headers()

with socketserver.TCPServer(('', 9999), Handler) as httpd:
    print("Serving on port 9999")
    httpd.serve_forever()
