using System;
using System.ComponentModel.DataAnnotations;

namespace Succinctly.Model
{
	using System.ComponentModel.DataAnnotations.Schema;

	public class ProjectResource
	{
		public Int32 ProjectResourceId
		{
			get;
			set;
		}

		[Required]
		public virtual Project Project
		{
			get;
			set;
		}

		[Required]
		public virtual Resource Resource
		{
			get;
			set;
		}

		public Role Role
		{
			get;
			set;
		}
	}
}
