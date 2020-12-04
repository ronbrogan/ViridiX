using Serilog.Parsing;
using System;
using System.Text;
using ViridiX.Mason.Logging;

namespace DiskDumper
{
    public class ConsoleLogger : LoggerBase
    {
        public override bool IsEnabled { get; set; } = true;
        public override LogLevel Level { get; set; } = LogLevel.Info;
        public override string Path { get; set; } = null;

        public override void Dispose()
        {
        }

        public override void Flush()
        {
        }

        public override void Log(LogLevel level, Exception exception, string message, params object[] parameters)
        {
            var parser = new MessageTemplateParser();
            var template = parser.Parse(message);
            var format = new StringBuilder();
            var index = 0;
            foreach (var tok in template.Tokens)
            {
                if (tok is TextToken)
                    format.Append(tok);
                else
                    format.Append("{" + index++ + "}");
            }
            var netStyle = format.ToString();


            Console.WriteLine($"[{level}] {string.Format(netStyle, parameters)}");
            if(exception != null)
                Console.WriteLine($"\t {exception}");
        }
    }
}
