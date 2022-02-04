using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GCLILib.Common.Enum;
using GCLILib.Core;

namespace GCLILib.Util;

public static class ConsoleTools
{
    public static bool ShouldGetUsage(string[] args)
    {
        return args.Length == 0 ||
               args.Length > 0 && (args[0] == "-h" || args[0] == "--help") ||
               args.Length == 1 && string.IsNullOrWhiteSpace(args[0]);
    }

    public static void ClearConsoleLine(int lines = 1, bool padLine = false)
    {
        var currentLineCursor = Console.CursorTop;
        for (var i = 0; i < lines; i++)
        {
            Console.SetCursorPosition(0, Console.CursorTop - i);
            Console.Write(new string(' ', Console.WindowWidth));
        }

        Console.SetCursorPosition(0, currentLineCursor - (lines - 1));
        if (padLine) Console.WriteLine();
    }

    private static void ConsoleMessage(string message, ConsoleColor color, string messageType = null)
    {
        Console.ForegroundColor = color;
        Console.WriteLine((string.IsNullOrWhiteSpace(messageType) ? string.Empty : $"[{messageType}] ") +
                          $"{message}");
        Console.ForegroundColor = ConsoleColor.White;
    }

    public static void CompleteMessage(string message = "Complete!")
    {
        ConsoleMessage(message, ConsoleColor.Green);
    }

    public static void SubtleMessage(string message)
    {
        ConsoleMessage(message, ConsoleColor.DarkGray);
    }

    public static void InfoMessage(string message)
    {
        ConsoleMessage(message, ConsoleColor.Blue, "INFO");
    }

    public static void WarningMessage(string message)
    {
        ConsoleMessage(message, ConsoleColor.DarkYellow, "WARNING");
    }

    public static void ErrorMessage(string message)
    {
        ConsoleMessage(message, ConsoleColor.Red, "ERROR");
    }

    public static void ShowUsage(string usageExample, ConsoleOption[] consoleOptions)
    {
        Console.WriteLine(usageExample);

        if (consoleOptions.Length > 0)
        {
            var shortOpMaxLength =
                consoleOptions.Select(co => co.ShortOp).OrderByDescending(s => s.Length).First().Length;

            Console.WriteLine("Options:");
            Console.WriteLine(PadElementsInLines(
                consoleOptions.Select(co => new[] {co.ShortOp, co.LongOp, co.Description}).ToArray(),
                shortOpMaxLength, Console.WindowWidth));
        }
    }

    public static void ShowSpecialOptions(string optionsType, ConsoleOption[] consoleOptions)
    {
        if (consoleOptions.Length > 0)
        {
            var shortOpMaxLength =
                consoleOptions.Select(co => co.ShortOp).OrderByDescending(s => s.Length).First().Length;

            Console.WriteLine(optionsType + ":");
            Console.WriteLine(PadElementsInLines(
                consoleOptions.Select(co => new[] {co.ShortOp, co.LongOp, co.Description}).ToArray(),
                shortOpMaxLength, Console.WindowWidth));
        }
    }

    public static void Pause(bool skipCondition = false, string message = "\rPress Any Key to exit...")
    {
        if (skipCondition)
            return;

        Console.Write(message);
        Console.ReadKey();
        ClearConsoleLine(2, true);
    }

    public static T ProcessOptions<T>(string[] args, ConsoleOption[] consoleOptions) where T : Enum
    {
        var options = 0;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.First() != '-')
                continue;

            foreach (var co in consoleOptions)
                if (arg == co.ShortOp || arg == co.LongOp)
                {
                    options |= (int) co.Flag;
                    if (co.HasArg)
                    {
                        var subArgsList = new List<string>();
                        var lastArg = string.Empty;
                        for (var j = i; j < args.Length - 1; j++)
                        {
                            var subArg = args[j + 1];

                            if (subArg.First() == '-')
                                break;

                            if (string.IsNullOrWhiteSpace(lastArg) || subArg.ToLower() != lastArg.ToLower())
                                subArgsList.Add(subArg);
                            i++;
                        }

                        co.SpecialObject = subArgsList.ToArray();
                    }
                }
        }

        foreach (var co in consoleOptions)
        {
            if (co.Flag == null)
                continue;

            if (co.HasArg && (options & (int) co.Flag) != 0 && co.Func != null)
            {
                var subArgs = (string[]) co.SpecialObject;
                co.Func(subArgs);
            }
        }

        return (T) (object) options;
    }

    public static bool ConfirmPrompt(string message)
    {
        var firstTime = true;

#pragma warning disable IDE0059
        var result = false;
#pragma warning restore IDE0059

        while (true)
        {
            if (firstTime)
            {
                InfoMessage(message + " Y/N/A");
                firstTime = false;
            }

            var overwrite = Convert.ToString(Console.ReadKey().KeyChar);
            if (overwrite.ToUpper().Equals("Y"))
            {
                result = true;
                break;
            }

            if (overwrite.ToUpper().Equals("N"))
            {
                result = false;
                break;
            }
        }

        ClearConsoleLine(2, true);
        return result;
    }

    public static bool OverwritePrompt(string file, ref OverwriteMode overwriteMode)
    {
        switch (overwriteMode)
        {
            case OverwriteMode.Overwrite:
                return true;
            case OverwriteMode.Skip:
                return false;
        }

        var firstTime = true;

#pragma warning disable IDE0059
        var result = false;
#pragma warning restore IDE0059

        while (true)
        {
            if (firstTime)
            {
                InfoMessage($"The file: {file} already exists. Do you want to overwrite it? Y/N/A");
                firstTime = false;
            }

            var overwrite = Convert.ToString(Console.ReadKey().KeyChar);
            if (overwrite.ToUpper().Equals("Y"))
            {
                result = true;
                break;
            }

            if (overwrite.ToUpper().Equals("N"))
            {
                result = false;
                break;
            }

            if (overwrite.ToUpper().Equals("A"))
            {
                result = true;
                overwriteMode = OverwriteMode.Overwrite;
                break;
            }
        }

        ClearConsoleLine(2, true);
        return result;
    }

    public static string PadElementsInLines(string[][] lines, int padding = 1, int maxLength = 0)
    {
        var numElements = lines[0].Length;
        var maxValues = new int[numElements];
        for (var i = 0; i < numElements; i++) maxValues[i] = lines.Max(x => x[i].Length) + padding;
        var sb = new StringBuilder();
        var isFirst = true;
        foreach (var line in lines)
        {
            if (!isFirst) sb.AppendLine();
            isFirst = false;
            var length = 0;
            var str = string.Empty;
            for (var i = 0; i < line.Length - 1; i++)
                str += line[i].PadRight(maxValues[i]).Replace(Environment.NewLine, " ");
            var pad = str.Length;
            str += line[line.Length - 1].Replace(Environment.NewLine, " ");
            length += str.Length;
            while (length > maxLength)
            {
                var len = Math.Min(maxLength - 1, str.Length);
                var wrapAt = Math.Max(str.LastIndexOf('|', len), str.LastIndexOf(' ', len));
                sb.Append(str.Substring(0, wrapAt + 1));
                sb.AppendLine();
                str = str.Remove(0, wrapAt + 1);
                str = str.PadLeft(str.Length + pad, ' ');
                length = str.Length;
            }

            sb.Append(str);
        }

        return sb.ToString();
    }
}