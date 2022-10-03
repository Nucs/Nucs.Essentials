using System;

namespace Nucs.Events.Legacy {
    public class SerialEventToken<TArguments> : SerialEventToken {
        public new SerialEvent<TArguments>.Token Token {
            get => (SerialEvent<TArguments>.Token) base.Token;
            set => base.Token = value;
        }
    }

    public class SerialEventToken {
        /// <summary>
        ///     After the execution, this subscription will be unregistered.
        /// </summary>
        public bool Unsubscribe { get; set; }

        /// <summary>
        ///     If set to true, the event will stop from firing the next registered token (ordered by registeration).
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     If true and <see cref="Handled"/> is false, the event will be re-entered. Beware from <see cref="StackOverflowException"/>.
        /// </summary>
        public bool Reenter { get; set; }

        /// <summary>
        ///     The actual token of this registeration.
        /// </summary>
        public IToken Token { get; set; }
    }
}