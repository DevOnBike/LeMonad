namespace DevOnBike.LeMonad
{
    public static class ResultExtensions
    {
        // =======================================================================
        // 1. ASYNC BRIDGES (Bridges for Task<Result>)
        // =======================================================================

        /// <summary>
        /// Asynchronous Map: Awaits the Task, then executes Map on the result.
        /// </summary>
        public static async Task<Result<U, TError>> MapAsync<T, U, TError>(
            this Task<Result<T, TError>> resultTask,
            Func<T, U> mapper,
            CancellationToken token)
        {
            var result = await resultTask.WaitAsync(token).ConfigureAwait(false);
            return result.Map(mapper);
        }

        public static async Task<Result<T, F>> MapErrorAsync<T, TError, F>(
            this Task<Result<T, TError>> resultTask,
            Func<TError, F> errorMapper,
            CancellationToken token)
        {
            var result = await resultTask.WaitAsync(token).ConfigureAwait(false);
            return result.MapError(errorMapper);
        }

        /// <summary>
        /// Asynchronous Map accepting a Token.
        /// </summary>
        public static async Task<Result<U, TError>> MapAsync<T, U, TError>(
            this Task<Result<T, TError>> resultTask,
            Func<T, CancellationToken, Task<U>> mapper,
            CancellationToken token)
        {
            var result = await resultTask.WaitAsync(token).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return Result<U, TError>.Failure(result.Error);
            }

            // Removed explicit ThrowIfCancellationRequested
            var newValue = await mapper(result.Value, token).ConfigureAwait(false);

            return Result<U, TError>.Success(newValue);
        }

        /// <summary>
        /// Consumes the Result (Unit). Synchronous Version.
        /// </summary>
        public static async Task HandleFailureAsync<TError>(
            this Task<Result<Unit, TError>> resultTask,
            Action<TError> onFailure,
            CancellationToken token)
        {
            var result = await resultTask.WaitAsync(token).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                onFailure(result.Error);
            }
        }

        /// <summary>
        /// Consumes the Result (Unit). Asynchronous Version with Token.
        /// </summary>
        public static async Task HandleFailureAsync<TError>(
            this Task<Result<Unit, TError>> resultTask,
            Func<TError, CancellationToken, Task> onFailure,
            CancellationToken token)
        {
            var result = await resultTask.WaitAsync(token).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                await onFailure(result.Error, token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// BindAsync propagating the token.
        /// </summary>
        public static async Task<Result<U, TError>> BindAsync<T, U, TError>(
            this Task<Result<T, TError>> resultTask,
            Func<T, CancellationToken, Task<Result<U, TError>>> binder,
            CancellationToken token)
        {
            var result = await resultTask.WaitAsync(token).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return Result<U, TError>.Failure(result.Error);
            }

            // Removed explicit ThrowIfCancellationRequested
            return await binder(result.Value, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Bind version where the binding function is synchronous.
        /// </summary>
        public static async Task<Result<U, TError>> BindAsync<T, U, TError>(
            this Task<Result<T, TError>> resultTask,
            Func<T, Result<U, TError>> binder,
            CancellationToken token)
        {
            var result = await resultTask.WaitAsync(token).ConfigureAwait(false);

            return result.Bind(binder);
        }

        // =======================================================================
        // 2. TAP / SIDE EFFECTS
        // =======================================================================

        public static Result<T, TError> Tap<T, TError>(
            this Result<T, TError> result,
            Action<T> action)
        {
            if (result.IsSuccess) action(result.Value);
            return result;
        }

        public static Result<T, TError> TapError<T, TError>(
            this Result<T, TError> result,
            Action<TError> action)
        {
            if (!result.IsSuccess) action(result.Error);
            return result;
        }

        public static async Task<Result<T, TError>> TapErrorAsync<T, TError>(
            this Task<Result<T, TError>> resultTask,
            Action<TError> action,
            CancellationToken token)
        {
            var result = await resultTask.WaitAsync(token).ConfigureAwait(false);
            if (!result.IsSuccess) action(result.Error);
            return result;
        }

        /// <summary>
        /// Asynchronous Tap with Token (e.g., logging to a database).
        /// </summary>
        public static async Task<Result<T, TError>> TapAsync<T, TError>(
            this Task<Result<T, TError>> resultTask,
            Func<T, CancellationToken, Task> action,
            CancellationToken token)
        {
            var result = await resultTask.WaitAsync(token).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                // Removed explicit ThrowIfCancellationRequested
                await action(result.Value, token).ConfigureAwait(false);
            }

            return result;
        }

        public static async Task<Result<T, TError>> TapAsync<T, TError>(
            this Task<Result<T, TError>> resultTask,
            Action<T> action,
            CancellationToken token)
        {
            var result = await resultTask.WaitAsync(token).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                action(result.Value);
            }

            return result;
        }

        // =======================================================================
        // 3. FACTORIES & CONVERSIONS
        // =======================================================================

        public static Result<T, TError> Ensure<T, TError>(
            this T value,
            Func<T, bool> predicate,
            TError error)
        {
            if (predicate(value)) return Result<T, TError>.Success(value);
            return Result<T, TError>.Failure(error);
        }

        public static Result<T, TError> Ensure<T, TError>(
            this Result<T, TError> result,
            Func<T, bool> predicate,
            TError error)
        {
            if (!result.IsSuccess) return result;
            if (!predicate(result.Value)) return Result<T, TError>.Failure(error);
            return result;
        }

        public static async Task<Result<T, TError>> EnsureAsync<T, TError>(
            this Task<Result<T, TError>> resultTask,
            Func<T, bool> predicate,
            TError error,
            CancellationToken token)
        {
            var result = await resultTask.WaitAsync(token).ConfigureAwait(false);
            return result.Ensure(predicate, error);
        }

        public static async Task<R> MatchAsync<T, TError, R>(
            this Task<Result<T, TError>> resultTask,
            Func<T, R> onSuccess,
            Func<TError, R> onFailure,
            CancellationToken token)
        {
            var result = await resultTask.WaitAsync(token).ConfigureAwait(false);
            return result.Match(onSuccess, onFailure);
        }

        /// <summary>
        /// PIPELINE START (Sync Mapper): Task<T> -> Result<U>
        /// </summary>
        public static async Task<Result<U, TError>> ToResultAsync<T, U, TError>(
            this Task<T> task,
            Func<T, Result<U, TError>> mapper,
            CancellationToken token)
        {
            var input = await task.WaitAsync(token).ConfigureAwait(false);
            return mapper(input);
        }

        /// <summary>
        /// PIPELINE START (Async Mapper with Token).
        /// </summary>
        public static async Task<Result<U, TError>> ToResultAsync<T, U, TError>(
            this Task<T> task,
            Func<T, CancellationToken, Task<Result<U, TError>>> mapper,
            CancellationToken token)
        {
            var input = await task.WaitAsync(token).ConfigureAwait(false);
            // Removed explicit ThrowIfCancellationRequested
            return await mapper(input, token).ConfigureAwait(false);
        }
    }
}