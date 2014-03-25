using System.Data.Entity.Infrastructure;
using System.Data.Objects;
using System.Data.Services;
using System.Data.Services.Common;
using System.ServiceModel;
using Succinctly.Model;

namespace Succinctly.Service
{
	[ServiceBehavior(IncludeExceptionDetailInFaults = true)]
	public class ProjectsService : DataService<ObjectContext>
	{
		public static void InitializeService(DataServiceConfiguration config)
		{
			config.SetEntitySetAccessRule("*", EntitySetRights.AllRead);
			config.SetServiceOperationAccessRule("*", ServiceOperationRights.All);
			config.DataServiceBehavior.MaxProtocolVersion = DataServiceProtocolVersion.V3;
			config.UseVerboseErrors = true;
		}

		protected override ObjectContext CreateDataSource()
		{
			ProjectsContext ctx = new ProjectsContext();
			ObjectContext octx = (ctx as IObjectContextAdapter).ObjectContext;

			return (octx);
		}
	}
}
