using System;
using System.Data.Entity.Infrastructure;
using System.Data.Objects;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Succinctly.Model
{
	public static class QueryableExtensions
	{
		#region Sql String
		public static String ToSqlString<TEntity>(this IQueryable<TEntity> queryable) where TEntity : class
		{
			ObjectQuery<TEntity> objectQuery = null;

			if (queryable is ObjectQuery<TEntity>)
			{
				objectQuery = queryable as ObjectQuery<TEntity>;
			}
			else if (queryable is DbQuery<TEntity>)
			{
				DbQuery<TEntity> dbQuery = queryable as DbQuery<TEntity>;

				PropertyInfo iqProp = dbQuery.GetType().GetProperty("InternalQuery", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

				Object iq = iqProp.GetValue(dbQuery);

				PropertyInfo oqProp = iq.GetType().GetProperty("ObjectQuery", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

				objectQuery = oqProp.GetValue(iq) as ObjectQuery<TEntity>;
			}
			else
			{
				throw (new ArgumentException("queryable"));
			}

			String sqlString = objectQuery.ToTraceString();

			foreach (ObjectParameter objectParam in objectQuery.Parameters)
			{
				if ((objectParam.ParameterType == typeof(String)) || (objectParam.ParameterType == typeof(DateTime)) || (objectParam.ParameterType == typeof(DateTime?)))
				{
					sqlString = sqlString.Replace(String.Format("@{0}", objectParam.Name), String.Format("'{0}'", objectParam.Value.ToString()));
				}
				else if ((objectParam.ParameterType == typeof(Boolean)) || (objectParam.ParameterType == typeof(Boolean?)))
				{
					sqlString = sqlString.Replace(String.Format("@{0}", objectParam.Name), String.Format("{0}", Boolean.Parse(objectParam.Value.ToString()) ? 1 : 0));
				}
				else
				{
					sqlString = sqlString.Replace(String.Format("@{0}", objectParam.Name), String.Format("{0}", objectParam.Value.ToString()));
				}
			}

			return (sqlString);
		}
		#endregion

		#region Compare
		public enum Operand
		{
			Equal,
			NotEqual,
			GreaterThan,
			GreaterThanOrEqual,
			LessThan,
			LessThanOrEqual,
			TypeIs,
			IsNull,
			IsNotNull
		}

		public static IQueryable<TSource> Compare<TSource>(this IQueryable<TSource> query, Operand op, String propertyName, Object value = null)
		{
			Type type = typeof(TSource);
			ParameterExpression pe = Expression.Parameter(type, "p");
			MemberExpression propertyReference = Expression.Property(pe, propertyName);
			ConstantExpression constantReference = Expression.Constant(value);

			switch (op)
			{
				case Operand.Equal:
					return (query.Where(Expression.Lambda<Func<TSource, Boolean>>(Expression.Equal(propertyReference, constantReference), new ParameterExpression[] { pe })));

				case Operand.NotEqual:
					return (query.Where(Expression.Lambda<Func<TSource, Boolean>>(Expression.NotEqual(propertyReference, constantReference), new ParameterExpression[] { pe })));

				case Operand.GreaterThan:
					return (query.Where(Expression.Lambda<Func<TSource, Boolean>>(Expression.GreaterThan(propertyReference, constantReference), new ParameterExpression[] { pe })));

				case Operand.GreaterThanOrEqual:
					return (query.Where(Expression.Lambda<Func<TSource, Boolean>>(Expression.GreaterThanOrEqual(propertyReference, constantReference), new ParameterExpression[] { pe })));

				case Operand.LessThan:
					return (query.Where(Expression.Lambda<Func<TSource, Boolean>>(Expression.LessThan(propertyReference, constantReference), new ParameterExpression[] { pe })));

				case Operand.LessThanOrEqual:
					return (query.Where(Expression.Lambda<Func<TSource, Boolean>>(Expression.LessThanOrEqual(propertyReference, constantReference), new ParameterExpression[] { pe })));

				case Operand.TypeIs:
					return (query.Where(Expression.Lambda<Func<TSource, Boolean>>(Expression.TypeIs(propertyReference, value as Type))));

				case Operand.IsNull:
					return (query.Where(Expression.Lambda<Func<TSource, Boolean>>(Expression.Equal(propertyReference, constantReference), new ParameterExpression[] { pe })));

				case Operand.IsNotNull:
					return (query.Where(Expression.Lambda<Func<TSource, Boolean>>(Expression.NotEqual(propertyReference, constantReference), new ParameterExpression[] { pe })));

			}

			throw (new NotImplementedException("Operand is not implemented"));
		}
		#endregion

		#region Order
		public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> query, String propertyName)
		{
			PropertyInfo propInfo = typeof(T).GetProperty(propertyName);
			Type propType = propInfo.PropertyType;
			Type delegateType = typeof(Func<,>).MakeGenericType(typeof(T), propType);
			ParameterExpression exprParam = Expression.Parameter(typeof(T), "it");
			MemberExpression exprProp = Expression.Property(exprParam, propertyName);
			LambdaExpression exprLambda = Expression.Lambda(delegateType, exprProp, new ParameterExpression[] { exprParam });
			Object[] args = new Object[] { query, exprLambda };

			MethodInfo orderByMethod = typeof(Queryable).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod).Where(x => x.Name == "OrderBy" && x.GetParameters().Length == 2).Single();
			orderByMethod = orderByMethod.MakeGenericMethod(typeof(T), propType);

			query = orderByMethod.Invoke(null, args) as IOrderedQueryable<T>;

			return (query as IOrderedQueryable<T>);
		}

		public static IOrderedQueryable<T> OrderByDescending<T>(this IQueryable<T> query, String propertyName)
		{
			PropertyInfo propInfo = typeof(T).GetProperty(propertyName);
			Type propType = propInfo.PropertyType;
			Type delegateType = typeof(Func<,>).MakeGenericType(typeof(T), propType);
			ParameterExpression exprParam = Expression.Parameter(typeof(T), "it");
			MemberExpression exprProp = Expression.Property(exprParam, propertyName);
			LambdaExpression exprLambda = Expression.Lambda(delegateType, exprProp, new ParameterExpression[] { exprParam });
			Object[] args = new Object[] { query, exprLambda };

			MethodInfo fetchMethodInfo = typeof(Queryable).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod).Where(x => x.Name == "OrderByDescending" && x.GetParameters().Length == 2).Single();
			fetchMethodInfo = fetchMethodInfo.MakeGenericMethod(typeof(T), propType);

			query = fetchMethodInfo.Invoke(null, args) as IOrderedQueryable<T>;

			return (query as IOrderedQueryable<T>);
		}

		public static IOrderedQueryable<T> ThenBy<T>(this IOrderedQueryable<T> query, String propertyName)
		{
			PropertyInfo propInfo = typeof(T).GetProperty(propertyName);
			Type propType = propInfo.PropertyType;
			Type delegateType = typeof(Func<,>).MakeGenericType(typeof(T), propType);
			ParameterExpression exprParam = Expression.Parameter(typeof(T), "it");
			MemberExpression exprProp = Expression.Property(exprParam, propertyName);
			LambdaExpression exprLambda = Expression.Lambda(delegateType, exprProp, new ParameterExpression[] { exprParam });
			Object[] args = new Object[] { query, exprLambda };

			MethodInfo fetchMethodInfo = typeof(Queryable).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod).Where(x => x.Name == "ThenBy" && x.GetParameters().Length == 2).Single();
			fetchMethodInfo = fetchMethodInfo.MakeGenericMethod(typeof(T), propType);

			query = fetchMethodInfo.Invoke(null, args) as IOrderedQueryable<T>;

			return (query as IOrderedQueryable<T>);
		}

		public static IOrderedQueryable<T> ThenByDescending<T>(this IOrderedQueryable<T> query, String propertyName)
		{
			PropertyInfo propInfo = typeof(T).GetProperty(propertyName);
			Type propType = propInfo.PropertyType;
			Type delegateType = typeof(Func<,>).MakeGenericType(typeof(T), propType);
			ParameterExpression exprParam = Expression.Parameter(typeof(T), "it");
			MemberExpression exprProp = Expression.Property(exprParam, propertyName);
			LambdaExpression exprLambda = Expression.Lambda(delegateType, exprProp, new ParameterExpression[] { exprParam });
			Object[] args = new Object[] { query, exprLambda };

			MethodInfo fetchMethodInfo = typeof(Queryable).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod).Where(x => x.Name == "ThenByDescending" && x.GetParameters().Length == 2).Single();
			fetchMethodInfo = fetchMethodInfo.MakeGenericMethod(typeof(T), propType);

			query = fetchMethodInfo.Invoke(null, args) as IOrderedQueryable<T>;

			return (query as IOrderedQueryable<T>);
		}
		#endregion

		#region Between
		public static IQueryable<TSource> Between<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> property, TKey low, TKey high) where TKey : IComparable<TKey>
		{
			ParameterExpression sourceParameter = Expression.Parameter(typeof(TSource));
			Expression body = property.Body;
			ParameterExpression parameter = property.Parameters[0];
			MethodInfo compareMethod = typeof(TKey).GetMethod("CompareTo", new Type[] { typeof(TKey) });
			ConstantExpression zero = Expression.Constant(0, typeof(Int32));
			Expression upper = Expression.LessThanOrEqual(Expression.Call(body, compareMethod, Expression.Constant(high)), zero);
			Expression lower = Expression.GreaterThanOrEqual(Expression.Call(body, compareMethod, Expression.Constant(low)), zero);
			Expression andExpression = Expression.AndAlso(upper, lower);

			MethodCallExpression whereCallExpression = Expression.Call
			(
				typeof(Queryable),
				"Where",
				new Type[] { source.ElementType },
				source.Expression,
				Expression.Lambda<Func<TSource, Boolean>>(andExpression, new ParameterExpression[] { parameter })
			);

			return (source.Provider.CreateQuery<TSource>(whereCallExpression));
		}
		#endregion
	}
}
