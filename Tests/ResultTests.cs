namespace DevOnBike.LeMonad.Tests
{
    public class ResultTests
    {
        public record Val(int Id);
        public record Err(string Code);

        // =======================================================================
        // 1. KONSTRUKCJA I WŁAŚCIWOŚCI (State & Properties)
        // =======================================================================

        [Fact]
        public void Success_Factory_Creates_Valid_State()
        {
            var val = new Val(1);
            var result = Result<Val, Err>.Success(val);

            Assert.True(result.IsSuccess);
            Assert.Equal(val, result.Value);
        }

        [Fact]
        public void Failure_Factory_Creates_Valid_State()
        {
            var err = new Err("FAIL");
            var result = Result<Val, Err>.Failure(err);

            Assert.False(result.IsSuccess);
            Assert.Equal(err, result.Error);
        }

        [Fact]
        public void Implicit_Operator_From_Value_Creates_Success()
        {
            Result<int, string> result = 100;
            Assert.True(result.IsSuccess);
            Assert.Equal(100, result.Value);
        }

        [Fact]
        public void Implicit_Operator_From_Error_Creates_Failure()
        {
            Result<int, string> result = "Error";
            Assert.False(result.IsSuccess);
            Assert.Equal("Error", result.Error);
        }

        // =======================================================================
        // 2. OCHRONA STANU (Guard Clauses / Exceptions)
        // =======================================================================

        [Fact]
        public void Accessing_Value_On_Failure_Throws_InvalidOperationException()
        {
            var result = Result<Val, Err>.Failure(new Err("oops"));

            var ex = Assert.Throws<InvalidOperationException>(() => result.Value);

            Assert.Contains("Nie można pobrać wartości z błędnego wyniku", ex.Message);
        }

        [Fact]
        public void Accessing_Error_On_Success_Throws_InvalidOperationException()
        {
            var result = Result<Val, Err>.Success(new Val(1));

            var ex = Assert.Throws<InvalidOperationException>(() => result.Error);

            Assert.Contains("Nie można pobrać błędu z sukcesu", ex.Message);
        }

        [Fact]
        public void Accessing_Error_On_Default_Struct_Throws_InvalidOperationException()
        {
            // EDGE CASE: default(struct) ma IsSuccess = false, ale _error = null.
            // Kod w Result.cs ma zabezpieczenie: _error is not null ? ... : throw

            Result<Val, Err> result = default;

            Assert.False(result.IsSuccess);

            var ex = Assert.Throws<InvalidOperationException>(() => result.Error);

            Assert.Contains("Result jest w stanie 'default'", ex.Message);
        }

        // =======================================================================
        // 3. METODY FUNKCYJNE (Core Logic)
        // =======================================================================

        [Fact]
        public void Match_Executes_OnSuccess_When_Success()
        {
            Result<int, string> result = 10;

            var output = result.Match(
                onSuccess: v => v * 2,
                onFailure: e => -1
            );
            Assert.Equal(20, output);
        }

        [Fact]
        public void Match_Executes_OnFailure_When_Failure()
        {
            Result<int, string> result = "error";

            var output = result.Match(
                onSuccess: v => "Success",
                onFailure: e => $"Failed: {e}"
            );

            Assert.Equal("Failed: error", output);
        }

        [Fact]
        public void Switch_Executes_OnSuccess_When_Success()
        {
            Result<int, string> result = 10;
            bool successCalled = false;
            bool failureCalled = false;

            result.Switch(
                onSuccess: v => successCalled = true,
                onFailure: e => failureCalled = true
            );

            Assert.True(successCalled);
            Assert.False(failureCalled);
        }

        [Fact]
        public void Switch_Executes_OnFailure_When_Failure()
        {
            Result<int, string> result = "error";
            bool successCalled = false;
            bool failureCalled = false;

            result.Switch(
                onSuccess: v => successCalled = true,
                onFailure: e => failureCalled = true
            );

            Assert.False(successCalled);
            Assert.True(failureCalled);
        }

        // =======================================================================
        // 4. MAPOWANIE I BINDING (Map, MapError, Bind)
        // =======================================================================

        [Fact]
        public void Map_Transforms_Value_On_Success()
        {
            Result<int, string> result = 5;
            var mapped = result.Map(x => x.ToString());

            Assert.True(mapped.IsSuccess);
            Assert.Equal("5", mapped.Value);
        }

        [Fact]
        public void Map_Propagates_Error_On_Failure()
        {
            Result<int, string> result = "Original Error";
            var mapped = result.Map(x => x * 2);

            Assert.False(mapped.IsSuccess);
            Assert.Equal("Original Error", mapped.Error);
        }

        [Fact]
        public void MapError_Transforms_Error_On_Failure()
        {
            Result<int, int> result = Result<int, int>.Failure(404);
            var mapped = result.MapError(code => $"Error code: {code}");

            Assert.False(mapped.IsSuccess);
            Assert.Equal("Error code: 404", mapped.Error);
        }

        [Fact]
        public void MapError_Propagates_Value_On_Success()
        {
            Result<int, int> result = Result<int, int>.Success(200);
            var mapped = result.MapError(code => $"Error: {code}");

            Assert.True(mapped.IsSuccess);
            Assert.Equal(200, mapped.Value);
        }

        [Fact]
        public void Bind_Executes_Binder_On_Success()
        {
            Result<int, string> result = 10;

            // Binder zwraca sukces
            var bound = result.Bind(x => Result<string, string>.Success($"Val: {x}"));

            Assert.True(bound.IsSuccess);
            Assert.Equal("Val: 10", bound.Value);
        }

        [Fact]
        public void Bind_Propagates_Error_From_Binder()
        {
            Result<int, string> result = 10;

            // Binder zwraca porażkę
            var bound = result.Bind(x => Result<string, string>.Failure("New Error"));

            Assert.False(bound.IsSuccess);
            Assert.Equal("New Error", bound.Error);
        }

        [Fact]
        public void Bind_Short_Circuits_On_Initial_Failure()
        {
            Result<int, string> result = "Initial Error";
            bool binderExecuted = false;

            var bound = result.Bind(x =>
            {
                binderExecuted = true;
                return Result<string, string>.Success("Should not happen");
            });

            Assert.False(bound.IsSuccess);
            Assert.Equal("Initial Error", bound.Error);
            Assert.False(binderExecuted);
        }

        // =======================================================================
        // 5. LINQ QUERY SYNTAX (Select, SelectMany)
        // =======================================================================

        [Fact]
        public void Select_Works_Like_Map()
        {
            Result<int, string> result = 10;
            var selected = result.Select(x => x * 2); // LINQ method syntax

            Assert.True(selected.IsSuccess);
            Assert.Equal(20, selected.Value);
        }

        [Fact]
        public void SelectMany_Happy_Path()
        {
            // Scenario: from a in res1 from b in res2 select a + b
            Result<int, string> r1 = 10;
            Result<int, string> r2 = 20;

            var query = r1.SelectMany(
                binder: x => r2,
                projector: (x, y) => x + y
            );

            Assert.True(query.IsSuccess);
            Assert.Equal(30, query.Value);
        }

        [Fact]
        public void SelectMany_Fails_When_First_Result_Is_Failure()
        {
            // Scenario: Pierwszy element jest błędem -> Binder i Projector nie powinny ruszyć
            Result<int, string> r1 = "Error 1";

            bool binderCalled = false;

            var query = r1.SelectMany(
                binder: x => { binderCalled = true; return Result<int, string>.Success(1); },
                projector: (x, y) => x + y
            );

            Assert.False(query.IsSuccess);
            Assert.Equal("Error 1", query.Error);
            Assert.False(binderCalled);
        }

        [Fact]
        public void SelectMany_Fails_When_Binder_Returns_Failure()
        {
            // Scenario: Pierwszy OK, ale Binder zwraca błąd -> Projector nie powinien ruszyć
            Result<int, string> r1 = 10;
            Result<int, string> r2 = "Error 2";

            bool projectorCalled = false;

            var query = r1.SelectMany(
                binder: x => r2, // Tu zwracamy błąd
                projector: (x, y) => { projectorCalled = true; return x + y; }
            );

            Assert.False(query.IsSuccess);
            Assert.Equal("Error 2", query.Error);
            Assert.False(projectorCalled);
        }
    }
}