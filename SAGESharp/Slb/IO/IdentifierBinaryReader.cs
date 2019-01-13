﻿using SAGESharp.Extensions;
using System;
using System.IO;

namespace SAGESharp.Slb.IO
{
    /// <summary>
    /// Class to read an binary Identifier from a stream.
    /// </summary>
    public class IdentifierBinaryReader : ISlbReader<Identifier>
    {
        private readonly Stream stream;

        /// <summary>
        /// Creates a new reader with the given input.
        /// </summary>
        /// 
        /// <param name="stream">The input to read, cannot be null</param>
        public IdentifierBinaryReader(Stream stream)
        {
            this.stream = stream ?? throw new ArgumentNullException("Input stream cannot be null.");
        }

        /// <inheritdoc/>
        public Identifier ReadSlbObject()
        {
            return new Identifier(new byte[]
            {
                stream.ForceReadByte(),
                stream.ForceReadByte(),
                stream.ForceReadByte(),
                stream.ForceReadByte()
            });
        }
    }
}