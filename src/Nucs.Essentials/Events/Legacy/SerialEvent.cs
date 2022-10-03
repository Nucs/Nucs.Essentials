using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Nucs.Collections;
using Nucs.Extensions;
using Nucs.Threading;

namespace Nucs.Events.Legacy {
    /// <summary>
    ///     An <see cref="Event{TArguments}"/> that supports parallel execution via locking.
    ///     Every subscription can be executed once at a time.
    /// </summary>
    /// <typeparam name="TArguments"></typeparam>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class SerialEvent<TArguments> : IDisposable {
        //todo this type of event is similar to Event<arg> but when subscribing to it, it'll pass all logged events by now.
        //token will have an option to NOT remember this fire.
        //subscribing will have an option to receive only new data.

        private readonly SynchronizedCollection<(object sender, TArguments arguments)> _history;

        public SynchronizedCollection<(object sender, TArguments arguments)> History => _history;

        protected ConcurrentAccess _lock = new ConcurrentAccess();

        public delegate void EventDelegate(object sender, SerialEventToken<TArguments> eventToken, TArguments args);

        public delegate bool EventPredicateDelegate(object sender, SerialEventToken<TArguments> eventToken, TArguments args);

        /// <summary>
        ///     A computed event to <see cref="Subscribe"/> and <see cref="Unsubscribe(Ebby.Events.Event{TArguments}.Token)"/>
        /// </summary>
        public event EventDelegate Default {
            add => Subscribe(value);
            remove => Unsubscribe(value);
        }

        /// <summary>
        ///     A computed event to <see cref="Subscribe"/> with <see cref="SubscriptionType.OneShot"/> and <see cref="Unsubscribe(Ebby.Events.Event{TArguments}.Token)"/>
        /// </summary>
        public event EventDelegate OneShot {
            add => Subscribe(value, SubscriptionType.OneShot);
            remove => Unsubscribe(value);
        }

        private readonly IList<Token> _tokens;


        public IReadOnlyList<Token> Tokens => (IReadOnlyList<Token>) _tokens;

        public SerialEvent() {
            _tokens = new SynchronizedCollection<Token>();
            _history = new SynchronizedCollection<(object sender, TArguments arguments)>();
        }

        #region Subscribtion

        public Token Subscribe(EventDelegate @delegate, SubscriptionType subtype = SubscriptionType.Default, bool refeed = true) {
            Token item;
            //lock write:
            using (var up = _lock.RequestUpgradableAccess()) { //upgradable because in refeeding it might ask for upgrading.
                up.Demand();
                item = new Token(this, @delegate, subtype);
                _tokens.Add(item);

                if (refeed) {
                    this._refeed(item);
                    if (item.markedForDeletion)
                        return null;
                }
            }

            return item;
        }

        public bool Unsubscribe(Token token) {
            if (token == null)
                throw new ArgumentNullException(nameof(token));

            using (_lock.RequestWrite()) {
                token.markedForDeletion = true;
                return _tokens.Remove(token);
            }
        }

        /// <summary>
        ///     Might result in unexpected way. Only the first occurence (the last that was added) will be removed.
        /// </summary>
        public int Unsubscribe(EventDelegate del) {
            if (del == null)
                throw new ArgumentNullException(nameof(del));
            return UnsubscribeWhere(token => token.Delegate.Equals(del), true);
        }

        public int UnsubscribeWhere(Predicate<Token> predicate, bool firstOccurenceOnly = false) {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            int count = 0;
            using (_lock.RequestWrite()) {
                for (int i = Tokens.Count - 1; i >= 0; i--) {
                    if (predicate(Tokens[i])) {
                        count++;
                        _tokens.RemoveAt(i);
                        if (firstOccurenceOnly)
                            break;
                    }
                }
            }

            return count;
        }

        public bool Unsubscribe(Guid id) {
            if (id.Equals(Guid.Empty))
                throw new ArgumentNullException(nameof(id));

            return UnsubscribeWhere(token => token.Id.Equals(id), true) == 1;
        }

        #endregion

        #region Execution

        public void Fire(object sender, TArguments args = default) {
            //add data for refeed
            using (var req = _lock.RequestUpgradableAccess()) {
                req.Demand();
                History.Add((sender, args));
                req.Free();
                //handle firing
                if (Tokens.Count == 0)
                    return;

                bool anyMarked = false;
                foreach (var tokenTuple in Tokens.Select((t, i) => (t, i)).ToArray()) {
                    var token = tokenTuple.t;
                    if (token.markedForDeletion)
                        continue;

                    _reenter:
                    var eArgs = new SerialEventToken<TArguments>() { Token = token };

                    //fire here
                    token.Delegate(sender, eArgs, args);

                    if (eArgs.Unsubscribe || token.SubscriptionType == SubscriptionType.OneShot) {
                        //unsubscribe
                        token.markedForDeletion = true;
                        anyMarked = true;
                    }

                    if (eArgs.Handled) {
                        //break
                        break;
                    }

                    if (eArgs.Reenter)
                        goto _reenter;
                }

                if (anyMarked) {
                    req.Demand();
                    UnsubscribeWhere(token => token.markedForDeletion);
                    req.Free();
                }
            }
        }

