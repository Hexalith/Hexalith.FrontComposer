using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

using NSubstitute;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

internal sealed class RecordingDialogService : DialogService
{
    public RecordingDialogService()
        : this(
            new ServiceCollection()
                .AddSingleton(Substitute.For<IJSRuntime>())
                .BuildServiceProvider(),
            Substitute.For<IFluentLocalizer>())
    {
    }

    public RecordingDialogService(IServiceProvider serviceProvider, IFluentLocalizer localizer)
        : base(serviceProvider, localizer)
    {
    }

    public Type? LastDialogType { get; private set; }

    public DialogOptions? LastOptions { get; private set; }

    public int ShowDialogCallCount { get; private set; }

    public override Task<DialogResult> ShowDialogAsync(Type dialogComponent, DialogOptions options)
    {
        LastDialogType = dialogComponent;
        LastOptions = options;
        ShowDialogCallCount++;
        return Task.FromResult<DialogResult>(DialogResult<object?>.Ok());
    }
}
