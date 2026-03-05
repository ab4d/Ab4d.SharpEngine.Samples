var currentDirectory = Environment.CurrentDirectory;

if (currentDirectory.Contains(' '))
{
    WriteError("Current path contains spaces and this may prevent compiling the WebAssembly because the wasm tools may not work with folders names with spaces.");
    if (!AskToContinue())
        return 1;
}

if (currentDirectory.Length > 120)
{
    WriteError($"Current path is very long ({currentDirectory.Length}). This may lead to compile errors because the max path length is exceeded. Consider moving the samples to a folder with shorted path.");
    if (!AskToContinue())
        return 1;
}

return 0; // Current path correct


bool AskToContinue()
{
    Console.WriteLine("Do you want to continue with compiling (Y / N)?");
    var key = Console.ReadKey();
    return key.Key == ConsoleKey.Y;
}

void WriteError(string message)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(message);
    Console.ResetColor();
}

