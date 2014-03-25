using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;

namespace Succinctly.Model
{
	using System.Runtime.Serialization;

	//[MetadataType(typeof(ProjectMetadata))]
	public partial class Project : IValidatableObject
	{
		/*sealed class ProjectMetadata
		{
			[Required]
			[MaxLength(50)]
			[ConcurrencyCheck]
			public String Name
			{
				get;
				set;
			}
		}*/

		//public int? X { get; set; }

		public Project()
		{
			this.ProjectResources = new HashSet<ProjectResource>();
		}

		public Int32 ProjectId
		{
			get;
			set;
		}

		[Required]
		[MaxLength(50)]
		[ConcurrencyCheck]
		public String Name
		{
			get;
			set;
		}

		public DateTime Start
		{
			get;
			set;
		}

		public DateTime? End
		{
			get;
			set;
		}

		public virtual ProjectDetail Detail
		{
			get;
			set;
		}

		[Required]
		public virtual Customer Customer
		{
			get;
			set;
		}

		public void AddResource(Resource resource, Role role)
		{
			this.ProjectResources.Add(new ProjectResource() { Project = this, Resource = resource, Role = role });
			resource.ProjectResources.Add(new ProjectResource() { Project = this, Resource = resource, Role = role });
		}

		public Resource ProjectManager
		{
			get
			{
				return (this.ProjectResources.ToList().Where(x => x.Role == Role.ProjectManager).Select(x => x.Resource).SingleOrDefault());
			}
		}

		public IEnumerable<Resource> Developers
		{
			get
			{
				return (this.ProjectResources.Where(x => x.Role == Role.Developer).Select(x => x.Resource).ToList());
			}
		}

		public IEnumerable<Resource> Testers
		{
			get
			{
				return (this.ProjectResources.Where(x => x.Role == Role.Tester).Select(x => x.Resource)).ToList();
			}
		}

		public virtual ICollection<ProjectResource> ProjectResources
		{
			get;
			protected set;
		}

		public override String ToString()
		{
			return (this.Name);
		}

		#region IValidatableObject Members

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (this.ProjectManager == null)
			{
				yield return (new ValidationResult("No project manager specified"));
			}

			if (this.Developers.Any() == false)
			{
				yield return (new ValidationResult("No developers specified"));
			}

			if ((this.End != null) && (this.End.Value < this.Start))
			{
				yield return (new ValidationResult("End of project is before start"));
			}
		}

		#endregion
	}
}
