using System;

namespace SteamContentPackager.Annotations;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface)]
public sealed class NoReorderAttribute : Attribute
{
}
