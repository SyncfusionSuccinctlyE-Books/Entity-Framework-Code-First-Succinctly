using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Succinctly.Model
{
	[Table("DevelopmentTool")]
	public class DevelopmentTool : Tool
	{
		public String Language
		{
			get;
			set;
		}
	}
}
