using System;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Data.Entity.Validation;
using System.Data.Objects;
using System.Linq;
using System.Transactions;
using EFTracingProvider;
using Succinctly.Model;

namespace Succinctly.Console
{
	using System.Collections.Generic;
	using System.ComponentModel.DataAnnotations.Schema;
	using System.Data.Spatial;
	using System.Diagnostics;
	using System.Security.Cryptography;


	internal class Program
	{
		static void Deleting()
		{
			using (var tx = new TransactionScope())
			using (var ctx = new ProjectsContext())
			{
				//load a project by id
				var p = ctx.Projects.Find(1);
				//var p = new Project() { ProjectId = 1/*, Customer = new Customer() { CustomerId = 1 }*/ };

				ctx.Delete(p);

				var result = ctx.SaveChanges();
			}
		}

		static void LazyLoading()
		{
			using (var ctx = new ProjectsContext())
			{
				var project = ctx.Projects.Find(1);
				
				var customerLoaded = ctx.Entry(project).Reference(x => x.Customer).IsLoaded;

				var customer = project.Customer;

				customerLoaded = ctx.Entry(project).Reference(x => x.Customer).IsLoaded;
			}

			using (var ctx = new ProjectsContext())
			{
				ctx.Configuration.LazyLoadingEnabled = false;

				//load a project
				var project = ctx.Projects.Find(1);
				
				//see if the ProjectResources collection is loaded
				var resourcesLoaded = ctx.Entry(project).Collection(x => x.ProjectResources).IsLoaded;

				if (resourcesLoaded == false)
				{
					//explicitly load the ProjectResources collection
					ctx.Entry(project).Collection(x => x.ProjectResources).Load();
				}

				//see if the Customer property is loaded
				var customerLoaded = ctx.Entry(project).Reference(x => x.Customer).IsLoaded;

				var customer = project.Customer;

				customerLoaded = ctx.Entry(project).Reference(x => x.Customer).IsLoaded;

				ctx.Entry(project).Reference(x => x.Customer).Load();

				customerLoaded = ctx.Entry(project).Reference(x => x.Customer).IsLoaded;

				customer = project.Customer;
			}

			using (var ctx = new ProjectsContext())
			{
				ctx.Configuration.LazyLoadingEnabled = false;

				var customer = ctx.Customers.Find(1);

				ctx.Entry(customer).Collection(x => x.Projects).Query().Where(x => x.End != null).Load();

				var projectsCount = customer.Projects.Count;
			}
		}

