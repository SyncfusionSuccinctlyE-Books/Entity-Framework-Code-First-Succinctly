using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Metadata.Edm;
using System.Data.Objects;
using System.Data.Objects.DataClasses;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;

namespace Succinctly.Model
{
	using System.IO.Compression;
	using System.Text;

	public static class DbContextExtensions
	{
		private static readonly Regex ParametersRegex = new Regex("{(?<x>\\d+)}");//, RegexOptions.Compiled | RegexOptions.ExplicitCapture);

		[System.Data.Objects.DataClasses.EdmFunction("SqlServer", "SOUNDEX")]
		public static String Soundex(this String phrase)
		{
			throw (new NotImplementedException());
		}

		#region Delete
		public static void Delete(this DbContext ctx, Object entity)
		{
			ctx.Entry(entity).State = EntityState.Deleted;
		}
		#endregion

		#region Keys
		public static IDictionary<String, Object> GetKeys(this DbContext ctx, Object entity)
		{
			return ((ctx as IObjectContextAdapter).ObjectContext.ObjectStateManager.GetObjectStateEntry(entity).EntityKey.EntityKeyValues.ToDictionary(x => x.Key, x => x.Value));
		}
		#endregion

		#region Metadata
		public static DataTable GetSchema(this DbContext ctx)
		{
			if (ctx.Database.Connection.State == ConnectionState.Closed)
			{
				ctx.Database.Connection.Open();
			}

			DataTable schema = ctx.Database.Connection.GetSchema();

			ctx.Database.Connection.Close();

			return (schema);
		}

		public static IDictionary<Type, String> GetTables(this DbContext ctx)
		{
			ObjectContext octx = (ctx as IObjectContextAdapter).ObjectContext;
			IEnumerable<EntityType> entities = octx.MetadataWorkspace.GetItemCollection(DataSpace.OSpace).GetItems<EntityType>().ToList();

			return(entities.ToDictionary(x => Type.GetType(x.FullName), x => GetTableName(ctx, Type.GetType(x.FullName))));
		}

		public static IEnumerable<PropertyInfo> ManyToMany(this DbContext ctx, Type entityType)
		{
			ObjectContext octx = (ctx as IObjectContextAdapter).ObjectContext;
			EntityType et = octx.MetadataWorkspace.GetItems(DataSpace.OSpace).Where(x => x.BuiltInTypeKind == BuiltInTypeKind.EntityType).OfType<EntityType>().Where(x => x.Name == entityType.Name).Single();
			
			return(et.NavigationProperties.Where(x => x.FromEndMember.RelationshipMultiplicity == RelationshipMultiplicity.Many && x.ToEndMember.RelationshipMultiplicity == RelationshipMultiplicity.Many).Select(x => entityType.GetProperty(x.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.SetProperty)).ToList());
		}

		public static IEnumerable<PropertyInfo> ManyToMany<T>(this DbContext ctx)
		{
			return (ManyToMany(ctx, typeof(T)));
		}

		public static IEnumerable<PropertyInfo> OneToMany(this DbContext ctx, Type entityType)
		{
			ObjectContext octx = (ctx as IObjectContextAdapter).ObjectContext;
			EntityType et = octx.MetadataWorkspace.GetItems(DataSpace.OSpace).Where(x => x.BuiltInTypeKind == BuiltInTypeKind.EntityType).OfType<EntityType>().Where(x => x.Name == entityType.Name).Single();

			return (et.NavigationProperties.Where(x => x.FromEndMember.RelationshipMultiplicity == RelationshipMultiplicity.One && x.ToEndMember.RelationshipMultiplicity == RelationshipMultiplicity.Many).Select(x => entityType.GetProperty(x.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.SetProperty)).ToList());
		}

		public static IEnumerable<PropertyInfo> OneToMany<T>(this DbContext ctx)
		{
			return (OneToMany(ctx, typeof(T)));
		}

