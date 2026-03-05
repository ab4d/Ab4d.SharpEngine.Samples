import http.server
import socketserver
import os

# NOTE:
# This is a very simple python socketserver that has very limited functionality
# and is much slower than express.js server (started with start-node-express-web-server.bat) 
# or Asp.Net Core web server (started by starting Ab4d.SharpEngine.Samples.AspNetCoreApp project)

PORT = 8000

WEB_DIR = os.path.join(os.path.dirname(__file__), 'wwwroot')
os.chdir(WEB_DIR)  # Change working directory to serve files from wwwroot

Handler = http.server.SimpleHTTPRequestHandler
with socketserver.TCPServer(("", PORT), Handler) as httpd:
    print("Serving from", WEB_DIR, "at port", PORT)
    httpd.serve_forever()