		static void Querying()
		{
			using (var ctx = new ProjectsContext())
			{
				//id
				var bigProject = ctx.Projects.Find(1);

				//LINQ
				var usersInProjectWithLINQ = (from r in ctx.Resources
											  from p in ctx.Projects
											  where p.Name == "Big Project"
											  && r.ProjectResources.Select(x => x.Project).Contains(p)
											  select r).ToList();

				var projectsByName = ctx.Projects.Where(x => x.Name == "Big Project").ToList();
				var customersWithoutProjects = ctx.Customers.Where(c => c.Projects.Any() == false).ToList();

				//or
				var resourcesKnowingVBOrCS = ctx.Technologies.Where(t => t.Name == "VB.NET" || t.Name == "C#").SelectMany(x => x.Resources).Select(x => x.Name).ToList();

				//grouping
				var resourcesGroupedByProjectRole = ctx.Projects.SelectMany(x => x.ProjectResources).Select(x => new { Role = x.Role, Resource = x.Resource.Name }).GroupBy(x => x.Role).Select(x => new { Role = x.Key, Resources = x }).ToList();

				//grouping and counting
				var projectsByCustomer = ctx.Projects.GroupBy(x => x.Customer).Select(x => new { Customer = x.Key.Name, Count = x.Count() }).ToList();

				//top 10 customers having more projects in descending order
				var top10CustomersWithMoreProjects = ctx.Projects.GroupBy(x => x.Customer.Name).Select(x => new { x.Key, Count = x.Count() }).OrderByDescending(x => x.Count).Take(10).ToList();

				//grouping by date part and counting
				var countOfProjectsByMonth = ctx.Projects.GroupBy(x => EntityFunctions.CreateDateTime(x.Start.Year, x.Start.Month, 1, 0, 0, 0)).Select(x => new { Month = x.Key, Count = x.Count() }).ToList();

				//group and count the days between two dates
				var projectsGroupedByDurationDays = ctx.Projects.Where(x => x.End != null).GroupBy(x => EntityFunctions.DiffDays(x.Start, x.End.Value)).Select(x => new { Duration = x.Key, List = x }).ToList();

				//order by extension method
				var technologiesSortedByName = ctx.Technologies.OrderBy("Name").ThenBy("TechnologyId").ToList();

				//create a base query
				var projectsQuery = from p in ctx.Projects select p;

				//add sorting
				var projectsSortedByDateQuery = projectsQuery.OrderBy(x => x.Start);

				//execute and get the sorted results
				var projectsSortedByDateResults = projectsSortedByDateQuery.ToList();

				//add paging
				var projectsWithPagingQuery = projectsQuery.OrderBy(x => x.Start).Take(5).Skip(0);

				//execute and get the first 5 results
				var projectsWithPagingResults = projectsWithPagingQuery.ToList();

				//add a restriction
				var projectsStartingAWeekAgoQuery = projectsQuery.Where(x => x.Start >= EntityFunctions.AddDays(DateTime.Today, -7));

				//execute and get the projects that started a week ago
				var projectsStartingAWeekAgoResults = projectsStartingAWeekAgoQuery.ToList();

				//eager load properties							
				var resourcesIncludingTechnologies = ctx.Resources.Include(x => x.Technologies).ToList();
				
				var projectsIncludingCustomers = ctx.Projects.Include("Customer").ToList();

				//distinct
				var roles = ctx.Resources.SelectMany(x => x.ProjectResources).Where(x => x.Resource.Name == "Ricardo Peres").Select(x => x.Role).Distinct().ToList();

				//check existence
				var existsProjectBySomeCustomer = ctx.Projects.Any(x => x.Customer.Name == "Some Customer");

				//count
				var numberOfClosedProjects = ctx.Projects.Where(x => x.End != null && x.End < DateTime.Now).Count();

				//average
				var averageProjectDuration = ctx.Projects.Where(x => x.End != null).Average(x => EntityFunctions.DiffDays(x.Start, x.End));

				//sum
				var sumProjectDurationsByCustomer = ctx.Projects.Where(x => x.End != null).Select(x => new { Customer = x.Customer.Name, Days = EntityFunctions.DiffDays(x.Start, x.End) }).GroupBy(x => x.Customer).Select(x => new { Customer = x.Key, Sum = x.Sum(y => y.Days) }).ToList();

				//return the resources and project names only
				var resourcesXprojects = ctx.Projects.SelectMany(x => x.ProjectResources).Select(x => new { Resource = x.Resource.Name, Project = x.Project.Name }).ToList();

				//return the customer names and their project counts
				var customersAndProjectCount = ctx.Customers.Select(x => new { x.Name, Count = x.Projects.Count() }).ToList();

				//subquery
				var usersKnowingATechnology = (from r in ctx.Resources where r.Technologies.Any(x => (ctx.Technologies.Where(t => t.Name == "ASP.NET")).Contains(x)) select r).ToList();
				var usersKnowingATechnology2 = (from r in ctx.Resources where r.Technologies.Any(x => (from t in ctx.Technologies where t.Name == "ASP.NET" select t).Contains(x)) select r).ToList();

				//contains
				var customersToFind = new String[] { "Some Customer", "Another Customer" };
				var projectsOfCustomers = ctx.Projects.Where(x => customersToFind.Contains(x.Customer.Name)).ToList();

				//spatial
				var location = DbGeography.FromText("POINT(41 8)");

				var area = DbGeography.MultiPointFromText("MULTIPOINT(53.095124 -0.864716, 53.021255 -1.337128, 52.808019 -1.345367, 52.86153 -1.018524)", 4326);

				/*var pointInsideArea = ctx.Venues.Where(x => area.Intersects(x.Location)).ToList();

				var venuesAndDistanceToLocation = (from v in ctx.Venues
							  orderby v.Location.Distance(location)
							  select new { Venue = v, Distance = v.Location.Distance(location) }).ToList();*/

				//Entity-SQL
				ObjectContext octx = (ctx as IObjectContextAdapter).ObjectContext;
				
				//filtering
				var usersInProjectWithESQL = octx.CreateQuery<Resource>("SELECT VALUE pr.Resource FROM ProjectResources AS pr WHERE pr.Project.Name = @name", new ObjectParameter("name", "Big Project")).ToList();
	
				//contains
				var usersKnowingATechnologyESQL = octx.CreateQuery<Resource>("SELECT VALUE r FROM Resources AS r WHERE EXISTS (SELECT VALUE t FROM Technologies AS t WHERE t.Name = @name AND r IN t.Resources)", new ObjectParameter("name", "ASP.NET")).ToList();

				//flatten
				var userTechnologiesESQL = octx.CreateQuery<Technology>("FLATTEN(SELECT VALUE r.Technologies FROM Resources AS r)").ToList();

				//paging
				var pagedResourcesESQL = octx.CreateQuery<Resource>("SELECT VALUE r FROM Resources AS r ORDER BY r.Name SKIP 5 LIMIT(5)").ToList();

				//paging with parameters
				var pagedResourcesWithParametersESQL = octx.CreateQuery<Resource>("SELECT VALUE r FROM Resources AS r ORDER BY r.Name SKIP @skip LIMIT(@limit)", new ObjectParameter("skip", 5), new ObjectParameter("limit", 5)).ToList();

				//top
				var lastProjectESQL = octx.CreateQuery<Project>("SELECT VALUE TOP(1) p FROM Projects AS p ORDER BY p.Start DESC").SingleOrDefault();

				//between
				var projectsStartingInADateIntervalESQL = octx.CreateQuery<Project>("SELECT VALUE p FROM Projects AS P WHERE p.Start BETWEEN @start AND @end", new ObjectParameter("start", DateTime.Today.AddDays(-14)), new ObjectParameter("end", DateTime.Today.AddDays(-7))).ToList();

				//in
				var projectsStartingInSetOfDatesESQL = octx.CreateQuery<Project>("SELECT VALUE p FROM Projects AS P WHERE p.Start IN MULTISET(DATETIME '2013-12-25 0:0:0', DATETIME '2013-12-31 0:0:0')").ToList();

				//projection
				var projectNameAndDurationESQL = octx.CreateQuery<Object>("SELECT p.Name, DIFFDAYS(p.Start, p.[End]) FROM Projects AS p WHERE p.[End] IS NOT NULL").ToList();

				//count
				var numberOfClosedProjectsESQL = octx.CreateQuery<Int32>("SELECT VALUE COUNT(p.ProjectId) FROM Projects AS p WHERE p.[End] IS NOT NULL AND p.[End] < @now", new ObjectParameter("now", DateTime.Now)).Single();

				//group
				var customersAndProjectCountIndicatorESQL = octx.CreateQuery<Object>("SELECT p.Customer.Name, COUNT(p.Name) FROM Projects AS p GROUP BY p.Customer").ToList();

				//case
				var customersAndProjectRangeESQL = octx.CreateQuery<Object>("SELECT p.Customer.Name, CASE WHEN COUNT(p.Name) > 10 THEN 'Lots' ELSE 'Few' END AS Amount FROM Projects AS p GROUP BY p.Customer").ToList();

				if (customersAndProjectRangeESQL.Any() == true)
				{
					var r = customersAndProjectRangeESQL.OfType<IExtendedDataRecord>().First();
					var nameIndex = r.GetOrdinal("Name");
					var name = r.GetString(nameIndex);
				}				

				//max number of days
				var maxDurationESQL = octx.CreateQuery<Int32?>("SELECT VALUE MAX(DIFFDAYS(p.Start, p.[End])) FROM Projects AS p WHERE p.[End] IS NOT NULL").SingleOrDefault();

				//string contains
				var technologiesContainingNetESQL = octx.CreateQuery<String>("SELECT VALUE t.Name FROM Technologies AS T WHERE CONTAINS(t.Name, '.NET')").ToList();

				//SQL
				var projectFromSQL = ctx.Projects.SqlQuery("SELECT * FROM Project WHERE Name = @p0", "Big Project").ToList();
				
				//stored procedure
				var projectFromProcedure = ctx.Projects.SqlQuery("SELECT * FROM dbo.GetProjectById(@p0)", 1).SingleOrDefault();

				var result = ctx.Database.ExecuteSqlCommand("UPDATE Project SET [End] = null WHERE ProjectId = {0}", 100);

				//current date and time
				var now = ctx.Database.SqlQuery<DateTime>("SELECT GETDATE()").Single();
	
				var model = ctx.Database.SqlQuery(typeof(Byte[]), "SELECT Model FROM __MigrationHistory").OfType<Object>().Single();

				//call function
				var serverTimestamp = ctx.ExecuteScalar<DateTime>("SELECT GETDATE()");

				//update records
				var updateCount = ctx.ExecuteNonQuery("UPDATE ProjectDetail SET Budget = Budget * 1.1 WHERE ProjectId = {0}", 1);

				//extensions
				var projectsBetweenTodayAndBeforeToday = ctx.Projects.Between(x => x.Start, DateTime.Today.AddDays(-1), DateTime.Today).ToList();

				//projects with 10 to 20 resources
				var projectsWithTwoOrThreeResources = ctx.Projects.Select(x => new { x.Name, ResourceCount = x.ProjectResources.Count() }).Between(x => x.ResourceCount, 10, 20).ToList();

				//extension method
				var soundex = ctx.Projects.Select(x => x.Name.Soundex()).ToList();

				//first level cache
				var user = ctx.Resources.Local.SingleOrDefault(x => x.Technologies.Any(y => y.Name == "ASP.NET"));

				//no caching
				var technologies = ctx.Technologies.AsNoTracking().ToList();

			}
		}

