using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Succinctly.Model
{
#if ORACLE
	[Table("MY_SILLY_TABLE", Schema = "SUCCINCTLY")]
#else
	[Table("MY_SILLY_TABLE")]
#endif
	public class Test
	{
		public Test()
		{
#if ORACLE
			this.Id = Guid.NewGuid().ToString();
#else
			this.Id = Guid.NewGuid();
#endif
		}

		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.None)]
#if ORACLE
		[Column("ID")]
		[MaxLength(36)]
		public String Id
#else
		[Column("ID")]
		public Guid Id
#endif
		{
			get;
			set;
		}

		[Required]
		[MaxLength(50)]
#if ORACLE
		[Column("TEXT")]
#else
		[Column(TypeName = "NVARCHAR")]
#endif
		public String Text
		{
			get;
			set;
		}

#if ORACLE
		[Column("CLOB", TypeName = "CLOB")]
#else
#endif
		[MaxLength(-1)]
		public String Clob
		{
			get;
			set;
		}

#if ORACLE
		[Column("BLOB", TypeName = "BLOB")]
#else
#endif
		[MaxLength(-1)]
		public Byte[] Blob
		{
			get;
			set;
		}
	}
}
