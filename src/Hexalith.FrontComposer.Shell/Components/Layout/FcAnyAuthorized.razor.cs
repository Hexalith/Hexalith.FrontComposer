using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

// Authorization results must resume on the component synchronization context.
#pragma warning disable CA2007

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>Shows a secondary menu when at least one destination is visible to the operator.</summary>
public sealed partial class FcAnyAuthorized : ComponentBase
{
    private bool _visible;
    private long _evaluationVersion;

    [CascadingParameter] private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    [Inject] private IServiceProvider Services { get; set; } = default!;

    /// <summary>Whether a projection or ungated entry already makes this menu visible.</summary>
    [Parameter] public bool HasPublicDestination { get; set; }

    /// <summary>Policies of gated destinations in the menu.</summary>
    [Parameter] public IReadOnlyList<string> Policies { get; set; } = [];

    /// <summary>The menu to render when any destination is available.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        long evaluation = ++_evaluationVersion;
        _visible = HasPublicDestination;
        if (_visible || AuthenticationStateTask is null
            || Services.GetService(typeof(IAuthorizationService)) is not IAuthorizationService authorization)
        {
            return;
        }

        AuthenticationState state = await AuthenticationStateTask;
        foreach (string policy in Policies.Distinct(StringComparer.Ordinal))
        {
            if ((await authorization.AuthorizeAsync(state.User, policy)).Succeeded)
            {
                if (evaluation == _evaluationVersion)
                {
                    _visible = true;
                }
                return;
            }
        }
    }
}
