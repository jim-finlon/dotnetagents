// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Governance.Identity;

/// <summary>
/// Default <see cref="IInvokerContextAccessor"/> backed by an <see cref="AsyncLocal{T}"/>.
/// Safe for multi-request server hosts because async-local flow follows the logical call chain.
/// </summary>
public sealed class InvokerContextAccessor : IInvokerContextAccessor
{
    private static readonly AsyncLocal<Holder> Slot = new();

    public InvokerContext? Current
    {
        get => Slot.Value?.Context;
        set
        {
            var holder = Slot.Value;
            if (holder is null)
                Slot.Value = new Holder { Context = value };
            else
                holder.Context = value;
        }
    }

    private sealed class Holder
    {
        public InvokerContext? Context;
    }
}
