using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using CsvHelper;
using CsvHelper.Configuration;
using McMaster.Extensions.CommandLineUtils;
using VeerPerforma;
using VeerPerforma.Presentation.Csv;
using VeerPerforma.Statistics;

namespace AsAConsoleApp
{
    internal class Program
    {
        [Option("-t|--tests", CommandOptionType.MultipleValue, Description = "List of tests to execute")]
        public string[] TestNames { get; set; } = { };

        [Option("-o|--outputDir", CommandOptionType.SingleValue, Description = "Path to an output directory. Absolute or relative.")]
        public string OutputDirectory { get; set; }

        [Option("-n|--no-track", CommandOptionType.SingleValue, Description = "Disable tracking. Tracking is where we emit results to enabled targets for later reference when performing statistical analysis.")]
        public bool NoTrack { get; set; }

        [Option("-a|--analyze", CommandOptionType.SingleValue, Description = "Use this option to enable analysis mode, where a directory is nominated, and it is used to track and retrieve historical performance test runs for use in statistical tests against new runs.")]
        public bool Analyze { get; set; } = true;

        private static async Task Main(string[] userRequestedTestNames)
        {
            await CommandLineApplication.ExecuteAsync<Program>(userRequestedTestNames);
        }

        public class Thingo
        {
            public string Id { get; set; }
            public int Number { get; set; }
        }

        public class ThingoMap : ClassMap<Thingo>
        {
            public ThingoMap()
            {
                Map(u => u.Id).Index(0);
                Map(u => u.Number).Index(1);
            }
        }

        public async Task OnExecute()
        {
            // var filePath = $"C:\\Users\\paule\\code\\VeerPerformaRelated\\tracking_output\\Test_{Guid.NewGuid().ToString()}.csv";
            //
            // var data = new List<Thingo>()
            // {
            //     new Thingo()
            //     {
            //         Id = "wow",
            //         Number = 123
            //     },
            //     new Thingo()
            //     {
            //         Id = "hey",
            //         Number = 246
            //     }
            // };
            //
            //
            // using (var writer = new StreamWriter(filePath))
            // using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            // {
            //     csv.Context.RegisterClassMap<ThingoMap>();
            //     csv.WriteRecords(data);
            // }
            //
            //
            // using (var reader = new StreamReader(filePath))
            // using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            // {
            //     csv.Context.RegisterClassMap<ThingoMap>();
            //
            //
            //     var loaded = csv.GetRecords<Thingo>();
            //     ;
            // }

            var logger = Logging.CreateLogger("ConsoleAppLogs.log");
            
            if (OutputDirectory is null)
            {
                OutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "performance_output");
            }
            
            // testing
            OutputDirectory = "C:\\Users\\paule\\code\\VeerPerformaRelated";
            
            await ContainerConfiguration
                .CompositionRoot()
                .Resolve<VeerPerformaExecutor>()
                .Run(TestNames, OutputDirectory, NoTrack, Analyze, GetType());
        }
    }
}