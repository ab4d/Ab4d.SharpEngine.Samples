using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Wpf.Common;

public static class LogHelper
{
    // Setup SharpEngine logging
    // In case of problems and then please send the log text with the description of the problem
    public static void SetupSharpEngineLogger(bool enableFullLogging, string? logFileName = null)
    {
        // The alpha and beta version are compiled with release build options but support full logging.
        // This means that it is possible to get Trace level log messages
        // (production version will have only Warning and Error logging compiled into the assembly).

        // When you have some problems, then please enable Trace level logging and writing log messages to a file or debug output.
        // To do this please find the existing code that sets up logging an change it to:

        // Remove any log listener that may be registered by some other demo
        SharpEngine.Utilities.Log.RemoveAllLogListeners();

        if (enableFullLogging)
        {
            SharpEngine.Utilities.Log.LogLevel = LogLevels.Trace;
            SharpEngine.Utilities.Log.WriteSimplifiedLogMessage = false; // write full log messages timestamp, thread id and other details

            // Use one of the following:

            // Write log messages to output window (for example Visual Studio Debug window):
            if (logFileName == null)
            {
                Log.IsLoggingToDebugOutput = true;
            }
            else
            {
                Log.IsLoggingToDebugOutput = false;
                Log.LogFileName = logFileName;
            }

            //// Write to local StringBuilder:
            //// First create a new StringBuilder field:
            //private System.Text.StringBuilder _logStringBuilder;
            //// Then call AddLogListener:
            //Ab4d.SharpEngine.Utilities.Log.AddLogListener((logLevel, message) => _logStringBuilder.AppendLine(message));
        }
        else
        {
            // Setup minimal logging (write warnings and error to output window)
            SharpEngine.Utilities.Log.LogLevel = LogLevels.Warn;        // Log Warnings and Errors
            SharpEngine.Utilities.Log.WriteSimplifiedLogMessage = true; // write log messages without timestamp, thread id and other details
            SharpEngine.Utilities.Log.IsLoggingToDebugOutput = true;    // write log messages to output window
        }
    }
}