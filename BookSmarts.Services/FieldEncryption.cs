using System.Text.Json;
using BookSmarts.Core.Encryption;

namespace BookSmarts.Services;

/// <summary>
/// Encrypts/decrypts sensitive fields on IEncryptable models.
/// Sensitive fields are serialized to JSON, encrypted into EncryptedPayload, and originals nulled/zeroed.
/// </summary>
public static class FieldEncryption
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false
    };

    /// <summary>
    /// Encrypts sensitive fields of a model into EncryptedPayload and clears the originals.
    /// No-op if model is not IEncryptable or ADK is null.
    /// </summary>
    public static void EncryptFields<T>(T model, byte[] adk) where T : class
    {
        if (model is not IEncryptable encryptable) return;

        var fields = EncryptionFieldMaps.GetFields<T>();
        if (fields.Length == 0) return;

        var payload = new Dictionary<string, JsonElement>();
        var type = typeof(T);

        foreach (var fieldName in fields)
        {
            var prop = type.GetProperty(fieldName);
            if (prop == null) continue;

            var value = prop.GetValue(model);
            if (value != null)
            {
                var jsonElement = JsonSerializer.SerializeToElement(value, prop.PropertyType, JsonOptions);
                payload[fieldName] = jsonElement;
            }

            // Clear the original field
            ClearProperty(model, prop);
        }

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        encryptable.EncryptedPayload = AesGcmHelper.EncryptString(json, adk);
    }

    /// <summary>
    /// Decrypts EncryptedPayload back into the model's sensitive fields.
    /// No-op if EncryptedPayload is null (encryption not enabled or already decrypted).
    /// </summary>
    public static void DecryptFields<T>(T model, byte[] adk) where T : class
    {
        if (model is not IEncryptable encryptable) return;
        if (string.IsNullOrEmpty(encryptable.EncryptedPayload)) return;

        var json = AesGcmHelper.DecryptString(encryptable.EncryptedPayload, adk);
        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, JsonOptions);
        if (payload == null) return;

        var type = typeof(T);
        foreach (var (fieldName, element) in payload)
        {
            var prop = type.GetProperty(fieldName);
            if (prop == null) continue;

            var value = element.Deserialize(prop.PropertyType, JsonOptions);
            prop.SetValue(model, value);
        }

        encryptable.EncryptedPayload = null;
    }

    /// <summary>
    /// Decrypts a list of models.
    /// </summary>
    public static void DecryptAll<T>(List<T> models, byte[] adk) where T : class
    {
        foreach (var model in models)
            DecryptFields(model, adk);
    }

    private static void ClearProperty<T>(T model, System.Reflection.PropertyInfo prop) where T : class
    {
        var propType = prop.PropertyType;

        if (propType == typeof(string))
            prop.SetValue(model, null);
        else if (propType == typeof(string))
            prop.SetValue(model, null);
        else if (propType == typeof(decimal))
            prop.SetValue(model, 0m);
        else if (propType == typeof(decimal?))
            prop.SetValue(model, null);
        else if (propType == typeof(int))
            prop.SetValue(model, 0);
        else if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(List<>))
            prop.SetValue(model, null);
        else if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>))
            prop.SetValue(model, null);
        else if (!propType.IsValueType)
            prop.SetValue(model, null);
        else
            prop.SetValue(model, Activator.CreateInstance(propType));
    }
}
