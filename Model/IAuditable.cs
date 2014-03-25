using System;

namespace Succinctly.Model
{
	public interface IAuditable
	{
		String CreatedBy
		{
			get;
			set;
		}

		DateTime CreatedAt
		{
			get;
			set;
		}

		String UpdatedBy
		{
			get;
			set;
		}

		DateTime UpdatedAt
		{
			get;
			set;
		}
	}
}
