using System;

namespace Succinctly.Model
{
	public abstract class Tool
	{
		public Tool()
		{
			this.ToolId = Guid.NewGuid();
		}

		public String Name
		{
			get;
			set;
		}

		public Guid ToolId
		{
			get;
			set;
		}

		public override String ToString()
		{
			return (this.Name);
		}
	}
}
