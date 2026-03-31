echo This script clears the compiled files so when start-electron-app script is started agian, it recompiles all the projects.

rd wwwroot /s /q
rd ..\Ab4d.SharpEngine.Samples.BlazorWebAssembly\bin\Release\net10.0\publish\wwwroot /s /q

echo wwwroot files deleted. Start start-electron-app.bat script again to rebuild the project.
pause