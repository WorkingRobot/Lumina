using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Lumina;

/// <summary>
/// Logger extensibility hook - used to capture log messages from lumina in consumers
/// </summary>
public interface ILogger
{
    public void Verbose(
        string message,
        [CallerFilePath] string sourceFilePath = ""
    );

    public void Debug(
        string message,
        [CallerFilePath] string sourceFilePath = ""
    );

    public void Information(
        string message,
        [CallerFilePath] string sourceFilePath = ""
    );

    public void Warning(
        string message,
        [CallerFilePath] string sourceFilePath = ""
    );

    public void Error(
        string message,
        [CallerFilePath] string sourceFilePath = ""
    );

    public void Fatal(
        string message,
        [CallerFilePath] string sourceFilePath = ""
    );
}

public sealed class ConsoleLogger : ILogger
{
    private static string GetSource( string sourceFilePath ) => Path.GetFileNameWithoutExtension( sourceFilePath );

    public void Debug( string message, string sourceFilePath ) =>
        Console.WriteLine( $"[DEBUG] {GetSource(sourceFilePath)} - {message}" );

    public void Error( string message, string sourceFilePath ) =>
        Console.WriteLine( $"[ERROR] {GetSource(sourceFilePath)} - {message}" );

    public void Fatal( string message, string sourceFilePath ) =>
        Console.Error.WriteLine( $"[FATAL] {GetSource(sourceFilePath)} - {message}" );

    public void Information( string message, string sourceFilePath ) =>
        Console.WriteLine( $"[INFO] {GetSource(sourceFilePath)} - {message}" );

    public void Verbose( string message, string sourceFilePath ) =>
        Console.WriteLine( $"[VERB] {GetSource(sourceFilePath)} - {message}" );

    public void Warning( string message, string sourceFilePath ) =>
        Console.Error.WriteLine( $"[WARN] {GetSource(sourceFilePath)} - {message}" );
}