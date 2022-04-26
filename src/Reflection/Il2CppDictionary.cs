#if CPP
using System;
using System.Collections.Generic;
using System.Collections;

namespace UniverseLib
{
    internal class Il2CppDictionary : IEnumerator<DictionaryEntry>
    {
        readonly Il2CppEnumerator keysEnumerator;
        readonly Il2CppEnumerator valuesEnumerator;

        public object Current => new DictionaryEntry(keysEnumerator.Current, valuesEnumerator.Current);

        DictionaryEntry IEnumerator<DictionaryEntry>.Current => new(keysEnumerator.Current, valuesEnumerator.Current);

        public Il2CppDictionary(Il2CppEnumerator keysEnumerator, Il2CppEnumerator valuesEnumerator)
        {
            this.keysEnumerator = keysEnumerator;
            this.valuesEnumerator = valuesEnumerator;
        }

        public bool MoveNext()
        {
            return keysEnumerator.MoveNext() && valuesEnumerator.MoveNext();
        }

        public void Dispose() => throw new NotImplementedException();
        public void Reset() => throw new NotImplementedException();
    }
}

#endif