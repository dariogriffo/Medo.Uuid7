using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Medo;

namespace Tests;

[TestClass]
public class Uuid7_Tests {

    [TestMethod]
    public void Uuid7_Empty() {
        Assert.AreEqual(Guid.Empty, Uuid7.Empty.ToGuid());
        Assert.IsTrue(Uuid7.Empty.Equals(Guid.Empty));
        Assert.IsTrue(Uuid7.Empty.Equals(Guid.Empty));
        Assert.AreEqual("00000000-0000-0000-0000-000000000000", Uuid7.Empty.ToString());
        Assert.AreEqual("0000000000000000000000000", Uuid7.Empty.ToId25String());
    }

    [TestMethod]
    public void Uuid7_New() {
        var uuid1 = Uuid7.NewUuid7();
        var uuid2 = Uuid7.NewUuid7();
        Assert.AreNotEqual(uuid1, uuid2);
    }

    [TestMethod]
    public void Uuid7_NewGuid() {
        var uuid1 = Uuid7.NewGuid();
        var uuid2 = Uuid7.NewGuid();
        Assert.AreNotEqual(uuid1, uuid2);
    }

    [TestMethod]
    public void Uuid7_NewFromBytes() {
        var uuid = new Uuid7(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });
        Assert.AreEqual("01020304-0506-0708-090a-0b0c0d0e0f10", uuid.ToString());
    }

#if NET6_0_OR_GREATER
    [TestMethod]
    public void Uuid7_NewFromSpan() {
        var bytes = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
        var uuid = new Uuid7(bytes.AsSpan(1));
        Assert.AreEqual("01020304-0506-0708-090a-0b0c0d0e0f10", uuid.ToString());
    }
#endif

#if NET6_0_OR_GREATER
    [TestMethod]
    public void Uuid7_WriteToSpan() {
        var uuid = Uuid7.NewUuid7();

        var bytes = new byte[16];
        Assert.IsTrue(uuid.TryWriteBytes(bytes));

        Assert.IsTrue(CompareArrays(bytes, uuid.ToByteArray()));
    }

    [TestMethod]
    public void Uuid7_WriteToLargerSpan() {
        var uuidBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
        var uuid = new Uuid7(uuidBytes);

        var bytes = new byte[17];
        Assert.IsTrue(uuid.TryWriteBytes(bytes.AsSpan()[1..]));

        Assert.AreEqual("00-01-02-03-04-05-06-07-08-09-0A-0B-0C-0D-0E-0F-10", BitConverter.ToString(bytes));
    }

    [TestMethod]
    public void Uuid7_WriteToSmallSpan() {
        var uuidBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
        var uuid = new Uuid7(uuidBytes);

        var bytes = new byte[15];
        Assert.IsFalse(uuid.TryWriteBytes(bytes.AsSpan()));

        Assert.AreEqual("00-00-00-00-00-00-00-00-00-00-00-00-00-00-00", BitConverter.ToString(bytes));
    }
#endif

    [TestMethod]
    public void Uuid7_HashCode() {
        var uuidBytes = new byte[] { 0xFA, 0xBB, 0x0D, 0xF2, 0xCB, 0xE8, 0xDD, 0x68, 0x5D, 0xC4, 0x84, 0xD8, 0xC6, 0x27, 0xEA, 0x4A };
        var uuid1 = new Uuid7(uuidBytes);
        var uuid2 = new Uuid7(uuidBytes);
        Assert.AreEqual(uuid1.GetHashCode(), uuid2.GetHashCode());
    }

#if NET6_0 || NET7_0
    [TestMethod]
    public void Uuid7_HashCodeGuidCompatible() {
        var uuid = Uuid7.NewUuid7();
        var guid = uuid.ToGuid();
        Assert.AreEqual(uuid.GetHashCode(), guid.GetHashCode());
    }