        private void Fire(Token token, object sender, TArguments args = default) {
            bool anyMarked = false;
            using (var req = _lock.RequestUpgradableAccess()) {
                if (token.markedForDeletion)
                    return;

                _reenter:
                var eArgs = new SerialEventToken<TArguments>() { };

                //fire here
                token.Delegate(sender, eArgs, args);
                if (eArgs.Unsubscribe || token.SubscriptionType == SubscriptionType.OneShot) {
                    //unsubscribe
                    token.markedForDeletion = true;
                    anyMarked = true;
                }

                if (eArgs.Handled) {
                    //break
                    return;
                }

                if (eArgs.Reenter)
                    goto _reenter;

                if (anyMarked) {
                    req.Demand();
                    Unsubscribe(token);
                    req.Free();
                }
            }
        }

        private void _refeed(Token token) {
            using (var req = _lock.RequestUpgradableAccess()) {
                req.Demand();
                var arr = History.ToArray();
                req.Free();
                foreach (var couple in arr) {
                    bool anyMarked = false;
                    if (token.markedForDeletion)
                        return;

                    _reenter:
                    var eArgs = new SerialEventToken<TArguments>();

                    //fire here
                    token.Delegate(couple.sender, eArgs, couple.arguments);
                    if (eArgs.Unsubscribe || token.SubscriptionType == SubscriptionType.OneShot) {
                        //unsubscribe
                        token.markedForDeletion = true;
                        anyMarked = true;
                    }

                    if (eArgs.Handled) {
                        //break
                        return;
                    }

                    if (eArgs.Reenter)
                        goto _reenter;

                    if (anyMarked) {
                        req.Demand();
                        Unsubscribe(token);
                        req.Free();
                    }
                }
            }
        }

        #endregion

        /// <summary>
        ///     Unsubscribes all events.
        /// </summary>
        public void Clear() {
            using (_lock.RequestWrite()) {
                _tokens.Clear();
                History.Clear();
            }
        }

        public void Dispose() {
            Clear();
            _lock.SafeDispose();
        }

        /// <summary>
        ///     An <see cref="Event"/> token similar to <see cref="CancellationToken"/> .
        /// </summary>
        [DebuggerStepThrough]
        public class Token : IToken, IEquatable<Token> {
            private readonly WeakReference<SerialEvent<TArguments>> _event;
            internal bool markedForDeletion;
            public Guid Id { get; }
            public EventDelegate Delegate { get; set; }
            public SubscriptionType SubscriptionType { get; set; }
            Type IToken.ReturnType => typeof(TArguments);

            public Token(EventDelegate @delegate) {
                Id = Guid.NewGuid();
                Delegate = @delegate;
                SubscriptionType = SubscriptionType.Default;
            }

            public Token(EventDelegate @delegate, SubscriptionType subtype) : this(@delegate) {
                SubscriptionType = subtype;
            }

            internal Token(SerialEvent<TArguments> _event, EventDelegate @delegate) : this(@delegate) {
                this._event = new WeakReference<SerialEvent<TArguments>>(_event);
            }

            internal Token(SerialEvent<TArguments> _event, EventDelegate @delegate, SubscriptionType subtype) : this(@delegate, subtype) {
                this._event = new WeakReference<SerialEvent<TArguments>>(_event);
            }


            Delegate IToken.Delegate => Delegate;

            #region Equality

            /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
            /// <param name="other">An object to compare with this object.</param>
            /// <returns>
            /// <see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false" />.</returns>
            public bool Equals(Token other) {
                if (ReferenceEquals(null, other))
                    return false;
                if (ReferenceEquals(this, other))
                    return true;
                return Id.Equals(other.Id);
            }

            /// <summary>Determines whether the specified object is equal to the current object.</summary>
            /// <param name="obj">The object to compare with the current object. </param>
            /// <returns>
            /// <see langword="true" /> if the specified object  is equal to the current object; otherwise, <see langword="false" />.</returns>
            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj))
                    return false;
                if (ReferenceEquals(this, obj))
                    return true;
                if (obj.GetType() != this.GetType())
                    return false;
                return Equals((Token) obj);
            }

            /// <summary>Serves as the default hash function. </summary>
            /// <returns>A hash code for the current object.</returns>
            public override int GetHashCode() {
                return Id.GetHashCode();
            }

            /// <summary>Returns a value that indicates whether the values of two <see cref="T:Ebby.Events.Event`1.Token" /> objects are equal.</summary>
            /// <param name="left">The first value to compare.</param>
            /// <param name="right">The second value to compare.</param>
            /// <returns>true if the <paramref name="left" /> and <paramref name="right" /> parameters have the same value; otherwise, false.</returns>
            public static bool operator ==(Token left, Token right) {
                return Equals(left, right);
            }

            /// <summary>Returns a value that indicates whether two <see cref="T:Ebby.Events.Event`1.Token" /> objects have different values.</summary>
            /// <param name="left">The first value to compare.</param>
            /// <param name="right">The second value to compare.</param>
            /// <returns>true if <paramref name="left" /> and <paramref name="right" /> are not equal; otherwise, false.</returns>
            public static bool operator !=(Token left, Token right) {
                return !Equals(left, right);
            }

            #endregion

            public void Dispose() {
                if (_event != null && _event.TryGetTarget(out var e))
                    e.Unsubscribe(this);
            }
        }

        public static Token operator +(SerialEvent<TArguments> eve, EventDelegate del) {
            return eve.Subscribe(del);
        }

        public static SerialEvent<TArguments> operator -(SerialEvent<TArguments> eve, Token del) {
            eve.Unsubscribe(del);
            return eve;
        }

        public override string ToString() {
            return $"{nameof(SerialEvent<TArguments>)} | History: {History.Count}";
        }

        private string GetDebuggerDisplay() {
            return ToString();
        }
    }
}