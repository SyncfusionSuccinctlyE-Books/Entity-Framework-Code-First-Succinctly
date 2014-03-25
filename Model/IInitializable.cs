using System.Data.Entity;

namespace Succinctly.Model
{
	public interface IInitializable
	{
		void Initialize(DbContext context);
	}
}
