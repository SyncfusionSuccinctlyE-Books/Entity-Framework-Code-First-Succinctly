using System;
using System.ComponentModel.DataAnnotations;

namespace Succinctly.Model
{
	using System.ComponentModel.DataAnnotations.Schema;

	[ComplexType]
	public class ContactInformation
	{
		[Required]
		[MaxLength(50)]
		public String Email
		{
			get;
			set;
		}

		[MaxLength(20)]
		public String Phone
		{
			get;
			set;
		}

		public override String ToString()
		{
			return (String.Format("Email={0}, Phone={1}", this.Email, this.Phone));
		}
	}
}
