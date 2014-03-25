using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Succinctly.Model
{
	public class ProjectDetail
	{
		[Key]
		[ForeignKey("Project")]
		public Int32 ProjectId
		{
			get;
			set;
		}

		[Required]
		public Project Project
		{
			get;
			set;
		}

		//[IsEven(ErrorMessage = "Budget must be an even number")]
		[CustomValidation(typeof(ProjectDetail), "IsEven", ErrorMessage = "Budget must be an even number")]
		public Decimal Budget
		{
			get;
			set;
		}

		public Boolean Critical
		{
			get;
			set;
		}

		public static ValidationResult IsEven(Object value, ValidationContext validationContext)
		{
			//check if the value is not empty
			if ((value != null) && (String.IsNullOrWhiteSpace(value.ToString()) == false))
			{
				TypeConverter converter = TypeDescriptor.GetConverter(value);

				//check if the value can be converted to a long
				if (converter.CanConvertTo(typeof(Int64)) == true)
				{
					Int64 number = (Int64)converter.ConvertTo(value, typeof(Int64));

					//fail if the number is even
					if ((number % 2) != 0)
					{
						return (new ValidationResult("Value is odd", new String[] { validationContext.MemberName }));
					}
				}
			}

			return (ValidationResult.Success);
		}
	}
}
