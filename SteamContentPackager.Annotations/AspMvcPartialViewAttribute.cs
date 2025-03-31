using System;

namespace SteamContentPackager.Annotations;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
public sealed class AspMvcPartialViewAttribute : Attribute
{
}
