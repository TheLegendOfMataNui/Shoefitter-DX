﻿using Moq;
using NUnit.Framework;
using SAGESharp.SLB;
using SAGESharp.SLB.Level.Conversation;
using System;
using System.IO;

namespace SAGESharpTests.SLB.Level.Conversation
{
    [TestFixture]
    public static class CharacterBinaryReaderTests
    {
        [Test]
        public static void TestCharacterBinaryReaderConstructor()
        {
            var stream = new Mock<Stream>().Object;
            var identifierReader = new Mock<ISLBBinaryReader<Identifier>>().Object;
            var infoReader = new Mock<ISLBBinaryReader<Info>>().Object;

            Assert.That(() => new CharacterBinaryReader(null, identifierReader, infoReader), Throws.InstanceOf<ArgumentNullException>());
            Assert.That(() => new CharacterBinaryReader(stream, null, infoReader), Throws.InstanceOf<ArgumentNullException>());
            Assert.That(() => new CharacterBinaryReader(stream, identifierReader, null), Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public static void TestReadCharacterSlb()
        {
            var streamMock = new Mock<Stream>();
            var identifierReaderMock = new Mock<ISLBBinaryReader<Identifier>>();
            var infoReaderMock = new Mock<ISLBBinaryReader<Info>>();

            var reader = new CharacterBinaryReader(streamMock.Object, identifierReaderMock.Object, infoReaderMock.Object);

            var toaName = (Identifier)0x11223344;
            var charName = (Identifier)0x11223345;
            var charCont = (Identifier)0x11223346;

            identifierReaderMock
                .SetupSequence(identifierReader => identifierReader.ReadSLBObject())
                .Returns(toaName)
                .Returns(charName)
                .Returns(charCont);

            streamMock
                .SetupSequence(stream => stream.ReadByte())
                // Info entry count
                .ReturnsIntBytes(2)
                // Info entries position
                .ReturnsIntBytes(0x44);

            streamMock
                .Setup(stream => stream.Position)
                .Returns(0x20);

            var info1 = new Info();
            var info2 = new Info();
            infoReaderMock
                .SetupSequence(infoReader => infoReader.ReadSLBObject())
                .Returns(info1)
                .Returns(info2);

            var character = reader.ReadSLBObject();

            Assert.AreEqual(character.ToaName, toaName);
            Assert.AreEqual(character.CharName, charName);
            Assert.AreEqual(character.CharCont, charCont);
            Assert.AreEqual(character.Entries.Count, 2);
            Assert.IsTrue(character.Entries.Contains(info1));
            Assert.IsTrue(character.Entries.Contains(info2));

            streamMock.Verify(stream => stream.ReadByte(), Times.Exactly(8));
            streamMock.VerifyGet(stream => stream.Position, Times.Once);
            streamMock.VerifySet(stream => stream.Position = 0x44, Times.Once);
            streamMock.VerifySet(stream => stream.Position = 0x20, Times.Once);
            streamMock.VerifyNoOtherCalls();

            identifierReaderMock.Verify(identifierReader => identifierReader.ReadSLBObject(), Times.Exactly(3));
            identifierReaderMock.VerifyNoOtherCalls();

            infoReaderMock.Verify(infoReader => infoReader.ReadSLBObject(), Times.Exactly(2));
            infoReaderMock.VerifyNoOtherCalls();
        }



        [Test]
        public static void TestReadCharacterSlbWithNoInfo()
        {
            var streamMock = new Mock<Stream>();
            var identifierReaderMock = new Mock<ISLBBinaryReader<Identifier>>();
            var infoReaderMock = new Mock<ISLBBinaryReader<Info>>();

            var reader = new CharacterBinaryReader(streamMock.Object, identifierReaderMock.Object, infoReaderMock.Object);

            var toaName = (Identifier)0x11223344;
            var charName = (Identifier)0x11223345;
            var charCont = (Identifier)0x11223346;

            identifierReaderMock
                .SetupSequence(identifierReader => identifierReader.ReadSLBObject())
                .Returns(toaName)
                .Returns(charName)
                .Returns(charCont);

            streamMock
                .SetupSequence(stream => stream.ReadByte())
                // Info entry count
                .ReturnsIntBytes(0);

            var character = reader.ReadSLBObject();

            Assert.AreEqual(character.ToaName, toaName);
            Assert.AreEqual(character.CharName, charName);
            Assert.AreEqual(character.CharCont, charCont);
            Assert.IsTrue(character.Entries.Count == 0);

            streamMock.Verify(stream => stream.ReadByte(), Times.Exactly(4));
            streamMock.VerifyNoOtherCalls();

            identifierReaderMock.Verify(identifierReader => identifierReader.ReadSLBObject(), Times.Exactly(3));
            identifierReaderMock.VerifyNoOtherCalls();

            infoReaderMock.VerifyNoOtherCalls();
        }
    }
}