#endif

    [TestMethod]
    public void Uuid7_GuidAndBack() {
        var uuid1 = Uuid7.NewUuid7();
        var guid = uuid1.ToGuid();
        var uuid2 = new Uuid7(guid);
        Assert.AreEqual(uuid1, uuid2);
    }

    [TestMethod]
    public void Uuid7_GuidToString() {
        var uuid = new Uuid7(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });
        var guid = uuid.ToGuid();
        Assert.AreEqual("01020304-0506-0708-090a-0b0c0d0e0f10", uuid.ToString());
        Assert.AreEqual("04030201-0605-0807-090a-0b0c0d0e0f10", guid.ToString());  // GUIDs are little-endian so string representation will not be the same
    }

    [TestMethod]
    public void Uuid7_TestAlwaysIncreasing() {
        var oldUuid = Uuid7.Empty;
        for (var i = 0; i < 1000000; i++) {  // assuming we're not generating more than 2^21 each millisecond, they will be monotonically increasing
            var newUuid = Uuid7.NewUuid7();
            Assert.IsTrue(UuidToNumber(newUuid) > UuidToNumber(oldUuid), $"Failed at iteration {i}\n{oldUuid}\n{newUuid}");  // using UuidToNumber intentionaly as to avoid trusting operator overloads
            oldUuid = newUuid;
        }
    }

    [TestMethod]
    public void Uuid7_TestUniqueness() {
        var set = new HashSet<Uuid7>();

        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < 1000) {
            if (!set.Add(Uuid7.NewUuid7())) {
                throw new InvalidOperationException("Duplicate UUIDv7 generated.");
            }
        }
    }

    [TestMethod]
    public void Uuid7_TestThreadedUniqueness() {
        var set = new ConcurrentDictionary<Uuid7, nint>();

        Parallel.For(0, Math.Max(1, Environment.ProcessorCount / 2), (i) => {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 1000) {
                if (!set.TryAdd(Uuid7.NewUuid7(), 0)) {
                    throw new InvalidOperationException("Duplicate UUIDv4 generated.");
                }
            }
        });
    }

    [TestMethod]
    public void Uuid7_OperatorLessThan() {
        var uuid1 = new Uuid7(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, });
        var uuid2 = new Uuid7(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, });
        var uuid3 = new Uuid7(new byte[] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, });
        var uuid4 = new Uuid7(new byte[] { 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, });
        Assert.IsTrue(uuid1 < uuid2);
        Assert.IsTrue(uuid1 < uuid3);
        Assert.IsTrue(uuid1 < uuid4);
        Assert.IsTrue(uuid2 < uuid3);
        Assert.IsTrue(uuid3 < uuid4);
        Assert.IsTrue(uuid3 < uuid4);
        Assert.IsFalse(uuid1 > uuid2);
        Assert.IsFalse(uuid1 > uuid3);
        Assert.IsFalse(uuid1 > uuid4);
        Assert.IsFalse(uuid2 > uuid3);
        Assert.IsFalse(uuid3 > uuid4);
        Assert.IsFalse(uuid3 > uuid4);
        Assert.IsFalse(uuid1 == uuid2);
        Assert.IsFalse(uuid1 == uuid3);
        Assert.IsFalse(uuid1 == uuid4);
        Assert.IsFalse(uuid2 == uuid3);
        Assert.IsFalse(uuid3 == uuid4);
        Assert.IsFalse(uuid3 == uuid4);
    }

    [TestMethod]
    public void Uuid7_OperatorMoreThan() {
        var uuid1 = new Uuid7(new byte[] { 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, });
        var uuid2 = new Uuid7(new byte[] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, });
        var uuid3 = new Uuid7(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, });
        var uuid4 = new Uuid7(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, });
        Assert.IsTrue(uuid1 > uuid2);
        Assert.IsTrue(uuid1 > uuid3);
        Assert.IsTrue(uuid1 > uuid4);
        Assert.IsTrue(uuid2 > uuid3);
        Assert.IsTrue(uuid3 > uuid4);
        Assert.IsTrue(uuid3 > uuid4);
        Assert.IsFalse(uuid1 < uuid2);
        Assert.IsFalse(uuid1 < uuid3);
        Assert.IsFalse(uuid1 < uuid4);
        Assert.IsFalse(uuid2 < uuid3);
        Assert.IsFalse(uuid3 < uuid4);
        Assert.IsFalse(uuid3 < uuid4);
        Assert.IsFalse(uuid1 == uuid2);
        Assert.IsFalse(uuid1 == uuid3);
        Assert.IsFalse(uuid1 == uuid4);
        Assert.IsFalse(uuid2 == uuid3);
        Assert.IsFalse(uuid3 == uuid4);
        Assert.IsFalse(uuid3 == uuid4);
    }

    [TestMethod]
    public void Uuid7_OperatorFromGuid() {
        Guid guid = Guid.NewGuid();
        Uuid7 uuid = guid;
        Assert.AreEqual(uuid, guid);  // binary equality
        if (!BitConverter.IsLittleEndian) {
            Assert.AreEqual(guid, uuid);  // same only on BE platforms
        }
    }

    [TestMethod]
    public void Uuid7_OperatorToGuid() {
        Uuid7 uuid = Uuid7.NewUuid7();
        Guid guid = uuid;
        Assert.AreEqual(uuid, guid);  // binary equality
        if (!BitConverter.IsLittleEndian) {
            Assert.AreEqual(guid, uuid);  // same only on BE platforms
        }
    }


    [TestMethod]
    public void Uuid7_CompareTo() {
        var uuid1 = new Uuid7(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, });
        var uuid2 = new Uuid7(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, });
        var uuid3 = new Uuid7(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 9, 0, 0, 0, 0, 0, 0, 0, });
        var uuid4 = new Uuid7(new byte[] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, });
        var uuid5 = new Uuid7(new byte[] { 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, });

        Assert.IsTrue(uuid1.CompareTo(uuid1) == 0);
        Assert.IsTrue(uuid1.CompareTo(uuid2) < 0);
        Assert.IsTrue(uuid1.CompareTo(uuid3) < 0);
        Assert.IsTrue(uuid1.CompareTo(uuid4) < 0);
        Assert.IsTrue(uuid1.CompareTo(uuid5) < 0);

        Assert.IsTrue(uuid2.CompareTo(uuid1) > 0);
        Assert.IsTrue(uuid2.CompareTo(uuid2) == 0);
        Assert.IsTrue(uuid2.CompareTo(uuid3) < 0);
        Assert.IsTrue(uuid2.CompareTo(uuid4) < 0);
        Assert.IsTrue(uuid2.CompareTo(uuid5) < 0);

        Assert.IsTrue(uuid3.CompareTo(uuid1) > 0);
        Assert.IsTrue(uuid3.CompareTo(uuid2) > 0);
        Assert.IsTrue(uuid3.CompareTo(uuid3) == 0);
        Assert.IsTrue(uuid3.CompareTo(uuid4) < 0);
        Assert.IsTrue(uuid3.CompareTo(uuid5) < 0);

        Assert.IsTrue(uuid4.CompareTo(uuid1) > 0);
        Assert.IsTrue(uuid4.CompareTo(uuid2) > 0);
        Assert.IsTrue(uuid4.CompareTo(uuid3) > 0);
        Assert.IsTrue(uuid4.CompareTo(uuid4) == 0);
        Assert.IsTrue(uuid4.CompareTo(uuid5) < 0);

        Assert.IsTrue(uuid5.CompareTo(uuid1) > 0);
        Assert.IsTrue(uuid5.CompareTo(uuid2) > 0);
        Assert.IsTrue(uuid5.CompareTo(uuid3) > 0);
        Assert.IsTrue(uuid5.CompareTo(uuid4) > 0);
        Assert.IsTrue(uuid5.CompareTo(uuid5) == 0);
    }

    [TestMethod]
    public void Uuid7_CompareToGuid() {
        var uuid1 = new Uuid7(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, });
        var uuid2 = new Uuid7(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, });
        var uuid3 = new Uuid7(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 9, 0, 0, 0, 0, 0, 0, 0, });
        var uuid4 = new Uuid7(new byte[] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, });
        var uuid5 = new Uuid7(new byte[] { 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, });

        Assert.IsTrue(uuid1.CompareTo(new Guid(uuid1.ToByteArray())) == 0);
        Assert.IsTrue(uuid1.CompareTo(new Guid(uuid2.ToByteArray())) < 0);
        Assert.IsTrue(uuid1.CompareTo(new Guid(uuid2.ToByteArray())) < 0);
        Assert.IsTrue(uuid1.CompareTo(new Guid(uuid2.ToByteArray())) < 0);
        Assert.IsTrue(uuid1.CompareTo(new Guid(uuid2.ToByteArray())) < 0);

        Assert.IsTrue(uuid2.CompareTo(new Guid(uuid1.ToByteArray())) > 0);
        Assert.IsTrue(uuid2.CompareTo(new Guid(uuid2.ToByteArray())) == 0);
        Assert.IsTrue(uuid2.CompareTo(new Guid(uuid3.ToByteArray())) < 0);
        Assert.IsTrue(uuid2.CompareTo(new Guid(uuid4.ToByteArray())) < 0);
        Assert.IsTrue(uuid2.CompareTo(new Guid(uuid5.ToByteArray())) < 0);

        Assert.IsTrue(uuid3.CompareTo(new Guid(uuid1.ToByteArray())) > 0);
        Assert.IsTrue(uuid3.CompareTo(new Guid(uuid2.ToByteArray())) > 0);
        Assert.IsTrue(uuid3.CompareTo(new Guid(uuid3.ToByteArray())) == 0);
        Assert.IsTrue(uuid3.CompareTo(new Guid(uuid4.ToByteArray())) < 0);
        Assert.IsTrue(uuid3.CompareTo(new Guid(uuid5.ToByteArray())) < 0);

        Assert.IsTrue(uuid4.CompareTo(new Guid(uuid1.ToByteArray())) > 0);
        Assert.IsTrue(uuid4.CompareTo(new Guid(uuid2.ToByteArray())) > 0);
        Assert.IsTrue(uuid4.CompareTo(new Guid(uuid3.ToByteArray())) > 0);
        Assert.IsTrue(uuid4.CompareTo(new Guid(uuid4.ToByteArray())) == 0);
        Assert.IsTrue(uuid4.CompareTo(new Guid(uuid5.ToByteArray())) < 0);

        Assert.IsTrue(uuid5.CompareTo(new Guid(uuid1.ToByteArray())) > 0);
        Assert.IsTrue(uuid5.CompareTo(new Guid(uuid2.ToByteArray())) > 0);
        Assert.IsTrue(uuid5.CompareTo(new Guid(uuid3.ToByteArray())) > 0);
        Assert.IsTrue(uuid5.CompareTo(new Guid(uuid4.ToByteArray())) > 0);
        Assert.IsTrue(uuid5.CompareTo(new Guid(uuid5.ToByteArray())) == 0);
    }

    [TestMethod]
    public void Uuid7_Equals() {
        var uuid1 = new Uuid7(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, });
        var uuid2 = new Uuid7(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, });
        var uuid3 = new Uuid7(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 9, 0, 0, 0, 0, 0, 0, 0, });
        var uuid4 = new Uuid7(new byte[] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, });
        var uuid5 = new Uuid7(new byte[] { 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, });

        Assert.IsTrue(uuid1.Equals(uuid1));
        Assert.IsFalse(uuid1.Equals(uuid2));
        Assert.IsFalse(uuid1.Equals(uuid3));
        Assert.IsFalse(uuid1.Equals(uuid4));
        Assert.IsFalse(uuid1.Equals(uuid5));

        Assert.IsFalse(uuid2.Equals(uuid1));
        Assert.IsTrue(uuid2.Equals(uuid2));
        Assert.IsFalse(uuid2.Equals(uuid3));
        Assert.IsFalse(uuid2.Equals(uuid4));
        Assert.IsFalse(uuid2.Equals(uuid5));

        Assert.IsFalse(uuid3.Equals(uuid1));
        Assert.IsFalse(uuid3.Equals(uuid2));
        Assert.IsTrue(uuid3.Equals(uuid3));
        Assert.IsFalse(uuid3.Equals(uuid4));
        Assert.IsFalse(uuid3.Equals(uuid5));

        Assert.IsFalse(uuid4.Equals(uuid1));
        Assert.IsFalse(uuid4.Equals(uuid2));
        Assert.IsFalse(uuid4.Equals(uuid3));
        Assert.IsTrue(uuid4.Equals(uuid4));
        Assert.IsFalse(uuid4.Equals(uuid5));

        Assert.IsFalse(uuid5.Equals(uuid1));
        Assert.IsFalse(uuid5.Equals(uuid2));
        Assert.IsFalse(uuid5.Equals(uuid3));
        Assert.IsFalse(uuid5.Equals(uuid4));
        Assert.IsTrue(uuid5.Equals(uuid5));
    }

    [TestMethod]
    public void Uuid7_EqualsGuid() {
        var uuid1 = new Uuid7(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, });
        var uuid2 = new Uuid7(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, });
        var uuid3 = new Uuid7(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 9, 0, 0, 0, 0, 0, 0, 0, });
        var uuid4 = new Uuid7(new byte[] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, });
        var uuid5 = new Uuid7(new byte[] { 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, });

        Assert.IsTrue(uuid1.Equals(new Guid(uuid1.ToByteArray())));
        Assert.IsFalse(uuid1.Equals(new Guid(uuid2.ToByteArray())));
        Assert.IsFalse(uuid1.Equals(new Guid(uuid3.ToByteArray())));
        Assert.IsFalse(uuid1.Equals(new Guid(uuid4.ToByteArray())));
        Assert.IsFalse(uuid1.Equals(new Guid(uuid5.ToByteArray())));

        Assert.IsFalse(uuid2.Equals(new Guid(uuid1.ToByteArray())));
        Assert.IsTrue(uuid2.Equals(new Guid(uuid2.ToByteArray())));
        Assert.IsFalse(uuid2.Equals(new Guid(uuid3.ToByteArray())));
        Assert.IsFalse(uuid2.Equals(new Guid(uuid4.ToByteArray())));
        Assert.IsFalse(uuid2.Equals(new Guid(uuid5.ToByteArray())));

        Assert.IsFalse(uuid3.Equals(new Guid(uuid1.ToByteArray())));
        Assert.IsFalse(uuid3.Equals(new Guid(uuid2.ToByteArray())));
        Assert.IsTrue(uuid3.Equals(new Guid(uuid3.ToByteArray())));
        Assert.IsFalse(uuid3.Equals(new Guid(uuid4.ToByteArray())));
        Assert.IsFalse(uuid3.Equals(new Guid(uuid5.ToByteArray())));

        Assert.IsFalse(uuid4.Equals(new Guid(uuid1.ToByteArray())));
        Assert.IsFalse(uuid4.Equals(new Guid(uuid2.ToByteArray())));
        Assert.IsFalse(uuid4.Equals(new Guid(uuid3.ToByteArray())));
        Assert.IsTrue(uuid4.Equals(new Guid(uuid4.ToByteArray())));
        Assert.IsFalse(uuid4.Equals(new Guid(uuid5.ToByteArray())));

        Assert.IsFalse(uuid5.Equals(new Guid(uuid1.ToByteArray())));
        Assert.IsFalse(uuid5.Equals(new Guid(uuid2.ToByteArray())));
        Assert.IsFalse(uuid5.Equals(new Guid(uuid3.ToByteArray())));
        Assert.IsFalse(uuid5.Equals(new Guid(uuid4.ToByteArray())));
        Assert.IsTrue(uuid5.Equals(new Guid(uuid5.ToByteArray())));
    }


    [DataTestMethod]
    [DataRow(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, "00000000-0000-0000-0000-000000000000")]
    [DataRow(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 }, "00000000-0000-0000-0000-000000000001")]
    [DataRow(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2 }, "00000000-0000-0000-0000-000000000002")]
    [DataRow(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 9, 0, 0, 0, 0, 0, 0, 0 }, "00000000-0000-0000-0900-000000000000")]
    [DataRow(new byte[] { 0, 1, 2, 3, 52, 53, 118, 119, 184, 185, 250, 251, 252, 253, 254, 255 }, "00010203-3435-7677-b8b9-fafbfcfdfeff")]
    [DataRow(new byte[] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, "01000000-0000-0000-0000-000000000000")]
    [DataRow(new byte[] { 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, "02000000-0000-0000-0000-000000000000")]
    [DataRow(new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 }, "ffffffff-ffff-ffff-ffff-ffffffffffff")]
    public void Uuid7_String(byte[] bytes, string text) {
        var uuid = new Uuid7(bytes);
        Assert.AreEqual(text, uuid.ToString());
        Assert.AreEqual(uuid, Uuid7.FromString(text));
    }

    [TestMethod]
    public void Uuid7_StringFormat() {
        var bytes = new byte[] { 0, 1, 2, 3, 52, 53, 118, 119, 184, 185, 250, 251, 252, 253, 254, 255 };
        var uuid = new Uuid7(bytes);
        Assert.AreEqual("00010203-3435-7677-b8b9-fafbfcfdfeff", uuid.ToString(null));
        Assert.AreEqual("00010203-3435-7677-b8b9-fafbfcfdfeff", uuid.ToString(""));
        Assert.AreEqual("0001020334357677b8b9fafbfcfdfeff", uuid.ToString("N"));
        Assert.AreEqual("0001020334357677b8b9fafbfcfdfeff", uuid.ToString("n"));
        Assert.AreEqual("00010203-3435-7677-b8b9-fafbfcfdfeff", uuid.ToString("D"));
        Assert.AreEqual("00010203-3435-7677-b8b9-fafbfcfdfeff", uuid.ToString("d"));
        Assert.AreEqual("{00010203-3435-7677-b8b9-fafbfcfdfeff}", uuid.ToString("B"));
        Assert.AreEqual("{00010203-3435-7677-b8b9-fafbfcfdfeff}", uuid.ToString("b"));
        Assert.AreEqual("(00010203-3435-7677-b8b9-fafbfcfdfeff)", uuid.ToString("P"));
        Assert.AreEqual("(00010203-3435-7677-b8b9-fafbfcfdfeff)", uuid.ToString("p"));
        Assert.AreEqual("{0x00010203,0x3435,0x7677,{0xb8,0xb9,0xfa,0xfb,0xfc,0xfd,0xfe,0xff}}", uuid.ToString("X"));
        Assert.AreEqual("{0x00010203,0x3435,0x7677,{0xb8,0xb9,0xfa,0xfb,0xfc,0xfd,0xfe,0xff}}", uuid.ToString("x"));
        Assert.ThrowsException<FormatException>(() => {
            uuid.ToString("y");
        });
    }

    [DataTestMethod]
    [DataRow(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, "1111111111111111111111")]
    [DataRow(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 }, "1111111111111111111112")]
    [DataRow(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2 }, "1111111111111111111113")]
    [DataRow(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 9, 0, 0, 0, 0, 0, 0, 0 }, "111111111112WK48GNSUQf")]
    [DataRow(new byte[] { 0, 1, 2, 3, 52, 53, 118, 119, 184, 185, 250, 251, 252, 253, 254, 255 }, "112drYSDr45nCJ6chixdxJ")]
    [DataRow(new byte[] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, "18AQGAut7N92awznwCnjuR")]
    [DataRow(new byte[] { 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, "1FKoXLpmDjH4AtzasQaUoq")]
    [DataRow(new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 }, "YcVfxkQb6JRzqk5kF2tNLv")]
    public void Uuid7_Id22(byte[] bytes, string id22Text) {
        var uuid = new Uuid7(bytes);
        Assert.AreEqual(id22Text, uuid.ToId22String());
        Assert.AreEqual(uuid, Uuid7.FromId22String(id22Text));
    }

    [DataTestMethod]
    [DataRow("0001/0203/3435/7677/b8b9/fafb/fcfd/feff", "112d/rYSD/r45/nCJ6/chix/dxJ")]
    [DataRow("ffffffff-ffff-ffff-ffff-ffffffffffff", "YcVfx kQb6JR zqk5k F2tNLv")]
    public void Uuid7_Id22Dirty(string uuidText, string id22Text) {
        var expectedUuid = Uuid7.FromString(uuidText);
        var id22Uuid = Uuid7.FromId22String(id22Text);
        Assert.AreEqual(expectedUuid, id22Uuid);
        Assert.AreNotEqual(Uuid7.Empty, expectedUuid);
        Assert.AreNotEqual(Uuid7.Empty, id22Uuid);
    }


    [DataTestMethod]
    [DataRow(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, "0000000000000000000000000")]
    [DataRow(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 }, "0000000000000000000000001")]
    [DataRow(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2 }, "0000000000000000000000002")]
    [DataRow(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 9, 0, 0, 0, 0, 0, 0, 0 }, "00000000000006q3abpe675nu")]
    [DataRow(new byte[] { 0, 1, 2, 3, 52, 53, 118, 119, 184, 185, 250, 251, 252, 253, 254, 255 }, "000jnpiacvek52kvka6to5ogn")]
    [DataRow(new byte[] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, "042kt5d5ybb4r50zg5p72g3f1")]
    [DataRow(new byte[] { 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, "0856marbwnn9ha1yxbde4x6v2")]
    [DataRow(new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 }, "usz5xbbiqsfq7s727n0pzr2xa")]
    public void Uuid7_Id25(byte[] bytes, string id25Text) {
        var uuid = new Uuid7(bytes);
        Assert.AreEqual(id25Text, uuid.ToId25String());
        Assert.AreEqual(uuid, Uuid7.FromId25String(id25Text));
    }

    [DataTestMethod]
    [DataRow("0001/0203/3435/7677/b8b9/fafb/fcfd/feff", "00/0jn/pi/acv/ek/52k/vk/a6t/o5/ogn")]
    [DataRow("ffffffff-ffff-ffff-ffff-ffffffffffff", "usz5x bbiqs fq7s7 27n0p zr2xa")]
    public void Uuid7_Id25Dirty(string uuidText, string id25Text) {
        var expectedUuid = Uuid7.FromString(uuidText);
        var id25Uuid = Uuid7.FromId25String(id25Text);
        Assert.AreEqual(expectedUuid, id25Uuid);
        Assert.AreNotEqual(Uuid7.Empty, expectedUuid);
        Assert.AreNotEqual(Uuid7.Empty, id25Uuid);
    }


