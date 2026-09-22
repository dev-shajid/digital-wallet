using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace WalletSystem.Api.Conventions;

/// <summary>
/// Automatically glues a fixed prefix (e.g. "api/v1") onto the front of every
/// controller's route, so each controller only declares its own resource name
/// (<c>[Route("diagnostics")]</c>) instead of repeating the version prefix everywhere.
///
/// This is the .NET equivalent of Express's <c>app.use('/api/v1', router)</c> - one
/// mount point instead of hardcoding the prefix inside every individual route file.
/// Registered once in Program.cs via <c>AddControllers(o => o.Conventions.Add(...))</c>.
/// </summary>
public class ApiPrefixConvention(string prefix) : IApplicationModelConvention
{
    private readonly AttributeRouteModel _prefix = new(new Microsoft.AspNetCore.Mvc.RouteAttribute(prefix));

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            foreach (var selector in controller.Selectors)
            {
                selector.AttributeRouteModel = selector.AttributeRouteModel is null
                    ? _prefix
                    : AttributeRouteModel.CombineAttributeRouteModel(_prefix, selector.AttributeRouteModel);
            }
        }
    }
}
