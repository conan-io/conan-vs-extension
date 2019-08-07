using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Conan.VisualStudio
{
    public static class Utils
    {
        private static void AppendLinesFunc(object packedParams)
        {
            var paramsTuple = (Tuple<StreamWriter, StreamReader>)packedParams;
            StreamWriter writer = paramsTuple.Item1;
            StreamReader reader = paramsTuple.Item2;

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                lock (writer)
                {
                    Logger.Log(line);
                    writer.WriteLine(line);
                }
            }
        }

        public static async Task<int> RunProcessAsync(ProcessStartInfo process, StreamWriter logStream)
        {
            string message = $"[Conan.VisualStudio] Calling process '{process.FileName}' " +
                                         $"with arguments '{process.Arguments}'";
            Logger.Log(message);
            await logStream.WriteLineAsync(message);

            using (Process exeProcess = Process.Start(process))
            {
                exeProcess.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Logger.Log(e.Data);
                        logStream.WriteLine(e.Data);
                    }
                };
                exeProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Logger.Log(e.Data);
                        logStream.WriteLine(e.Data);
                    }
                };

                exeProcess.BeginOutputReadLine();
                exeProcess.BeginErrorReadLine();

                var tokenSource = new CancellationTokenSource();
                var token = tokenSource.Token;

                /*Task outputReader = Task.Factory.StartNew(AppendLinesFunc,
                    Tuple.Create(logStream, exeProcess.StandardOutput),
                    token, TaskCreationOptions.None, TaskScheduler.Default);
                Task errorReader = Task.Factory.StartNew(AppendLinesFunc,
                    Tuple.Create(logStream, exeProcess.StandardError),
                    token, TaskCreationOptions.None, TaskScheduler.Default);*/

                int exitCode = await exeProcess.WaitForExitAsync();

                //Task.WaitAll(outputReader, errorReader);

                return exitCode;
            }
        }
    }
}
