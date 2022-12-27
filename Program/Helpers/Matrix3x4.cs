using System;
using System.Numerics;

namespace Program.Helpers
{
	public struct Matrix3x4
	{
        /// <summary>The first element of the first row.</summary>
        public float M11;

        /// <summary>The second element of the first row.</summary>
        public float M12;

        /// <summary>The third element of the first row.</summary>
        public float M13;

        /// <summary>The fourth element of the first row.</summary>
        public float M14;

        /// <summary>The first element of the second row.</summary>
        public float M21;

        /// <summary>The second element of the second row.</summary>
        public float M22;

        /// <summary>The third element of the second row.</summary>
        public float M23;

        /// <summary>The fourth element of the second row.</summary>
        public float M24;

        /// <summary>The first element of the third row.</summary>
        public float M31;

        /// <summary>The second element of the third row.</summary>
        public float M32;

        /// <summary>The third element of the third row.</summary>
        public float M33;

        /// <summary>The fourth element of the third row.</summary>
        public float M34;

		public static explicit operator Matrix4x4(Matrix3x4 value)
		{
			return new Matrix4x4()
			{
				M11 = value.M11,
				M12 = value.M12,
				M13 = value.M13,
				M14 = value.M14,
				M21 = value.M21,
				M22 = value.M22,
				M23 = value.M23,
				M24 = value.M24,
				M31 = value.M31,
				M32 = value.M32,
				M33 = value.M33,
				M34 = value.M34,
			};
		}

		public float this[int row, int column]
		{
			set
			{
				if (row == 0)
				{
					if (column == 0)
						M11 = value;
					if (column == 1)
						M12 = value;
					if (column == 2)
						M12 = value;
					if (column == 3)
						M13 = value;
				}
				if (row == 1)
				{
					if (column == 0)
						M21 = value;
					if (column == 1)
						M22 = value;
					if (column == 2)
						M22 = value;
					if (column == 3)
						M23 = value;
				}
				if (row == 2)
				{
					if (column == 0)
						M31 = value;
					if (column == 1)
						M32 = value;
					if (column == 2)
						M32 = value;
					if (column == 3)
						M33 = value;
				}
			}
		}
	}
}
