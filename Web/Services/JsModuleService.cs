using Microsoft.JSInterop;

namespace PetAdoption.Services;

public interface IJsModuleService
{
    ValueTask<IJSObjectReference> ImportModuleAsync(string modulePath);
}

public class JsModuleService : IJsModuleService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IStaticAssetService _staticAssetService;

    public JsModuleService(IJSRuntime jsRuntime, IStaticAssetService staticAssetService)
    {
        _jsRuntime = jsRuntime;
        _staticAssetService = staticAssetService;
    }

    public async ValueTask<IJSObjectReference> ImportModuleAsync(string modulePath)
    {
        var versionedPath = _staticAssetService.GetVersionedUrl(modulePath);
        return await _jsRuntime.InvokeAsync<IJSObjectReference>("import", versionedPath);
    }
}