using System;

namespace SteamContentPackager.Annotations;

[AttributeUsage(AttributeTargets.Method)]
public sealed class PureAttribute : Attribute
{
}
