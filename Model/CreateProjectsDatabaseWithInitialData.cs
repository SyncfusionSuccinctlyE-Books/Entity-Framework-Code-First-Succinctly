using System.Data.Entity;

namespace Succinctly.Model
{
	public class CreateProjectsDatabaseWithInitialData : CreateDatabaseIfNotExists<ProjectsContext>
	{
		protected override void Seed(ProjectsContext context)
		{
			var developmentTool = new DevelopmentTool() { Name = "Visual Studio 2012", Language = "C#" };
			var managementTool = new ManagementTool() { Name = "Project 2013", CompatibleWithProject = true };
			var testingTool = new TestingTool() { Name = "Selenium", Automated = true };

			context.Tools.Add(developmentTool);
			context.Tools.Add(managementTool);
			context.Tools.Add(testingTool);

			context.SaveChanges();

			base.Seed(context);
		}
	}
}