#if NET6_0_OR_GREATER
    [TestMethod]
    public void Uuid7_Fill() {
        var uuids = new Uuid7[10000];
        Uuid7.Fill(uuids);

        var prevUuid = Uuid7.Empty;
        foreach (var uuid in uuids) {
            Assert.AreNotEqual(Uuid7.Empty, uuid);
            Assert.IsTrue(uuid > prevUuid);
            prevUuid = uuid;
        }
    }
#endif


    [TestMethod]
    public void Uuid7_MarshalBytes() {
        var uuid = Uuid7.NewUuid7();

        int size = Marshal.SizeOf(uuid);
        Assert.AreEqual(16, size);

        var bytes = new byte[size];
        var ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(uuid, ptr, true);
        Marshal.Copy(ptr, bytes, 0, size);
        Marshal.FreeHGlobal(ptr);  // it's a test, no need for try/finally block

        Assert.IsTrue(CompareArrays(uuid.ToByteArray(), bytes));
    }

    [TestMethod]
    public void Uuid7_UnmarshalBytes() {
        var bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };

        var ptr = Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, ptr, bytes.Length);
        var uuid = (Uuid7)Marshal.PtrToStructure(ptr, typeof(Uuid7));
        Marshal.FreeHGlobal(ptr);

        Assert.IsTrue(CompareArrays(bytes, uuid.ToByteArray()));
    }


    [TestMethod]
    public void Uuid7_Equals_Null() {  // https://github.com/medo64/Medo.Uuid7/issues/1
        var x = Uuid7.NewUuid7();
        var method = typeof(IEquatable<Uuid7>).GetMethod("Equals");
        var result = method.Invoke(x, new object[] { null });
        Assert.AreEqual(false, result);
    }

    [TestMethod]
    public void Uuid7_Equals_NullGuid() {  // https://github.com/medo64/Medo.Uuid7/issues/1
        var x = Uuid7.NewUuid7();
        var method = typeof(IEquatable<Guid>).GetMethod("Equals");
        var result = method.Invoke(x, new object[] { null });
        Assert.AreEqual(false, result);
    }

    [TestMethod]
    public void Uuid7_CompareTo_Null() {  // https://github.com/medo64/Medo.Uuid7/issues/1
        var x = Uuid7.NewUuid7();
        var method = typeof(IComparable<Uuid7>).GetMethod("CompareTo");
        var result = method.Invoke(x, new object[] { null });
        Assert.AreEqual(+1, result);
    }

    [TestMethod]
    public void Uuid7_CompareTo_NullGuid() {  // https://github.com/medo64/Medo.Uuid7/issues/1
        var x = Uuid7.NewUuid7();
        var method = typeof(IComparable<Guid>).GetMethod("CompareTo");
        var result = method.Invoke(x, new object[] { null });
        Assert.AreEqual(+1, result);
    }


    [TestMethod]
    public void Uuid7_MsSqlGuid_1() {
        var uuid = Uuid7.NewUuid7();
        var guid = uuid.ToGuidMsSql();
        if (BitConverter.IsLittleEndian) {
            Assert.AreNotEqual(guid, uuid);  // not the same binary
            Assert.AreEqual(uuid.ToString(), guid.ToString());  // same text representation
        } else {  // on BE platforms, they are equal both binary and in string format
            Assert.AreEqual(guid, uuid);  // same binary
            Assert.AreEqual(uuid.ToString(), guid.ToString());  // same text representation
        }
    }

    [TestMethod]
    public void Uuid7_MsSqlGuid_2() {
        var uuid = new Uuid7(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });
        var guid = uuid.ToGuidMsSql();
        if (BitConverter.IsLittleEndian) {
            Assert.AreNotEqual(guid, uuid);  // not the same binary
            Assert.AreEqual("01020304-0506-0708-090a-0b0c0d0e0f10", uuid.ToString());
            Assert.AreEqual("01020304-0506-0708-090a-0b0c0d0e0f10", guid.ToString());
        } else {  // on BE platforms, they are equal both binary and in string format
            Assert.AreEqual(guid, uuid);  // same binary
            Assert.AreEqual("01020304-0506-0708-090a-0b0c0d0e0f10", uuid.ToString());
            Assert.AreEqual("01020304-0506-0708-090a-0b0c0d0e0f10", guid.ToString());
        }
    }


    #region Helpers

#if NET6_0_OR_GREATER
    private BigInteger UuidToNumber(Uuid7 uuid) {
        return new BigInteger(uuid.ToByteArray(), isUnsigned: true, isBigEndian: true);
    }
#else
    private BigInteger UuidToNumber(Uuid7 uuid) {
        var bytes = uuid.ToByteArray();
        if (BitConverter.IsLittleEndian) {
            Array.Reverse(bytes);
        }
        var bytes2 = new byte[bytes.Length + 1];
        Buffer.BlockCopy(bytes, 0, bytes2, 1, bytes.Length);
        return new BigInteger(bytes2);
    }
#endif

    private static bool CompareArrays(byte[] buffer1, byte[] buffer2) {
        var comparer = EqualityComparer<byte>.Default;
        if (buffer1.Length != buffer2.Length) { return false; }  // should not really happen
        for (int i = 0; i < buffer1.Length; i++) {
            if (!comparer.Equals(buffer1[i], buffer2[i])) {
                return false;
            }
        }
        return true;
    }

    #endregion Helpers

}
