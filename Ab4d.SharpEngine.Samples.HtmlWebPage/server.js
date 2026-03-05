const path = require('path');
const fs = require('fs');

try {
    require('express')
} catch {
    console.log("Cannot start web sever because express package is missing. Install it by 'npm i express'")
    process.exit(1)
}

const express = require('express');


const app = express();
const PORT = 8000;
const WWWROOT = path.join(__dirname, 'wwwroot');

app.use((req, res, next) => {
  // Check if the requets is .wasm or .js file and if the client accepts Brotli and the .br file exists
  if ((req.url.endsWith('.wasm') || req.url.endsWith('.js')) &&
       req.acceptsEncodings('br') && 
       fs.existsSync(path.join(WWWROOT, req.url + '.br'))) {
    
    req.url = req.url + '.br'; // Modify the request URL to point to the .br file
    res.set('Content-Encoding', 'br'); // Set the response header
    
    if (req.url.endsWith('.js.br'))
		res.set('Content-Type', 'application/javascript; charset=UTF-8');
	else
		res.set('Content-Type', 'application/wasm');
  }
  next(); // Continue to the next middleware (express.static)
});

app.use(express.static(WWWROOT, { etag: true }));

app.listen(PORT, () => {
  console.log(`Server running at http://localhost:${PORT}`);
});
