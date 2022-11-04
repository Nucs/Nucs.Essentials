using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FluentAssertions;
using Nucs.Reflection;
using Nucs.Threading;
using Xunit;


namespace Nucs.Essentials.UnitTests {
    public class AsyncProcedureTests {
        [Fact]
        public async Task ProcedureOnce() {
            var box = new StrongBox<int>(0);
            var procedure = new AsyncProcedure<int>(Procedure);
            procedure.RunProcedure();
            (await procedure.AwaitTask().ConfigureAwait(false)).Should().Be(box.Value);
            (await procedure.EnsureCompleted().ConfigureAwait(false)).Should().Be(box.Value);

            Task<int> Procedure() {
                box.Value++;
                switch (box.Value) {
                    case 1:
                        return Task.FromResult(box.Value);
                }

                return Task.FromResult(0);
            }
        }

        [Fact]
        public async Task ProcedureTwice() {
            var box = new StrongBox<int>(0);
            var procedure = new AsyncProcedure<int>(Procedure, allowedAttempts: 2 /*can fail once*/);

            box.Value.Should().Be(0);
            procedure.RunProcedure();
            
            (await procedure.AwaitTask().ConfigureAwait(false)).Should().Be(box.Value);
            (await procedure.EnsureCompleted().ConfigureAwait(false)).Should().Be(box.Value);
            
            box.Value.Should().Be(1);
            procedure.RunProcedure();

            (await procedure.AwaitTask().ConfigureAwait(false)).Should().Be(box.Value);
            (await procedure.EnsureCompleted().ConfigureAwait(false)).Should().Be(box.Value);

            Task<int> Procedure() {
                box.Value++;
                switch (box.Value) {
                    case 1:
                        return Task.FromResult(box.Value);
                    case 2:
                        throw new Exception();
                    case 3:
                        return Task.FromResult(box.Value);
                }

                return Task.FromResult(0);
            }
        }

        [Fact]
        public async Task FaultyProcedureOnce() {
            var box = new StrongBox<int>(0);
            var procedure = new AsyncProcedure<int>(Procedure);
            procedure.RunProcedure();
            (await procedure.AwaitTask().ConfigureAwait(false)).Should().Be(box.Value);
            (await procedure.EnsureCompleted().ConfigureAwait(false)).Should().Be(box.Value);

            Task<int> Procedure() {
                box.Value++;
                switch (box.Value) {
                    case 1:
                        throw new Exception("Failed procedure");
                    case 2:
                        return Task.FromResult(box.Value);
                }

                return Task.FromResult(0);
            }
        }

        [Fact]
        public async Task FaultyProcedureOnceTooMuch() {
            var box = new StrongBox<int>(0);
            var procedure = new AsyncProcedure<int>(Procedure, allowedAttempts: 0);
            procedure.RunProcedure();
            new Action(() => procedure.AwaitTask().GetAwaiter().GetResult()).Should().Throw<Exception>();

            Task<int> Procedure() {
                box.Value++;
                switch (box.Value) {
                    case 1:
                        throw new Exception("Failed procedure");
                    case 2:
                        throw new Exception("Failed procedure");
                }

                return Task.FromResult(0);
            }
        }

        [Fact]
        public async Task FaultyProcedureBeStoppedRegardlessToAttempts() {
            var box = new StrongBox<int>(0);
            var procedure = new AsyncProcedure<int>(Procedure, allowedAttempts: int.MaxValue);
            procedure.RunProcedure();
            new Action(() => procedure.AwaitTask().GetAwaiter().GetResult()).Should().Throw<ProcedureFailedException>();

            Task<int> Procedure() {
                box.Value++;
                switch (box.Value) {
                    case 1:
                        throw new ProcedureFailedException("Failed procedure");
                    case 2:
                        throw new Exception("Failed procedure");
                }

                return Task.FromResult(0);
            }
        }
    }
}