		static void Events()
		{
			using (var tx = new TransactionScope())
			{
				using (var ctx = new ProjectsContext())
				{
					ctx.AddEventHandlers();
					ctx.ObjectMaterialized += delegate(Object sender, ObjectMaterializedEventArgs e)
					{
						if (e.Entity is Project)
						{
							var p = e.Entity as Project;
							p.Customer.ToString();
							p.ProjectManager.ToString();
							p.Name += "_modified";
						}
					};

					ctx.SavingChanges += delegate(Object sender, EventArgs e)
					{
						var context = sender as DbContext;
						context.ChangeTracker.Entries<Project>().Single(x => x.State == EntityState.Modified).State = EntityState.Unchanged;
					};

					ctx.Projects.Find(1);

					var r = ctx.SaveChanges();
				}
			}
		}

		static void Modifying()
		{
			using (var ctx = new ProjectsContext())
			{
				using (var tx = new TransactionScope())
				{
					ctx.Tools.AddOrUpdate(x => x.Name, new DevelopmentTool() { Name = "Visual Studio 2012", Language = "C#" });

					var result = ctx.SaveChanges();
				}
			}
		}

		static void CreateInitialData()
		{
			//check if the database identified by a named connection string exists
			var existsByName = Database.Exists("Name=ProjectsContext");

			//check if the database identified by a connection string exists
			var existsByConnectionString = Database.Exists(@"Data Source=.\SQLEXPRESS;Integrated Security=SSPI;Initial Catalog=Succinctly;MultipleActiveResultSets=true");

			using (var ctx = new ProjectsContext())
			{
				var created = ctx.Database.CreateIfNotExists();

				if (ctx.Database.CompatibleWithModel(false) == true)
				{
					return;
				}

				new DropCreateDatabaseAlways<ProjectsContext>().InitializeDatabase(ctx);

				using (var tx = new TransactionScope())
				{
					//venues
					var home = new Venue() { Name = "Home, Sweet Home", Location = DbGeography.FromText("POINT(40.2112 8.4292)") };
					var somePlace = new Venue() { Name = "Some Place Else", Location = DbGeography.FromText("POINT(41 8)") };

					/*ctx.Venues.Add(home);
					ctx.Venues.Add(somePlace);*/

					//customers
					var bigCustomer = new Customer() { Name = "Big Customer" };
					bigCustomer.Contact.Email = "big.customer@contact.com";
					bigCustomer.Contact.Phone = "00 1 555 111 333";

					var smallCustomer = new Customer() { Name = "Small Customer" };
					smallCustomer.Contact.Email = "small.customer@email.com";
					smallCustomer.Contact.Phone = "00 351 111 222 333";

					ctx.Customers.Add(bigCustomer);
					ctx.Customers.Add(smallCustomer);

					var developer = new Resource() { Name = "Ricardo Peres" };
					developer.Contact.Email = "rjperes@hotmail.com";
					developer.Contact.Phone = "?";

					var projectManager = new Resource() { Name = "Succinct Project Manager" };
					projectManager.Contact.Email = "succinct@some.place";

					var tester = new Resource() { Name = "Succinct Tester" };
					tester.Contact.Email = "succinct@some.place";

					ctx.Resources.Add(developer);
					ctx.Resources.Add(projectManager);
					ctx.Resources.Add(tester);

					//start technologies
					var aspNet = new Technology() { Name = "ASP.NET" };
					var entityFramework = new Technology() { Name = "Entity Framework" };
					var selenium = new Technology() { Name = "Selenium" };

					aspNet.Resources.Add(developer);
					entityFramework.Resources.Add(developer);
					selenium.Resources.Add(tester);

					developer.Technologies.Add(aspNet);
					developer.Technologies.Add(entityFramework);
					tester.Technologies.Add(selenium);

					ctx.Technologies.Add(aspNet);
					ctx.Technologies.Add(entityFramework);
					ctx.Technologies.Add(selenium);
					//end technologies

					//start tools
					var developmentTool = new DevelopmentTool() { Name = "Visual Studio 2012", Language = "C#" };
					var managementTool = new ManagementTool() { Name = "Project 2013", CompatibleWithProject = true };
					var testingTool = new TestingTool() { Name = "Selenium", Automated = true };

					ctx.Tools.Add(developmentTool);
					ctx.Tools.Add(managementTool);
					ctx.Tools.Add(testingTool);
					//end tools

					//start big project
					var bigProject = new Project() { Name = "Big Project", Start = DateTime.Today, Customer = bigCustomer };

					var bigProjectDetail = new ProjectDetail() { Project = bigProject, Budget = 10000M, Critical = true };

					bigProject.Detail = bigProjectDetail;

					ctx.SaveChanges();

					var bigProjectDeveloperResource = new ProjectResource() { Project = bigProject, Resource = developer, Role = Role.Developer };
					var bigProjectProjectManagerResource = new ProjectResource() { Project = bigProject, Resource = projectManager, Role = Role.ProjectManager };
					var bigProjectTesterResource = new ProjectResource() { Project = bigProject, Resource = tester, Role = Role.Tester };

					bigProject.ProjectResources.Add(bigProjectDeveloperResource);
					bigProject.ProjectResources.Add(bigProjectProjectManagerResource);
					bigProject.ProjectResources.Add(bigProjectTesterResource);

					developer.ProjectResources.Add(bigProjectDeveloperResource);
					projectManager.ProjectResources.Add(bigProjectProjectManagerResource);
					tester.ProjectResources.Add(bigProjectTesterResource);

					bigCustomer.Projects.Add(bigProject);
					//end big project

					//small project
					var smallProject = new Project() { Name = "Small Project", Start = DateTime.Today.AddDays(-7), End = DateTime.Today.AddDays(-1), Customer = smallCustomer };

					var smallProjectDetail = new ProjectDetail() { Project = smallProject, Budget = 5000M, Critical = false };

					var smallProjectDeveloperResource = new ProjectResource() { Project = smallProject, Resource = developer, Role = Role.Developer };
					var smallProjectProjectManagerResource = new ProjectResource() { Project = smallProject, Resource = projectManager, Role = Role.ProjectManager };
					var smallProjectTesterResource = new ProjectResource() { Project = smallProject, Resource = tester, Role = Role.Tester };

					smallProject.Detail = smallProjectDetail;

					smallProject.ProjectResources.Add(smallProjectDeveloperResource);
					smallProject.ProjectResources.Add(smallProjectProjectManagerResource);
					smallProject.ProjectResources.Add(smallProjectTesterResource);

					developer.ProjectResources.Add(smallProjectDeveloperResource);
					projectManager.ProjectResources.Add(smallProjectProjectManagerResource);
					tester.ProjectResources.Add(smallProjectTesterResource);

					smallCustomer.Projects.Add(smallProject);
					//end small project

					ctx.SaveChanges();
					tx.Complete();
				}
			}
		}

