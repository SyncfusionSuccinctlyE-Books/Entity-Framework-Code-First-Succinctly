using System;

namespace Succinctly.Model
{
	using System.Data.Spatial;

	public class Venue
	{
		public Int32 VenueId { get; set; }

		public String Name { get; set; }

		public DbGeography Location { get; set; }

		public override string ToString()
		{
			return (this.Name);
		}
	}
}
