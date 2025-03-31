using System;

namespace SteamContentPackager.Annotations;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class NoEnumerationAttribute : Attribute
{
}
