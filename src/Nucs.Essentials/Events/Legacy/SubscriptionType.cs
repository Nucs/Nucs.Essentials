namespace Nucs.Events.Legacy {
    /// <summary>
    ///     
    /// </summary>
    public enum SubscriptionType {
        /// Regular subscription like any other event.
        Default,

        /// Will be unsubscribed after firing once.
        OneShot
    }
}