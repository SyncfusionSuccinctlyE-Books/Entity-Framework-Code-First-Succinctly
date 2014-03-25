using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Succinctly.Model
{
	using System.ComponentModel.DataAnnotations.Schema;

	public class Resource
	{
		public Resource()
		{
			this.ProjectResources = new HashSet<ProjectResource>();
			this.Technologies = new HashSet<Technology>();
			this.Contact = new ContactInformation();
		}

		public Int32 ResourceId
		{
			get;
			set;
		}

		public virtual ContactInformation Contact
		{
			get;
			protected set;
		}

		[Required]
		[MaxLength(50)]
		public String Name
		{
			get;
			set;
		}

		public virtual ICollection<Technology> Technologies
		{
			get;
			protected set;
		}

		public virtual ICollection<ProjectResource> ProjectResources
		{
			get;
			protected set;
		}

		public IEnumerable<Project> Projects
		{
			get
			{
				return (this.ProjectResources.Select(x => x.Project).Distinct());
			}
		}

		public override String ToString()
		{
			return (this.Name);
		}
	}
}
