namespace DevOnBike.LeMonad.Tests
{
    public class ResultExtensionsTests
    {
        // Token do testów metod asynchronicznych
        private readonly CancellationToken _ct = CancellationToken.None;

        // Pomocnicze metody fabryczne dla Task<Result>
        private Task<Result<int, string>> TaskSuccess(int val)
            => Task.FromResult(Result<int, string>.Success(val));

        private Task<Result<int, string>> TaskFailure(string err)
            => Task.FromResult(Result<int, string>.Failure(err));

        private Task<Result<Unit, string>> TaskUnitFailure(string err)
            => Task.FromResult(Result<Unit, string>.Failure(err));

        // =======================================================================
        // 1. MAP ASYNC (MapAsync, MapErrorAsync)
        // =======================================================================

        [Fact]
        public async Task MapAsync_SyncMapper_Transforms_Success()
        {
            var task = TaskSuccess(10);
            var result = await task.MapAsync(x => x * 2, _ct);

            Assert.True(result.IsSuccess);
            Assert.Equal(20, result.Value);
        }

        [Fact]
        public async Task MapAsync_SyncMapper_Propagates_Failure()
        {
            var task = TaskFailure("Error");
            var result = await task.MapAsync(x => x * 2, _ct);

            Assert.False(result.IsSuccess);
            Assert.Equal("Error", result.Error);
        }

        [Fact]
        public async Task MapAsync_AsyncMapper_Transforms_Success()
        {
            var task = TaskSuccess(10);
            // Testuje overload: Func<T, CT, Task<U>>
            var result = await task.MapAsync(async (x, ct) =>
            {
                await Task.Delay(1, ct);
                return x.ToString();
            }, _ct);

            Assert.True(result.IsSuccess);
            Assert.Equal("10", result.Value);
        }

        [Fact]
        public async Task MapAsync_AsyncMapper_Propagates_Failure_And_Skips_Mapper()
        {
            var task = TaskFailure("Error");
            bool executed = false;

            var result = await task.MapAsync(async (x, ct) =>
            {
                executed = true;
                return await Task.FromResult(x.ToString());
            }, _ct);

            Assert.False(result.IsSuccess);
            Assert.Equal("Error", result.Error);
            Assert.False(executed);
        }

        [Fact]
        public async Task MapErrorAsync_Transforms_Failure()
        {
            var task = TaskFailure("Error");
            var result = await task.MapErrorAsync(e => $"New {e}", _ct);

            Assert.False(result.IsSuccess);
            Assert.Equal("New Error", result.Error);
        }

        [Fact]
        public async Task MapErrorAsync_Propagates_Success()
        {
            var task = TaskSuccess(10);
            var result = await task.MapErrorAsync(e => "New Error", _ct);

            Assert.True(result.IsSuccess);
            Assert.Equal(10, result.Value);
        }

        // =======================================================================
        // 2. BIND ASYNC (BindAsync)
        // =======================================================================

        [Fact]
        public async Task BindAsync_AsyncBinder_Chains_Success()
        {
            var task = TaskSuccess(5);

            var result = await task.BindAsync(async (x, ct) =>
            {
                await Task.Delay(1, ct);
                return Result<string, string>.Success($"Val: {x}");
            }, _ct);

            Assert.True(result.IsSuccess);
            Assert.Equal("Val: 5", result.Value);
        }

        [Fact]
        public async Task BindAsync_AsyncBinder_ShortCircuits_On_Input_Failure()
        {
            var task = TaskFailure("Input Fail");
            bool executed = false;

            var result = await task.BindAsync(async (x, ct) =>
            {
                executed = true;
                return Result<string, string>.Success("Skipped");
            }, _ct);

            Assert.False(result.IsSuccess);
            Assert.Equal("Input Fail", result.Error);
            Assert.False(executed);
        }

        [Fact]
        public async Task BindAsync_SyncBinder_Chains_Success()
        {
            var task = TaskSuccess(5);

            // Testuje overload: Func<T, Result<U, E>> (bez Task w binderze)
            var result = await task.BindAsync(x => Result<string, string>.Success($"Sync: {x}"), _ct);

            Assert.True(result.IsSuccess);
            Assert.Equal("Sync: 5", result.Value);
        }

        // =======================================================================
        // 3. TAP & SIDE EFFECTS (Tap, TapAsync, HandleFailureAsync)
        // =======================================================================

        [Fact]
        public void Tap_Sync_Executes_On_Success()
        {
            Result<int, string> res = 10;
            int sideEffect = 0;

            res.Tap(x => sideEffect = x);

            Assert.Equal(10, sideEffect);
        }

        [Fact]
        public void Tap_Sync_Skips_On_Failure()
        {
            Result<int, string> res = "Err";
            bool executed = false;

            res.Tap(x => executed = true);

            Assert.False(executed);
        }

        [Fact]
        public void TapError_Sync_Executes_On_Failure()
        {
            Result<int, string> res = "Err";
            string log = "";

            res.TapError(e => log = e);

            Assert.Equal("Err", log);
        }

        [Fact]
        public void TapError_Sync_Skips_On_Success()
        {
            Result<int, string> res = 10;
            bool executed = false;

            res.TapError(e => executed = true);

            Assert.False(executed);
        }

