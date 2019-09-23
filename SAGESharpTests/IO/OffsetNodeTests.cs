﻿/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
using FluentAssertions;
using NSubstitute;
using NSubstitute.ClearExtensions;
using NUnit.Framework;
using SAGESharp.Testing;
using System;

namespace SAGESharp.IO
{
    class OffsetNodeTests
    {
        private readonly IBinaryWriter binaryWriter;

        private readonly IDataNode childNode;

        private readonly IOffsetNode offsetNode;

        public OffsetNodeTests()
        {
            childNode = Substitute.For<IDataNode>();
            binaryWriter = Substitute.For<IBinaryWriter>();
            offsetNode = new OffsetNode(childNode);
        }

        [SetUp]
        public void Setup()
        {
            childNode.ClearSubstitute();
            binaryWriter.ClearSubstitute();
        }

        [Test]
        public void Test_Creating_An_OffsetNode_With_A_Null_Child_Node()
        {
            Action action = () => new OffsetNode(null);

            action.Should()
                .ThrowArgumentNullException("childNode");
        }

        [Test]
        public void Test_Getting_Child_Node()
        {
            offsetNode.ChildNode
                .Should()
                .BeSameAs(childNode);
        }
        [Test]
        public void Test_Writing_An_Object()
        {
            uint offset = 0xFFEEDDCC;
            binaryWriter.Position.Returns(offset);
            binaryWriter.WriteUInt32(Arg.Do<uint>(_ => binaryWriter.Position.Returns(0)));

            uint result = offsetNode.Write(binaryWriter, "value");

            result.Should().Be(offset);

            binaryWriter.Received().WriteUInt32(0); // offset placeholder
        }

        [Test]
        public void Test_Writing_An_Object_With_An_Offset_Greater_Than_4_bytes()
        {
            Action action = () => offsetNode.Write(binaryWriter, "value");

            binaryWriter.Position.Returns(0xAABBCCDDEE);

            action.Should()
                .ThrowExactly<InvalidOperationException>()
                .WithMessage("Offset is bigger than 4 bytes.");
        }

        [Test]
        public void Test_Writing_An_Object_With_A_Null_BinaryWriter()
        {
            Action action = () => offsetNode.Write(null, string.Empty);

            action.Should()
                .ThrowArgumentNullException("binaryWriter");
        }

        [Test]
        public void Test_Writing_A_Null_Object()
        {
            Action action = () => offsetNode.Write(binaryWriter, null);

            action.Should()
                .ThrowArgumentNullException("value");
        }
    }
}