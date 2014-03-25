using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Succinctly.Model
{
	[Table("ManagementTool")]
	public class ManagementTool : Tool
	{
		public Boolean CompatibleWithProject
		{
			get;
			set;
		}
	}
}
