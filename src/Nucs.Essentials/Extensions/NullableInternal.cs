// Because we have special type system support that says a boxed Nullable<T>
// can be used where a boxed<T> is use, Nullable<T> can not implement any intefaces
// at all (since T may not).   Do NOT add any interfaces to Nullable!
//

using System;

namespace Nucs.Extensions {
    [Serializable]
    public partial struct NullableShim<T> where T : struct {
        public readonly bool hasValue; // Do not rename (binary serialization)
        public T value; // Do not rename (binary serialization) or make readonly (can be mutated in ToString, etc.)
    }
}