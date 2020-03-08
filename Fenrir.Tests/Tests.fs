module Fenrir.Tests.ProgramTests

open Xunit

[<Fact>]
let ``Main function should return zero``(): unit =
    Assert.Equal(0, global.Program.main([||]))
