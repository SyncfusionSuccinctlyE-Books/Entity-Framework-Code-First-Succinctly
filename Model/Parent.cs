using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Succinctly.Model
{
	public partial class Parent
	{
		public Parent()
		{
			this.CurrentChildren = new HashSet<Child>();
			this.PastChildren = new HashSet<Child>();
		}

		[DatabaseGenerated(DatabaseGeneratedOption.None)]
		public Int32 ParentId { get; set; }

		public String Name { get; set; }

		[InverseProperty("CurrentParent")]
		public virtual ICollection<Child> CurrentChildren { get; protected set; }

		[InverseProperty("PastParent")]
		public virtual ICollection<Child> PastChildren { get; protected set; }
	}

	public partial class Child
	{
		public Int32 ChildId { get; set; }

		public String Name { get; set; }

		public virtual Parent CurrentParent { get; set; }

		public virtual Parent PastParent { get; set; }
	}
}
