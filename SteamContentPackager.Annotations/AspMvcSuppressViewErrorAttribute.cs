using System;

namespace SteamContentPackager.Annotations;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AspMvcSuppressViewErrorAttribute : Attribute
{
}