		static void Hierarchies()
		{
			using (var ctx = new ProjectsContext())
			{
				var octx = (ctx as IObjectContextAdapter).ObjectContext;

				var firstTool = ctx.Tools.AsNoTracking().First();

				var id = firstTool.ToolId;

				var toolFromFind = ctx.Tools.Find(id);

				var toolFromQuery = ctx.Tools.Where(x => x.ToolId == id);
				
				var allTools = ctx.Tools.ToList();
				
				var developmentToolFromLinq = ctx.Tools.OfType<DevelopmentTool>().Single();

				var managementToolFromLinq = (from t in ctx.Tools where t is ManagementTool select t as ManagementTool).Single();

				var testingToolFromEntitySql = octx.CreateQuery<TestingTool>("SELECT VALUE t FROM OFTYPE(Tools, Succinctly.Model.TestingTool) AS t").OfType<TestingTool>().Single();

				var managementToolFromEntitySql = octx.CreateQuery<ManagementTool>("SELECT VALUE TREAT(t AS Succinctly.Model.ManagementTool) FROM Tools AS t WHERE t IS OF (Succinctly.Model.ManagementTool)").OfType<ManagementTool>().Single();

				//not applicable if using Concrete Table Inheritance / Table Per Concrete Type
				//var testingToolFromSql = ctx.Database.SqlQuery<TestingTool>("SELECT * FROM Tool AS t WHERE t.Discriminator = @p0", "TestingTool").Single();
			}
		}