		public static IEnumerable<PropertyInfo> OneToOne(this DbContext ctx, Type entityType)
		{
			ObjectContext octx = (ctx as IObjectContextAdapter).ObjectContext;
			EntityType et = octx.MetadataWorkspace.GetItems(DataSpace.OSpace).Where(x => x.BuiltInTypeKind == BuiltInTypeKind.EntityType).OfType<EntityType>().Where(x => x.Name == entityType.Name).Single();

			return (et.NavigationProperties.Where(x => (x.FromEndMember.RelationshipMultiplicity == RelationshipMultiplicity.One || x.FromEndMember.RelationshipMultiplicity == RelationshipMultiplicity.ZeroOrOne) && (x.ToEndMember.RelationshipMultiplicity == RelationshipMultiplicity.One || x.ToEndMember.RelationshipMultiplicity == RelationshipMultiplicity.ZeroOrOne)).Select(x => entityType.GetProperty(x.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.SetProperty)).ToList());
		}

		public static IEnumerable<PropertyInfo> OneToOne<T>(this DbContext ctx)
		{
			return (OneToOne(ctx, typeof(T)));
		}

		public static IEnumerable<PropertyInfo> ManyToOne(this DbContext ctx, Type entityType)
		{
			ObjectContext octx = (ctx as IObjectContextAdapter).ObjectContext;
			EntityType et = octx.MetadataWorkspace.GetItems(DataSpace.OSpace).Where(x => x.BuiltInTypeKind == BuiltInTypeKind.EntityType).OfType<EntityType>().Where(x => x.Name == entityType.Name).Single();

			return (et.NavigationProperties.Where(x => x.FromEndMember.RelationshipMultiplicity == RelationshipMultiplicity.Many && x.ToEndMember.RelationshipMultiplicity == RelationshipMultiplicity.One).Select(x => entityType.GetProperty(x.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.SetProperty)).ToList());
		}

		public static IEnumerable<PropertyInfo> ManyToOne<T>(this DbContext ctx)
		{
			return (ManyToOne(ctx, typeof(T)));
		}

		public static IEnumerable<PropertyInfo> GetIdProperties(this DbContext ctx, Type entityType)
		{
			ObjectContext octx = (ctx as IObjectContextAdapter).ObjectContext;
			EntityType et = octx.MetadataWorkspace.GetItems(DataSpace.OSpace).Where(x => x.BuiltInTypeKind == BuiltInTypeKind.EntityType).OfType<EntityType>().Where(x => x.Name == entityType.Name).Single();

			return (et.KeyMembers.Select(x => entityType.GetProperty(x.Name)).ToList());
		}

		public static IEnumerable<PropertyInfo> GetIdProperties<T>(this DbContext ctx)
		{
			return (GetIdProperties(ctx, typeof(T)));
		}

		public static IEnumerable<PropertyInfo> GetProperties(this DbContext ctx, Type entityType)
		{
			ObjectContext octx = (ctx as IObjectContextAdapter).ObjectContext;
			EntityType et = octx.MetadataWorkspace.GetItems(DataSpace.OSpace).Where(x => x.BuiltInTypeKind == BuiltInTypeKind.EntityType).OfType<EntityType>().Where(x => x.Name == entityType.Name).Single();

			return (et.Properties.Select(x => entityType.GetProperty(x.Name)).ToList());
		}

		public static IEnumerable<PropertyInfo> GetProperties<T>(this DbContext ctx)
		{
			return (GetProperties(ctx, typeof(T)));
		}

		public static IEnumerable<PropertyInfo> GetNavigationProperties(this DbContext ctx, Type entityType)
		{
			ObjectContext octx = (ctx as IObjectContextAdapter).ObjectContext;
			EntityType et = octx.MetadataWorkspace.GetItems(DataSpace.OSpace).Where(x => x.BuiltInTypeKind == BuiltInTypeKind.EntityType).OfType<EntityType>().Where(x => x.Name == entityType.Name).Single();

			return (et.NavigationProperties.Select(x => entityType.GetProperty(x.Name)).ToList());
		}

