namespace BookSmarts.SDK;

/// <summary>
/// Factory for creating BookSmartsClient instances with pre-configured authentication.
/// </summary>
public class BookSmartsClientFactory
{
    private readonly string _baseUrl;
    private readonly string _clientKey;

    public BookSmartsClientFactory(string baseUrl, string clientKey)
    {
        _baseUrl = baseUrl;
        _clientKey = clientKey;
    }

    public BookSmartsClient Create()
    {
        return new BookSmartsClient(_baseUrl, _clientKey);
    }
}
