using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Succinctly.Model
{
#if ORACLE
	public class ProjectsContextOracle : DbContext
	{
		static ProjectsContextOracle()
		{
			Database.SetInitializer<ProjectsContext>(null);
		}

		public ProjectsContextOracle() : this("Name=ProjectsContextOracle")
		{
		}

		public ProjectsContextOracle(String nameOrConnectionString) : base(nameOrConnectionString)
		{
		}

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);
		}

		public DbSet<Test> Tests
		{
			get;
			set;
		}
	}
#endif
}
