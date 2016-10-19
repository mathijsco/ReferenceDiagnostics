using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReferenceDiagnostics
{
    public class Output
    {
        public static readonly Output Current = new Output();

        private const ConsoleColor AccentColor = ConsoleColor.Cyan;
        private bool _isNewLine = true;
        private bool _isOnAction;
        private readonly Stack<string> _statusStack = new Stack<string>();

        private Output()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => RequiresNewLine();
        }

        public void WriteTitle(string title)
        {
            RequiresNewLine();

            Console.Clear();
            Console.ForegroundColor = AccentColor;
            Console.WriteLine();
            Console.Write(new string(' ', Console.BufferWidth - title.Length - 1));
            Console.WriteLine(title.ToUpperInvariant());
            Console.WriteLine(new string('_', Console.BufferWidth));
            Console.WriteLine();
            Console.ResetColor();
        }

        public void WriteEntry(string text)
        {
            RequiresNewLine();
            Write(text);
            _isNewLine = false;
            _isOnAction = false;
        }

        public void WriteEntry(Exception ex)
        {
            RequiresNewLine();
            Console.ForegroundColor = Console.BackgroundColor;
            Console.BackgroundColor = ConsoleColor.Red;
            Console.WriteLine("--- EXCEPTION ---");

            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex.ToString());
            Console.ResetColor();
            _isNewLine = false;
            _isOnAction = false;
        }

        public void WriteAction(string text)
        {
            RequiresNewLine();
            Write(text + "...");
            _statusStack.Push(text);
            _isOnAction = true;
            _isNewLine = false;
        }

        public void WriteStatus(bool status)
        {
            if (status)
                WriteStatus("OK", ConsoleColor.Green);
            else
                WriteStatus("FAILED", ConsoleColor.Red);
        }

        private void WriteStatus(string statusText, ConsoleColor color)
        {
            var text = _statusStack.Pop();
            if (_isNewLine || !_isOnAction)
            {
                RequiresNewLine();
                Write("`^` " + text);
            }

            Console.Write("  [ ");
            Console.ForegroundColor = color;
            Console.Write(statusText.ToUpperInvariant());
            Console.ResetColor();
            Console.WriteLine(" ]");

            _isNewLine = true;
            _isOnAction = false;
        }

        public void WaitForTask(Task task, int milisecondsTimeout = int.MaxValue)
        {
            int timeLeft = milisecondsTimeout;
            bool timeOut = false;
            while (!(task.IsCompleted || task.IsFaulted || task.IsCanceled || timeOut))
            {
                for (int i = 0; i < 8; i++)
                {
                    var sleep = Math.Min(250, timeLeft);
                    timeLeft -= sleep;
                    if (timeLeft <= 0)
                    {
                        timeOut = true;
                        break;
                    }
                    Thread.Sleep(sleep);
                }
                Console.Write(".");
            }
            if (timeOut)
                WriteStatus("TIMEOUT", ConsoleColor.DarkYellow);
            else if (task.IsFaulted || task.IsCanceled)
            {
                WriteStatus(false);
                if (task.IsFaulted)
                    WriteEntry(task.Exception);
            }
            else
                WriteStatus(true);
        }

        public void EmptyLine()
        {
            RequiresNewLine();
            Console.WriteLine();
            _isNewLine = true;
            _isOnAction = false;
        }

        private void RequiresNewLine()
        {
            if (!_isNewLine)
            {
                Console.WriteLine();
                _isNewLine = true;
            }
        }

        private void Write(string text)
        {
            text = new string(' ', _statusStack.Count * 2) + text;
            bool state = false;
            foreach (var item in text.Split('`'))
            {
                if (!state)
                    Console.ResetColor();
                else
                    Console.ForegroundColor = AccentColor;

                Console.Write(item);
                state = !state;
            }
            Console.ResetColor();
        }
    }

    public static class OutputExtensions
    {
        public static Task WithLogging(this Task task, string taskDescription, int milisecondsTimeout = int.MaxValue)
        {
            var output = Output.Current;
            output.WriteAction(taskDescription);
            output.WaitForTask(task, milisecondsTimeout);

            return task;
        }
    }
}
