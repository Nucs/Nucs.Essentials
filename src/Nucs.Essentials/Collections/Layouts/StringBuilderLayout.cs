using System.Text;

namespace Nucs.Collections.Layouts {
    public class StringBuilderLayout {
        // A StringBuilder is internally represented as a linked list of blocks each of which holds
        // a chunk of the string.  It turns out string as a whole can also be represented as just a chunk,
        // so that is what we do.

        /// <summary>
        /// The character buffer for this chunk.
        /// </summary>
        public char[] m_ChunkChars;

        /// <summary>
        /// The chunk that logically precedes this chunk.
        /// </summary>
        public StringBuilder? m_ChunkPrevious;

        /// <summary>
        /// The number of characters in this chunk.
        /// This is the number of elements in <see cref="m_ChunkChars"/> that are in use, from the start of the buffer.
        /// </summary>
        public int m_ChunkLength;

        /// <summary>
        /// The logical offset of this chunk's characters in the string it is a part of.
        /// This is the sum of the number of characters in preceding blocks.
        /// </summary>
        public int m_ChunkOffset;

        /// <summary>
        /// The maximum capacity this builder is allowed to have.
        /// </summary>
        public int m_MaxCapacity;
    }
}
       