		public static IEnumerable<PropertyInfo> GetNavigationProperties<T>(this DbContext ctx)
		{
			return (GetNavigationProperties(ctx, typeof(T)));
		}

		public static IDictionary<String, String> GetTableColumnsAndTypes(this DbContext ctx, Type entityType)
		{
			ObjectContext octx = (ctx as IObjectContextAdapter).ObjectContext;
			EntityType et = octx.MetadataWorkspace.GetItems(DataSpace.SSpace).Where(x => x.BuiltInTypeKind == BuiltInTypeKind.EntityType).OfType<EntityType>().Where(x => x.Name == entityType.Name).Single();

			return (et.Members.ToDictionary(x => x.Name, x => x.TypeUsage.EdmType.Name + (x.TypeUsage.Facets.Any(y => y.Name == "MaxLength") ? String.Format("({0})", x.TypeUsage.Facets["MaxLength"].Value) : String.Empty)));
		}

		public static IDictionary<String, String> GetTableColumnsAndTypes<T>(this DbContext ctx)
		{
			return (GetTableColumnsAndTypes(ctx, typeof(T)));
		}

		public static String GetTableName(this DbContext ctx, Type entityType)
		{
			ObjectContext octx = (ctx as IObjectContextAdapter).ObjectContext;
			EntitySetBase et = octx.MetadataWorkspace.GetItemCollection(DataSpace.SSpace)
				.GetItems<EntityContainer>()
				.Single()
				.BaseEntitySets
				.Where(x => x.Name == entityType.Name)
				.Single();

			String tableName = String.Concat(et.MetadataProperties["Schema"].Value, ".", et.MetadataProperties["Table"].Value);

			return (tableName);
		}

		public static String GetTableName<T>(this DbContext ctx)
		{
			return (GetTableName(ctx, typeof(T)));
		}

		public static IDictionary<String, PropertyInfo> GetTableKeyColumns(this DbContext ctx, Type entityType)
		{
			ObjectContext octx = (ctx as IObjectContextAdapter).ObjectContext;
			EntityType storageEntityType = octx.MetadataWorkspace.GetItems(DataSpace.SSpace).Where(x => x.BuiltInTypeKind == BuiltInTypeKind.EntityType).OfType<EntityType>().Where(x => x.Name == entityType.Name).Single();
			EntityType objectEntityType = octx.MetadataWorkspace.GetItems(DataSpace.OSpace).Where(x => x.BuiltInTypeKind == BuiltInTypeKind.EntityType).OfType<EntityType>().Where(x => x.Name == entityType.Name).Single();
			return (storageEntityType.KeyMembers.Select((elm, index) => new { elm.Name, Property = entityType.GetProperty((objectEntityType.MetadataProperties["Members"].Value as IEnumerable<EdmMember>).ElementAt(index).Name) }).ToDictionary(x => x.Name, x => x.Property));
		}

		public static IDictionary<String, PropertyInfo> GetTableKeyColumns<T>(this DbContext ctx)
		{
			return (GetTableKeyColumns(ctx, typeof(T)));
		}

		public static IDictionary<String, PropertyInfo> GetTableColumns(this DbContext ctx, Type entityType)
		{
			ObjectContext octx = (ctx as IObjectContextAdapter).ObjectContext;
			EntityType storageEntityType = octx.MetadataWorkspace.GetItems(DataSpace.SSpace).Where(x => x.BuiltInTypeKind == BuiltInTypeKind.EntityType).OfType<EntityType>().Where(x => x.Name == entityType.Name).Single();
			EntityType objectEntityType = octx.MetadataWorkspace.GetItems(DataSpace.OSpace).Where(x => x.BuiltInTypeKind == BuiltInTypeKind.EntityType).OfType<EntityType>().Where(x => x.Name == entityType.Name).Single();
			return (storageEntityType.Members.Select((elm, index) => new { elm.Name, Property = entityType.GetProperty(objectEntityType.Members[index].Name) }).ToDictionary(x => x.Name, x => x.Property));
		}

