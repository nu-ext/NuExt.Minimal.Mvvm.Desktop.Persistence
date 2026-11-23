using System;
using System.Text.Json.Nodes;

namespace Minimal.Mvvm.Persistence;

/// <summary>
/// Defines the supported setting value type discriminators and maps them to and from <see cref="Type"/>.
/// </summary>
/// <remarks>
/// The string constants are the tokens persisted by storage layers (for example, a settings table).
/// The model layer uses <see cref="Type"/>; the conversion happens only at the persistence boundary.
/// </remarks>
internal static class SettingValueTypes
{
    /// <summary>
    /// A string value.
    /// </summary>
    public const string String = "string";

    /// <summary>
    /// An integer value.
    /// </summary>
    public const string Integer = "integer";

    /// <summary>
    /// A floating-point value.
    /// </summary>
    public const string Real = "real";

    /// <summary>
    /// A Boolean value.
    /// </summary>
    public const string Boolean = "boolean";

    /// <summary>
    /// A date value without a time component.
    /// </summary>
    public const string Date = "date";

    /// <summary>
    /// A date and time value.
    /// </summary>
    public const string DateTime = "datetime";

    /// <summary>
    /// A JSON value.
    /// </summary>
    public const string Json = "json";

    /// <summary>
    /// Gets the string token persisted for the specified setting value type.
    /// </summary>
    /// <param name="valueType">The setting value type.</param>
    /// <returns>The persisted token.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="valueType"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="valueType"/> is not supported.</exception>
    public static string ToToken(Type valueType)
    {
        ArgumentNullException.ThrowIfNull(valueType);

        if (valueType == typeof(string))
        {
            return String;
        }

        if (valueType == typeof(int) || valueType == typeof(long))
        {
            return Integer;
        }

        if (valueType == typeof(double))
        {
            return Real;
        }

        if (valueType == typeof(bool))
        {
            return Boolean;
        }

#if NET6_0_OR_GREATER
        if (valueType == typeof(DateOnly))
        {
            return Date;
        }
#else
        if (valueType == typeof(DateTime))
        {
            return Date;
        }
#endif

        if (valueType == typeof(DateTimeOffset))
        {
            return DateTime;
        }

        if (valueType == typeof(JsonNode))
        {
            return Json;
        }

        throw new ArgumentOutOfRangeException(nameof(valueType), valueType, "Unsupported setting value type.");
    }

    /// <summary>
    /// Gets the setting value type for the specified persisted token.
    /// </summary>
    /// <param name="token">The persisted token.</param>
    /// <returns>The setting value type.</returns>
    /// <exception cref="ArgumentException"><paramref name="token"/> is <see langword="null"/> or white space.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="token"/> is not supported.</exception>
    public static Type ToType(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        return token switch
        {
            String => typeof(string),
            Integer => typeof(long),
            Real => typeof(double),
            Boolean => typeof(bool),
#if NET6_0_OR_GREATER
            Date => typeof(DateOnly),
#else
            Date => typeof(DateTime),
#endif
            DateTime => typeof(DateTimeOffset),
            Json => typeof(JsonNode),
            _ => throw new ArgumentOutOfRangeException(nameof(token), token, "Unsupported setting value type token.")
        };
    }
}
