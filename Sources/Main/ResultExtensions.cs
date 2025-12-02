namespace DevOnBike.LeMonad
{
    public static class ResultExtensions
    {
        /// <summary>
        /// Asynchroniczny Map: Czeka na Task, a potem wykonuje Map na wyniku.
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
        /// Asynchroniczny Map przyjmujący Token.
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

            // Usunięto explicit ThrowIfCancellationRequested
            var newValue = await mapper(result.Value, token).ConfigureAwait(false);

            return Result<U, TError>.Success(newValue);
        }

        /// <summary>
        /// Konsumuje Result (Unit). Wersja Synchroniczna.
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
        /// Konsumuje Result (Unit). Wersja Asynchroniczna z Tokenem.
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
        /// BindAsync propagujący token.
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

            // Usunięto explicit ThrowIfCancellationRequested
            return await binder(result.Value, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Wersja Bind, gdzie funkcja wiążąca jest synchroniczna.
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
        /// Asynchroniczny Tap z Tokenem (np. logowanie do bazy).
        /// </summary>
        public static async Task<Result<T, TError>> TapAsync<T, TError>(
            this Task<Result<T, TError>> resultTask,
            Func<T, CancellationToken, Task> action,
            CancellationToken token)
        {
            var result = await resultTask.WaitAsync(token).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                // Usunięto explicit ThrowIfCancellationRequested
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
        /// ROZRUCH POTOKU (Sync Mapper): Task<T> -> Result<U>
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
        /// ROZRUCH POTOKU (Async Mapper z Tokenem).
        /// </summary>
        public static async Task<Result<U, TError>> ToResultAsync<T, U, TError>(
            this Task<T> task,
            Func<T, CancellationToken, Task<Result<U, TError>>> mapper,
            CancellationToken token)
        {
            var input = await task.WaitAsync(token).ConfigureAwait(false);
            // Usunięto explicit ThrowIfCancellationRequested
            return await mapper(input, token).ConfigureAwait(false);
        }
    }
}