		public static IDictionary<String, PropertyInfo> GetTableColumns<T>(this DbContext ctx)
		{
			return (GetTableColumns(ctx, typeof(T)));
		}

		public static String GetTableColumnName(this DbContext ctx, Type entityType, String propertyName)
		{
			return(GetTableColumns(ctx, entityType).Where(x => x.Value.Name == propertyName).Select(x => x.Key).Single());
		}

		public static String GetTableColumnName<T>(this DbContext ctx, String propertyName)
		{
			return (GetTableColumnName(ctx, typeof(T), propertyName));
		}

		public static IDictionary<String, PropertyInfo> GetTableNavigationColumns(this DbContext ctx, Type entityType)
		{
			ObjectContext octx = (ctx as IObjectContextAdapter).ObjectContext;
			EntityType storageEntityType = octx.MetadataWorkspace.GetItems(DataSpace.SSpace).Where(x => x.BuiltInTypeKind == BuiltInTypeKind.EntityType).OfType<EntityType>().Where(x => x.Name == entityType.Name).Single();
			EntityType objectEntityType = octx.MetadataWorkspace.GetItems(DataSpace.OSpace).Where(x => x.BuiltInTypeKind == BuiltInTypeKind.EntityType).OfType<EntityType>().Where(x => x.Name == entityType.Name).Single();
			return (storageEntityType.NavigationProperties.Select((elm, index) => new { elm.Name, Property = entityType.GetProperty(objectEntityType.Members[index].Name) }).ToDictionary(x => x.Name, x => x.Property));
		}

		public static IDictionary<String, PropertyInfo> GetTableNavigationColumns<T>(this DbContext ctx)
		{
			return (GetTableNavigationColumns(ctx, typeof(T)));
		}
		#endregion

		#region Filters
		public static void Filter<TParentEntity, TCollectionEntity>(this DbContext context, String navigationProperty, Expression<Func<TCollectionEntity, Boolean>> filter)
			where TParentEntity : class, new()
			where TCollectionEntity : class
		{
			(context as IObjectContextAdapter).ObjectContext.ObjectMaterialized += delegate(Object sender, ObjectMaterializedEventArgs e)
			{
				if (e.Entity is TParentEntity)
				{
					DbCollectionEntry col = context.Entry(e.Entity).Collection(navigationProperty);
					col.CurrentValue = new FilteredCollection<TCollectionEntity>(null, col, filter);
				}
			};
		}

		public static void Filter<TContext, TParentEntity, TCollectionEntity>(this TContext context, Expression<Func<TContext, IDbSet<TParentEntity>>> path, Expression<Func<TParentEntity, ICollection<TCollectionEntity>>> collection, Expression<Func<TCollectionEntity, Boolean>> filter)
			where TContext : DbContext
			where TParentEntity : class, new()
			where TCollectionEntity : class
		{
			Filter(context, collection, filter);
		}

		public static void Filter<TParentEntity, TCollectionEntity>(this DbContext context, Expression<Func<TParentEntity, ICollection<TCollectionEntity>>> path, Expression<Func<TCollectionEntity, Boolean>> filter)
			where TParentEntity : class, new()
			where TCollectionEntity : class
		{
			String navigationProperty = path.ToString().Split('.')[1];

			Filter<TParentEntity, TCollectionEntity>(context, navigationProperty, filter);
		}
		#endregion

		#region Entity SQL
		public static IQueryable<T> CreateQuery<T>(this DbContext ctx, String esql, params Object[] parameters)
		{
			ObjectContext octx = (ctx as IObjectContextAdapter).ObjectContext;
			String resql = ParametersRegex.Replace(esql, "@p${x}");
			return (octx.CreateQuery<T>(resql, parameters.Select((elm, index) => new ObjectParameter(String.Format("p{0}", index), elm)).ToArray()));
		}
		#endregion

