﻿// <auto-generated>
//   This code file has automatically been added by the "System.Runtime.CompilerServices.RuntimeHelpers.GetSubArray" NuGet package (https://www.nuget.org/packages/System.Runtime.CompilerServices.RuntimeHelpers.GetSubArray).
//
//   IMPORTANT:
//   DO NOT DELETE THIS FILE if you are using a "packages.config" file to manage your NuGet references.
//   Consider migrating to PackageReferences instead:
//   https://docs.microsoft.com/en-us/nuget/consume-packages/migrate-packages-config-to-package-reference
//   Migrating brings the following benefits:
//   * The "System.Runtime.CompilerServices.RuntimeHelpers" folder and the "GetSubArray.cs" file doesn't appear in your project.
//   * The added files are immutable and can therefore not be modified by coincidence.
//   * Updating/Uninstalling the package will work flawlessly.
// </auto-generated>

namespace System.Runtime.CompilerServices
{
	/*public sealed class RuntimeHelpers
	{
		public static T[] GetSubArray<T>(T[] array, Range range)
		{
			var (offset, length) = range.GetOffsetAndLength(array.Length);
			if (length == 0)
				return Array.Empty<T>();
			T[] dest;
			if (typeof(T).IsValueType || typeof(T[]) == array.GetType())
			{
				// We know the type of the array to be exactly T[] or an array variance
				// compatible value type substitution like int[] <-> uint[].

				if (length == 0)
				{
					return Array.Empty<T>();
				}

				dest = new T[length];
			}
			else
			{
				// The array is actually a U[] where U:T. We'll make sure to create
				// an array of the exact same backing type. The cast to T[] will
				// never fail.

				dest = (T[])(Array.CreateInstance(array.GetType().GetElementType()!, length));
			}
			Array.Copy(array, offset, dest, 0, length);
			return dest;
		}
	}*/
}
