namespace DevOnBike.LeMonad
{
    /// <summary>
    /// Struktura reprezentująca wynik operacji: Sukces (Value) lub Porażkę (Error).
    /// Zoptymalizowana pod kątem .NET 9 (Zero-Allocation na stercie dla samej struktury).
    /// </summary>
    public readonly record struct Result<TValue, TError>
    {
        private readonly TValue _value;
        private readonly TError _error;

        /// <summary>
        /// Czy operacja zakończyła się sukcesem?
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Pobiera wartość. Rzuca wyjątek, jeśli wynik jest błędem.
        /// </summary>
        public TValue Value => IsSuccess
            ? _value
            : throw new InvalidOperationException($"Nie można pobrać wartości z błędnego wyniku. Błąd: {_error}");

        /// <summary>
        /// Pobiera błąd. Rzuca wyjątek, jeśli wynik jest sukcesem.
        /// </summary>
        public TError Error => !IsSuccess
                ? _error is not null
                    ? _error
                    : throw new InvalidOperationException("Result jest w stanie 'default' (niezainicjowanym błędem). Użyj Result.Failure(...)")
                : throw new InvalidOperationException($"Nie można pobrać błędu z sukcesu. Wartość: {_value}");

        // Prywatny konstruktor zapewnia spójność stanu
        private Result(TValue value, TError error, bool isSuccess)
        {
            _value = value;
            _error = error;

            IsSuccess = isSuccess;
        }

        // --- Factory Methods ---

        public static Result<TValue, TError> Success(TValue value)
        {
            return new(value, default, true);
        }

        public static Result<TValue, TError> Failure(TError error)
        {
            return new(default, error, false);
        }

        // --- Implicit Operators (Syntactic Sugar) ---
        // Pozwala pisać: return "Mój Błąd"; zamiast return Result<...>.Failure("...");

        public static implicit operator Result<TValue, TError>(TValue value)
        {
            return Success(value);
        }

        public static implicit operator Result<TValue, TError>(TError error)
        {
            return Failure(error);
        }

        // --- Metody Funkcyjne (Core) ---

        /// <summary>
        /// Rozpakowuje wynik, wymuszając obsługę obu ścieżek.
        /// </summary>
        public R Match<R>(Func<TValue, R> onSuccess, Func<TError, R> onFailure)
        {
            return IsSuccess ? onSuccess(_value!) : onFailure(_error!);
        }

        /// <summary>
        /// Wykonuje akcję (Side Effect) dla obu ścieżek (np. logowanie).
        /// </summary>
        public void Switch(Action<TValue> onSuccess, Action<TError> onFailure)
        {
            if (IsSuccess)
            {
                onSuccess(_value!);
                return;
            }
            
            onFailure(_error!);
        }

        /// <summary>
        /// Map (Select): Zmienia Wartość (T -> U), jeśli sukces. Błąd przechodzi bez zmian.
        /// </summary>
        public Result<U, TError> Map<U>(Func<TValue, U> mapper)
        {
            if (!IsSuccess)
            {
                return Result<U, TError>.Failure(_error);
            }
            
            return Result<U, TError>.Success(mapper(_value));
        }

        /// <summary>
        /// MapError: Zmienia Błąd (E -> F), jeśli porażka. Wartość przechodzi bez zmian.
        /// Przydatne przy mapowaniu błędów z warstwy niższej na wyższą.
        /// </summary>
        public Result<TValue, F> MapError<F>(Func<TError, F> errorMapper)
        {
            if (IsSuccess)
            {
                return Result<TValue, F>.Success(_value!);
            }
            
            return Result<TValue, F>.Failure(errorMapper(_error!));
        }

        /// <summary>
        /// Bind (FlatMap): Łączy operacje, które mogą zwrócić błąd.
        /// </summary>
        public Result<U, TError> Bind<U>(Func<TValue, Result<U, TError>> binder)
        {
            if (!IsSuccess)
            {
                return Result<U, TError>.Failure(_error!);
            }
            
            return binder(_value!);
        }

        // --- Wsparcie dla LINQ (Query Syntax) ---
        // Pozwala używać składni: from x in result ...

        public Result<U, TError> Select<U>(Func<TValue, U> selector)
        {
            return Map(selector);
        }

        public Result<R, TError> SelectMany<U, R>(
            Func<TValue, Result<U, TError>> binder,
            Func<TValue, U, R> projector)
        {
            if (!IsSuccess)
            {
                return Result<R, TError>.Failure(_error!);
            }

            // 1. Uruchomienie bindera (pośrednia operacja)
            var bound = binder(_value!);

            // 2. Jeśli pośrednia operacja zawiodła -> zwróć jej błąd
            if (!bound.IsSuccess)
            {
                return Result<R, TError>.Failure(bound.Error);
            }

            // 3. Jeśli obie się udały -> uruchom projekcję (połączenie wyników)
            return Result<R, TError>.Success(projector(_value!, bound.Value));
        }
    }
}