		#region Classic

		public static T ExecuteScalar<T>(this DbContext ctx, String sql, params Object[] parameters)
		{
			return ((T)ExecuteScalar(ctx, sql, parameters));
		}

		public static Object ExecuteScalar(this DbContext ctx, String sql, params Object[] parameters)
		{
			using (DbCommand cmd = ctx.Database.Connection.CreateCommand())
			{
				cmd.CommandText = ParametersRegex.Replace(sql, "@p${x}");

				for (Int32 i = 0; i < parameters.Length; ++i)
				{
					DbParameter parameter = cmd.CreateParameter();
					parameter.ParameterName = String.Format("p{0}", i);
					parameter.Value = parameters[i];

					cmd.Parameters.Add(parameter);
				}

				if (ctx.Database.Connection.State == ConnectionState.Closed)
				{
					ctx.Database.Connection.Open();
				}

				return (cmd.ExecuteScalar());
			}
		}

		public static Int32 ExecuteNonQuery(this DbContext ctx, String sql, params Object[] parameters)
		{
			using (DbCommand cmd = ctx.Database.Connection.CreateCommand())
			{
				cmd.CommandText = ParametersRegex.Replace(sql, "@p${x}");

				for (Int32 i = 0; i < parameters.Length; ++i)
				{
					DbParameter parameter = cmd.CreateParameter();
					parameter.ParameterName = String.Format("p{0}", i);
					parameter.Value = parameters[i];

					cmd.Parameters.Add(parameter);
				}

				if (ctx.Database.Connection.State == ConnectionState.Closed)
				{
					ctx.Database.Connection.Open();
				}

				return (cmd.ExecuteNonQuery());
			}
		}
		#endregion

		#region Change tracking
		public static Boolean IsModified<T>(this DbContext ctx, T entity) where T : class
		{
			var entry = ctx.Entry(entity);

			return ((entry != null) && (entry.State == EntityState.Modified));
		}

		public static Boolean IsModified<T>(this DbContext ctx, T entity, Expression<Func<T, Object>> property) where T : class
		{
			var entry = ctx.Entry(entity);

			return ((entry != null) && (entry.Property(property).CurrentValue != entry.Property(property).OriginalValue));
		}

		public static Boolean IsModified<T>(this DbContext ctx, T entity, String propertyName) where T : class
		{
			var entry = ctx.Entry(entity);

			return ((entry != null) && (entry.Property(propertyName).CurrentValue != entry.Property(propertyName).OriginalValue));
		}

		public static void Reset<T>(this DbContext ctx, T entity) where T : class
		{
			var entry = ctx.Entry(entity);

			if ((entry != null) && (entry.State == EntityState.Modified))
			{
				entry.CurrentValues.SetValues(entry.OriginalValues);
				entry.State = EntityState.Unchanged;
			}
		}

		public static void Reset<T>(this DbContext ctx, T entity, Expression<Func<T, Object>> property) where T : class
		{
			var entry = ctx.Entry(entity);

			if ((entry != null) && (entry.State == EntityState.Modified))
			{
				entry.Property(property).CurrentValue = entry.Property(property).OriginalValue;
				entry.Property(property).IsModified = false;
			}
		}

		public static void Reset<T>(this DbContext ctx, T entity, String propertyName) where T : class
		{
			var entry = ctx.Entry(entity);

			if ((entry != null) && (entry.State == EntityState.Modified))
			{
				entry.Property(propertyName).CurrentValue = entry.OriginalValues[propertyName];
				entry.Property(propertyName).IsModified = false;
			}
		}
		#endregion

		#region Lazy loading
		public static Boolean IsLoaded<T>(this DbContext ctx, T entity, Expression<Func<T, ICollection<Object>>> property) where T : class
		{
			return (ctx.Entry(entity).Collection(property).IsLoaded);
		}

		public static Boolean IsLoaded<T>(this DbContext ctx, T entity, Expression<Func<T, Object>> property) where T : class
		{
			return (ctx.Entry(entity).Reference(property).IsLoaded);
		}

