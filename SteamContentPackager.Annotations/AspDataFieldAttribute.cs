using System;

namespace SteamContentPackager.Annotations;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
public sealed class AspDataFieldAttribute : Attribute
{
}
