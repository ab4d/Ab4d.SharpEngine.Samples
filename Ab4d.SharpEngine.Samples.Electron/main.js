const { app, BrowserWindow } = require('electron/main')

const createWindow = () => {
  const win = new BrowserWindow({
    width: 1400,
    height: 800,
    icon: 'wwwroot/favicon.png'
  });

  win.loadFile('wwwroot/index.html');
}

app.whenReady().then(() => {
 
  createWindow()

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow()
    }
  })
})

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit()
  }
})