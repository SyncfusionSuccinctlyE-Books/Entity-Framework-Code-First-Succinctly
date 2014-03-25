using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Succinctly.Model
{
	[Serializable]
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public sealed class IsEvenAttribute : ValidationAttribute
	{
		protected override ValidationResult IsValid(Object value, ValidationContext validationContext)
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
						return (new ValidationResult(this.ErrorMessage, new String[] { validationContext.MemberName }));
					}
				}
			}

			return (ValidationResult.Success);
		}
	}
}