		static void Validation()
		{
			using (var tx = new TransactionScope())
			{
				using (var ctx = new ProjectsContext())
				{					
					//var p = ctx.Projects.Find(1);
					var p = ctx.Projects.Include(x => x.Customer).Where(x => x.ProjectId == 1).SingleOrDefault();
					p.Name = String.Empty;
					p.Detail.Budget = 1M;
					
					var allErrors = ctx.GetValidationErrors();
					var errorsInEntity = ctx.GetValidationErrors().Where(x => x.Entry.Entity == p).ToList();

					//to prevent a validation error
					//ctx.LoadEverything(p);
					//p.ProjectManager.ToString();
					//p.Customer.ToString();

					try
					{
						ctx.SaveChanges();
					}
					catch (DbEntityValidationException ex)
					{
						var e = ex.EntityValidationErrors.ToList();
					}
				}
			}
		}

		static void ConcurrencyControl()
		{
			using (var ctx = new ProjectsContext())
			{		
				using (var tx = new TransactionScope())
				{
					//first one wins
					var p = ctx.Projects.Find(1);

					var r = ctx.Database.ExecuteSqlCommand("UPDATE Project SET Name = 'a' WHERE ProjectId = @p0", p.ProjectId);

					p.Name = "sadsa";

					//to prevent a validation error
					p.ProjectManager.ToString();
					p.Customer.ToString();

					try
					{
						r = ctx.SaveChanges();
					}
					catch (DbUpdateConcurrencyException)
					{
						//the record was changed in the database, warn the user and fail
					}
				}

				using (var tx = new TransactionScope())
				{
					//first one wins
					var projects = ctx.Projects.ToList();

					var p = ctx.Projects.Find(1);

					var r = ctx.Database.ExecuteSqlCommand("UPDATE Project SET Name = 'a' WHERE ProjectId = @p0", p.ProjectId);

					p.Name = "sadsa";

					//to prevent a validation error
					p.ProjectManager.ToString();
					p.Customer.ToString();

					var failed = true;

					do
					{						
						try
						{
							ctx.SaveChanges();
							
							failed = false;
						}
						catch (DbUpdateConcurrencyException ex)
						{
							var entity = ex.Entries.Single();
							var databaseValues = entity.GetDatabaseValues();
						
							entity.OriginalValues.SetValues(databaseValues);
						}
					}
					while (failed);
				}
			}
		}

