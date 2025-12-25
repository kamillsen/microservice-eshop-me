using Microsoft.JSInterop;

namespace Web.UI.Services;

public class UserStateService
{
    private readonly IJSRuntime _jsRuntime;
    private string? _userName;

    public UserStateService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<string> GetUserNameAsync()
    {
        if (_userName != null)
            return _userName;

        try
        {
            _userName = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "userName") ?? "guest";
        }
        catch
        {
            _userName = "guest";
        }

        return _userName;
    }

    public async Task SetUserNameAsync(string userName)
    {
        _userName = userName;
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "userName", userName);
        }
        catch
        {
            // Ignore if localStorage is not available
        }
    }
}

