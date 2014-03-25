using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.Objects;
using System.Linq;
using System.Threading;
using EFTracingProvider;

namespace Succinctly.Model
{
	using System.ComponentModel.DataAnnotations.Schema;
	using System.Data.Entity.Migrations;

	public class ProjectsContext : DbContext
	{
		static ProjectsContext()
		{
			EFTracingProviderFactory.Register();
			//Database.SetInitializer(new MigrateDatabaseToLatestVersion<ProjectsContext, Succinctly.Model.Migrations.Configuration>());
			Database.SetInitializer<ProjectsContext>(null);
		}

		public ProjectsContext() : this("Name=ProjectsContext")
		{
			//this.AddEventHandlers();
		}

		public ProjectsContext(String nameOrConnectionString) : base(nameOrConnectionString)
		{
			//this.AddEventHandlers();
		}

		protected ProjectsContext(DbConnection existingConnection) : base(existingConnection, true)
		{
			//this.AddEventHandlers();
		}

		public event EventHandler<EventArgs> SavingChanges;

		public event EventHandler<ObjectMaterializedEventArgs> ObjectMaterialized;

		public DbSet<Test> Tests
		{
			get;
			set;
		}

		public DbSet<Tool> Tools
		{
			get;
			set;
		}

		public DbSet<Resource> Resources
		{
			get;
			set;
		}

		public DbSet<Project> Projects
		{
			get;
			set;
		}

		public DbSet<Customer> Customers
		{
			get;
			set;
		}

		public DbSet<Technology> Technologies
		{
			get;
			set;
		}

		/*public DbSet<Parent> Parents
		{
			get;
			set;
		}

		public DbSet<Master> Masters
		{
			get;
			set;
		}*/

		public DbSet<Venue> Venues
		{
			get;
			set;
		}

		public static ProjectsContext CreateTracingContext(String nameOrConnectionString, Action<CommandExecutionEventArgs> logAction, Boolean logToConsole = true, String logToFile = null)
		{
			EFTracingProviderConfiguration.LogToFile = logToFile;
			EFTracingProviderConfiguration.LogToConsole = logToConsole;
			EFTracingProviderConfiguration.LogAction = logAction;

			var ctx = new ProjectsContext(CreateConnection(nameOrConnectionString));
			(ctx as IObjectContextAdapter).ObjectContext.EnableTracing();
			return (ctx);
		}

		public void AddEventHandlers()
		{
			var octx = (this as IObjectContextAdapter).ObjectContext;
			octx.SavingChanges += (s, e) => this.OnSavingChanges(e);
			octx.ObjectMaterialized += (s, e) => this.OnObjectMaterialized(e);
		}

		protected virtual void OnObjectMaterialized(ObjectMaterializedEventArgs e)
		{
			var handler = this.ObjectMaterialized;

			if (handler != null)
			{
				handler(this, e);
			}

			if (e.Entity is IInitializable)
			{
				(e.Entity as IInitializable).Initialize(this);
			}

			if (e.Entity is IImmutable)
			{
				this.Entry(e.Entity).State = EntityState.Detached;
			}
		}

		protected virtual void OnSavingChanges(EventArgs e)
		{
			var handler = this.SavingChanges;

			if (handler != null)
			{
				handler(this, e);
			}

			foreach (var entity in this.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).Select(x => x.Entity).OfType<IImmutable>())
			{
				this.Entry(entity).State = EntityState.Detached;
			}

			foreach (var auditable in this.ChangeTracker.Entries().Where(x => x.State == EntityState.Added).Select(x => x.Entity).OfType<IAuditable>())
			{
				auditable.CreatedAt = DateTime.Now;
				auditable.CreatedBy = Thread.CurrentPrincipal.Identity.Name;
			}

			foreach (var auditable in this.ChangeTracker.Entries().Where(x => x.State == EntityState.Modified).Select(x => x.Entity).OfType<IAuditable>())
			{
				auditable.UpdatedAt = DateTime.Now;
				auditable.UpdatedBy = Thread.CurrentPrincipal.Identity.Name;
			}
		}

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

			/*modelBuilder.Entity<Master>().HasRequired(x => x.Detail).WithRequiredPrincipal(x => x.Master).WillCascadeOnDelete(true);
			modelBuilder.Entity<Detail>().HasKey(x => x.MasterId);*/

			modelBuilder.Configurations.Add(new CustomerConfiguration());

			modelBuilder.Entity<Project>().HasOptional(x => x.Detail).WithRequired(x => x.Project).WillCascadeOnDelete(true);

			modelBuilder.Entity<ManagementTool>().Map(m => m.MapInheritedProperties());
			modelBuilder.Entity<TestingTool>().Map(m => m.MapInheritedProperties());
			modelBuilder.Entity<DevelopmentTool>().Map(m => m.MapInheritedProperties());

			base.OnModelCreating(modelBuilder);
		}

		private static DbConnection CreateConnection(String nameOrConnectionString)
		{
			var connectionStringSetting = ConfigurationManager.ConnectionStrings[nameOrConnectionString];
			var connectionString = null;
			var providerName = String.Empty;

			if (connectionStringSetting != null)
			{
				connectionString = connectionStringSetting.ConnectionString;
				providerName = connectionStringSetting.ProviderName;
			}
			else
			{
				connectionString = nameOrConnectionString;
				providerName = "System.Data.SqlClient";
			}

			return (CreateConnection(connectionString, providerName));
		}

		private static DbConnection CreateConnection(String connectionString, String providerInvariantName)
		{
			var wrapperConnectionString = String.Format(@"wrappedProvider={0};{1}", providerInvariantName, connectionString);
			var connection = new EFTracingConnection { ConnectionString = wrapperConnectionString };
			return (connection);
		}
	}
}
