namespace Miruken.AspNetCore
{
    using Context;
    using Microsoft.AspNetCore.SignalR;

    public abstract class ContextualHub : Hub, IContextual
    {
        private Context _context;

        public event ContextChangingDelegate ContextChanging;
        public event ContextChangedDelegate ContextChanged;

        public Context MirukenContext => ((IContextual)this).Context;

        Context IContextual.Context
        {
            get => _context;
            set
            {
                if (_context == value) return;
                var newContext = value;
                ContextChanging?.Invoke(this, _context, ref newContext);
                _context?.RemoveHandlers(this);
                var oldContext = _context;
                _context = newContext;
                _context?.InsertHandlers(0, this);
                ContextChanged?.Invoke(this, oldContext, _context);
            }
        }
    }
}
