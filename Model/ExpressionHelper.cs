using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Succinctly.Model
{
	public static class ExpressionHelper
	{
		public static Expression<Func<TEntity, TResult>> GetMember<TEntity, TResult>(String memberName)
		{
			ParameterExpression parameter = Expression.Parameter(typeof(TEntity), "p");
			MemberExpression member = Expression.MakeMemberAccess(parameter, typeof(TEntity).GetMember(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Single());
			Expression<Func<TEntity, TResult>> expression = Expression.Lambda<Func<TEntity, TResult>>(member, parameter);
			return (expression);
		}
	}
}
