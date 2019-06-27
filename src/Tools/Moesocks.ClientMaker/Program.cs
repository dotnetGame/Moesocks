using System;
using System.Diagnostics;
using System.IO;

namespace Moesocks.ClientMaker
{
    class Program
    {
        struct MakeContext
        {
            public string ClientName;
            public string CAFileNamePrefix;
            public string OutDir;
            public int Days;
        }

        static void Main(string[] args)
        {
            var context = new MakeContext
            {
                OutDir = "clients",
                Days = 30
            };
            ArgumentSyntax.Parse(args, syntax =>
            {
                syntax.ErrorOnUnexpectedArguments = true;

                syntax.DefineOption("n|name", ref context.ClientName, true, "client username.");
                syntax.DefineOption("c", ref context.CAFileNamePrefix, true, "CA filename prefix, there should be {ca}.pem.crt, {ca}.pem.key.unsecure, {ca}.srl");
                syntax.DefineOption("d|days", ref context.Days, "client cert valid days.");
                syntax.DefineOption("o|outDir", ref context.OutDir, "Output directory.");
            });

            MakeClient(ref context);
        }

        private const string _openssl = "openssl";

        private static void MakeClient(ref MakeContext context)
        {
            var targetDir = Path.Combine(context.OutDir, context.ClientName);
            Directory.CreateDirectory(targetDir);
            var keyFileName = Path.Combine(targetDir, $"{context.ClientName}.pem.key");
            var reqFileName = Path.Combine(targetDir, $"{context.ClientName}.pem.csr");
            var crtFileName = Path.Combine(targetDir, $"{context.ClientName}.pem.crt");
            var p12FileName = Path.Combine(targetDir, $"{context.ClientName}.p12");

            Call(_openssl, $"genrsa -out {keyFileName} 1024");
            Call(_openssl, $"req -new -key {keyFileName} -out {reqFileName} -subj /CN={context.ClientName}/");
            Call(_openssl, $"x509 -req -days {context.Days} -in {reqFileName} -CA {context.CAFileNamePrefix}.pem.crt -CAkey {context.CAFileNamePrefix}.pem.key.unsecure -CAserial {context.CAFileNamePrefix}.srl -out {crtFileName}");
            Call(_openssl, $"pkcs12 -export -inkey {keyFileName} -in {crtFileName} -out {p12FileName}");
            Console.WriteLine("Done.");
        }

        private static void Call(string fileName, string arguments)
        {
            using (var process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    CreateNoWindow = false,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            })
            {
                process.Start();
                process.OutputDataReceived += Process_OutputDataReceived;
                process.ErrorDataReceived += Process_ErrorDataReceived;
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();
                if (process.ExitCode != 0)
                    throw new InvalidOperationException();
            }
        }

        private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }

        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }
    }
}