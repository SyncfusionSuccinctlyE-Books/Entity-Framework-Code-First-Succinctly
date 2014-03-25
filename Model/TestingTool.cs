using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Succinctly.Model
{
	[Table("TestingTool")]
	public class TestingTool : Tool
	{
		public Boolean Automated
		{
			get;
			set;
		}
	}
}
