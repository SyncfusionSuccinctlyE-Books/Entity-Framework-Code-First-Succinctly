using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Succinctly.Model
{
	public class Technology
	{
		public Technology()
		{
			this.Resources = new HashSet<Resource>();
		}

		public Int32 TechnologyId
		{
			get;
			set;
		}

		[Required]
		[MaxLength(50)]
		public String Name
		{
			get;
			set;
		}

		public virtual ICollection<Resource> Resources
		{
			get;
			protected set;
		}

		public override String ToString()
		{
			return (this.Name);
		}
	}
}
