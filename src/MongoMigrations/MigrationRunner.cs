namespace MongoMigrations
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using MongoDB.Bson.Serialization;
	using MongoDB.Driver;

	public class MigrationRunner
	{
		static MigrationRunner()
		{
			Init();
		}

		public MigrationRunner(string mongoServerLocation, string databaseName)
            : this(new MongoClient(mongoServerLocation).GetDatabase(databaseName))
		{
		}

        public MigrationRunner(IMongoDatabase database)
		{
			Database = database;
			DatabaseStatus = new DatabaseMigrationStatus(this);
			MigrationLocator = new MigrationLocator();
		}

        public static void Init()
        {
            MongoDefaults.GuidRepresentation = MongoDB.Bson.GuidRepresentation.Standard;

            BsonSerializer.RegisterSerializer(typeof(MigrationVersion), new MigrationVersionSerializer());
        }

        public IMongoDatabase Database { get; set; }
		public MigrationLocator MigrationLocator { get; set; }
		public DatabaseMigrationStatus DatabaseStatus { get; set; }

		public virtual void UpdateToLatest()
		{
			Console.WriteLine(WhatWeAreUpdating() + " to latest...");
			UpdateTo(MigrationLocator.LatestVersion());
		}

		private string WhatWeAreUpdating()
		{
            return $"Updating server(s) \"{this.ServerAddresses()}\" for database \"{this.Database.DatabaseNamespace.DatabaseName}\"";
		}

	    private string ServerAddresses()
	    {
            return String.Join(",", Database.Client.Settings.Server.ToString());
	    }

	    protected virtual void ApplyMigrations(IEnumerable<Migration> migrations)
		{
			migrations.ToList()
			          .ForEach(ApplyMigration);
		}

		protected virtual void ApplyMigration(Migration migration)
		{
            Console.WriteLine(new { Message = "Applying migration", migration.Version, migration.Description, DatabaseName = Database.DatabaseNamespace.DatabaseName });

			var appliedMigration = DatabaseStatus.StartMigration(migration);
			migration.Database = Database;
			try
			{
				migration.Update();
                DatabaseStatus.CompleteMigration(appliedMigration);
			}
			catch (Exception exception)
			{
				OnMigrationException(migration, exception);
			}
		}

		protected virtual void OnMigrationException(Migration migration, Exception exception)
		{
			var message = new
				{
					Message = "Migration failed to be applied: " + exception.Message,
					migration.Version,
					Name = migration.GetType(),
					migration.Description,
                DatabaseName = Database.DatabaseNamespace.DatabaseName
				};
			Console.WriteLine(message);
			throw new MigrationException(message.ToString(), exception);
		}

		public virtual void UpdateTo(MigrationVersion updateToVersion)
		{
			var currentVersion = DatabaseStatus.GetLastAppliedMigration();
            Console.WriteLine(new { Message = WhatWeAreUpdating(), currentVersion, updateToVersion, DatabaseName = Database.DatabaseNamespace.DatabaseName });

			var migrations = MigrationLocator.GetMigrationsAfter(currentVersion)
			                                 .Where(m => m.Version <= updateToVersion);

			ApplyMigrations(migrations);
		}
	}
}