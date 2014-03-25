
namespace Succinctly.Model
{
	public class Master
	{
		public int MasterId { get; set; }
		public string Name { get; set; }
		public virtual Detail Detail { get; set; }
	}

	public class Detail
	{
		public int MasterId { get; set; }
		public string Name { get; set; }
		public virtual Master Master { get; set; }
	}
}