        [Fact]
        public async Task TapErrorAsync_Executes_On_Failure()
        {
            var task = TaskFailure("Async Err");
            string log = "";

            await task.TapErrorAsync(e => log = e, _ct);

            Assert.Equal("Async Err", log);
        }

        [Fact]
        public async Task TapErrorAsync_Skips_On_Success()
        {
            var task = TaskSuccess(1);
            bool executed = false;

            await task.TapErrorAsync(e => executed = true, _ct);

            Assert.False(executed);
        }

        [Fact]
        public async Task TapAsync_Action_Executes_On_Success()
        {
            var task = TaskSuccess(10);
            int val = 0;

            // Overload: Action<T>
            await task.TapAsync(x => val = x, _ct);

            Assert.Equal(10, val);
        }

        [Fact]
        public async Task TapAsync_Func_Executes_On_Success()
        {
            var task = TaskSuccess(10);
            bool executed = false;

            // Overload: Func<T, CT, Task>
            await task.TapAsync(async (x, ct) =>
            {
                await Task.Delay(1, ct);
                executed = true;
            }, _ct);

            Assert.True(executed);
        }

        [Fact]
        public async Task TapAsync_Func_Skips_On_Failure()
        {
            var task = TaskFailure("Err");
            bool executed = false;

            await task.TapAsync(async (x, ct) => executed = true, _ct);

            Assert.False(executed);
        }

        [Fact]
        public async Task HandleFailureAsync_SyncAction_Executes()
        {
            var task = TaskUnitFailure("Fatal");
            string log = "";

            await task.HandleFailureAsync(e => log = e, _ct);

            Assert.Equal("Fatal", log);
        }

        [Fact]
        public async Task HandleFailureAsync_AsyncFunc_Executes()
        {
            var task = TaskUnitFailure("Fatal Async");
            string log = "";

            await task.HandleFailureAsync(async (e, ct) =>
            {
                await Task.Delay(1);
                log = e;
            }, _ct);

            Assert.Equal("Fatal Async", log);
        }

        [Fact]
        public async Task HandleFailureAsync_Skips_On_Success()
        {
            var task = Task.FromResult(Result<Unit, string>.Success(Unit.Value));
            bool executed = false;

            await task.HandleFailureAsync(e => executed = true, _ct);

            Assert.False(executed);
        }

        // =======================================================================
        // 4. FACTORIES & ENSURE (Ensure, EnsureAsync, MatchAsync)
        // =======================================================================

        [Fact]
        public void Ensure_Extension_On_Value_Success()
        {
            int val = 10;
            var res = val.Ensure(x => x > 5, "Too low");

            Assert.True(res.IsSuccess);
            Assert.Equal(10, res.Value);
        }

        [Fact]
        public void Ensure_Extension_On_Value_Failure()
        {
            int val = 1;
            var res = val.Ensure(x => x > 5, "Too low");

            Assert.False(res.IsSuccess);
            Assert.Equal("Too low", res.Error);
        }

        [Fact]
        public void Ensure_On_Result_Success_Predicate_Met()
        {
            Result<int, string> res = 10;
            var ensured = res.Ensure(x => x > 5, "Error");

            Assert.True(ensured.IsSuccess);
        }

        [Fact]
        public void Ensure_On_Result_Success_Predicate_Failed()
        {
            Result<int, string> res = 1;
            var ensured = res.Ensure(x => x > 5, "Too low");

            Assert.False(ensured.IsSuccess);
            Assert.Equal("Too low", ensured.Error);
        }

        [Fact]
        public void Ensure_On_Result_Failure_ShortCircuits()
        {
            Result<int, string> res = "Initial";
            var ensured = res.Ensure(x => x > 5, "New Error");

            // Powinno zachować stary błąd, nie nadpisać go nowym
            Assert.False(ensured.IsSuccess);
            Assert.Equal("Initial", ensured.Error);
        }

        [Fact]
        public async Task EnsureAsync_Success_Predicate_Met()
        {
            var task = TaskSuccess(10);
            var ensured = await task.EnsureAsync(x => x > 5, "Err", _ct);

            Assert.True(ensured.IsSuccess);
        }

        [Fact]
        public async Task MatchAsync_Success()
        {
            var task = TaskSuccess(100);
            var res = await task.MatchAsync(s => s.ToString(), e => "Fail", _ct);

            Assert.Equal("100", res);
        }

        [Fact]
        public async Task MatchAsync_Failure()
        {
            var task = TaskFailure("Err");
            var res = await task.MatchAsync(s => "Success", e => e + "!", _ct);

            Assert.Equal("Err!", res);
        }

        // =======================================================================
        // 5. ENTRY POINTS (ToResultAsync)
        // =======================================================================

        [Fact]
        public async Task ToResultAsync_SyncMapper_Starts_Pipeline()
        {
            var task = Task.FromResult(10); // Task<int>

            var result = await task.ToResultAsync(x => Result<string, string>.Success(x.ToString()), _ct);

            Assert.True(result.IsSuccess);
            Assert.Equal("10", result.Value);
        }

        [Fact]
        public async Task ToResultAsync_AsyncMapper_Starts_Pipeline()
        {
            var task = Task.FromResult(10); // Task<int>

            var result = await task.ToResultAsync(async (x, ct) =>
            {
                await Task.Delay(1, ct);
                return Result<string, string>.Success(x.ToString());
            }, _ct);

            Assert.True(result.IsSuccess);
            Assert.Equal("10", result.Value);
        }
    }
}