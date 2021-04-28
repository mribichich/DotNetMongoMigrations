using System;
using System.Reflection;

using MongoMigrations;

namespace RunMongoMigrations
{
    using CommandLine;

    public class Program
    {
        public static void Main(string[] args)
        {
#if DEBUG
            args = new[]
               {
                                "--database",
                                "test1",
                                "--migrations",
                                @"..\..\..\MongoMigrations.Stubs\bin\Debug\MongoMigrations.Stubs.dll"
                            };
#endif

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(Run)
                .WithNotParsed(errors => { Environment.Exit(1); });
        }

        private static void Run(Options options)
        {
            var runner = new MigrationRunner($"mongodb://{options.Host}:{options.Port}", options.Database);

            runner.MigrationLocator.LookForMigrationsInAssembly(Assembly.LoadFrom(options.Migrations));

            try
            {
                runner.UpdateToLatest();
                Environment.Exit(0);
            }
            catch (MigrationException e)
            {
                Console.WriteLine("Migrations Failed: " + e);
                ////Console.WriteLine(server, database, migrationsAssembly);
                Environment.Exit(1);
            }
        }
    }
}
