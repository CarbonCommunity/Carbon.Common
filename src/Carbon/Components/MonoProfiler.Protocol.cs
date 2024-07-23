/*
 *
 * Copyright (c) 2024 Carbon Community
 * Copyright (c) 2024 Patrette
 * All rights reserved.
 *
 */

using Newtonsoft.Json;
using ProtoBuf;
using MathEx = Carbon.Extensions.MathEx;

namespace Carbon.Components;

public partial class MonoProfiler
{
	public const int NATIVE_PROTOCOL = 3;
	public const int MANAGED_PROTOCOL = NATIVE_PROTOCOL + 123;
}
