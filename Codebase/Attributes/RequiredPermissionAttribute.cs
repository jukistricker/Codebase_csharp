using Codebase.Models.Dtos.Auth;
using Codebase.Models.Dtos.Responses.Shared;
using Codebase.Utils;
using StackExchange.Redis;

namespace Codebase.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequiredPermissionAttribute(string permission) : Attribute
{
    public string Permission { get; } = permission;
}

