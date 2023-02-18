using FluentAssertions;
using Nucs.Streams;
using Xunit;

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Tests {
    public class MemoryStreamTests {
        [Fact]
        public static void MemoryStream_WriteToTests() {
            using (RentalMemoryStream ms2 = new RentalMemoryStream()) {
                byte[] bytArrRet;
                byte[] bytArr = new byte[] { byte.MinValue, byte.MaxValue, 1, 2, 3, 4, 5, 6, 128, 250 };

                // [] Write to FileStream, check the filestream
                ms2.Write(bytArr, 0, bytArr.Length);

                using (RentalMemoryStream readonlyStream = new RentalMemoryStream()) {
                    ms2.WriteTo(readonlyStream);
                    readonlyStream.Flush();
                    readonlyStream.Position = 0;
                    bytArrRet = new byte[(int) readonlyStream.Length];
                    readonlyStream.Read(bytArrRet, 0, (int) readonlyStream.Length);
                    for (int i = 0; i < bytArr.Length; i++) {
                        bytArr[i].Should().Be(bytArrRet[i]);
                    }
                }
            }

            // [] Write to memoryStream, check the memoryStream
            using (RentalMemoryStream ms2 = new RentalMemoryStream())
            using (RentalMemoryStream ms3 = new RentalMemoryStream()) {
                byte[] bytArrRet;
                byte[] bytArr = new byte[] { byte.MinValue, byte.MaxValue, 1, 2, 3, 4, 5, 6, 128, 250 };

                ms2.Write(bytArr, 0, bytArr.Length);
                ms2.WriteTo(ms3);
                ms3.Position = 0;
                bytArrRet = new byte[(int) ms3.Length];
                ms3.Read(bytArrRet, 0, (int) ms3.Length);
                for (int i = 0; i < bytArr.Length; i++) {
                    bytArr[i].Should().Be(bytArrRet[i]);
                }
            }
        }

        [Fact]
        public static void MemoryStream_WriteToTests_Negative() {
            using (RentalMemoryStream ms2 = new RentalMemoryStream()) {
                new Action(() => ms2.WriteTo(null)).Should().Throw<ArgumentNullException>();
                ms2.Write(new byte[] { 1 }, 0, 1);
                RentalMemoryStream readonlyStream = new RentalMemoryStream(1028);
                ms2.WriteTo(readonlyStream);

                readonlyStream.Dispose();

                // [] Pass in a closed stream
                new Action(() => ms2.WriteTo(readonlyStream)).Should().Throw<ObjectDisposedException>();
            }
        }
    }
}