		static void Export()
		{
			using (var ctx = new ProjectsContext())
			{
				ctx.ExportDatabaseToFile("ProjectContext.sql");
				ctx.ExportModelToFile("ProjectContext.edmx");
				var edmx = ctx.GetEdmxFromDatabase();
			}
		}

		static void Tracing()
		{
			using (var ctx = ProjectsContext.CreateTracingContext("ProjectsContext", x =>
			{
				System.Console.WriteLine(x.ToFlattenedTraceString());
			}, true, "MyContext.output"))
			{
				ctx.Customers.AddOrUpdate(x => x.Name, new Customer { Name = "Big Customer" });

				//load a project by id<
				var p = ctx.Projects.Find(1);

				var developersOnly = ctx.Entry(p).Collection(x => x.ProjectResources).Query().Where(x => x.Role == Role.Developer).ToList();

				//count an entity's collection entities without loading them
				var developersCount = ctx.Entry(p).Collection(x => x.ProjectResources).Query().Count(x => x.Role == Role.Developer);

				var resourcesProjectsCustomers = ctx.Resources.Include(x => x.ProjectResources.Select(y => y.Project.Customer)).ToList();

				//two include paths
				var resourcesProjectResourcesAndTechnologies = ctx.Resources.Include(x => x.ProjectResources).Include(x => x.Technologies).ToList();


				var projectsAndTheirCustomers = ctx.Projects.Include("Customer").ToList();

				//access the customer
				var c = p.Customer;

				ctx.Entry(p).Reference(x => x.Customer).Load();

				var customer = ctx.Customers.Find(1);

				var projects = customer.Projects;
				
				ctx.Projects.Where(x => x.Name.Contains(".net")).ToList();
			}
		}

#if ORACLE
		static void Oracle()
		{
			using (var ctx = new ProjectsContextOracle())
			{
				var t = new Test() { Text = "xpto" };

				ctx.Tests.Add(t);

				var r = ctx.SaveChanges();

				var tests = ctx.Tests.ToList();

				tests.First().Clob = "Large text...";

				ctx.SaveChanges();
			}
		}
#endif

		static void Main(String[] args)
		{
			EFTracingProviderFactory.Register();
#if ORACLE
			Oracle();
#endif	
			CreateInitialData();
			Tracing();
			ConcurrencyControl();
			Modifying();
			Querying();
			Deleting();
			LazyLoading();
			Hierarchies();
			Events();
			Export();
			Validation();
		}		
	}
}
