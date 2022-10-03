using System;

namespace Nucs.Events.Legacy {
    public interface IToken : IDisposable {
        Guid Id { get; }
        Delegate Delegate { get; }
        Type ReturnType { get; }
    }
}