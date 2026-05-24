// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.StructuredOutput;

/// <summary>Helper for JSON Schema from types. FR-SO-003.</summary>
public static class JsonSchema
{
    /// <summary>Generate JSON Schema from type T.</summary>
    public static JsonSchemaDefinition FromType<T>() => SchemaGenerator.FromType<T>();
}
