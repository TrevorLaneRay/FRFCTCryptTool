using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using GCLILib.Core;
using GCLILib.Util;
using static GCLILib.Util.ConsoleTools;

namespace FRFCTCryptTool;

internal class Program
{
    private static byte[] Key = Encoding.UTF8.GetBytes("tN6f3&pxGHqR");

    public static ConsoleOption[] ConsoleOptions =
    {
        new()
        {
            Name = "Key",
            ShortOp = "-k",
            LongOp = "--key",
            Description =
                "Specifies the key to be used in the encryption/decryption.",
            HasArg = true,
            Flag = Options.Key,
            Func = delegate(string[] subArgs)
            {
                Key = Encoding.UTF8.GetBytes(string.Join(" ", subArgs));
            }
        }
    };

    public static string AssemblyPath = string.Empty;
    private static Options options;

    private static void Main(string[] args)
    {
        var codeBase = Assembly.GetExecutingAssembly().CodeBase;
        var uri = new UriBuilder(codeBase);
        AssemblyPath = Path.GetFullPath(Uri.UnescapeDataString(uri.Path));

        if (ShouldGetUsage(args))
        {
            ShowUsage();
            return;
        }

        options = ProcessOptions<Options>(args, ConsoleOptions);

        var filePath = args[0];
        if (args.Length > 1 && !string.IsNullOrWhiteSpace(args[1])) Key = Encoding.UTF8.GetBytes(args[1]);
        var isURL = Uri.TryCreate(filePath, UriKind.Absolute, out var fileURI)
                    && (fileURI.Scheme == Uri.UriSchemeHttp || fileURI.Scheme == Uri.UriSchemeHttps);

        if (!isURL)
        {
            filePath = Path.GetFullPath(filePath);
            var ext = Path.GetExtension(filePath).ToUpper();
            try
            {
                var attr = File.GetAttributes(filePath);
            }
            catch (Exception ex)
            {
                ErrorMessage(ex.Message);
                return;
            }

            var fileBytes = File.ReadAllBytes(filePath);
            switch (ext)
            {
                case ".CT":
                    File.WriteAllBytes(
                        Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath)),
                        Encrypt(fileBytes));
                    return;
                case "":
                    File.WriteAllBytes(
                        Path.Combine(Path.GetDirectoryName(filePath),
                            Path.GetFileNameWithoutExtension(filePath) + ".CT"),
                        Decrypt(fileBytes));
                    return;
                default:
                    WarningMessage("Inappropriate file extension.");
                    return;
            }
        }

        if (fileURI.AbsoluteUri.Contains(@"://fearlessrevolution.com/download/file.php?id="))
        {
            var fileName = HttpUtility.UrlDecode(GetFilenameFromWebServer(fileURI.AbsoluteUri));
            while (fileName.Length > 3 && fileName.Substring(0, 3) == "1FR") fileName = fileName.Substring(3);
            Uri.TryCreate(fileURI.AbsoluteUri.Replace(@"/download/file.php?id=", @"/app/getcheattable.php?frftabid="),
                UriKind.Absolute, out fileURI);
            try
            {
                var fileBytes = Decrypt(new WebClient().DownloadData(fileURI));
                File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), fileName),
                    fileBytes);
            }
            catch (Exception ex)
            {
                ErrorMessage(ex.Message);
            }

            return;
        }

        InfoMessage("File or URL does not match any qualification.");
    }

    private static byte[] Decrypt(byte[] fileBytes)
    {
        var decryptedBytes = new byte[fileBytes.Length];
        for (var i = 0; i < fileBytes.Length; i++) decryptedBytes[i] = (byte) (fileBytes[i] ^ Key[i % Key.Length]);

        return Convert.FromBase64String(Encoding.UTF8.GetString(decryptedBytes));
    }

    private static byte[] Encrypt(byte[] fileBytes)
    {
        var b64 = Encoding.UTF8.GetBytes(Convert.ToBase64String(fileBytes));
        var encryptedBytes = new byte[b64.Length];
        for (var i = 0; i < fileBytes.Length; i++) encryptedBytes[i] = (byte) (b64[i] ^ Key[i % Key.Length]);

        return encryptedBytes;
    }

    public static string GetFilenameFromWebServer(string url)
    {
        var result = "";

        var req = WebRequest.Create(url);
        req.Method = "HEAD";
        using (var resp = req.GetResponse())
        {
            if (!string.IsNullOrEmpty(resp.Headers["Content-Disposition"]))
            {
                result = resp.Headers["Content-Disposition"];
                var match = Regex.Match(result, WildCardToRegular("filename^=^'"));
                if (match.Success) result = result.Substring(match.Index + match.Length).Replace("\"", "");
            }
        }

        return result;
    }

    private static string WildCardToRegular(string value)
    {
        return "^" + string.Join(".*",
            Regex.Escape(value).Split(new[] {"\\^"}, StringSplitOptions.RemoveEmptyEntries).Select(s => '[' + s + ']'));
    }

    private static void ShowUsage()
    {
        ConsoleTools.ShowUsage(
            $"Usage: {Path.GetFileName(AssemblyPath)} <file path/url> [options...]", ConsoleOptions);
    }

    [Flags]
    private enum Options
    {
        Key = 0x1
    }
}