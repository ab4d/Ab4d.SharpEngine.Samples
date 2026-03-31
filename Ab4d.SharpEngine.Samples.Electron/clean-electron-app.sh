echo "This script clears the compiled files so when start-electron-app script is started agian, it recompiles all the projects."

rm -rf wwwroot
rm -rf ../Ab4d.SharpEngine.Samples.BlazorWebAssembly/bin/Release/net10.0/publish/wwwroot

echo "wwwroot files deleted. Start start-electron-app.bat script again to rebuild the project."
read -p "Press enter to continue"