using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Program.Helpers
{
	public static class ReaderExtensions
	{
		public static Matrix3x4 ReadMatrix3x4(this BinaryReader reader)
		{
			return new Matrix3x4()
			{
				M11 = reader.ReadSingle(),
				M12 = reader.ReadSingle(),
				M13 = reader.ReadSingle(),
				M14 = reader.ReadSingle(),
				M21 = reader.ReadSingle(),
				M22 = reader.ReadSingle(),
				M23 = reader.ReadSingle(),
				M24 = reader.ReadSingle(),
				M31 = reader.ReadSingle(),
				M32 = reader.ReadSingle(),
				M33 = reader.ReadSingle(),
				M34 = reader.ReadSingle(),
			};
		}
	}
}
