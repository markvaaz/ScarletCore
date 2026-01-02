using Stunlock.Core;
using Unity.Mathematics;
using System;
using System.Globalization;
using ScarletCore.Services;

namespace ScarletCore.Commanding;

/// <summary>
/// Helper that converts string tokens into CLR/game types used by commands.
/// </summary>
public static class TypeConverter {
  /// <summary>
  /// Converts a string input into a value of <paramref name="targetType"/>.
  /// Returns a default value for the type when conversion fails.
  /// </summary>
  /// <param name="input">Input string.</param>
  /// <param name="targetType">Target <see cref="Type"/> to convert to.</param>
  /// <returns>Converted value or default for the target type.</returns>
  public static object ConvertToType(string input, Type targetType) {
    if (string.IsNullOrWhiteSpace(input)) {
      return GetDefaultValue(targetType);
    }

    if (targetType == typeof(string)) {
      return input;
    }

    if (targetType == typeof(int)) {
      return int.TryParse(input, out var result) ? result : 0;
    }

    if (targetType == typeof(long)) {
      return long.TryParse(input, out var result) ? result : 0L;
    }

    if (targetType == typeof(ulong)) {
      return ulong.TryParse(input, out var result) ? result : 0UL;
    }

    if (targetType == typeof(float)) {
      return float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : 0f;
    }

    if (targetType == typeof(double)) {
      return double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : 0.0;
    }

    if (targetType == typeof(bool)) {
      return bool.TryParse(input, out var result) && result;
    }

    if (targetType == typeof(byte)) {
      return byte.TryParse(input, out var result) ? result : (byte)0;
    }

    if (targetType == typeof(short)) {
      return short.TryParse(input, out var result) ? result : (short)0;
    }

    if (targetType == typeof(uint)) {
      return uint.TryParse(input, out var result) ? result : 0u;
    }

    // Tipos especiais do jogo
    if (targetType == typeof(PlayerData)) {
      return ConvertToPlayerData(input);
    }

    if (targetType == typeof(PrefabGUID)) {
      return ConvertToPrefabGUID(input);
    }

    if (targetType == typeof(float2)) {
      return ConvertToFloat2(input);
    }

    if (targetType == typeof(float3)) {
      return ConvertToFloat3(input);
    }

    if (targetType == typeof(float4)) {
      return ConvertToFloat4(input);
    }

    if (targetType == typeof(quaternion)) {
      return ConvertToQuaternion(input);
    }

    if (targetType.IsEnum) {
      return Enum.TryParse(targetType, input, true, out var result) ? result : GetDefaultValue(targetType);
    }

    return GetDefaultValue(targetType);
  }

  private static object GetDefaultValue(Type type) {
    return type.IsValueType ? Activator.CreateInstance(type) : null;
  }

  /// <summary>Attempts to convert the given input into a <see cref="PlayerData"/>.</summary>
  /// <param name="input">Player identifier or name.</param>
  /// <returns>Found <see cref="PlayerData"/> or null.</returns>
  public static PlayerData ConvertToPlayerData(string input) {
    var player = input.GetPlayerData();
    return player;
  }

  /// <summary>Converts numeric id into <see cref="PlayerData"/>.</summary>
  /// <param name="input">Player ID.</param>
  /// <returns>Found <see cref="PlayerData"/> or null.</returns>
  public static PlayerData ConvertToPlayerData(ulong input) {
    var player = input.GetPlayerData();
    return player;
  }

  /// <summary>Parses a prefab GUID from string.</summary>
  /// <param name="input">GUID text.</param>
  /// <returns>Parsed <see cref="PrefabGUID"/> or <see cref="PrefabGUID.Empty"/>.</returns>
  public static PrefabGUID ConvertToPrefabGUID(string input) {
    if (PrefabGUID.TryParse(input, out var guid)) {
      return guid;
    }

    return PrefabGUID.Empty;
  }

  /// <summary>Parses a float2 from a comma separated string ("x,y").</summary>
  public static float2 ConvertToFloat2(string input) {
    var parts = input.Split(',');
    if (parts.Length != 2) {
      return float2.zero;
    }

    if (float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
        float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var y)) {
      return new float2(x, y);
    }

    return float2.zero;
  }

  /// <summary>Parses a float3 from a comma separated string ("x,y,z").</summary>
  public static float3 ConvertToFloat3(string input) {
    var parts = input.Split(',');
    if (parts.Length != 3) {
      return float3.zero;
    }

    if (float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
        float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var y) &&
        float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var z)) {
      return new float3(x, y, z);
    }

    return float3.zero;
  }

  /// <summary>Parses a float4 from a comma separated string ("x,y,z,w").</summary>
  public static float4 ConvertToFloat4(string input) {
    var parts = input.Split(',');
    if (parts.Length != 4) {
      return float4.zero;
    }

    if (float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
        float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var y) &&
        float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var z) &&
        float.TryParse(parts[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var w)) {
      return new float4(x, y, z, w);
    }

    return float4.zero;
  }

  /// <summary>Parses a quaternion from a comma separated string ("x,y,z,w").</summary>
  public static quaternion ConvertToQuaternion(string input) {
    var parts = input.Split(',');
    if (parts.Length != 4) {
      return quaternion.identity;
    }

    if (float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
        float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var y) &&
        float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var z) &&
        float.TryParse(parts[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var w)) {
      return new quaternion(x, y, z, w);
    }

    return quaternion.identity;
  }

  /// <summary>Returns a friendly short name used in help / usage messages for a given type.</summary>
  public static string GetFriendlyTypeName(Type type) {
    return type.Name switch {
      "String" => "text",
      "Int32" => "number",
      "Int64" => "number",
      "UInt64" => "number",
      "UInt32" => "number",
      "Single" => "decimal",
      "Double" => "decimal",
      "Boolean" => "true/false",
      "Byte" => "number",
      "Int16" => "number",
      "PlayerData" => "player",
      "PrefabGUID" => "prefabGuid",
      "float2" => "x,y",
      "float3" => "x,y,z",
      "float4" => "x,y,z,w",
      "quaternion" => "x,y,z,w",
      _ => type.Name
    };
  }
}