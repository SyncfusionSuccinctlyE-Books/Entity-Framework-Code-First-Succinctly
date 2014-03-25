using System.Data.Entity.ModelConfiguration;

namespace Succinctly.Model
{
	public class CustomerConfiguration : EntityTypeConfiguration<Customer>
	{
		public CustomerConfiguration()
		{
			this.Property(x => x.Name).HasMaxLength(50).IsRequired();
		}
	}
}
