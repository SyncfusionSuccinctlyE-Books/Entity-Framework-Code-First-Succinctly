using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;

namespace Succinctly.Model
{
	public class FilteredDbSet<TEntity> : IDbSet<TEntity>, IOrderedQueryable<TEntity> where TEntity : class
	{
		#region Private readonly fields
		private readonly DbSet<TEntity> _set;
		private readonly Action<TEntity> _initializeEntity;
		private readonly Func<TEntity, Boolean> _matchesFilter;
		#endregion

		#region Public constructors
		public FilteredDbSet(DbContext context, Expression<Func<TEntity, Boolean>> filter) : this(context.Set<TEntity>(), filter, null)
		{
		}

		public FilteredDbSet(DbContext context, Expression<Func<TEntity, Boolean>> filter, Action<TEntity> initializeEntity) : this(context.Set<TEntity>(), filter, initializeEntity)
		{
		}
		#endregion

		#region Private constructor
		private FilteredDbSet(DbSet<TEntity> set, Expression<Func<TEntity, Boolean>> filter, Action<TEntity> initializeEntity)
		{
			this._set = set;
			this.Filter = filter;
			this._matchesFilter = filter.Compile();
			this._initializeEntity = initializeEntity;
		}
		#endregion

		#region Public properties
		public Expression<Func<TEntity, Boolean>> Filter
		{
			get;
			protected set;
		}

		public IQueryable<TEntity> Unfiltered
		{
			get
			{
				return (this._set);
			}
		}
		#endregion

		#region Public methods
		public IQueryable<TEntity> Include(String path)
		{
			return (this._set.Include(path).Where(this.Filter));
		}

		public DbSqlQuery<TEntity> SqlQuery(String sql, params Object[] parameters)
		{
			return (this._set.SqlQuery(sql, parameters));
		}
		#endregion

		#region IDbSet<TEntity> Members
		TEntity IDbSet<TEntity>.Add(TEntity entity)
		{
			this.InitializeEntity(entity);
			this.ThrowIfEntityDoesNotMatchFilter(entity);
			return (this._set.Add(entity));
		}

		TEntity IDbSet<TEntity>.Attach(TEntity entity)
		{
			this.ThrowIfEntityDoesNotMatchFilter(entity);
			return (this._set.Attach(entity));
		}

		TDerivedEntity IDbSet<TEntity>.Create<TDerivedEntity>()
		{
			var entity = this._set.Create<TDerivedEntity>();
			this.InitializeEntity(entity);
			return (entity as TDerivedEntity);
		}

		TEntity IDbSet<TEntity>.Create()
		{
			var entity = this._set.Create();
			this.InitializeEntity(entity);
			return (entity);
		}

		TEntity IDbSet<TEntity>.Find(params Object[] keyValues)
		{
			var entity = this._set.Find(keyValues);
			ThrowIfEntityDoesNotMatchFilter(entity);
			return (entity);
		}

		TEntity IDbSet<TEntity>.Remove(TEntity entity)
		{
			ThrowIfEntityDoesNotMatchFilter(entity);
			return (this._set.Remove(entity));
		}

		ObservableCollection<TEntity> IDbSet<TEntity>.Local
		{
			get { return (this._set.Local); }
		}
		#endregion

		#region IEnumerable<TEntity> Members
		IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator()
		{
			return (this._set.Where(this.Filter).GetEnumerator());
		}
		#endregion

		#region IEnumerable Members
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((this as IEnumerable<TEntity>).GetEnumerator());
		}
		#endregion

		#region IQueryable Members
		Type IQueryable.ElementType
		{
			get { return ((this._set as IQueryable).ElementType); }
		}

		Expression IQueryable.Expression
		{
			get
			{
				return (this._set.Where(this.Filter).Expression);
			}
		}

		IQueryProvider IQueryable.Provider
		{
			get
			{
				return ((this._set as IQueryable).Provider);
			}
		}
		#endregion

		#region Private methods
		private void ThrowIfEntityDoesNotMatchFilter(TEntity entity)
		{
			if ((entity != null) && (this._matchesFilter(entity) == false))
			{
				throw (new ArgumentException("Entity does not match filter", "entity"));
			}
		}

		private void InitializeEntity(TEntity entity)
		{
			if (this._initializeEntity != null)
			{
				this._initializeEntity(entity);
			}
		}
		#endregion
	}
}