		public static Boolean IsLoaded<T>(this DbContext ctx, T entity, String propertyName) where T : class
		{
			if (ctx.Entry(entity).Reference(propertyName) != null)
			{
				return (ctx.Entry(entity).Reference(propertyName).IsLoaded);
			}
			else
			{
				return (ctx.Entry(entity).Collection(propertyName).IsLoaded);
			}
		}

		public static void Load<T>(this DbContext ctx, T entity, Expression<Func<T, ICollection<Object>>> property) where T : class
		{
			ctx.Entry(entity).Collection(property).Load();
		}

		public static void Load<T>(this DbContext ctx, T entity, Expression<Func<T, Object>> property) where T : class
		{
			ctx.Entry(entity).Reference(property).Load();
		}

		public static void Load<T>(this DbContext ctx, T entity, String propertyName) where T : class
		{
			if (ctx.Entry(entity).Reference(propertyName) != null)
			{
				ctx.Entry(entity).Reference(propertyName).Load();
			}
			else
			{
				ctx.Entry(entity).Collection(propertyName).Load();
			}
		}

		public static T LoadEverything<T>(this DbContext ctx, T entity) where T : class
		{
			ObjectContext octx = (ctx as IObjectContextAdapter).ObjectContext;
			var ends = octx.ObjectStateManager.GetRelationshipManager(entity).GetAllRelatedEnds();

			foreach (IRelatedEnd end in ends)
			{
				end.Load();
			}

			return (entity);
		}
		#endregion

		public static String GetEdmxFromDatabase(this DbContext ctx)
		{
			using (var memory = new MemoryStream())
			{
				var sql = "DECLARE @binaryContent VARBINARY(MAX); " +
						"SELECT @binaryContent = Model FROM [dbo].[__MigrationHistory]; "
					  + "SELECT CAST('' AS XML).value('xs:base64Binary(sql:variable(\"@binaryContent\"))', 'NVARCHAR(MAX)')";

				var modelBase64 = ctx.Database.SqlQuery<String>(sql).Single();
				var modelBytesGZipped = Convert.FromBase64String(modelBase64);

				using (var stream = new GZipStream(new MemoryStream(modelBytesGZipped), CompressionMode.Decompress))
				{
					var buffer = new Byte[8 * 1024];

					do
					{
						var count = stream.Read(buffer, 0, buffer.Length);

						if (count <= 0)
						{
							break;
						}

						memory.Write(buffer, 0, count);
					}
					while (true);

					var modelEdmx = Encoding.UTF8.GetString(memory.GetBuffer());

					return (modelEdmx);
				}
			}
		}

		public static void ExportDatabaseToFile(this DbContext ctx, String filename)
		{
			ObjectContext octx = (ctx as IObjectContextAdapter).ObjectContext;

			File.WriteAllText(filename, octx.CreateDatabaseScript());
		}

		public static void ExportModelToFile(this DbContext ctx, String filename)
		{
			XmlWriterSettings settings = new XmlWriterSettings { Indent = true };

			 using (XmlWriter writer = XmlWriter.Create(filename, settings))  
			 {  
				 EdmxWriter.WriteEdmx(ctx, writer);  
			 }  
		}

		public static IQueryable<T> LocalOrDatabase<T>(this DbContext context, Expression<Func<T, Boolean>> expression) where T : class
		{
			IEnumerable<T> localResults = context.Set<T>().Local.Where(expression.Compile());

			if (localResults.Any() == true)
			{
				return (localResults.AsQueryable());
			}

			return (context.Set<T>().Where(expression));
		}

		public static TEntity AddOrUpdate<TEntity>(DbContext context, TEntity entity) where TEntity : class
		{
			context.Entry(entity).State = (context.Set<TEntity>().Local.Contains(entity) == true) ? EntityState.Modified : EntityState.Added;

			return (entity);
		}
	}
}